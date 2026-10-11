import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import test from "node:test";
import worker from "../src/index.js";

const testDirectory = path.dirname(fileURLToPath(import.meta.url));
const docsDirectory = path.resolve(testDirectory, "../../docs");

/** Reads a public website source file independently of the test runner's directory. */
async function readDocsFile(fileName) {
  return readFile(path.join(docsDirectory, fileName), "utf8");
}

test("agent guide includes concrete when-to-use and recovery guidance", async () => {
  const guide = await readDocsFile("llms.txt");

  assert.match(guide, /## When to use Card Game Simulator/);
  assert.match(guide, /game designers, playtest groups, and players/i);
  assert.match(guide, /contact\//i);
  assert.match(guide, /sitemap\.xml/i);
});

test("trust pages have substantial content and usable public routes", async () => {
  for (const [page, route] of [["about.md", "/about/"], ["contact.md", "/contact/"], ["PRIVACY.md", "/privacy/"]]) {
    const content = await readDocsFile(page);
    const prose = content.replace(/^---[\s\S]*?---\s*/, "").replace(/\s+/g, " ").trim();

    assert.ok(prose.length >= 500, `${page} must contain at least 500 characters of content`);
    assert.match(content, new RegExp(`^permalink: ${route}\\r?$`, "m"));
  }
});

test("homepage has agent-discoverable identity and metadata", async () => {
  const homeLayout = await readDocsFile("_layouts/home.html");

  assert.match(homeLayout, /property="og:image"/);
  assert.match(homeLayout, /{% seo %}/);
  assert.match(homeLayout, /"@type": "Organization"/);
  assert.match(homeLayout, /"@type": "SoftwareApplication"/);
  assert.match(homeLayout, /"contactPoint"/);
  assert.match(homeLayout, /"email": "david@finoldigital\.com"/);
});

test("static social pages retain a redirect and usable link without the Worker", async () => {
  const socialLayout = await readDocsFile("_layouts/social_link.html");

  assert.match(socialLayout, /http-equiv="refresh" content="0; URL={{ redirect_url \| escape }}"/i);
  assert.match(socialLayout, /<a href="{{ redirect_url \| escape }}">/);
});

test("Organization JSON-LD has a complete mailing address and support contact", async () => {
  const layout = await readDocsFile("_layouts/home.html");
  const scripts = [...layout.matchAll(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/g)];
  const nodes = scripts.flatMap(match => JSON.parse(match[1])["@graph"]);
  const organization = nodes.find(node => node["@type"] === "Organization");
  assert.ok(organization);
  assert.deepEqual(organization.address, {
    "@type": "PostalAddress",
    streetAddress: "10601 Clarence Dr, Suite 250",
    addressLocality: "Frisco",
    addressRegion: "TX",
    postalCode: "75033",
    addressCountry: "US"
  });
  assert.equal(organization.contactPoint["@type"], "ContactPoint");
  assert.equal(organization.contactPoint.contactType, "customer support");
  assert.equal(organization.contactPoint.email, "david@finoldigital.com");
  const contact = await readDocsFile("contact.md");
  for (const value of [organization.address.streetAddress, "Frisco, TX 75033", "United States"]) {
    assert.ok(contact.includes(value), `Contact page must publish ${value}`);
  }
});

test("every published social link has matching path and subdomain redirects and a Worker route", async (t) => {
  t.mock.method(globalThis, "fetch", async () => assert.fail("social redirects must not fetch the origin"));
  const config = await readFile(path.resolve(testDirectory, "../wrangler.toml"), "utf8");
  for (const file of await readdir(path.join(docsDirectory, "social_links"))) {
    if (!file.endsWith(".md")) continue;
    const page = await readDocsFile(`social_links/${file}`);
    const shortcut = page.match(/^permalink:\s*(\S+)/m)?.[1];
    const target = page.match(/^slink:\s*(\S+)/m)?.[1];
    assert.ok(shortcut && target, `Missing social route or target in ${file}`);
    const hostname = `${shortcut}.cardgamesimulator.com`;
    assert.ok(config.includes(`pattern = "${hostname}/*"`), `Missing Worker route for ${hostname}`);
    for (const scheme of ["http", "https"]) {
      for (const url of [
        `${scheme}://www.cardgamesimulator.com/${shortcut}?ignored=yes`,
        `${scheme}://www.cardgamesimulator.com/${shortcut}/?ignored=yes`,
        `${scheme}://${hostname}/`,
        `${scheme}://${hostname}/play?ignored=yes`
      ]) {
        for (const method of ["GET", "HEAD"]) {
          const response = await worker.fetch(new Request(url, { method }));
          assert.equal(response.status, 301, `${method} ${url}`);
          assert.equal(response.headers.get("Location"), target, url);
          assert.equal(await response.text(), "", "HTTP redirects must not depend on an HTML stub");
        }
      }
    }
  }
});

test("browser 404 page points visitors and agents to recovery resources", async () => {
  const notFound = await readDocsFile("404.html");

  assert.match(notFound, /sitemap\.xml/);
  assert.match(notFound, /llms\.txt/);
  assert.match(notFound, /contact\//);
});
