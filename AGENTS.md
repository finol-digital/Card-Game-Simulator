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

## Build/Test Commands

Builds and tests run through the [Unity CLI](https://docs.unity.com/en-us/unity-cli) (experimental) with the [Unity Pipeline package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package), which lets the CLI control a running Unity Editor.

**One-time setup:**
```bash
# Install the Unity CLI (macOS/Linux/Windows PowerShell), then verify with `unity --version`
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash

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

1. **Branch**: Always open PRs from `develop` to `main`.
2. **Description length**: The PR description must be under 500 characters.
3. **Audience**: Descriptions are used as release notes for end users. Write in simple, plain language — not developer jargon.
4. **Format**: Use exactly this format, replacing the placeholder bullets with meaningful, user-facing changes. Remove any placeholder text before submitting and do not include "Generated with Claude Code":

```markdown
## What's Changed
- First user-facing change
- Second user-facing change
```

## Resources
- Unity Documentation: https://docs.unity3d.com/
- Unity CLI: https://docs.unity.com/en-us/unity-cli
- Unity Pipeline package: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package
- Unity Netcode: https://docs-multiplayer.unity3d.com/
- NUnit Documentation: https://nunit.org/
- Project Wiki: https://github.com/finol-digital/Card-Game-Simulator/wiki