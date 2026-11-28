// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Web startup benchmark for CGS with Playwright (Firebase preview channel or a local build).
//
//   npm install --no-save --prefix tools/startup-benchmark playwright
//   tools/startup-benchmark/node_modules/.bin/playwright install chromium
//   node tools/startup-benchmark/web_benchmark.mjs --url https://<preview-channel-url>/ --runs 30 --mode first
//   node tools/startup-benchmark/web_benchmark.mjs --url https://<preview-channel-url>/ --runs 30 --mode repeat
//
// "first" starts a new browser with a new profile for each run (empty caches); "repeat" warms one persistent
// profile, then measures reloads with caches retained; --warmup-ms (default 60000) keeps the warm-up page open so
// the first visit's game downloads finish first. All times are relative to navigation start, the origin the player's
// performance.now() shares. Headless Chromium renders WebGL in software; add --headless false to use the GPU, and
// --angle vulkan where its OpenGL driver loses the WebGL context.
//
// For Chrome on an Android phone, forward its DevTools socket and pass --cdp; first visits use incognito tabs:
//   adb forward tcp:9333 localabstract:chrome_devtools_remote
//   node tools/startup-benchmark/web_benchmark.mjs --url <url> --cdp http://127.0.0.1:9333 --runs 30 --mode first

import { chromium } from 'playwright';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const args = Object.fromEntries(process.argv.slice(2).reduce((pairs, value, index, all) => {
    if (value.startsWith('--')) pairs.push([value.slice(2), all[index + 1]?.startsWith('--') ? 'true' : all[index + 1]]);
    return pairs;
}, []));
const runs = Number(args.runs ?? 30);
const mode = args.mode ?? 'first';
const timeoutMs = Number(args.timeout ?? 180000);
const warmupMs = Number(args['warmup-ms'] ?? 60000);
// --pause-ms leaves a blank page between runs so a phone can cool down; the benchmark page keeps rendering otherwise
const pauseMs = Number(args['pause-ms'] ?? 0);
// --console-log appends every run's browser console to a file, for diagnosing failed runs
const consoleLog = args['console-log'] ? fs.createWriteStream(args['console-log'], { flags: 'a' }) : null;
function logConsole(page, label) {
    if (!consoleLog) return;
    page.on('console', message => consoleLog.write(`[${label()} ${message.type()}] ${message.text()}\n`));
    page.on('pageerror', error => consoleLog.write(`[${label()} pageerror] ${error}\n`));
    page.on('crash', () => consoleLog.write(`[${label()} crash]\n`));
}
let currentRun = 0;
// --angle picks Chromium's graphics backend, e.g. vulkan where the OpenGL driver loses the WebGL context
const browserArgs = args.angle ? [`--use-angle=${args.angle}`,
    ...(args.angle === 'vulkan' ? ['--enable-features=Vulkan,DefaultANGLEVulkan,VulkanFromANGLE'] : [])] : [];
if (!args.url) {
    console.error('usage: node web_benchmark.mjs --url <url> [--runs 30] [--mode first|repeat] [--csv out.csv]');
    process.exit(2);
}

function benchmarkUrl(run) {
    const url = new URL(args.url);
    url.searchParams.set('benchmark', '1');
    url.searchParams.set('run', String(run));
    url.searchParams.set('delay', '0');
    return url.toString();
}

function median(values) {
    if (values.length === 0) return null;
    const ordered = [...values].sort((a, b) => a - b);
    const middle = Math.floor(ordered.length / 2);
    return ordered.length % 2 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2;
}

// Nearest-rank percentile, matching startup_stats.py
function percentile(values, pct) {
    if (values.length === 0) return null;
    const ordered = [...values].sort((a, b) => a - b);
    return ordered[Math.max(1, Math.ceil(pct / 100 * ordered.length)) - 1];
}

