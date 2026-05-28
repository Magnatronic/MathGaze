# Phase 5: Angle Measurement - Research

**Researched:** 2026-05-28
**Domain:** ToolViewModel state machine extension, ProtractorObject placement math, ghost preview update, session serialization compatibility
**Confidence:** HIGH

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PROT-07 | User can place a protractor via two-point click (click vertex → click a point on one arm) without needing pre-drawn lines, for measuring angles already drawn on exam papers | State machine extended: first click on empty canvas (non-line) sets AnchorPt; second click computes BaselineAngleDeg from vertex→arm direction in screen space; ProtractorObject constructed identically to two-line path; all existing right-rail controls work without change |
</phase_requirements>

---

## Summary

Phase 5 is a surgical extension of the existing Protractor tool state machine in `ToolViewModel.cs`. The two-line placement path (PROT-01, already complete) uses `GeometryHitTester.TryHitObject` to require a `LineObject` on click 1. The new two-point path branches on click 1: if no `LineObject` is hit, the click position is stored as `AnchorPt` (the vertex, in PDF-point coordinates) and the state machine advances to `DrawState.AnchorPlaced`. On click 2, the angle from vertex to the clicked point is computed in screen space, producing `BaselineAngleDeg`. A `ProtractorObject` is constructed with `Line1Id` and `Line2Id` set to `Guid.Empty` to signal "no source lines". The ghost preview in `PdfCanvasViewModel.DrawGhostProtractor` needs a minor update to handle the case where `AnchorLine` is null (two-point mode) — it should draw the ghost at the vertex anchor with rotation toward the current cursor. No other files require changes: `ProtractorObject` fields are unchanged, all commands (`RotateProtractorCommand`, `FlipProtractorCommand`, `StyleProtractorCommand`) operate on those same fields, `RightRailViewModel` checks `SelectedObject is ProtractorObject` which is placement-path-agnostic, `ComputeMeasuredAngle` in `GeometryLayerViewModel` already handles the null-line case (`if (line1 is null || line2 is null) return 0f`), and `SessionService` serializes `ProtractorObject` via existing `[JsonDerivedType]` already.

The key math insight: `BaselineAngleDeg` is stored as a screen-space clockwise angle from right (0° = east). For the two-point path, compute `Math.Atan2(click2Screen.Y - click1Screen.Y, click2Screen.X - click1Screen.X) * 180/π`. The arc occupies negative-Y in local space, so the baseline already faces toward click 2 naturally — no flip-check against a second line is needed. The existing flip check in the two-line path (`if (localY > 0) line1AngleDeg += 180`) is irrelevant here because the student explicitly pointed the baseline direction with click 2.

**Primary recommendation:** Implement in one plan. The changes are minimal and contained to two methods in `ToolViewModel.HandleCanvasClick` plus the `DrawGhostProtractor` method in `PdfCanvasViewModel`. No new commands, no new model fields, no new XAML.

---

## Project Constraints (from CLAUDE.md)

| Directive | Implication for This Phase |
|-----------|---------------------------|
| No drag gestures; every action click-to-commit; max 2 clicks per primitive | Two-point placement must complete on exactly click 2; no intermediate drag or hover-confirm |
| Every interactive element ≥56×56px | Ghost preview and anchor dot must be ≥56px visually for gaze accuracy; existing ghost anchor dot (12px ring) is fine as decoration — the click target is the full canvas |
| Platform: WPF + SkiaSharp | All geometry rendered in `DrawGhostProtractor`; no new XAML needed |
| All input as standard Windows pointer events | No special input handling; `HandleCanvasClick` already receives normalized `SKPoint` from `PdfCanvas.xaml.cs` |
| Tech stack: .NET 9, CommunityToolkit.Mvvm 8.x, SkiaSharp 3.119.2 | No new packages; `[ObservableProperty]` and `[RelayCommand]` patterns unchanged |
| Exam integrity: no computed answers in Exam Mode | Two-point protractor in Exam Mode still hides readout (same `IsPracticeMode` guard in `DrawProtractor`) |
| `CoordinateMapper.Scale` is private | Screen radius must continue to use the proxy-point offset pattern: `mapper.PageToScreen(ProtractorObject.DefaultRadiusPt, 0).X - mapper.PageToScreen(0, 0).X` |
| `SKFont`-based `DrawText` API (not `SKPaint.TextSize`) | No new text rendering needed in this phase, but if any is added use `SKFont` + `DrawText` overload (avoids CS0618) |
| `PlaceObjectCommand` is the standard command for all object placement | Two-point protractor uses same `new PlaceObjectCommand(protractor)` as two-line path |
| GSD workflow: edit/write only through GSD commands | Enforced at session level; research acknowledges |

