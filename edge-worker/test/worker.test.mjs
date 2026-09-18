import assert from "node:assert/strict";
import test from "node:test";
import worker, { acceptsMarkdown, mergeVary } from "../src/index.js";

/** Restores global fetch even if a mocked origin request or assertion fails. */
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
  assert.equal(acceptsMarkdown("text/markdown;q=NaN"), false);
  assert.equal(acceptsMarkdown("text/markdown;q=Infinity"), false);
  assert.equal(acceptsMarkdown("text/markdown;q=-1"), false);
});

test("only compatible Markdown parameters match, with more specific ranges taking precedence", () => {
  assert.equal(acceptsMarkdown('text/markdown; charset="UTF-8"'), true);
  assert.equal(acceptsMarkdown("text/markdown;charset=utf-16"), false);
  assert.equal(acceptsMarkdown("text/markdown;variant=GFM"), false);
  assert.equal(acceptsMarkdown("text/markdown;charset=utf-16, text/markdown;q=0.5"), true);
  assert.equal(acceptsMarkdown("text/markdown;q=1, text/markdown;charset=utf-8;q=0"), false);
  assert.equal(acceptsMarkdown("text/markdown;charset=utf-8;q=0, text/markdown;q=1"), false);
  assert.equal(acceptsMarkdown("text/markdown;q=0, text/markdown;charset=utf-8;q=0.5"), true);
  assert.equal(acceptsMarkdown("*/*, text/*"), false);
});

test("Vary preserves origin fields, ignores case, and respects wildcard variance", () => {
  assert.equal(mergeVary("accept, Origin", "Accept"), "accept, Origin");
  assert.equal(mergeVary("*", "Accept"), "*");
});

test("incompatible Markdown requests retain HTML with both cache variance fields", async () => {
  for (const [path, status] of [["/", 200], ["/missing", 404]]) {
    await withMockFetch(async (request) => {
      assert.equal(new URL(request.url).pathname, path);
      return new Response("<html>origin</html>", { status, headers: { "Content-Type": "text/html" } });
    }, async () => {
      const response = await worker.fetch(new Request(`https://www.cardgamesimulator.com${path}`, {
        headers: { Accept: "text/markdown;charset=utf-16" }
      }));
      assert.equal(response.status, status);
      assert.equal(response.headers.get("Content-Type"), "text/html");
      assert.equal(response.headers.get("Vary"), "Accept, Accept-Encoding");
      assert.equal(await response.text(), "<html>origin</html>");
    });
  }
});

test("social paths issue permanent HTTP redirects without fetching the origin", async () => {
  const expectedRedirects = new Map([
    ["bluesky", "https://bsky.app/profile/cardgamesim.bsky.social"],
    ["discord", "https://discord.gg/RkCCAXb5sz"],
    ["facebook", "https://www.facebook.com/cardgamesimulator/"],
    ["games", "https://cgs.games/"],
    ["github", "https://github.com/finol-digital/Card-Game-Simulator"],
    ["play", "https://cgs.gg/"],
    ["reddit", "https://www.reddit.com/r/CardGameSimulator/"],
    ["twitter", "https://twitter.com/cardgamesim"],
    ["x", "https://x.com/cardgamesim"]
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

test("Markdown GET and HEAD ignore range and validators for the original representation", async () => {
  const incomingHeaders = {
    Accept: "text/markdown",
    Range: "bytes=0-3",
    "If-Range": '"html-etag"',
    "If-None-Match": '"html-etag"',
    "If-Match": '"html-etag"',
    "If-Modified-Since": "Thu, 17 Sep 2026 00:00:00 GMT",
    "If-Unmodified-Since": "Thu, 17 Sep 2026 00:00:00 GMT"
  };
  for (const method of ["GET", "HEAD"]) {
    await withMockFetch(async (request) => {
      assert.equal(new URL(request.url).pathname, "/llms.txt");
      assert.equal(request.method, "GET");
      for (const header of Object.keys(incomingHeaders)) {
        if (header !== "Accept") assert.equal(request.headers.get(header), null, header);
      }
      return new Response("# Complete guide");
    }, async () => {
      const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/", {
        method, headers: incomingHeaders
      }));
      assert.equal(response.status, 200);
      assert.equal(await response.text(), method === "HEAD" ? "" : "# Complete guide");
    });
  }
});

test("an unexpected partial origin response is never relabeled as complete Markdown", async () => {
  for (const method of ["GET", "HEAD"]) {
    await withMockFetch(async () => new Response("# Pa", {
      status: 206,
      headers: { "Content-Range": "bytes 0-3/20", "Content-Type": "text/plain" }
    }), async () => {
      const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/", {
        method, headers: { Accept: "text/markdown" }
      }));
      assert.equal(response.status, 206);
      assert.equal(response.headers.get("Content-Range"), "bytes 0-3/20");
      assert.equal(response.headers.get("Cache-Control"), null);
      assert.equal(await response.text(), method === "HEAD" ? "" : "# Pa");
    });
  }
});

test("non-read homepage requests still reach the original resource", async () => {
  await withMockFetch(async (request) => {
    assert.equal(new URL(request.url).pathname, "/");
    assert.equal(request.method, "POST");
    return new Response("Method not allowed", { status: 405 });
  }, async () => {
    const response = await worker.fetch(new Request("https://www.cardgamesimulator.com/", {
      method: "POST", headers: { Accept: "text/markdown" }
    }));
    assert.equal(response.status, 405);
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
