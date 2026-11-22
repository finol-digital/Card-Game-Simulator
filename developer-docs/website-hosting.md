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
certificate's state from the GitHub API. When the certificate is still valid but expires within 21 days, it:

1. Switches the Cloudflare `A`, `AAAA`, and `CNAME` records for `www` and the bare domain to DNS only.
2. Waits until GitHub's own DNS check (`GET /repos/{owner}/{repo}/pages/health`) reports both domains as eligible
   for HTTPS. Cloudflare's proxied answers can be cached for 5 minutes, and GitHub will not issue a certificate
   while it still sees Cloudflare's addresses.
3. Removes and re-adds the custom domain in GitHub Pages, which restarts provisioning.
4. Waits up to 40 minutes for GitHub to report a new certificate. GitHub says HTTPS can take up to an hour, and it
   can keep showing the old failed state for much of that time, so a failed state only stops the wait after
   provisioning has visibly restarted.
5. Turns the Cloudflare proxy back on and checks the site again. It always does this, even if renewal fails.

DNS checks and certificate provisioning share a 50-minute renewal deadline, including the fixed waits and API
latency. When it expires, renewal stops and restores the domain and proxy. This leaves time within the workflow's
60-minute limit for restoration, DNS propagation, and the final website check; cleanup does not use the renewal
deadline.

While the records are DNS only, which takes from 10 minutes to nearly an hour, the Worker's redirects and Markdown
responses do not run. Renewing 21 days early means visitors still get GitHub's valid certificate during that time.

The daily run does not renew a certificate that has already failed or expired. Visitors would get HTTPS errors for
up to an hour, and it may not work: in November 2026 a certificate stuck in `bad_authz` stayed that way through
three renewals, even with GitHub's DNS check reporting both domains eligible. Instead the check fails every day
with a "Not renewing automatically" error until someone fixes it. Visitors are unaffected while the SSL mode is Full.

To renew by hand, run **Website Health** from the Actions tab with **Renew the GitHub Pages certificate now**
checked. The same steps can be run locally with `python3 tools/site-health/site_health.py renew` and the
environment variables below.

### Secrets and variables

| Name | Type | What it needs |
| --- | --- | --- |
| `CGS_PAT` | Organization secret | The existing personal access token CI already uses. Changing the custom domain needs a repository administrator's token, so it must be a classic token with the `repo` scope, or a fine-grained token with **Administration** and **Pages** write access, from an admin of this repository. The daily check only reads the certificate, so it uses the workflow's own `GITHUB_TOKEN`. |
| `CLOUDFLARE_API_TOKEN` | Repository secret | A Cloudflare API token with **Zone > DNS > Edit** for `cardgamesimulator.com`. |
| `CLOUDFLARE_ZONE_ID` | Repository variable | The zone ID shown on the `cardgamesimulator.com` overview page in Cloudflare. |

Without these, the daily check still tests the site and reports the certificate's state, but the renewal job fails. If the token is not an admin's, renewal stops before it changes Cloudflare or GitHub Pages.

## When the health check fails

- **A website check failed:** open the failed URL. Cloudflare error 526 means the SSL mode is on Full (strict) and
  GitHub's certificate is invalid; set it back to Full. Error 522 or 523 means Cloudflare cannot reach GitHub.
- **Renewal failed:** the job log shows GitHub's DNS check for each domain and each certificate state. Check that
  the secrets above are valid, that GitHub Pages deploys from GitHub Actions, and that there are no `CAA` records,
  or that one allows `letsencrypt.org`. Then run the workflow again with renewal checked.
- **"Not renewing automatically":** the certificate has failed or expired. Run the workflow with renewal checked
  at a quiet time. If the state still never leaves `bad_authz` or `errored`, ask GitHub Support to reset the
  certificate for the custom domain.