---

## Standard Stack

### Core (no new packages required — Phase 5 is zero-dependency)

| Library | Version | Purpose | Why |
|---------|---------|---------|-----|
| SkiaSharp | 3.119.2 [VERIFIED: codebase] | Canvas rendering for ghost preview update | Already in use; ghost protractor drawn in `DrawGhostProtractor` via `Save/Translate/RotateDegrees/Restore` |
| CommunityToolkit.Mvvm | 8.x [VERIFIED: codebase] | `[ObservableProperty]`, `[RelayCommand]` | All ViewModel mutation already uses this pattern |
| .NET 9 System.Math | inbox [VERIFIED: codebase] | `Math.Atan2` for baseline angle calculation | Already used in two-line path; identical usage for two-point path |

**No new NuGet packages needed.** [VERIFIED: codebase inspection — all required APIs already present]

---

## Architecture Patterns

### How the Existing Two-Line Path Works (Ground Truth)

The entire placement flow lives in `ToolViewModel.HandleCanvasClick`. The two cases are matched by a C# `switch (ActiveTool, DrawState)` expression:

```
case (ToolMode.Protractor, DrawState.Idle):
    hit = GeometryHitTester.TryHitObject(...)
    if hit is not LineObject → break (ignore)
    AnchorLine = hit; DrawState = AnchorPlaced; SetSelected(hit.Id)

case (ToolMode.Protractor, DrawState.AnchorPlaced):
    hit = GeometryHitTester.TryHitObject(...)
    if hit is not LineObject → break (ignore)
    ... compute intersection and BaselineAngleDeg ...
    ExecuteCommand(new PlaceObjectCommand(new ProtractorObject(...)))
```

### New Two-Point Path — What Changes

The `break` (ignore) on "not a line" becomes the entry point for the two-point path:

**Click 1 (`DrawState.Idle`):** If `TryHitObject` returns null or a non-`LineObject` object → store click as `AnchorPt` (PDF coords via `mapper.ScreenToPage`), advance to `DrawState.AnchorPlaced`. The existing `AnchorLine = null` state (already set by `ResetDrawState`) signals "two-point mode is active".

**Click 2 (`DrawState.AnchorPlaced`):** If `AnchorLine is null` → we are in two-point mode. Compute baseline angle from `AnchorPt` (vertex) to `screenPx` (arm direction) in screen space. Construct `ProtractorObject` using `AnchorPt` as center, computed angle as `BaselineAngleDeg`, and `Guid.Empty` for both line IDs (no source lines). Execute `PlaceObjectCommand`.

This requires no new state fields on `ToolViewModel`. `AnchorPt` stores the vertex (already exists for Line/Circle placement). `AnchorLine == null` at `DrawState.AnchorPlaced` is the discriminator between two-point mode and two-line mode.

### Dispatch Logic in `HandleCanvasClick`

```
case (ToolMode.Protractor, DrawState.Idle):
    var hit = GeometryHitTester.TryHitObject(...)
    if (hit is LineObject line1)
    {
        // === EXISTING two-line path ===
        AnchorLine = line1;
        DrawState  = DrawState.AnchorPlaced;
        StatusMessage = "Click 2nd line";
        _geometryService.SetSelected(line1.Id);
    }
    else
    {
        // === NEW two-point path ===
        var (xPt, yPt) = mapper.ScreenToPage(screenPx);
        AnchorPt  = (xPt, yPt);
        DrawState = DrawState.AnchorPlaced;
        StatusMessage = "Click to set baseline direction";
    }
    GhostChanged?.Invoke(this, EventArgs.Empty);
    break;

case (ToolMode.Protractor, DrawState.AnchorPlaced):
    if (AnchorLine is not null)
    {
        // === EXISTING two-line path (unchanged) ===
        ...
    }
    else
    {
        // === NEW two-point path ===
        var anchorScreen = mapper.PageToScreen(AnchorPt!.Value.xPt, AnchorPt!.Value.yPt);
        double baselineAngleDeg = Math.Atan2(
            screenPx.Y - anchorScreen.Y,
            screenPx.X - anchorScreen.X) * 180.0 / Math.PI;

        var protractor = new ProtractorObject(
            AnchorPt.Value.xPt, AnchorPt.Value.yPt,
            baselineAngleDeg,
            Guid.Empty, Guid.Empty);   // no source lines

        _geometryService.ExecuteCommand(new PlaceObjectCommand(protractor));
        _geometryService.SetSelected(protractor.Id);
        ResetDrawState();
        StatusMessage = "Protractor placed";
    }
    break;
```

