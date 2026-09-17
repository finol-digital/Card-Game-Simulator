const SOCIAL_REDIRECTS = new Map([
  ["/bluesky", "https://bsky.app/profile/cardgamesim.bsky.social"],
  ["/discord", "https://discord.gg/RkCCAXb5sz"],
  ["/facebook", "https://www.facebook.com/cardgamesimulator/"],
  ["/games", "https://cgs.games/"],
  ["/github", "https://github.com/finol-digital/Card-Game-Simulator"],
  ["/play", "https://cgs.gg/"],
  ["/reddit", "https://www.reddit.com/r/CardGameSimulator/"],
  ["/twitter", "https://twitter.com/cardgamesim"],
  ["/x", "https://x.com/cardgamesim"]
]);

const SITE_REDIRECTS = new Map([
  ["/PRIVACY.html", "https://www.cardgamesimulator.com/privacy/"]
]);

const MARKDOWN_NOT_FOUND = `# Page not found

The requested page was not found. Check the URL or continue with one of these resources:

- [Card Game Simulator home](https://www.cardgamesimulator.com/)
- [Sitemap](https://www.cardgamesimulator.com/sitemap.xml)
- [Agent guide (llms.txt)](https://www.cardgamesimulator.com/llms.txt)
- [Contact and support](https://www.cardgamesimulator.com/contact/)
`;

/**
 * Returns whether the request explicitly accepts a markdown representation.
 * A q=0 media range is an explicit rejection and must not select markdown.
 */
function acceptsMarkdown(acceptHeader) {
  if (!acceptHeader) {
    return false;
  }

  return acceptHeader.split(",").some((mediaRange) => {
    const [mediaType, ...parameters] = mediaRange.trim().toLowerCase().split(";");
    const quality = parameters
      .map((parameter) => parameter.trim())
      .find((parameter) => parameter.startsWith("q="));
    const qualityValue = quality ? Number.parseFloat(quality.slice(2)) : 1;

    return mediaType === "text/markdown" && Number.isFinite(qualityValue) && qualityValue > 0;
  });
}

function mergeVary(existingVary, headerName) {
  const values = (existingVary || "")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);

  if (!values.some((value) => value.toLowerCase() === headerName.toLowerCase())) {
    values.push(headerName);
  }

  return values.join(", ");
}

function withVary(response) {
  const headers = new Headers(response.headers);
  headers.set("Vary", mergeVary(headers.get("Vary"), "Accept"));

  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers
  });
}

function markdownResponse(body, status = 200) {
  return new Response(body, {
    status,
    headers: {
      "Content-Type": "text/markdown; charset=utf-8",
      "Cache-Control": "public, max-age=600",
      "Vary": "Accept, Accept-Encoding"
    }
  });
}

async function handleRequest(request) {
  const url = new URL(request.url);
  const redirectTarget = SOCIAL_REDIRECTS.get(url.pathname) || SITE_REDIRECTS.get(url.pathname);

  if (redirectTarget) {
    return Response.redirect(redirectTarget, 301);
  }

  const wantsMarkdown = acceptsMarkdown(request.headers.get("Accept"));
  const isHomepage = url.pathname === "/" || url.pathname === "/index.html";

  if (isHomepage && wantsMarkdown) {
    const markdownUrl = new URL("/llms.txt", url);
    const markdown = await fetch(new Request(markdownUrl, request));

    if (markdown.ok) {
      return markdownResponse(request.method === "HEAD" ? null : markdown.body);
    }

    return withVary(markdown);
  }

  const originResponse = await fetch(request);

  if (originResponse.status === 404 && wantsMarkdown) {
    return markdownResponse(request.method === "HEAD" ? null : MARKDOWN_NOT_FOUND, 404);
  }

  if (isHomepage || originResponse.status === 404) {
    return withVary(originResponse);
  }

  return originResponse;
}

export { acceptsMarkdown, handleRequest, mergeVary };

export default {
  fetch: handleRequest
};
