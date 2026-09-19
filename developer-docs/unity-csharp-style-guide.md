# Unity C# style guide

Applies to maintained C# code in `Assets/Scripts`. Read this together with
[AGENTS.md](../AGENTS.md) and [.editorconfig](../.editorconfig). Preserve third-party
notices and generated code; do not reformat vendored files to match this guide.

## What the current code establishes

The initial review covered all 101 C# files in `Assets/Scripts`, with detailed
samples from menus, multiplayer components, viewers, JSON adapters, and extension
methods. All 101 use block namespaces. Four-space indentation, braces on separate
lines, PascalCase APIs/constants, `_camelCase` private runtime fields, `var`, guard
clauses, and expression-bodied simple properties are recurring patterns.

Useful examples:

| Pattern | Existing example |
| --- | --- |
| Inspector references distinct from runtime state | [MainMenu.cs](../Assets/Scripts/Cgs/Menu/MainMenu.cs) |
| Lifecycle inheritance with an explicit base call | [Modal.cs](../Assets/Scripts/Cgs/Menu/Modal.cs), [Dialog.cs](../Assets/Scripts/Cgs/Menu/Dialog.cs) |
| Serialized-name compatibility | [PlayableViewer.cs](../Assets/Scripts/Cgs/CardGameView/Viewer/PlayableViewer.cs) |
| Static reset and coroutine cleanup | [ImageQueueService.cs](../Assets/Scripts/FinolDigital.Cgs.Json.Unity/ImageQueueService.cs) |
| Named error messages and service boundaries | [CardGameManager.cs](../Assets/Scripts/Cgs/CardGameManager.cs) |
| Framework-required public cleanup override | [CgsNetPlayable.cs](../Assets/Scripts/Cgs/CardGameView/Multiplayer/CgsNetPlayable.cs) |

Two conventions are deliberately changing: Inspector-only public fields become
`[SerializeField]` fields, and private Unity lifecycle methods become protected.
The initial scan found 148 private declarations across `Awake`, `Start`, `Update`,
`LateUpdate`, `FixedUpdate`, `OnEnable`, `OnDisable`, and `OnDestroy`. Existing files
are therefore migration work, not templates for these two decisions. Other
recommendations below describe expectations for new or substantively changed
code; they do not claim that every existing file already complies.

## File layout and formatting

- Start new maintained C# files with the MPL 2.0 header shown in `AGENTS.md`.
  Preserve any existing third-party license instead of replacing it.
- Follow `.editorconfig`: four spaces, LF line endings, a final newline, and no
  trailing whitespace.
- Keep `using` directives before the block namespace. Match neighboring import
  order; group platform-specific imports behind their compile-time guards.
- Match namespaces to their area: `Cgs.Menu`, `Cgs.Play.Multiplayer`,
  `FinolDigital.Cgs.Json.Unity`, or `UnityExtensionMethods`, for example.
- Keep the main component in a matching filename. Retain `.meta` files and GUIDs
  when moving assets. Respect assembly boundaries and keep editor-only code in
  editor assemblies or behind `UNITY_EDITOR` guards, including its imports.
- Use Allman braces for types, methods, and multiline blocks. Existing code often
  omits braces for a single guard or simple statement; keep that for short,
  unambiguous cases. Use braces for nested or multiline control flow.
- Use `var` when the expression makes the type clear; use an explicit type when it
  improves readability. Target-typed `new()` is already used for typed fields.
- Use expression bodies for simple accessors or forwarding methods. Prefer
  ordinary blocks for branching and side effects. Avoid nested ternaries.
- Group related constants, Inspector fields, runtime state, lifecycle methods,
  and behavior logically. Keep related properties/backing fields together where
  that is the surrounding style; avoid unrelated member reordering in fixes.

## Names and access

| Member | Convention | Example |
| --- | --- | --- |
| Type, method, property, event | PascalCase | `CardViewer`, `SelectNext`, `IsVisible` |
| Interface | `I` plus PascalCase | `ICardDisplay` |
| Constant, including private constants | PascalCase | `AnimationDuration` |
| Inspector field | camelCase, no underscore | `currentCardImage` |
| Private nonserialized field, including static fields | `_camelCase` | `_isAnimating`, `_instance` |
| Parameter or local | camelCase | `clientId`, `currentCard` |

Use explicit access modifiers except for the intentional Inspector-field syntax
below. Helpers stay private unless another type needs them. Expose behavior or
read-only properties rather than mutable fields. Existing public runtime state,
data-transfer members, network variables, and interface implementations need a
separate API review; they are not automatically Inspector fields.

## Inspector fields: the new default

Use this exact form for references/settings assigned through the Inspector:

```csharp
[SerializeField] Text versionText;
[SerializeField] List<GameObject> selectableButtons;

private bool _isAnimating;
```

