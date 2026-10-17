## ADDED Requirements

### Requirement: Expand touch acquisition independently of display size
CGS SHALL provide an additional bounded touch acquisition margin around the locally displayed dice and counter footprint for selection and drag initiation. The initial margin SHALL be 12 screen-canvas reference units per edge, converted using canvas scaling and independent of tabletop zoom. Assistance SHALL apply per touch event on native and browser platforms, including touch-enabled desktops, and SHALL NOT add a margin for mouse input. Standard visual sizing SHALL NOT disable touch assistance.

#### Scenario: Select from the touch margin
- **WHEN** a finger presses within an eligible die or counter's added margin with no higher-priority direct hit
- **THEN** that piece receives the existing selection interaction once
- **AND** the piece does not move or change value merely because it was acquired through assistance

#### Scenario: Mouse on desktop
- **WHEN** a mouse presses outside a Standard-sized piece's existing footprint but inside the touch margin
- **THEN** assistance does not select or begin dragging that piece
- **AND** existing table or underlying UI behavior applies

#### Scenario: Touch after choosing Standard
- **WHEN** a mobile player explicitly uses Standard and touches within a piece's eligible margin
- **THEN** touch assistance remains available without enlarging the visual

#### Scenario: Zoom and orientation
- **WHEN** the table zoom or viewport orientation changes
- **THEN** the acquisition footprint follows the current displayed piece
- **AND** the extra margin retains its screen-canvas-relative size instead of scaling with table zoom

### Requirement: Resolve overlapping targets predictably
CGS SHALL prioritize modal/UI blocking and direct hits on visible interactive content over another piece's assisted margin. Among candidates hit only through assisted margins, CGS SHALL select the nearest eligible projected center and break equal-distance ties using existing render order. Hidden, clipped, inactive, or non-interactable candidates SHALL NOT be acquired. Table backgrounds SHALL retain existing behavior when no eligible piece is acquired.

#### Scenario: Direct card or piece hit
- **WHEN** a touch directly hits a visible card or piece while also lying inside another piece's assisted margin
- **THEN** the direct-hit content receives the interaction according to normal ordering
- **AND** the assisted candidate does not steal it

#### Scenario: Two margins overlap
- **WHEN** a touch hits only the assisted margins of two eligible pieces
- **THEN** the nearest projected center wins
- **AND** an equal-distance result uses render order consistently

#### Scenario: Modal or hidden piece
- **WHEN** a modal blocks the table or a candidate is hidden, clipped, inactive, or non-interactable
- **THEN** assistance does not select the blocked or ineligible piece

### Requirement: Preserve gesture and pointer ownership
After acquisition, CGS SHALL keep the resolved piece associated with that pointer through release or cancellation, forwarding selection and drag interactions to the original playable identity exactly once. Existing drag thresholds, offsets, multi-touch behavior, roll interactions, and multiplayer authorization SHALL remain in effect. Cancelled or completed interactions SHALL release assisted pointer state.

#### Scenario: Begin a drag from the margin
- **WHEN** a touch begins in a counter's assisted margin and moves beyond the existing drag threshold
- **THEN** the original counter becomes the drag object using the existing initial pointer offset
- **AND** it neither jumps to the finger nor changes to a neighboring target mid-gesture

#### Scenario: Roll and cancellation
- **WHEN** a die is acquired through touch assistance and the player performs its existing roll interaction or cancels a gesture
- **THEN** rolling follows the current roll setting without duplicate dispatch
- **AND** cancellation leaves no assisted pointer state affecting the next interaction

#### Scenario: Multiple touches and ownership
- **WHEN** two fingers interact with pieces in a multiplayer session
- **THEN** each pointer retains its own resolved target
- **AND** existing ownership and authorization rules continue to govern every gameplay action

### Requirement: Preserve canonical drop and merge behavior
Visual enlargement and touch assistance SHALL NOT broaden, shrink, intercept, or otherwise change canonical drop targets. Counter merging SHALL use the same original root-space target geometry and ordering as Standard without assistance for every pointer type. Acquisition-only surfaces SHALL NOT receive or forward merge/drop events. Invalid visual-only hits SHALL NOT mask valid canonical drop targets underneath.

#### Scenario: Drop in an enlarged visual or touch margin only
- **WHEN** a counter is released outside another counter's canonical target but within its enlarged visual footprint or touch margin
- **THEN** that nearby counter does not merge due to the enlargement or assistance
- **AND** the remaining drop is resolved using the original canonical targets

#### Scenario: Drop in the canonical target
- **WHEN** a counter is released inside another counter's original eligible target
- **THEN** the existing merge behavior executes exactly once regardless of either player's visual size preference

#### Scenario: Valid target underneath an acquisition-only surface
- **WHEN** a dragged object has a valid canonical drop target underneath another piece's enlarged acquisition-only area
- **THEN** the valid canonical target receives the same drop it would receive without enlargement
- **AND** the acquisition-only surface does not consume or redirect the drop

### Requirement: Preserve dice-zone membership and totals
CGS SHALL determine dice-zone membership, totals, and roll scope using existing logical collider geometry. Local presentation size and touch assistance SHALL NOT modify the colliders or their overlap results.

#### Scenario: Visual overlap without logical overlap
- **WHEN** an enlarged die visually overlaps a dice zone but its original collider does not
- **THEN** the enlargement does not add it to that zone's membership, total, or roll scope

#### Scenario: Preference changes near a zone boundary
- **WHEN** players using different local sizes change their preferences while dice remain stationary near a zone boundary
- **THEN** zone membership, totals, and roll scope remain the same as before the preference changes
