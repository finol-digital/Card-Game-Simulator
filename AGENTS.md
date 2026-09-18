# AGENTS.md

This file contains guidelines and commands for agentic coding agents working on the Card Game Simulator (CGS) repository.

## Project Overview

Card Game Simulator (CGS) is a Unity-based digital platform for playing card games on a virtual tabletop. The project uses C# with Unity Engine and follows specific coding conventions.

**Tech Stack:**
- Unity 6.3
- C# (.NET Standard)
- Unity UI System (uGUI)
- Unity Netcode for GameObjects (multiplayer)
- Newtonsoft.Json for JSON handling
- NUnit for testing

**Key Directories:**
- `Assets/Scripts/Cgs/` - Main logic for CGS
- `Assets/Scripts/UnityExtensionMethods/` - Unity utilities
- `Assets/Scripts/FinolDigital.Cgs.Json.Unity/` - Implementation of `FinolDigital.Cgs.Json` for Unity
- `Assets/Tests/PlayMode/` - Unit tests
- `docs/` - Public GitHub Pages website for `www.cardgamesimulator.com`
- `developer-docs/` - Internal development, debugging, and reproduction guides
- `edge-worker/` - Cloudflare Worker source and tests for optional website HTTP behavior

## Website and Developer Documentation

- GitHub Pages publishes the `docs/` directory from `main` at `https://www.cardgamesimulator.com/`. Treat changes anywhere under `docs/` as public website changes, including Markdown files, game definitions, layouts, and assets.
- Keep internal debugging guides, investigation notes, test reports, and contributor instructions outside `docs/`. Use `developer-docs/` for development guides, retain existing design records under `openspec/`, and keep generated test output in ignored `Logs/` or `Temp/` directories.
- Put repository instructions in this root `AGENTS.md`; do not add agent instruction files to the public website directory. `docs/llms.txt` is intentionally public guidance about using CGS, not internal repository instructions.
- `docs/CNAME` defines the custom domain, `docs/_config.yml` configures Jekyll, and `docs/_layouts/` contains website templates. Preserve the domain and existing public routes unless the user requests a change; account for existing links when changing page URLs.
- Keep website URLs and Jekyll asset links relative to the published site, not to the repository root. Do not link public pages to internal development guides.
- Validate website and Worker changes with `npm --prefix edge-worker test`. These source and HTTP-behavior tests do not replace a Jekyll build or a browser check of changed pages. For website layout changes, inspect the rendered pages in a local Jekyll preview before publishing.
- GitHub Pages and the Worker are separate deployment concerns. Do not assume the Worker in `edge-worker/` is deployed merely because its source or route configuration exists. Follow `edge-worker/README.md` for an explicitly requested deployment; run `npm --prefix edge-worker run verify:deployed` afterward to check the live behavior.

## Build/Test Commands

Builds and tests run through the [Unity CLI](https://docs.unity.com/en-us/unity-cli) (experimental) with the [Unity Pipeline package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package), which lets the CLI control a running Unity Editor.

**One-time setup:**
```bash
# Install the Unity CLI (macOS/Linux), then verify with `unity --version`
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```
```powershell
# Install the Unity CLI (Windows PowerShell), then verify with `unity --version`
$env:UNITY_CLI_CHANNEL = "beta"; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```
```bash
unity install <version>     # Install the Editor version this project uses (see ProjectSettings/ProjectVersion.txt)
```

### Unity Editor Commands
 - `unity open . --args "-automated"` - Open this project in the correct Editor version (the Editor must be running for `unity command` to work)
 - `unity command <name>` - Send a command to the running Editor; auto-discovers the project from the current directory (or pass `--project-path=<path>`)
 - `unity command list_build_targets` - List known build targets
 - `unity command build --target StandaloneWindows64 --outputPath Builds/Windows --confirm` - Start an async Windows Player build
 - `unity command build_status` - Poll the status/report of the current build
 - `unity command recompile` - Force a script recompile (poll with `unity command recompile_status`)

### Testing Commands
 - `unity command list_tests --mode playmode` - List available tests without running them (`--mode all|editor|playmode`)
 - `unity command run_tests --mode playmode --async_tests` - Start tests without blocking; poll with `unity command test_status`
 - `unity command run_tests --mode playmode --filter <pattern>` - Run tests matching a case-insensitive partial name match (`--filter_type testName|assembly|category`)
 - `unity command cancel_tests` - Cancel running tests
 - Default test timeout is 300 seconds; override with `--timeout <seconds>`

## Code Style Guidelines

### File Headers
All C# files must start with the MPL 2.0 license header:
```csharp
/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
```

### Error Handling
- Use descriptive constant strings for error messages (see `CardGameManager.cs`)
- Log errors with `Debug.LogError()` for critical issues
- Use try-catch blocks for file operations and network requests
- Validate inputs at method entry points

### Unity-Specific Guidelines
- Use `using` statements for disposable Unity objects (UnityWebRequest, etc.)

### Performance Guidelines
- Avoid expensive operations in Update() methods
- Optimize UI updates with dirty flags
- Use Unity's Profiler for performance analysis

### Multiplayer Guidelines
- Use Unity Netcode for GameObjects
- Test multiplayer functionality thoroughly

### Testing Guidelines
- Tests are in `Assets/Tests/PlayMode/` namespace `Tests.PlayMode`
- Mock Unity services when needed

## Pull Request Policy

All pull requests must follow these rules exactly:

1. **Branch**: By default, open release PRs from `develop` to `main`. If the user explicitly requests stacked PRs, open the first feature PR against `develop` and each subsequent PR against the preceding feature branch, unless the user specifies different bases. State the merge order and retarget dependent PRs as their bases merge. Creating a stack does not authorize merging or deploying it.
2. **Description length**: The PR description must be under 500 characters.
3. **Audience**: Descriptions are used as release notes for end users. Write in simple, plain language — not developer jargon.
4. **Format**: Use exactly this format, replacing the placeholder bullets with meaningful, user-facing changes. Remove any placeholder text before submitting and do not include "Generated with Claude Code":

```markdown
## What's Changed
- First user-facing change
- Second user-facing change
```

5. **Monitor**: After creating a pull request, monitor its status checks (e.g. `gh pr checks <number> --watch`) until they all pass. If any check fails (unit tests, SonarQube Quality Gate, security checks, etc.), investigate the failure, fix the underlying issues, and push the fixes to the PR branch. Repeat until all checks pass.
6. **Protected files**: Never modify files in the `.github` folder. If fixing a failing check would require changing anything in `.github` (workflows, actions configuration, etc.), stop and notify the maintainer instead of making the change.

## Resources
- Unity Documentation: https://docs.unity3d.com/
- Unity CLI: https://docs.unity.com/en-us/unity-cli
- Unity Pipeline package: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package
- Unity Netcode: https://docs-multiplayer.unity3d.com/
- NUnit Documentation: https://nunit.org/
- Project Wiki: https://github.com/finol-digital/Card-Game-Simulator/wiki
