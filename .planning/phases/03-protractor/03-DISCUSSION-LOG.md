# Phase 3: Protractor - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-25
**Phase:** 03-protractor
**Areas discussed:** Parallel / off-screen lines, Protractor size, Right rail layout, Angle readout

---

## Parallel / off-screen lines

| Option | Description | Selected |
|--------|-------------|----------|
| Error toast + reset | Show brief toast message, reset click 1 selection | ✓ |
| Silent reset | Clear selection silently, no message | |
| Block at pick time | Reject the second click, stay in AnchorPlaced state | |

**User's choice:** Error toast + reset  
**Notes:** Toast text: "Lines are parallel — pick two non-parallel lines"

| Option | Description | Selected |
|--------|-------------|----------|
| Project intersection, clamp to canvas | Calculate true intersection, clamp center to canvas edge | ✓ |
| Project intersection exactly | Place at true intersection even if off-screen | |
| Error toast + reset | Treat same as parallel lines case | |

**User's choice:** Project intersection, clamp to canvas

---

## Protractor size

| Option | Description | Selected |
|--------|-------------|----------|
| Fixed PDF-space radius | Scales with zoom, consistent with D-10 from Phase 2 | ✓ |
| Fixed screen-pixel radius | Always same screen size regardless of zoom | |

**User's choice:** Fixed PDF-space radius

| Option | Description | Selected |
|--------|-------------|----------|
| No resize — fixed at default | Student zooms PDF view to see larger protractor | ✓ |
| Resize via right-rail step nudge | Repurpose nudge for radius change | |

**User's choice:** No resize — fixed at default

---

## Right rail layout

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — nudge moves center | UDLR shifts protractor center in 1/5/20px steps | ✓ |
| No nudge — anchored at intersection | Protractor always fixed at calculated intersection | |

**User's choice:** Nudge moves center

Multi-select: which controls to include alongside mandatory rotate ±1°/±5°?

| Option | Description | Selected |
|--------|-------------|----------|
| Flip inner/outer (PROT-03) | Toggle inner/outer scale | ✓ |
| Lock toggle (PROT-04) | Prevent accidental movement | |
| Style toggle 180°/360° (PROT-05) | Switch protractor style | ✓ |
| Delete button | Remove protractor | |

**User's choice:** Flip inner/outer + Style toggle (did not select Lock or Delete initially)

Follow-up on PROT-04 Lock (required by spec):

| Option | Description | Selected |
|--------|-------------|----------|
| Include it — keep spec coverage | Add Lock toggle to right rail | |
| Defer it from v1 | Skip for now | ✓ |

**User's choice:** Defer PROT-04 from v1

Follow-up on Delete:

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — consistent with other objects | Add delete button | ✓ |
| No — protractor is different | Skip delete | |

**User's choice:** Yes, add delete button

---

## Angle readout

| Option | Description | Selected |
|--------|-------------|----------|
| Angle read off the protractor | Value student would read off physical protractor | ✓ |
| Rotation offset from baseline | How many degrees rotated from auto-placed position | |

**User's choice:** Angle read off the protractor

| Option | Description | Selected |
|--------|-------------|----------|
| Inside the protractor | Arc + text at protractor center (as in shared.jsx) | ✓ |
| Right rail readout | Numeric value in right rail panel | |

**User's choice:** Inside the protractor

| Option | Description | Selected |
|--------|-------------|----------|
| Live on every rotation step | Updates on each ±1°/±5° press | ✓ |
| Only when baseline is locked | Shows value only after locking | |

**User's choice:** Live on every rotation step

---

## Claude's Discretion

- Default protractor radius in PDF points
- Line-line intersection math implementation
- Canvas-edge clamping logic for off-screen intersections
- Toast/error notification mechanism
- SkiaSharp rendering: arc, scale marks, readout text positioning
- Undo stack entry granularity for rotation

## Deferred Ideas

- PROT-04 Lock toggle — user explicitly deferred from v1
- Protractor resize — user decided student uses PDF zoom instead
