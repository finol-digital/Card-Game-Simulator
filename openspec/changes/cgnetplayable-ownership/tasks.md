## 1. Ownership Rules

- [ ] 1.1 Identify spawn/creation paths for `CgsNetPlayable` and ensure server assigns owner to the creating `CgsNetPlayer`
- [ ] 1.2 Add server-side validation to reject non-owner actions on non-shared playables
- [ ] 1.3 Add client-side gating for move/rotate/interaction attempts when not owner and not shared

## 2. Shared Deck Handling

- [ ] 2.1 Move or duplicate `IsDeckShared` to `CardStack` and set it when creating stacks from shared decks
- [ ] 2.2 Update ownership request logic to allow requests only when `CardStack.IsDeckShared` is true
- [ ] 2.3 Ensure stacks created from non-shared decks remain private and cannot be claimed by other players

## 3. Verification

- [ ] 3.1 Add or update playmode tests (or in-editor checks) covering creator ownership and shared stack exception
- [ ] 3.2 Verify existing shared-deck behavior remains intact and non-shared actions are blocked