[ASSUMED: The degenerate case where click 1 and click 2 are the same pixel is safe to ignore — the protractor will be placed with `baselineAngleDeg = 0.0` (east-facing baseline), which is a valid, usable starting orientation. No crash, no silent failure.]

### Ghost Preview Update in `DrawGhostProtractor`

Currently `DrawGhostProtractor` reads `_toolVm.AnchorLine` to compute `ghostRotDeg`. If `AnchorLine` is not null it aligns the ghost to Line 1's direction. If `AnchorLine` is null (currently only possible at `DrawState.Idle` — never called) the ghost uses 0°.

In Phase 5, `DrawState.AnchorPlaced` can now also have `AnchorLine == null` (two-point mode). In this case, `_toolVm.AnchorPt` holds the vertex. The ghost should:

1. Draw the ghost protractor centred at `AnchorPt` (vertex), not at the cursor.
2. Rotate it toward the current cursor position (preview of final baseline direction).
3. Draw a dashed line from vertex to cursor as an arm-direction indicator.

```csharp
// Inside DrawGhostProtractor, when AnchorLine is null but AnchorPt is set:
var anchorScreen = _coordinateMapper.PageToScreen(
    _toolVm.AnchorPt!.Value.xPt, _toolVm.AnchorPt!.Value.yPt);

double ghostRotDeg = Math.Atan2(
    centerPx.Y - anchorScreen.Y,   // centerPx = GhostCursorPx in two-point mode
    centerPx.X - anchorScreen.X) * 180.0 / Math.PI;

// Draw ghost at vertex (not cursor)
// Draw dashed line vertex → cursor as arm indicator
```

Note: the parameter `centerPx` is `_toolVm.GhostCursorPx` (passed in from `DrawGhostPreview`). In two-point mode, the protractor should be drawn at `anchorScreen`, not at `centerPx`. The method signature or caller must be adjusted to pass the correct center. Options:

- **Option A (recommended):** Move the two-point ghost center decision into `DrawGhostProtractor` — it already reads `_toolVm.AnchorLine` so reading `_toolVm.AnchorPt` is consistent. Determine center from context: if `AnchorLine is null && AnchorPt != null` → center at `anchorScreen`; else center at `centerPx` (cursor, as today for two-line path).
- **Option B:** Change the call site in `DrawGhostPreview` to pass the vertex when in two-point mode.

Option A is cleaner because `DrawGhostProtractor` already has the contextual knowledge.

[VERIFIED: codebase — `DrawGhostProtractor` is called with `_toolVm.GhostCursorPx` from `DrawGhostPreview`; `_toolVm.AnchorPt` and `_toolVm.AnchorLine` are both publicly readable]

### `ActivateProtractor` Status Message

Currently: `StatusMessage = "Click a line (baseline)"`. After the change, the first click can be anywhere, so update to: `"Click vertex (or a line)"`. [ASSUMED: this exact wording is reasonable; planner may refine]

### `HandleMouseMove` — No Change Needed

During two-point mode (`DrawState.AnchorPlaced`, `ActiveTool == ToolMode.Protractor`), `HandleMouseMove` already sets `LastSnap = null` and `StatusMessage = "Click 2nd line"`. The status message text is slightly wrong for two-point mode ("Click 2nd line" vs "Click to set baseline direction") but this is a cosmetic issue. The planner can decide whether to update this string in `HandleMouseMove` as well. [ASSUMED: updating this string is trivial and should be included]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Angle calculation from two screen-space points | Custom trig | `Math.Atan2(dy, dx)` | Already used identically in the two-line path and ghost preview; same formula, same coordinate space |
| Screen-to-PDF coordinate conversion | Custom scale math | `mapper.ScreenToPage(screenPx)` | `CoordinateMapper` is the single source of truth for all coordinate transforms |
| Committing the placement to the undo stack | Direct `AddObject` | `ExecuteCommand(new PlaceObjectCommand(...))` | All placement must go through the command pattern (D-09) |
| Serializing the two-point protractor | Custom JSON handling | Existing `[JsonDerivedType(typeof(ProtractorObject), "protractor")]` | `ProtractorObject` fields are unchanged; `SessionService` serializes it identically |
| Right-rail controls for two-point protractor | New XAML/ViewModel commands | Existing `ProtractorPanel` with all five command groups | `CanProtractor()` checks `SelectedObject is ProtractorObject` — path-agnostic |

