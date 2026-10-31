# beta.cgs.gg

`https://beta.cgs.gg` redirects to the latest test build of Card Game Simulator for the web.

Every pull request to `main` deploys its WebGL build to the `preview` channel of the `card-game-simulator` Firebase Hosting site. That channel keeps the same URL across deployments, so this separate `cgs-beta` site only needs a fixed 302 redirect to it. Paths and query strings are preserved, and responses are not cached.

## Deploy

From this directory, run `firebase deploy --only hosting`, then check that `curl -sI https://beta.cgs.gg/play` returns a 302 to the preview channel.

## Limitations

The preview channel expires 7 days after the last pull request deployment. Until the next deployment, beta.cgs.gg redirects to a Firebase "Site Not Found" page. If the channel is recreated with a different URL, update the destinations in `firebase.json` and `public/404.html` to match `firebase hosting:channel:list`, then redeploy.

## Custom domain

`beta.cgs.gg` is connected to the `cgs-beta` site under Firebase Console > Hosting. Its DNS records are managed at the `cgs.gg` registrar.
