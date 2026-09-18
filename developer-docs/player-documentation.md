# Maintaining the player guide

The canonical player guide is `https://www.cardgamesimulator.com/how-to-play/`.
Edit `docs/_includes/playing-guide.md`; Jekyll renders the same source into the
HTML page and `/how-to-play.md`. Keep the homepage, README, and `docs/llms.txt`
account summary consistent with the guide. The guide is public product help;
this file contains internal maintenance and publishing notes.

## Verify claims before changing the guide

- Account-free Internet play: `CgsNetManager.SignInAnonymouslyAsync` and `LobbyMenu`.
- Start/Join navigation: `MainMenu.StartGame`, `MainMenu.JoinGame`, and `PlayController.StartLobby`.
- Starting deck and hand: the Standard Playing Cards configuration in `Assets/StreamingAssets`, `PlayController.StartDecks`, and `HandDealer`.
- Current controls: `PlayHelpMenu`, `Assets/InputSystem_Actions.inputactions`, `CardStack`, and `CardActionPanel`.
- Deck sharing and hand behavior: `CgsNetPlayer` and `CardDrawer`.

Prefer a task, its visible control, and the expected result. Distinguish mouse
and touch. Do not promise automatic rules enforcement, unrestricted gamepad
support, or a universal second-click action. Recheck platform and game-specific
behavior when those features change. Screenshots should show a real app version,
include explanatory alt text, and supplement the written steps.

## Publish and migrate the wiki

Validation on 2026-09-17: browser app 1.164.0 entered without registration;
selected Single-player, loaded Standard 52-card Deck, dealt two cards, dragged
a card from the hand to the table, and flipped it. Mouse/touch reference was
also checked against source; a two-device multiplayer session and physical
mobile touch interactions were not exercised in this documentation pass.

1. Merge the website changes through the normal release process. GitHub Pages
   publishes `main:/docs`; an open PR or a merge into `develop` is not a deployment.
2. Confirm `/how-to-play/` and `/how-to-play.md` return 200 on the public site,
   the HTML contains the guide without JavaScript, and the sitemap includes
   `/how-to-play/`. Confirm `/play` retains its existing web-app destination.
3. In the separate `Card-Game-Simulator.wiki.git` repository, replace
   `Playing-a-Game.md` with the following pointer and add a direct player-guide
   link to `Home.md`. Keep the old wiki page URL so existing links remain useful.

```markdown
# Playing a Game

Card Game Simulator does not require you to create an account or register to
play, including online multiplayer.

The maintained player guide is now on the CGS website:

**[How to play Card Game Simulator](https://www.cardgamesimulator.com/how-to-play/)**

It covers your first table, loading a deck, using your hand, mouse and touch
controls, hosting or joining friends, and troubleshooting.

[Account requirements](https://www.cardgamesimulator.com/how-to-play/#do-i-need-an-account)
· [Plain Markdown](https://www.cardgamesimulator.com/how-to-play.md)
· [Open CGS](https://cgs.gg/)
```

Do not replace the wiki with a link to a page that has not been deployed yet.
Keep the detailed instructions in one source rather than maintaining a second
copy in the wiki. The wiki history preserves the previous guide.

## Check discoverability after publication

- Follow the homepage navigation, homepage call to action, and footer links to
  the guide. Check a narrow mobile viewport and keyboard navigation.
- Submit the updated sitemap and request reindexing of the homepage and guide
  using the site's existing search-console accounts, when available.
- Check that robots rules and any deployed firewall allow legitimate search
  crawlers to read public pages. Perplexity documents its search crawler and
  verified IP ranges at <https://docs.perplexity.ai/docs/resources/perplexity-crawlers>.
  Do not disable broad security protections to grant crawler access.
- Ask a new reader to load a deck, play one card, and join a friend's table using
  only the guide; revise any step where they get stuck.
- Recheck questions such as "Does CGS require an account?" and "How do I join
  a friend in CGS?" in search/AI tools, recording the date and cited sources.
  Clear public text and links improve the evidence available to those systems;
  `llms.txt` is supplementary and cannot guarantee any provider's answer.

Google's guidance likewise prioritizes accessible, useful content and ordinary
search fundamentals: <https://developers.google.com/search/docs/fundamentals/ai-optimization-guide>.
