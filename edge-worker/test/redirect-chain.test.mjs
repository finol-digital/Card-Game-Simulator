import assert from "node:assert/strict";
import test from "node:test";
import { verifyRedirectChain } from "../scripts/redirect-chain.mjs";

const start = "http://cardgamesimulator.com/bsky";
const canonical = "https://www.cardgamesimulator.com/bsky";
const target = "https://bsky.app/profile/cardgamesim.bsky.social";

test("apex verification follows canonical HTTP redirects for GET and HEAD without visiting the external site", async () => {
  for (const method of ["GET", "HEAD"]) {
    const calls = [];
    await verifyRedirectChain(start, target, method, async (url, options) => {
      calls.push(url.href);
      assert.equal(options.method, method);
      assert.equal(options.redirect, "manual");
      return Response.redirect(url.href === start ? canonical : target, 301);
    });
    assert.deepEqual(calls, [start, canonical]);
  }
});

test("apex verification rejects client-side redirect stubs and missing Location", async () => {
  for (const response of [
    new Response('<meta http-equiv="refresh" content="0;url=https://bsky.app/">'),
    new Response(null, { status: 301 })
  ]) {
    await assert.rejects(verifyRedirectChain(start, target, "GET", async () => response),
      /expected an HTTP redirect|missing Location/);
  }
});

test("apex verification rejects loops, unexpected destinations, and HTTPS downgrades", async () => {
  for (const [destination, message] of [
    [canonical, /redirect loop/],
    ["https://example.com/", /unexpected redirect destination/],
    ["http://www.cardgamesimulator.com/bsky", /must not downgrade/]
  ]) {
    await assert.rejects(verifyRedirectChain(canonical, target, "HEAD",
      async () => Response.redirect(destination, 301)), message);
  }
});

test("apex verification bounds non-repeating redirect chains", async () => {
  let calls = 0;
  await assert.rejects(verifyRedirectChain(start, target, "GET", async () =>
    Response.redirect(`https://www.cardgamesimulator.com/hop-${++calls}`, 302)), /exceeds five hops/);
  assert.equal(calls, 5);
});