---

## Common Pitfalls

### Pitfall 1: Placing Ghost at Cursor Instead of Vertex
**What goes wrong:** In two-point mode, ghost protractor follows cursor like the two-line path does. Student clicks vertex, then moves cursor to aim the baseline — but the ghost drifts away from the vertex instead of staying put and rotating.
**Why it happens:** `DrawGhostProtractor` currently takes `centerPx = GhostCursorPx`. The two-line path computes the intersection on click 2, so during the ghost phase the "center" is just wherever the cursor is. In two-point mode the center is fixed at click 1.
**How to avoid:** Add a branch in `DrawGhostProtractor`: when `AnchorLine is null && AnchorPt != null`, use `mapper.PageToScreen(AnchorPt.xPt, AnchorPt.yPt)` as center and rotate toward `GhostCursorPx`.
**Warning signs:** Ghost appears to float instead of being anchored to click 1 position.

### Pitfall 2: Forgetting `GhostChanged` on Click 1 of Two-Point Path
**What goes wrong:** Student clicks empty canvas for vertex, but ghost preview does not appear. Canvas is not invalidated after click 1.
**Why it happens:** The existing two-line path calls `GhostChanged?.Invoke(this, EventArgs.Empty)` after setting `AnchorLine`. The new two-point path must do the same after setting `AnchorPt`.
**How to avoid:** The `GhostChanged?.Invoke(this, EventArgs.Empty)` call is already in the `DrawState.Idle` case immediately after the if/else branches. Ensure it is placed after both branches, not inside only one. Looking at the existing code: the `GhostChanged` call is inside the `case (ToolMode.Protractor, DrawState.Idle)` block — it fires when `AnchorLine` is set. After the refactor, it must fire for both the line-hit branch and the empty-canvas branch.
**Warning signs:** No ghost appears after click 1 on empty canvas.

### Pitfall 3: `ComputeMeasuredAngle` Returns 0 for Two-Point Protractors
**What goes wrong:** Practice Mode readout always shows 0° for two-point-placed protractors.
**Why it happens:** `ComputeMeasuredAngle` looks up `Line1Id` and `Line2Id` by GUID. Two-point protractors store `Guid.Empty`. `_geometryService.Objects.FirstOrDefault(o => o.Id == Guid.Empty)` will return `null` (no object has `Guid.Empty` as its Id). The method already has `if (line1 is null || line2 is null) return 0f` — so it silently returns 0.
**Why this is correct behaviour:** A two-point protractor has no second arm defined — there is nothing to measure against. The readout should be meaningless or hidden. The student uses the scale marks and rotation to read the angle manually, exactly as with a physical protractor. The readout in Practice Mode (if shown) will display 0°, which is incorrect but harmless — the student will not use the readout for a manually-placed protractor.
**How to handle:** Either (a) accept 0° readout as-is (simple, no code change), or (b) suppress the readout arc entirely when `Line1Id == Guid.Empty`. Option (b) is cleaner UX but requires a one-line guard in `DrawProtractor`: `if (_mainVm.IsPracticeMode && obj.Line1Id != Guid.Empty)`. [ASSUMED: option (b) is preferred — showing "0°" for a manually placed protractor would confuse students who expect the readout to reflect the measured angle; hiding it is more honest]

### Pitfall 4: `AnchorPt` Field Collision with Line/Circle Tools
**What goes wrong:** Switching to Protractor tool while a Line or Circle anchor was set causes stale `AnchorPt` to interfere with two-point detection.
**Why it happens:** `AnchorPt` is shared state for all anchor-using tools.
**How to avoid:** `ResetDrawState()` already clears `AnchorPt = null` and is called by all `Activate*` commands. This is already correct; no change needed. The concern is theoretical, not practical.
**Warning signs:** Would not manifest in normal usage because tool activation always resets state.

