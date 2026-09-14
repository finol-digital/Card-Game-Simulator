# Card Game Simulator agent edge layer

GitHub Pages serves the public site, but it cannot issue external HTTP redirects or negotiate representations from an `Accept` header. This Cloudflare Worker is the production edge layer for those requirements while preserving GitHub Pages as the origin.

It provides permanent HTTP redirects for `/bluesky`, `/facebook`, `/github`, and `/reddit`; serves `llms.txt` as `text/markdown` when the homepage is requested with `Accept: text/markdown`; sets `Vary: Accept, Accept-Encoding` for both homepage variants; and gives markdown-capable clients a recoverable 404 response.

## Deploy

1. Add `cardgamesimulator.com` to the Finol Digital Cloudflare account and proxy `www.cardgamesimulator.com` to the existing GitHub Pages origin (`finol-digital.github.io`). Keep the GitHub Pages custom-domain configuration in place.
2. From this directory, authenticate with the account that owns the zone, then run `npx wrangler deploy`.
3. Confirm the configured route is `www.cardgamesimulator.com/*` and that it is enabled.
4. Run the verification commands in the repository root after the DNS and Worker changes have propagated.

The Worker does not alter HTML for normal browser requests, apart from adding the required cache variance header to the homepage and 404 response.
