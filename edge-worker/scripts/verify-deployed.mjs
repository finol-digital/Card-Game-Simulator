import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import { verifyRedirectChain } from "./redirect-chain.mjs";

const siteUrl = new URL(process.env.SITE_URL || "https://www.cardgamesimulator.com/");
const failures = [];
let checks = 0;

/** Records all failures so one broken shortcut cannot hide other endpoint failures. */
async function check(label, action) {
  checks++;
  try {
    await action();
  } catch (error) {
    failures.push(`${label}: ${error.message}`);
  }
}

/** Resolves a public route against the deployment under verification. */
function urlFor(path) {
  return new URL(path, siteUrl);
}

/** Fetches without following redirects so their status and destination can be checked. */
async function fetchResponse(path, options = {}) {
  return fetch(urlFor(path), { redirect: "manual", signal: AbortSignal.timeout(30000), ...options });
}

/** Checks that a legacy or social route returns its permanent HTTP redirect. */
async function verifyRedirect(path, target) {
  await Promise.all(["GET", "HEAD"].map(method =>
    check(`${method} ${path}`, async () => {
      const response = await fetchResponse(path, { method });
      assert.equal(response.status, 301, `${path} must return HTTP 301`);
      assert.equal(response.headers.get("location"), target, `${path} must have the expected Location header`);
      assert.equal(await response.text(), "", "redirect must not depend on a client-side HTML stub");
    })
  ));
}

const socialRedirects = [
  ["/bluesky", "https://bsky.app/profile/cardgamesim.bsky.social"],
  ["/bsky", "https://bsky.app/profile/cardgamesim.bsky.social"],
  ["/create", "https://github.com/finol-digital/Card-Game-Simulator/wiki/Crash-Course-into-Game-Development-with-CGS"],
  ["/discord", "https://discord.gg/RkCCAXb5sz"],
  ["/facebook", "https://www.facebook.com/cardgamesimulator/"],
  ["/games", "https://cgs.games/"],
  ["/github", "https://github.com/finol-digital/Card-Game-Simulator"],
  ["/play", "https://cgs.gg/"],
  ["/reddit", "https://www.reddit.com/r/CardGameSimulator/"],
  ["/share", "https://cgs.games/"],
  ["/twitter", "https://twitter.com/cardgamesim"],
  ["/wiki", "https://github.com/finol-digital/Card-Game-Simulator/wiki"],
  ["/x", "https://x.com/cardgamesim"]
];

await Promise.all(socialRedirects.flatMap(([path, target]) => {
  const hostname = `${path.slice(1)}.cardgamesimulator.com`;
  return [
    path,
    `${path}?ignored=yes`,
    `${path}/`,
    `${path}/?ignored=yes`,
    `https://${hostname}/`,
    `https://${hostname}/ignored/path?ignored=yes`,
    `http://${hostname}/ignored/path?ignored=yes`
  ].map(shortcutUrl => verifyRedirect(shortcutUrl, target));
}));

// Check chains in small batches so the extra hops do not overwhelm the origin.
for (const [path, target] of socialRedirects) {
  for (const scheme of ["http", "https"]) {
    await Promise.all([path, `${path}?ignored=yes`, `${path}/`, `${path}/?ignored=yes`].flatMap(variant =>
      ["GET", "HEAD"].map(method => {
        const url = `${scheme}://cardgamesimulator.com${variant}`;
        return check(`${method} ${url}`, () => verifyRedirectChain(url, target, method));
      })
    ));
  }
}

await verifyRedirect("/PRIVACY.html", "https://www.cardgamesimulator.com/privacy/");

await check("Markdown homepage", async () => {
  const markdownHome = await fetchResponse("/", { headers: { Accept: "text/markdown" } });
  assert.equal(markdownHome.status, 200, "markdown homepage must return 200");
  assert.match(markdownHome.headers.get("content-type") || "", /^text\/markdown; charset=utf-8$/i);
  assert.match(markdownHome.headers.get("vary") || "", /(^|,)\s*accept\s*(,|$)/i);
  assert.match(markdownHome.headers.get("vary") || "", /(^|,)\s*accept-encoding\s*(,|$)/i);
  assert.match(await markdownHome.text(), /When to use Card Game Simulator/);
});

