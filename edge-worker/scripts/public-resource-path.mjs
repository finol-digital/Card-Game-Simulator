/** Maps published filenames to routes without sending local file contents over HTTP. */
export function publicResourcePath(file) {
  // This Jekyll text template is published as Markdown, not at its source filename.
  if (file === "how-to-play.txt") return "/how-to-play.md";
  return `/${file.split(/[\\/]/).map(encodeURIComponent).join("/")}`;
}
