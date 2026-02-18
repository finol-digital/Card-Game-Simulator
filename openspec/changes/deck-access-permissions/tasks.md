## 1. Model And Metadata

- [ ] 1.1 Identify deck and card/stack data structures that carry deck origin metadata
- [ ] 1.2 Add or extend deck source metadata to include shared vs individual and owning player id
- [ ] 1.3 Ensure card and stack instances retain deck source metadata across moves, splits, and merges

## 2. Authorization Core

- [ ] 2.1 Add a centralized deck access validator that takes player id and deck metadata
- [ ] 2.2 Implement shared deck rule: all players authorized
- [ ] 2.3 Implement individual deck rule: only loading player authorized
- [ ] 2.4 Define a consistent denial result payload for unauthorized interactions

## 3. Interaction Integration

- [ ] 3.1 Inventory all card, stack, and deck interaction entry points that modify state
- [ ] 3.2 Wire the validator into each entry point, failing fast on unauthorized actions
- [ ] 3.3 Ensure server-side validation mirrors client-side checks to prevent desync

## 4. UX And Feedback

- [ ] 4.1 Surface denial feedback in the UI for rejected interactions
- [ ] 4.2 Add a developer-facing log or metric for denied actions to aid debugging

## 5. Validation

- [ ] 5.1 Add tests for shared deck access across multiple players
- [ ] 5.2 Add tests for individual deck access for owner vs non-owner
- [ ] 5.3 Add tests to confirm unauthorized interactions do not mutate state