await check("HTML homepage and JSON-LD", async () => {
  const htmlHome = await fetchResponse("/");
  assert.equal(htmlHome.status, 200, "HTML homepage must return 200");
  assert.match(htmlHome.headers.get("vary") || "", /(^|,)\s*accept\s*(,|$)/i);
  assert.match(htmlHome.headers.get("vary") || "", /(^|,)\s*accept-encoding\s*(,|$)/i);
  const html = await htmlHome.text();
  assert.match(html, /property="og:image"/i);
  assert.match(html, /property="og:type"/i);
  assert.match(html, /"@type": "Organization"/);
  assert.match(html, /"@type": "SoftwareApplication"/);
  const nodes = [...html.matchAll(/<script\b[^>]*type=["']application\/ld\+json["'][^>]*>([\s\S]*?)<\/script>/gi)]
    .flatMap((match) => {
      const document = JSON.parse(match[1]);
      return document["@graph"] || [document];
    });
  const organization = nodes.find(node => node["@id"] === "https://www.cardgamesimulator.com/#organization");
  assert.ok(organization, "Organization JSON-LD must be present");
  assert.equal(organization.contactPoint["@type"], "ContactPoint");
  assert.equal(organization.contactPoint.contactType, "customer support");
  assert.equal(organization.contactPoint.email, "david@finoldigital.com");
  assert.deepEqual(organization.address, {
    "@type": "PostalAddress",
    streetAddress: "10601 Clarence Dr, Suite 250",
    addressLocality: "Frisco",
    addressRegion: "TX",
    postalCode: "75033",
    addressCountry: "US"
  });
});

await Promise.all(["/how-to-play/", "/how-to-play.md", "/about/", "/contact/", "/privacy/", "/llms.txt", "/sitemap.xml"].map(async path => {
  await check(path, async () => {
    const response = await fetchResponse(path);
    assert.equal(response.status, 200, `${path} must return 200`);
    const body = await response.text();
    if (path === "/contact/") {
      assert.match(body, /10601 Clarence Dr, Suite 250, Frisco, TX 75033, United States/);
    }
  });
}));

await check("Sitemap routes", async () => {
  const response = await fetchResponse("/sitemap.xml");
  assert.equal(response.status, 200);
  const sitemap = await response.text();
  assert.match(sitemap, /<urlset\b[^>]*xmlns="http:\/\/www.sitemaps.org\/schemas\/sitemap\/0.9"/);
  const paths = [...sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)].map(match => new URL(match[1]).pathname);
  assert.ok(paths.length > 0, "sitemap must list public routes");
  await Promise.all(paths.map(path =>
    check(`sitemap ${path}`, async () => {
      const page = await fetchResponse(path);
      assert.ok([200, 301, 302].includes(page.status), `${path} returned ${page.status}`);
      if (page.status !== 200) assert.ok(page.headers.get("location"), "redirect needs Location");
      await page.arrayBuffer();
    })
  ));
});

// Check every published JSON and text resource, including game definitions and schemas.
const docs = new URL("../../docs/", import.meta.url);
const files = (await readdir(docs, { recursive: true })).filter(file =>
  !file.split(/[\\/]/).some(part => /^[_.]/.test(part)) && /\.(json|txt)$/.test(file));
await Promise.all(files.map(async file => {
  const relativePath = file.split(/[\\/]/).map(encodeURIComponent).join("/");
  const source = await readFile(new URL(relativePath, docs), "utf8");
  // Jekyll can publish a text template at a different machine-readable URL.
  const frontMatter = source.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  const permalink = frontMatter?.[1].match(/^permalink:\s*(\/\S+)\s*$/m)?.[1];
  const path = permalink || `/${relativePath}`;
  await check(path, async () => {
    const response = await fetchResponse(path);
    assert.equal(response.status, 200, `${path} must return 200`);
    const body = await response.text();
    assert.ok(body.trim(), "machine-readable file must not be empty");
    if (file.endsWith(".json")) JSON.parse(body.replace(/^\uFEFF/, ""));
  });
}));

await check("Recoverable Markdown 404", async () => {
  const missing = await fetchResponse("/agent-audit-path-that-does-not-exist", {
    headers: { Accept: "text/markdown" }
  });
  assert.equal(missing.status, 404, "missing paths must return 404");
  assert.match(missing.headers.get("content-type") || "", /^text\/markdown; charset=utf-8$/i);
  assert.match(await missing.text(), /Sitemap/);
});

console.log(`${checks - failures.length}/${checks} checks passed for ${siteUrl.origin}.`);
if (failures.length) {
  console.error(failures.join("\n"));
  process.exitCode = 1;
}