### Pitfall 5: Same-Line Click (existing edge case) Now Matches Two-Point Path
**What goes wrong:** The existing same-line-click logic (`line2.Id == AnchorLine!.Id`) fires correctly for two-line mode. If the dispatch is structured incorrectly, a click on the same line in two-point mode would be routed to this branch.
**Why it happens:** The branch condition `AnchorLine is not null` at the top of `case (ToolMode.Protractor, DrawState.AnchorPlaced)` guards the two-line path. As long as this guard is the first thing checked, all two-line logic (including same-line edge case) only runs when `AnchorLine` is set.
**How to avoid:** Structure the `AnchorPlaced` case as: `if (AnchorLine is not null) { ... entire existing two-line block ... } else { ... new two-point block ... }`. Do not interleave.
**Warning signs:** Student clicks a line as click 1 but gets two-point path behaviour instead of two-line path.

---

## Code Examples

### Baseline Angle from Two Screen Points (Two-Point Path)
```csharp
// Source: existing two-line path in ToolViewModel.cs (same formula, different application)
// Convert vertex from PDF space to screen space
var anchorScreen = mapper.PageToScreen(AnchorPt!.Value.xPt, AnchorPt!.Value.yPt);

// Screen-space CW angle from right (0° = east), matching BaselineAngleDeg convention
double baselineAngleDeg = Math.Atan2(
    screenPx.Y - anchorScreen.Y,
    screenPx.X - anchorScreen.X) * 180.0 / Math.PI;
```

### Constructing the Two-Point ProtractorObject
```csharp
// Source: existing two-line path in ToolViewModel.cs (nearly identical construction)
var protractor = new ProtractorObject(
    AnchorPt.Value.xPt, AnchorPt.Value.yPt,  // center = vertex (click 1)
    baselineAngleDeg,                          // baseline faces toward click 2
    Guid.Empty, Guid.Empty);                   // no source lines — two-point path

_geometryService.ExecuteCommand(new PlaceObjectCommand(protractor));
_geometryService.SetSelected(protractor.Id);
ResetDrawState();
StatusMessage = "Protractor placed";
```

### Ghost Preview When AnchorLine is Null (Two-Point Mode)
```csharp
// Inside DrawGhostProtractor, handling two-point mode:
var anchorLine = _toolVm.AnchorLine;
float ghostRotDeg;
SKPoint ghostCenterPx;

if (anchorLine is not null)
{
    // === EXISTING two-line path: ghost follows cursor, aligns to Line 1 ===
    ghostCenterPx = centerPx;  // centerPx = GhostCursorPx (cursor)
    var sp1 = _coordinateMapper.PageToScreen(anchorLine.X1Pt, anchorLine.Y1Pt);
    var sp2 = _coordinateMapper.PageToScreen(anchorLine.X2Pt, anchorLine.Y2Pt);
    ghostRotDeg = (float)(Math.Atan2(sp2.Y - sp1.Y, sp2.X - sp1.X) * 180.0 / Math.PI);
}
else if (_toolVm.AnchorPt.HasValue)
{
    // === NEW two-point path: ghost anchored at vertex, rotates toward cursor ===
    var anchorPt = _toolVm.AnchorPt.Value;
    ghostCenterPx = _coordinateMapper.PageToScreen(anchorPt.xPt, anchorPt.yPt);
    ghostRotDeg = (float)(Math.Atan2(
        centerPx.Y - ghostCenterPx.Y,
        centerPx.X - ghostCenterPx.X) * 180.0 / Math.PI);
}
else
{
    return; // nothing to preview
}

// ... existing Save/Translate/RotateDegrees/draw arc/Restore using ghostCenterPx and ghostRotDeg
```

