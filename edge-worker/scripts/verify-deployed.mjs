import assert from "node:assert/strict";

const siteUrl = new URL(process.env.SITE_URL || "https://www.cardgamesimulator.com/");

/** Resolves a public route against the deployment under verification. */
function urlFor(path) {
  return new URL(path, siteUrl);
}

/** Fetches without following redirects so their status and destination can be checked. */
async function fetchResponse(path, options = {}) {
  return fetch(urlFor(path), { redirect: "manual", ...options });
}

/** Checks that a legacy or social route returns its permanent HTTP redirect. */
async function verifyRedirect(path, target) {
  const response = await fetchResponse(path);
  assert.equal(response.status, 301, `${path} must return HTTP 301`);
  assert.equal(response.headers.get("location"), target, `${path} must have the expected Location header`);
}

for (const [path, target] of [
  ["/bluesky", "https://bsky.app/profile/cardgamesim.bsky.social"],
  ["/discord", "https://discord.gg/RkCCAXb5sz"],
  ["/facebook", "https://www.facebook.com/cardgamesimulator/"],
  ["/games", "https://cgs.games/"],
  ["/github", "https://github.com/finol-digital/Card-Game-Simulator"],
  ["/play", "https://cgs.gg/"],
  ["/reddit", "https://www.reddit.com/r/CardGameSimulator/"],
  ["/twitter", "https://twitter.com/cardgamesim"],
  ["/x", "https://x.com/cardgamesim"],
  ["/PRIVACY.html", "https://www.cardgamesimulator.com/privacy/"]
]) {
  await verifyRedirect(path, target);
}

const markdownHome = await fetchResponse("/", { headers: { Accept: "text/markdown" } });
assert.equal(markdownHome.status, 200, "markdown homepage must return 200");
assert.match(markdownHome.headers.get("content-type") || "", /^text\/markdown; charset=utf-8$/i);
assert.match(markdownHome.headers.get("vary") || "", /(^|,)\s*accept\s*(,|$)/i);
assert.match(markdownHome.headers.get("vary") || "", /(^|,)\s*accept-encoding\s*(,|$)/i);
assert.match(await markdownHome.text(), /When to use Card Game Simulator/);

const htmlHome = await fetchResponse("/");
assert.equal(htmlHome.status, 200, "HTML homepage must return 200");
assert.match(htmlHome.headers.get("vary") || "", /(^|,)\s*accept\s*(,|$)/i);
assert.match(htmlHome.headers.get("vary") || "", /(^|,)\s*accept-encoding\s*(,|$)/i);
const html = await htmlHome.text();
assert.match(html, /property="og:image"/i);
assert.match(html, /property="og:type"/i);
assert.match(html, /"@type": "Organization"/);
assert.match(html, /"@type": "SoftwareApplication"/);

for (const path of ["/about/", "/contact/", "/privacy/", "/llms.txt", "/sitemap.xml"]) {
  const response = await fetchResponse(path);
  assert.equal(response.status, 200, `${path} must return 200`);
}

const missing = await fetchResponse("/agent-audit-path-that-does-not-exist", {
  headers: { Accept: "text/markdown" }
});
assert.equal(missing.status, 404, "missing paths must return 404");
assert.match(missing.headers.get("content-type") || "", /^text\/markdown; charset=utf-8$/i);
assert.match(await missing.text(), /Sitemap/);

console.log(`Verified ${siteUrl.origin} successfully.`);
