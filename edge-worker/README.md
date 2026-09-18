# Card Game Simulator agent edge layer

GitHub Pages serves the public site, but it cannot issue external HTTP redirects or negotiate representations from an `Accept` header. This Cloudflare Worker is the production edge layer for those requirements while preserving GitHub Pages as the origin.

It provides permanent HTTP redirects for `/bluesky`, `/create`, `/discord`, `/facebook`, `/games`, `/github`, `/play`, `/reddit`, `/share`, `/twitter`, `/wiki`, and `/x`, plus the legacy `/PRIVACY.html` URL; serves `llms.txt` as `text/markdown` when the homepage is requested with `Accept: text/markdown`; sets `Vary: Accept, Accept-Encoding` for both homepage variants; and gives markdown-capable clients a recoverable 404 response.

Every social path also has a matching subdomain, such as `discord.cardgamesimulator.com`. Both forms use the same destination map. Subdomains ignore incoming paths and query strings and always issue a 301 to the configured destination, including `play` → `https://cgs.gg/` and `x` → `https://x.com/cardgamesim`.

## Deploy

1. Add `cardgamesimulator.com` to the Finol Digital Cloudflare account and proxy `www.cardgamesimulator.com` to the existing GitHub Pages origin (`finol-digital.github.io`). Keep the GitHub Pages custom-domain configuration in place.
2. Add proxied CNAME records pointing to `finol-digital.github.io` for each shortcut: `bluesky`, `create`, `discord`, `facebook`, `games`, `github`, `play`, `reddit`, `share`, `twitter`, `wiki`, and `x`. Replace any old registrar-forwarding A records for the same names. Compare the entire imported DNS inventory with the registrar before switching nameservers.
3. From this directory, authenticate with the account that owns the zone, then run `npx wrangler deploy`.
4. Confirm all thirteen explicit routes in `wrangler.toml` are enabled: `www` plus the twelve shortcut subdomains. Cloudflare Redirect Rules for these shortcuts are unnecessary; keep the Worker as their single routing configuration. Use Full (strict) SSL with the existing GitHub Pages HTTPS origin.
5. Run `npm --prefix edge-worker run verify:deployed` from the repository root after the DNS and Worker changes have propagated. It checks all path/subdomain pairs and verifies that subdomains discard incoming paths and query strings over HTTP and HTTPS. Even with `SITE_URL` overridden for homepage/path checks, subdomain checks always target the public `cardgamesimulator.com` domain.

The Worker does not alter HTML for normal browser requests, apart from adding the required cache variance header to the homepage and 404 response. When the Worker is absent, GitHub Pages social pages retain a browser refresh redirect and a clickable destination link; these fallbacks do not provide an HTTP 301 response.
