import assert from "node:assert/strict";

/** Follows only same-site HTTP redirects, stopping at the intended external destination. */
export async function verifyRedirectChain(start, target, method, fetcher = fetch) {
  let url = new URL(start);
  const visited = new Set();
  for (let hop = 0; hop < 5; hop++) {
    assert.ok(!visited.has(url.href), `redirect loop at ${url.href}`);
    visited.add(url.href);
    const response = await fetcher(url, {
      method, redirect: "manual", signal: AbortSignal.timeout(30000)
    });
    const location = response.headers.get("location");
    await response.arrayBuffer();
    assert.ok([301, 302, 303, 307, 308].includes(response.status),
      `${url.href} returned ${response.status}; expected an HTTP redirect`);
    assert.ok(location, `${url.href} is missing Location`);
    const next = new URL(location, url);
    if (next.href === target) return;
    assert.ok(["cardgamesimulator.com", "www.cardgamesimulator.com"].includes(next.hostname),
      `unexpected redirect destination ${next.href}`);
    assert.equal(next.protocol, "https:", "redirects must not downgrade to HTTP");
    url = next;
  }
  assert.fail("redirect chain exceeds five hops");
}
