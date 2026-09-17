import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import test from "node:test";

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
  for (const page of ["about.md", "contact.md", "PRIVACY.md"]) {
    const content = await readDocsFile(page);
    const prose = content.replace(/^---[\s\S]*?---\s*/, "").replace(/\s+/g, " ").trim();

    assert.ok(prose.length >= 500, `${page} must contain at least 500 characters of content`);
    assert.match(content, /permalink: \/(?:about|contact|privacy)\//);
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

test("static social fallback contains no client-side redirect", async () => {
  const socialLayout = await readDocsFile("_layouts/social_link.html");

  assert.doesNotMatch(socialLayout, /http-equiv="refresh"/i);
  assert.match(socialLayout, /served as an HTTP redirect/);
});

test("browser 404 page points visitors and agents to recovery resources", async () => {
  const notFound = await readDocsFile("404.html");

  assert.match(notFound, /sitemap\.xml/);
  assert.match(notFound, /llms\.txt/);
  assert.match(notFound, /contact\//);
});