### Guard for Readout Suppression on Two-Point Protractors
```csharp
// In GeometryLayerViewModel.DrawProtractor (line ~412 area):
// BEFORE: if (_mainVm.IsPracticeMode)
// AFTER:
if (_mainVm.IsPracticeMode && obj.Line1Id != Guid.Empty)
{
    float measuredAngleDeg = ComputeMeasuredAngle(obj);
    DrawReadout(canvas, measuredAngleDeg, radiusPx, obj.IsFlipped);
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Two-line placement only (PROT-01) | Two-line + two-point placement coexisting | Phase 5 | No API change; discriminated by `AnchorLine == null` at `DrawState.AnchorPlaced` |
| Ghost follows cursor in protractor mode | Ghost anchored at vertex in two-point mode | Phase 5 | `DrawGhostProtractor` gains a branch; no external interface change |
| Readout always shown in Practice Mode | Readout suppressed for `Line1Id == Guid.Empty` | Phase 5 | One-line guard in `DrawProtractor`; no model change |

---

## Scope Boundary — What Is NOT Changing

This is an explicit list of files and systems confirmed to need no changes:

| Component | Reason Unchanged |
|-----------|-----------------|
| `ProtractorObject.cs` | No new fields. `Line1Id = Guid.Empty` is a valid sentinel value already supported by all consumers. |
| `RotateProtractorCommand.cs` | Operates on `RotationOffsetDeg` — placement-path-agnostic. |
| `FlipProtractorCommand.cs` | Operates on `IsFlipped` — placement-path-agnostic. |
| `StyleProtractorCommand.cs` | Operates on `Style` — placement-path-agnostic. |
| `NudgeObjectCommand.cs` | Moves `CenterXPt`/`CenterYPt` — placement-path-agnostic. |
| `PlaceObjectCommand.cs` | Generic; works for any `GeometryObject`. |
| `RightRailViewModel.cs` | `CanProtractor()` checks `SelectedObject is ProtractorObject` — path-agnostic. All protractor command buttons appear identically for both placement paths. |
| `RightRail.xaml` | No new XAML. Protractor panel already complete. |
| `GeometryService.cs` | No change; `NudgeObject` protractor case already handles `CenterXPt`/`CenterYPt`. |
| `SessionService.cs` + `SidecarModel` | No change. `[JsonDerivedType(typeof(ProtractorObject), "protractor")]` on `GeometryObject` handles serialization. `Guid.Empty` is a valid `Guid` value in JSON. |
| `GeometryHitTester.cs` | No change. Hit testing for committed protractors unchanged. |
| `SnapEngine` | No change. Protractor still contributes no snap points. |
| `MainViewModel.cs` | No change. |

[VERIFIED: codebase inspection of all files listed above]

---

## Affected Files Summary

| File | Type of Change |
|------|----------------|
| `MathGaze/ViewModels/ToolViewModel.cs` | Extend `case (ToolMode.Protractor, DrawState.Idle)` to branch on line-hit vs empty-canvas; extend `case (ToolMode.Protractor, DrawState.AnchorPlaced)` to handle `AnchorLine is null` (two-point path). Update `ActivateProtractor` status message. Update `HandleMouseMove` status message for two-point mode. |
| `MathGaze/ViewModels/PdfCanvasViewModel.cs` | Update `DrawGhostProtractor` to anchor ghost at vertex and rotate toward cursor when `AnchorLine is null`. |
| `MathGaze/ViewModels/GeometryLayerViewModel.cs` | Add `Guid.Empty` guard to suppress readout for two-point protractors in `DrawProtractor`. |

Total: **3 files, ~40 lines of new/changed code.**

---

## Environment Availability

Step 2.6: SKIPPED — Phase 5 is purely code changes with no external tool dependencies. All required libraries are already installed in the project.

---

## Validation Architecture

Step 2.6 override: `workflow.nyquist_validation` is `false` in `.planning/config.json`. This section is omitted per config.

---

## Open Questions

1. **Should the two-point ghost show a dashed arm line from vertex to cursor?**
   - What we know: The two-line ghost does not draw a line from intersection to cursor. The Line tool ghost draws a dashed line from click 1 to cursor.
   - What's unclear: For a protractor, the arm direction matters more than in line drawing. A thin dashed line from vertex to cursor would make the baseline direction more legible.
   - Recommendation: Add a dashed line from `ghostCenterPx` to `centerPx` (cursor) inside the two-point branch of `DrawGhostProtractor`. This uses the existing `ghostArcPaint` (cobalt 50% alpha, 2px stroke). One extra `canvas.DrawLine()` call. Planner should confirm.

2. **Status message in `HandleMouseMove` during two-point AnchorPlaced state**
   - What we know: Currently `HandleMouseMove` always sets `StatusMessage = "Click 2nd line"` when `DrawState.AnchorPlaced && ActiveTool == ToolMode.Protractor`.
   - What's unclear: This is wrong for two-point mode. Should it say "Click to set baseline direction"?
   - Recommendation: Check `AnchorLine is null` in `HandleMouseMove` to distinguish and set appropriate message. Low-risk change.

3. **Degenerate: click 1 and click 2 at same pixel in two-point mode**
   - What we know: `Math.Atan2(0, 0)` returns 0.0 — no exception.
   - What's unclear: Is a 0° baseline a good default or should the placement be rejected?
   - Recommendation: Accept 0° baseline. The student can immediately rotate ±1°/±5° to any desired angle from the right rail. Rejection would require another click, violating the 2-click maximum.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Guid.Empty` as sentinel for "no source lines" in `ProtractorObject` works correctly with JSON serialization — `System.Text.Json` serializes `Guid.Empty` as `"00000000-0000-0000-0000-000000000000"` and deserializes it back | Scope Boundary | Low — `Guid.Empty` is a standard .NET value; `System.Text.Json` handles it correctly. Backwards-compatible with existing sidecars since no existing protractor has `Guid.Empty` IDs. |
