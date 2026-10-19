# Internet lobby password format

The user-facing password is optional and limited to 64 UTF-16 code units, matching
Unity's input field and the existing C# length checks. Null and empty input mean
no password. Whitespace and case are significant; no trimming or normalization is
performed. Every nonempty password, including 8–64-character passwords, is converted
before creating a lobby or making the first join request.

## Version 1 conversion

`lowercase_hex(SHA256(UTF8("CGS:lobby-password:v1:" + password)))`

The result is exactly 64 ASCII characters, within Unity Lobby's 8–64 character
requirement. Both join-by-ID and join-by-code use the same helper as hosting.
This conversion does not strengthen a short, guessable password. The derived value
is a credential: do not put it, the original password, or request bodies in logs or
public lobby metadata. Keep the v1 domain, encoding, casing and empty-input behavior
stable. A future format change requires a separate compatibility design.

Test vector: input `1234` produces
`6647f4c684f5a3118ee4b376a5f05a90d7b9c6936b599b1b75c8663dd6717bc4`.

## Compatibility

| Host | Client | Result |
| --- | --- | --- |
| Updated, protected room | Updated | Both derive v1 from the supplied password |
| Older, protected room | Updated | Try v1 first; retry the original 8–64-character password once only after `IncorrectPassword` |
| Updated, protected room | Older | Not supported; the client must update |
| Either, passwordless room | Either | Existing passwordless behavior is retained |

An empty or 1–7-character input never triggers the legacy retry. Invalid room
codes, missing/full lobbies, rate limits, authentication and network failures do
not trigger it either. A failed legacy retry propagates its error without further
attempts. No attempt falls back to a passwordless join. Other gameplay/protocol
compatibility requirements between releases still apply.

The legacy retry waits 1.1 seconds after rejection to respect Unity's
[one join request per second limit](https://docs.unity.com/en-us/mps-sdk/rate-limits).

## Validation and verification

The lobby menu validates before starting a host or join and retains invalid input.
Networking entry points independently validate before starting coroutines. Hosting
derives the credential before sign-in, Relay allocation, or starting Netcode. If
lobby creation fails after Netcode starts, the host shuts down.

`LobbyPasswordTests` covers stable conversion, empty input, boundaries, both join
credential formats, bounded fallback, unrelated errors, and invalid-input rejection
at the menu and networking entry points. Before release, verify actual Windows and
Android clients joining by both code and game list with empty, short, long, wrong,
case-sensitive and whitespace-containing passwords. Include an older host with a
raw 8–64-character password and confirm that older clients receive a join failure
for newly protected rooms rather than connecting without protection.
