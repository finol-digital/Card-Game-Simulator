# Website Hosting

How www.cardgamesimulator.com is served, how its certificates stay valid, and what to do when the daily health
check fails.

## How the site is served

- `.github/workflows/pages.yml` builds `docs/` with Jekyll and deploys it to GitHub Pages whenever `docs/` changes
  on `main`. Pull requests that change `docs/` only run the build. GitHub Pages must be set to deploy from
  **GitHub Actions** (Settings > Pages > Build and deployment > Source).
- Cloudflare proxies `www.cardgamesimulator.com` and `cardgamesimulator.com` and runs the Worker in
  `edge-worker/`. Visitors get Cloudflare's certificate, which Cloudflare renews on its own.
- Cloudflare's SSL/TLS encryption mode must stay on **Full**, not Full (strict). Cloudflare still connects to GitHub
  over HTTPS, but accepts GitHub's certificate even after it expires, so an expired GitHub certificate cannot take
  the site down. (On 2026-11-22 it did: with Full (strict), every request returned Cloudflare error 526.)

## The GitHub Pages certificate

GitHub issues its certificate only when the domain's DNS records point straight at GitHub Pages. While Cloudflare
proxies the records, public DNS returns Cloudflare's addresses, so GitHub cannot renew the certificate on its own.

`.github/workflows/site-health.yml` handles this. Every day it checks the site over HTTPS and reads the
certificate's state from the GitHub API. When the certificate has failed or expires within 21 days, it:

1. Switches the Cloudflare `A`, `AAAA`, and `CNAME` records for `www` and the bare domain to DNS only.
2. Removes and re-adds the custom domain in GitHub Pages, which restarts provisioning.
3. Waits up to 40 minutes for GitHub to report a new certificate.
4. Turns the Cloudflare proxy back on and checks the site again. It always does this, even if renewal fails.

While the records are DNS only, which takes a few minutes, the Worker's redirects and Markdown responses do not run.
Renewing 21 days early means visitors still get GitHub's valid certificate during that time. If the certificate has
already expired, HTTPS fails for those few minutes.

To renew by hand, run **Website Health** from the Actions tab with **Renew the GitHub Pages certificate now**
checked. The same steps can be run locally with `python3 tools/site-health/site_health.py renew` and the
environment variables below.

### Secrets and variables

| Name | Type | What it needs |
| --- | --- | --- |
| `PAGES_ADMIN_TOKEN` | Repository secret | A fine-grained token for this repository with **Administration** and **Pages** read and write access. Changing the custom domain requires an administrator; the workflow's own `GITHUB_TOKEN` cannot. |
| `CLOUDFLARE_API_TOKEN` | Repository secret | A Cloudflare API token with **Zone > DNS > Edit** for `cardgamesimulator.com`. |
| `CLOUDFLARE_ZONE_ID` | Repository variable | The zone ID shown on the `cardgamesimulator.com` overview page in Cloudflare. |

Without these, the daily check still tests the site and reports the certificate's state, but the renewal job fails.

## When the health check fails

- **A website check failed:** open the failed URL. Cloudflare error 526 means the SSL mode is on Full (strict) and
  GitHub's certificate is invalid; set it back to Full. Error 522 or 523 means Cloudflare cannot reach GitHub.
- **Renewal failed:** the job log shows each certificate state. Check that the secrets above are valid, that
  GitHub Pages deploys from GitHub Actions, and that there are no `CAA` records, or that one allows
  `letsencrypt.org`. Then run the workflow again with renewal checked.