async function measure(page, run) {
    currentRun = run;
    consoleLog?.write(`[run ${run}] ${new Date().toISOString()}\n`);
    await page.goto(benchmarkUrl(run), { waitUntil: 'commit' });
    // Listen only after the new page commits: unloading the previous page loses its WebGL context too
    let contextLost;
    const lost = new Promise((_, reject) => {
        contextLost = message => {
            if (message.text().includes('CONTEXT_LOST_WEBGL')) reject(new Error('webglContextLost'));
        };
    });
    lost.catch(() => {});
    page.on('console', contextLost);
    try {
        // Poll on a timer: the Unity player drives requestAnimationFrame, so Playwright's default per-frame polling
        // may never see the result
        await Promise.race([
            page.waitForFunction(() => window.cgsBenchmarkResult, null, { timeout: timeoutMs, polling: 250 }),
            lost,
        ]);
    } finally {
        page.off('console', contextLost);
    }
    const result = await page.evaluate(() => window.cgsBenchmarkResult);
    const launch = result.launch ?? {};
    const play = result.play ?? {};
    const offset = launch.processStartOffsetMs;
    const menu = launch.milestonesMs?.mainMenuInteractive;
    const start = play.milestonesMs?.startGameChosen;
    const action = play.milestonesMs?.firstCardAction;
    // Same exclusion rules as startup_stats.result_flags
    const flags = [];
    if (result.gameSwitched) flags.push('gameSwitched');
    if (result.runFileError) flags.push(String(result.runFileError));
    if ((result.startDelayMs ?? 0) > 0) flags.push('startDelay');
    return {
        run, ok: result.outcome === 'ok', failure: result.failedMilestone ?? null, flags,
        launchToMenuMs: offset != null && menu != null ? offset + menu : null,
        launchToActionMs: offset != null && action != null ? offset + action : null,
        startToActionMs: start != null && action != null ? action - start : null,
        longestFrameMs: play.frames?.longestMs ?? null,
    };
}

async function pause(page) {
    // Let the page finish saving before leaving it
    await page.waitForTimeout(2000);
    await page.goto('about:blank').catch(() => null);
    await page.waitForTimeout(pauseMs);
}

const records = [];
const launchOptions = { headless: args.headless !== 'false', args: browserArgs };
// A phone's browser is reached through Chrome DevTools at --cdp; it stays open, and the harness only adds tabs
const remote = args.cdp ? await chromium.connectOverCDP(args.cdp) : null;
if (mode === 'repeat') {
    const profile = remote ? null : fs.mkdtempSync(path.join(os.tmpdir(), 'cgs-web-'));
    // On a phone, one incognito context holds the whole set, so its caches stay warm between runs while the normal
    // profile is never touched; a null viewport keeps Playwright from resizing the window, which Android does not allow
    const context = remote
        ? await remote.newContext({ viewport: null })
        : await chromium.launchPersistentContext(profile, launchOptions);
    const page = await context.newPage();
    logConsole(page, () => currentRun);
    await measure(page, 0).catch(() => null); // warm the caches; not recorded
    // Keep the warm-up page open while the first visit downloads and saves the default games
    await page.waitForTimeout(warmupMs);
    for (let run = 1; run <= runs; run++) {
        records.push(await measure(page, run).catch(error => ({ run, ok: false, failure: String(error) })));
        if (pauseMs > 0) await pause(page);
    }
    // Closing the context discards everything the runs saved
    await context.close();
    if (profile)
        fs.rmSync(profile, { recursive: true, force: true });
} else {
    for (let run = 1; run <= runs; run++) {
        if (remote) {
            // An incognito context per run starts with empty caches and leaves the normal profile untouched;
            // a null viewport keeps Playwright from resizing the window, which Android does not allow
            const context = await remote.newContext({ viewport: null });
            const page = await context.newPage();
            logConsole(page, () => currentRun);
            records.push(await measure(page, run).catch(error => ({ run, ok: false, failure: String(error) })));
            await context.close();
            if (pauseMs > 0) await new Promise(resolve => setTimeout(resolve, pauseMs));
            continue;
        }
        // A new browser per run also empties the GPU shader cache, and recovers if a GPU driver fault hangs a run
        const browser = await chromium.launch(launchOptions);
        const page = await browser.newPage();
        logConsole(page, () => currentRun);
        records.push(await measure(page, run).catch(error => ({ run, ok: false, failure: String(error) })));
        await browser.close();
    }
}
await remote?.close();

const names = ['launchToMenuMs', 'launchToActionMs', 'startToActionMs', 'longestFrameMs'];
const usable = records.filter(r => r.ok && (r.flags ?? []).length === 0);
const report = {
    mode, runs: records.length, usableRuns: usable.length, failures: records.filter(r => !r.ok).length, intervals: {},
};
for (const name of names) {
    const values = usable.filter(r => r[name] != null).map(r => r[name]);
    report.intervals[name] = { count: values.length, medianMs: median(values), p95Ms: percentile(values, 95) };
}
const csvField = value => {
    const text = String(value ?? '');
    return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
};
if (args.csv)
    fs.writeFileSync(args.csv, [['run', 'ok', 'failure', 'flags', ...names].join(','),
        ...records.map(r => [r.run, r.ok, r.failure, (r.flags ?? []).join(';'), ...names.map(n => r[n])]
            .map(csvField).join(','))].join('\n'));
console.log(JSON.stringify(report, null, 2));
process.exit(report.failures === 0 ? 0 : 1);
