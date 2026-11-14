// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Web startup benchmark for CGS with Playwright (Firebase preview channel or a local build).
//
//   npx -y -p playwright node web_benchmark.mjs --url https://<preview-channel-url>/ --runs 30 --mode first
//   npx -y -p playwright node web_benchmark.mjs --url https://<preview-channel-url>/ --runs 30 --mode repeat
//
// "first" uses a new browser profile per run (empty caches); "repeat" warms one persistent profile, then
// measures reloads with caches retained. All times are relative to navigation start, the origin the player's
// performance.now() shares. Run `npx playwright install chromium` once beforehand.

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

function percentile(values, pct) {
    if (values.length === 0) return null;
    const ordered = [...values].sort((a, b) => a - b);
    return ordered[Math.max(1, Math.ceil(pct / 100 * ordered.length)) - 1];
}

async function measure(page, run) {
    await page.goto(benchmarkUrl(run), { waitUntil: 'commit' });
    await page.waitForFunction(() => window.cgsBenchmarkResult, null, { timeout: timeoutMs });
    const result = await page.evaluate(() => window.cgsBenchmarkResult);
    const launch = result.launch ?? {};
    const play = result.play ?? {};
    const offset = launch.processStartOffsetMs;
    const menu = launch.milestonesMs?.mainMenuInteractive;
    const start = play.milestonesMs?.startGameChosen;
    const action = play.milestonesMs?.firstCardAction;
    return {
        run, ok: result.outcome === 'ok', failure: result.failedMilestone ?? null,
        launchToMenuMs: offset != null && menu != null ? offset + menu : null,
        launchToActionMs: offset != null && action != null ? offset + action : null,
        startToActionMs: start != null && action != null ? action - start : null,
    };
}

const records = [];
if (mode === 'repeat') {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'cgs-web-'));
    const context = await chromium.launchPersistentContext(profile, { headless: args.headless !== 'false' });
    const page = await context.newPage();
    await measure(page, 0).catch(() => null); // warm the caches; not recorded
    for (let run = 1; run <= runs; run++)
        records.push(await measure(page, run).catch(error => ({ run, ok: false, failure: String(error) })));
    await context.close();
} else {
    const browser = await chromium.launch({ headless: args.headless !== 'false' });
    for (let run = 1; run <= runs; run++) {
        const context = await browser.newContext();
        const page = await context.newPage();
        records.push(await measure(page, run).catch(error => ({ run, ok: false, failure: String(error) })));
        await context.close();
    }
    await browser.close();
}

const names = ['launchToMenuMs', 'launchToActionMs', 'startToActionMs'];
const report = { mode, runs: records.length, failures: records.filter(r => !r.ok).length, intervals: {} };
for (const name of names) {
    const values = records.filter(r => r.ok && r[name] != null).map(r => r[name]);
    report.intervals[name] = { count: values.length, medianMs: percentile(values, 50), p95Ms: percentile(values, 95) };
}
if (args.csv)
    fs.writeFileSync(args.csv, ['run,ok,failure,' + names.join(','),
        ...records.map(r => [r.run, r.ok, r.failure ?? '', ...names.map(n => r[n] ?? '')].join(','))].join('\n'));
console.log(JSON.stringify(report, null, 2));
process.exit(report.failures === 0 ? 0 : 1);