| A2 | Suppressing Practice Mode readout for `Line1Id == Guid.Empty` is better UX than showing "0°" | Code Examples / Pitfall 3 | Low — if wrong, simply remove the guard; "0°" readout is cosmetically incorrect but not harmful |
| A3 | "Click vertex (or a line)" is the right activation status message | Architecture Patterns | Very low — cosmetic; planner may refine wording |
| A4 | Adding a dashed arm line in the two-point ghost is beneficial UX | Open Questions | Low — purely visual; can be omitted if planner decides ghost arc is sufficient |

**Critical claims with no assumptions:** placement math, state machine structure, affected file set, `ProtractorObject` field compatibility, serialization compatibility, right-rail compatibility — all VERIFIED by codebase inspection.

---

## Sources

### Primary (HIGH confidence — VERIFIED by codebase inspection)

- `MathGaze/ViewModels/ToolViewModel.cs` — complete state machine for protractor placement; two-line path confirmed; `AnchorLine` and `AnchorPt` fields confirmed public; `ResetDrawState` logic confirmed
- `MathGaze/Core/Geometry/ProtractorObject.cs` — all fields confirmed; `Guid Line1Id/Line2Id` are `init`-only; constructor signature confirmed
- `MathGaze/ViewModels/PdfCanvasViewModel.cs` — `DrawGhostProtractor` implementation confirmed; reads `_toolVm.AnchorLine` and `_toolVm.GhostCursorPx`; `_toolVm.AnchorPt` is readable
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — `ComputeMeasuredAngle` null-guard confirmed (`if (line1 is null || line2 is null) return 0f`); `DrawProtractor` IsPracticeMode guard location confirmed
- `MathGaze/ViewModels/RightRailViewModel.cs` — `CanProtractor()` is `SelectedObject is ProtractorObject`; all five command groups confirmed placement-path-agnostic
- `MathGaze/Services/SessionService.cs` — `ProtractorObject` serialized via `[JsonDerivedType]` on `GeometryObject`; no field-level filter that would break `Guid.Empty`
- `MathGaze/Core/Geometry/GeometryObject.cs` — `[JsonDerivedType(typeof(ProtractorObject), "protractor")]` confirmed present

### Secondary (HIGH confidence — from project STATE.md and phase 3 RESEARCH.md)

- `.planning/STATE.md` — decision log confirming `BaselineAngleDeg` is screen-space CW angle stored at placement time; `CoordinateMapper.Scale` is private; proxy-point pattern for screen radius
- `.planning/phases/03-protractor/03-RESEARCH.md` — confirmed `DrawGhostProtractor` intent and placement conventions from Phase 3 research

---

## Metadata

**Confidence breakdown:**
- Affected files and required changes: HIGH — verified by reading every file in scope
- Baseline angle math: HIGH — same `Math.Atan2` formula already used in two-line path
- Serialization compatibility: HIGH — `Guid.Empty` is a valid standard .NET value; no new fields added
- Right-rail compatibility: HIGH — `CanProtractor()` is type-check only, path-agnostic
- UX decisions (status messages, readout suppression, dashed arm line): ASSUMED — reasonable defaults, easily changed

**Research date:** 2026-05-28
**Valid until:** Stable indefinitely — this phase touches no external dependencies; only internal codebase patterns that change only when explicitly modified.
