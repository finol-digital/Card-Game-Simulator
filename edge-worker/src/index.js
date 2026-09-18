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
 * Only UTF-8 is available; a matching charset range overrides a generic range.
 */
function acceptsMarkdown(acceptHeader) {
  if (!acceptHeader) {
    return false;
  }

  let specificity = -1;
  let selectedQuality = 0;
  for (const mediaRange of acceptHeader.split(",")) {
    const [mediaType, ...parameters] = mediaRange.trim().toLowerCase().split(";");
    if (mediaType.trim() !== "text/markdown") {
      continue;
    }

    const normalized = parameters.map((parameter) => parameter.trim());
    const quality = normalized.find((parameter) => /^q\s*=/.test(parameter));
    const qualityValue = quality ? Number.parseFloat(quality.slice(quality.indexOf("=") + 1)) : 1;
    const mediaParameters = normalized.filter((parameter) => !/^q\s*=/.test(parameter));
    if (!mediaParameters.every((parameter) => /^charset\s*=\s*(?:utf-8|"utf-8")$/.test(parameter))) {
      continue;
    }

    const rangeSpecificity = mediaParameters.length > 0 ? 1 : 0;
    const usableQuality = Number.isFinite(qualityValue) && qualityValue > 0 ? qualityValue : 0;
    if (rangeSpecificity > specificity) {
      specificity = rangeSpecificity;
      selectedQuality = usableQuality;
    } else if (rangeSpecificity === specificity) {
      selectedQuality = Math.max(selectedQuality, usableQuality);
    }
  }
  return selectedQuality > 0;
}

/** Adds a cache variance field without duplicating existing case-insensitive names. */
function mergeVary(existingVary, headerName) {
  const values = (existingVary || "")
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);

  if (!values.includes("*") && !values.some((value) => value.toLowerCase() === headerName.toLowerCase())) {
    values.push(headerName);
  }

  return values.join(", ");
}

/** Preserves origin headers while declaring both representation and encoding variance. */
function withVary(response) {
  const headers = new Headers(response.headers);
  headers.set("Vary", mergeVary(headers.get("Vary"), "Accept"));
  headers.set("Vary", mergeVary(headers.get("Vary"), "Accept-Encoding"));

  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers
  });
}

/** Constructs the Worker's UTF-8 Markdown representation, including cache headers. */
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

/** Routes redirects and negotiated Markdown requests, otherwise preserving the Pages origin. */
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
