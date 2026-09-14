import assert from "node:assert/strict";
import test from "node:test";
import worker, { acceptsMarkdown } from "../src/index.js";

async function withMockFetch(mock, action) {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = mock;
  try {
    await action();
  } finally {
    globalThis.fetch = originalFetch;
  }
}

test("recognizes explicit markdown acceptance and rejects q=0", () => {
  assert.equal(acceptsMarkdown("text/html, text/markdown;q=0.8"), true);
  assert.equal(acceptsMarkdown("text/markdown;q=0"), false);
  assert.equal(acceptsMarkdown("text/markdown;q=0.0"), false);
  assert.equal(acceptsMarkdown("text/html"), false);
});

test("social paths issue permanent HTTP redirects without fetching the origin", async () => {
  const expectedRedirects = new Map([
    ["bluesky", "https://bsky.app/profile/cardgamesim.bsky.social"],
    ["facebook", "https://www.facebook.com/cardgamesimulator/"],
    ["github", "https://github.com/finol-digital/Card-Game-Simulator"],
    ["reddit", "https://www.reddit.com/r/CardGameSimulator/"]
  ]);

  await withMockFetch(async () => assert.fail("origin must not be fetched"), async () => {
    for (const [path, target] of expectedRedirects) {
      const response = await worker.fetch(new Request(`https://www.cardgamesimulator.com/${path}`));

      assert.equal(response.status, 301);
      assert.equal(response.headers.get("Location"), target);
    }
  });
});

test("legacy privacy URL permanently redirects to the canonical trust page", async () => {
  await withMockFetch(async () => assert.fail("origin must not be fetched"), async () => {
    const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/PRIVACY.html"));

    assert.equal(response.status, 301);
    assert.equal(response.headers.get("Location"), "https://www.cardgamesimulator.com/privacy/");
  });
});

test("homepage returns markdown and cache-safe Vary header when requested", async () => {
  await withMockFetch(async (request) => {
    assert.equal(new URL(request.url).pathname, "/llms.txt");
    return new Response("# Card Game Simulator", { status: 200, headers: { "Content-Type": "text/plain" } });
  }, async () => {
    const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/", {
      headers: { Accept: "text/markdown" }
    }));

    assert.equal(response.status, 200);
    assert.equal(response.headers.get("Content-Type"), "text/markdown; charset=utf-8");
    assert.equal(response.headers.get("Vary"), "Accept, Accept-Encoding");
    assert.match(await response.text(), /^# Card Game Simulator/);
  });
});

test("HTML homepage variant also varies by Accept", async () => {
  await withMockFetch(async () => new Response("<html></html>", {
    headers: { "Content-Type": "text/html", Vary: "Accept-Encoding" }
  }), async () => {
    const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/"));

    assert.equal(response.headers.get("Vary"), "Accept-Encoding, Accept");
    assert.equal(response.headers.get("Content-Type"), "text/html");
  });
});

test("missing markdown resource returns a real 404 with recovery links", async () => {
  await withMockFetch(async () => new Response("not found", { status: 404 }), async () => {
    const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/does-not-exist", {
      headers: { Accept: "text/markdown" }
    }));

    assert.equal(response.status, 404);
    assert.equal(response.headers.get("Content-Type"), "text/markdown; charset=utf-8");
    assert.equal(response.headers.get("Vary"), "Accept, Accept-Encoding");
    const body = await response.text();
    assert.match(body, /Sitemap/);
    assert.match(body, /Contact and support/);
  });
});
