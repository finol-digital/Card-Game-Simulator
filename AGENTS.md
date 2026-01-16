# AGENTS.md

This file contains guidelines and commands for agentic coding agents working on the Card Game Simulator repository.

## Project Overview

Card Game Simulator is a Unity-based digital platform for playing card games on a virtual tabletop. The project uses C# with Unity Engine and follows specific coding conventions.

**Tech Stack:**
- Unity 6000.3.2f1
- C# (.NET Standard)
- Unity Netcode for GameObjects (multiplayer)
- Unity UI System
- Newtonsoft.Json for JSON handling
- NUnit for testing

**Key Directories:**
- `Assets/Scripts/Cgs/` - Main game logic
- `Assets/Scripts/UnityExtensionMethods/` - Unity utilities
- `Assets/Scripts/FinolDigital.Cgs.Json.Unity/` - JSON handling
- `Assets/Tests/PlayMode/` - Unit tests
- `Assets/WebGLSupport/` - WebGL compatibility
- `docs/` - Documentation and game schemas

## Build/Test Commands

### Unity Editor Commands
- **Run in Unity Editor**: Open project in Unity and press Play
- **Build for WebGL**: Unity Build Settings → WebGL → Build
- **Build for Windows**: Unity Build Settings → StandaloneWindows64 → Build

### Testing Commands
- **Run All Tests**: In Unity Editor → Window → General → Test Runner → Run All
- **Run Single Test**: Use Unity Test Runner GUI to select specific test
- **Run Tests via Command Line**: 
  ```bash
  # Unity command line testing (requires Unity path)
  /path/to/Unity -batchmode -runTests -projectPath [ProjectPath] -testResults [ResultFile]
  ```

### CI/CD Commands
- **GitHub Actions**: Tests run automatically on PR/merge to develop branch
- **Test Coverage**: Uses Unity Test Tools Code Coverage package
- **Deployment**: Automated via GameCI workflows

## Code Style Guidelines

### File Headers
All C# files must start with the MPL 2.0 license header:
```csharp
/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
```

### Import Organization
Imports should be organized in this specific order:
1. System namespaces (System.*, System.Collections.*)
2. Unity namespaces (UnityEngine.*, Unity.*)
3. Third-party libraries (JetBrains.*, Newtonsoft.*)
4. Project namespaces (Cgs.*, FinolDigital.*)
5. Local/relative imports

Example:
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using JetBrains.Annotations;
using Cgs;
using Cgs.Menu;
using FinolDigital.Cgs.Json;
using UnityExtensionMethods;
```

### Naming Conventions
- **Classes**: PascalCase (e.g., `CardGameManager`, `PlayController`)
- **Methods**: PascalCase (e.g., `LoadGame`, `HandleInput`)
- **Variables**: camelCase (e.g., `cardGameManager`, `isFocused`)
- **Constants**: PascalCase with descriptive names (e.g., `LoadErrorMessage`, `MainMenuSceneIndex`)
- **Private fields**: camelCase with underscore prefix (e.g., `_moveAction`, `_inputFields`)
- **Properties**: PascalCase (e.g., `IsFocused`, `WasFocused`)

### Code Structure
- Use regions for organizing large classes
- Keep methods focused and under 50 lines when possible
- Use `[RequireComponent(typeof(Component))]` for mandatory Unity components
- Use `[UsedImplicitly]` JetBrains annotation for Unity-serialized fields accessed via reflection

### Error Handling
- Use descriptive constant strings for error messages (see `CardGameManager.cs`)
- Log errors with `Debug.LogError()` for critical issues
- Use try-catch blocks for file operations and network requests
- Validate inputs at method entry points

### Unity-Specific Guidelines
- Use `#if UNITY_ANDROID && !UNITY_EDITOR` for platform-specific code
- Use `using` statements for disposable Unity objects (UnityWebRequest, etc.)
- Implement proper Unity lifecycle methods (Awake, Start, Update, OnDestroy)
- Use ScriptableObject for data containers when appropriate
- Use Unity Events for UI interactions

### Testing Guidelines
- Tests are in `Assets/Tests/PlayMode/` namespace `Tests.PlayMode`
- Use NUnit framework with `[Test]`, `[SetUp]`, `[UnityTest]` attributes
- Use `GameObject.Instantiate()` for test objects
- Clean up test objects in `[TearDown]` or use `UnityTest` attribute
- Mock Unity services when needed

### Assembly Definitions
- Main code: `Cgs.asmdef`
- Tests: `PlayMode.asmdef`
- Utilities: `UnityExtensionMethods.asmdef`
- JSON handling: `FinolDigital.Cgs.Json.Unity.asmdef`

### Performance Guidelines
- Use object pooling for frequently instantiated objects
- Avoid expensive operations in Update() methods
- Use coroutines for async operations
- Optimize UI updates with dirty flags
- Use Unity's Profiler for performance analysis

### Multiplayer Guidelines
- Use Unity Netcode for GameObjects
- Implement `CgsNetPlayable` base class for networked objects
- Use NetworkVariables for synchronized data
- Handle client/server authority properly
- Test multiplayer functionality thoroughly

### Documentation
- Use XML documentation for public APIs
- Add comments for complex logic
- Update README.md for major feature changes
- Maintain docs/ folder with game schemas

## Common Patterns

### Singleton Pattern
```csharp
public static CardGameManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
        Destroy(gameObject);
    else
        Instance = this;
}
```

### Unity Event Handling
```csharp
private void OnEnable()
{
    MoveAction?.Enable();
}

private void OnDisable()
{
    MoveAction?.Disable();
}
```

### Input System Usage
```csharp
protected virtual void Awake()
{
    MoveAction = new InputAction(type: InputActionType.PassThrough);
    MoveAction.AddBinding("<Mouse>/scroll");
}

protected virtual void OnEnable()
{
    MoveAction.Enable();
    MoveAction.performed += OnMove;
}
```

## Git Workflow
- Main development branch: `develop`
- PRs target `main` branch
- Use descriptive commit messages
- Include tests for new features
- Update documentation as needed

## Resources
- Unity Documentation: https://docs.unity3d.com/
- Unity Netcode: https://docs-multiplayer.unity3d.com/
- NUnit Documentation: https://nunit.org/
- Project Wiki: https://github.com/finol-digital/Card-Game-Simulator/wiki