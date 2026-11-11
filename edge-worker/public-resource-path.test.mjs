import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { publicResourcePath } from "./scripts/public-resource-path.mjs";

test("the Markdown template route matches its published Jekyll permalink", async () => {
  const source = await readFile(new URL("../docs/how-to-play.txt", import.meta.url), "utf8");
  const permalink = source.match(/^permalink:\s*(\S+)\s*$/m)?.[1];
  assert.equal(publicResourcePath("how-to-play.txt"), permalink);
});

test("public resources keep nested paths and encode URL delimiters in filenames", () => {
  assert.equal(publicResourcePath("llms.txt"), "/llms.txt");
  assert.equal(publicResourcePath("games/My Game#1?.json"), "/games/My%20Game%231%3F.json");
  assert.equal(publicResourcePath("games\\deck.json"), "/games/deck.json");
});