Omitting the field access modifier makes it private in C#. `[SerializeField]`
keeps eligible fields serialized and visible in the Inspector. The lack of an
underscore intentionally distinguishes serialized configuration from runtime
state. See [Unity's SerializeField reference](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeField.html).

When migrating `public Type fieldName;`:

1. Check callers across the repository, including tests, editor tooling, derived
   classes, reflection, and serialized bindings. A compiling script is only part
   of compatibility verification.
2. Keep the name, type, initializer, and existing attributes unchanged. Replace
   `public` with `[SerializeField]`. This visibility change does not require a
   serialized rename or scene/prefab rewrite.
3. If code needs access, introduce a purposeful PascalCase property or method and
   update callers together. A read-only collection property must not accidentally
   expose unrestricted mutation. Preserve intentional public contracts until
   their consumers can be migrated.
4. If a rename is necessary, use `[FormerlySerializedAs("oldName")]` and verify
   prefabs, variants, scene instances, and editor property paths. Avoid replacing
   fields with auto-properties during this migration: their serialized identity
   differs. See [FormerlySerializedAs](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/serialization/formerlyserializedasattribute).
5. Recompile and inspect the affected assets in Unity. Check Inspector references,
   overrides, and UnityEvent bindings; run the affected screen/behavior.

Do not add `[SerializeField]` to every private field or unsupported type. Keep
runtime caches nonserialized. Add `Tooltip`, `Range`, or `Min` when they help an
asset author; still validate data at runtime boundaries where required.

## Unity lifecycle methods: the new default

Use `protected` for ordinary instance lifecycle messages on inheritable
components, including coroutine-returning `Start` methods:

```csharp
protected void Awake()
{
    _cachedTransform = transform;
}
```

This is a project convention for subclass access, not a Unity requirement.
`protected` alone does **not** make a method overridable or automatically invoke
base initialization. When a subclass must extend a callback, use an explicit
virtual/override chain and preserve required base work:

```csharp
// Base component
protected virtual void Start()
{
    InitializeSelection();
}

// Derived component
protected override void Start()
{
    base.Start();
    InitializeDialog();
}
```

- Inspect the base type and subclasses before changing a callback. Do not hide
  inherited initialization/cleanup with a new method of the same name. Introduce
  `virtual` only for an intended extension point; retain existing override chains.
- Keep access/signatures required by the framework. Netcode's public
  `OnDestroy`/`OnNetworkSpawn` overrides and public EventSystem interface handlers
  stay public. Preserve required `base` calls and their ordering.
- In a sealed class, use private callbacks unless overriding a base member;
  protected extension points have no purpose there.
- Static initialization hooks such as `[RuntimeInitializeOnLoadMethod]` remain
  static. Ordinary helpers and delegate handlers are not lifecycle methods just
  because their names start with `On`.
- Do not make a callback static merely to satisfy an analyzer. Manage shared state
  through a deliberate static API, and keep Unity messages as instance methods.
- Use `Awake` for local initialization and `Start` for work that depends on other
  initialized components. Do not assume cross-object `Awake` order; see
  [Unity's Awake reference](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.Awake.html).

## Practices to enforce during review

- **Lifetime and subscriptions:** pair subscriptions with unsubscriptions at the
  same lifetime boundary (`OnEnable`/`OnDisable`, `Awake`/`OnDestroy`, or network
  spawn/despawn). Clean up tweens, coroutines, callbacks, and owned resources when
  their target is gone. Static registries must only unregister their current
  owner; support static reset when domain reload is disabled.
- **Unity object lifetime:** use `== null`/`!= null` when destroyed
  `UnityEngine.Object` instances must count as null. `?.`, `??`, and `??=` do not
  use Unity's destroyed-object check; do not introduce them for that purpose.
  They remain useful for ordinary managed objects.
- **Performance:** cache repeated component lookups outside hot paths. Avoid
  searches, allocations, LINQ materialization, and repeated UI rebuilding in
  frame/input loops. Use dirty flags or change notifications, and profile before
  adding more complex optimizations. Existing LINQ outside hot paths is fine.
- **Errors and resources:** validate external data and public entry-point
  assumptions. Use named message constants for reusable errors. Catch file/network
  failures where recovery or useful reporting is possible; do not silently swallow
  them. Use `using` for `IDisposable` resources such as `UnityWebRequest` and file
  streams, and `Destroy` for owned Unity objects. Use `finally` for counters and
  cleanup that must survive a failed coroutine operation.
- **Async and platforms:** keep Unity object access on the main thread. Observe
  asynchronous failures and tie work to its owner's lifetime. Prefer `Task` over
  `async void` except where an event signature requires it. Preserve platform
  guards and verify affected player targets, not only Editor compilation.
- **Serialization boundaries:** distinguish Unity asset serialization from
  Newtonsoft.Json game/save formats and Netcode serialization. Do not rename JSON
  properties, change network payloads, or alter enum values in a style-only edit.
- **Multiplayer:** preserve ownership and sender validation. Treat client inputs
  as untrusted and test host/client behavior when changing RPCs, ownership, or
  synchronized state. Use the installed Netcode package's API conventions.
- **Inspector callbacks:** retain public UnityEvent targets and existing
  `[UsedImplicitly]` annotations where appropriate. Check serialized callers before
  deleting an apparently unused method. Prefer `nameof` to member-name strings
  in code when the target is accessible.
- **Analyzer findings:** fix the cause, not just the warning. Keep necessary
  framework-specific suppressions narrow and explain them. Do not add blanket
  suppressions or change `.github` to get a green check.

## Adoption and validation

Apply both new conventions to new code and focused migrations. The initial
example is `MainMenu`; unrelated existing files do not need a sweeping rewrite.
Keep behavior fixes separable from broad formatting/API migrations. For each
migration, check serialization and inheritance independently.

Use `.editorconfig` for the existing whitespace rules. Review enforces the
Inspector/lifecycle exceptions above; a global naming/accessibility rule cannot
express all of them. Consider a Unity-aware analyzer only after testing it against
serialized fields, sealed classes, inherited callbacks, and framework overrides.

Run the relevant Unity tests in `Assets/Tests/PlayMode` (`Tests.PlayMode` namespace)
using the commands in `AGENTS.md`. Add regression tests for changed behavior;
visibility-only edits usually need compilation and asset checks rather than tests
that merely assert modifiers.

Run `pwsh scripts/sonar-scan.ps1` for local analyzer feedback. Confirm that the
underlying `dotnet build` actually succeeded: the current script can report no
findings even if the build failed or no solution exists. It is an approximation
of cloud analysis, not proof that the remote quality gate passed. Check the
authoritative CI/Sonar result after publishing code.
