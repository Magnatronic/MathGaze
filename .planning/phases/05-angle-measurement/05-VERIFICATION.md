---
phase: 05-angle-measurement
verified: 2026-05-28T17:40:24Z
status: human_needed
score: 6/6 must-haves verified
human_verification:
  - test: "Activate Protractor, click empty canvas — confirm status bar shows 'Click to set baseline direction' and the ghost protractor arc is pinned at the clicked vertex (not floating at cursor)"
    expected: "Ghost protractor arc is stationary at click 1 position; dashed arm line stretches from that vertex to the moving cursor; status shows 'Click to set baseline direction'"
    why_human: "Ghost rendering and status bar are UI-layer behaviours driven by Paint() and WPF bindings; cannot be verified by static code inspection alone"
  - test: "While in two-point mode (after click 1 on empty canvas), click a second point. Confirm protractor is placed centred exactly at click 1 with baseline facing click 2"
    expected: "Protractor renders centred at vertex; baseline angle equals screen-space direction from vertex to click 2; right-rail rotate/flip/style controls are immediately active"
    why_human: "Placement correctness (angle math, visual alignment) requires runtime observation"
  - test: "Activate Protractor, click an existing Line, then click a second Line. Confirm the two-line path still auto-places at their intersection (regression check)"
    expected: "Protractor placed at intersection of two lines, baseline aligned to first line — identical behaviour to Phase 3"
    why_human: "Two-line regression requires both a rendered PDF with placed LineObjects and a live click sequence"
  - test: "In Practice Mode, select a two-point-placed protractor and confirm no angle readout ('0 deg') is displayed inside or around the arc"
    expected: "No numeric readout visible. The arc, ticks, and labels render normally; only the inner arc + degree label are absent"
    why_human: "Requires visual inspection of the rendered canvas in Practice Mode"
  - test: "Save a session containing a two-point-placed protractor (Guid.Empty line IDs), close the app, reopen the same PDF. Confirm the protractor restores at the correct position and angle"
    expected: "Protractor reappears at the placed vertex, same baseline angle. No crash, no missing fields in the sidecar JSON"
    why_human: "Requires running the app, closing it, reopening it, and visually inspecting the restored canvas"
---

# Phase 5: Angle Measurement Verification Report

**Phase Goal:** Students can measure pre-drawn angles on exam papers by placing the protractor via two clicks (vertex + arm direction) — no drawn lines required
**Verified:** 2026-05-28T17:40:24Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Clicking empty canvas in Protractor+Idle mode stores vertex as AnchorPt and advances to AnchorPlaced; status shows "Click to set baseline direction" | VERIFIED | ToolViewModel.cs line 154-159: AnchorPt set from mapper.ScreenToPage(screenPx); DrawState = AnchorPlaced; StatusMessage = "Click to set baseline direction" inside else-branch of Idle case |
| 2 | Second click on empty canvas places protractor centred at AnchorPt, baseline facing click 2, Line1Id=Guid.Empty, Line2Id=Guid.Empty | VERIFIED | ToolViewModel.cs lines 263-280: else-branch of AnchorPlaced case computes baseline angle via Math.Atan2, constructs ProtractorObject(AnchorPt, baselineAngleDeg, Guid.Empty, Guid.Empty) |
| 3 | Clicking two existing lines still places protractor at their intersection (two-line path unchanged — no regression) | VERIFIED | ToolViewModel.cs lines 167-261: entire existing two-line logic is intact inside `if (AnchorLine is not null)` block, including same-line fallback, parallel-line guard, and intersection math |
| 4 | Ghost in two-point mode is anchored at vertex (not floating at cursor) and rotates toward cursor with a dashed arm line | VERIFIED | PdfCanvasViewModel.cs lines 345-388: else-if branch derives ghostCenterPx from AnchorPt via PageToScreen; dashed arm line drawn from ghostCenterPx to centerPx (cursor) when anchorLine is null && AnchorPt.HasValue |
| 5 | Practice Mode readout is suppressed for two-point protractors (Line1Id == Guid.Empty guard) | VERIFIED | GeometryLayerViewModel.cs line 416: `if (_mainVm.IsPracticeMode && obj.Line1Id != Guid.Empty)` — readout block gated on non-empty Line1Id |
| 6 | Two-point protractors survive save/restore (Guid.Empty serialises as "00000000-0000-0000-0000-000000000000" — no new fields) | VERIFIED | System.Text.Json serialises Guid as string by default; Guid.Empty → "00000000-0000-0000-0000-000000000000". ProtractorObject.Line1Id and Line2Id are `{ get; init; }` Guid fields already present in the model (ProtractorObject.cs lines 53, 59). No new fields added. SessionService uses JsonSerializer.Serialize/Deserialize with no custom converters that would break Guid.Empty. |

**Score:** 6/6 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `MathGaze/ViewModels/ToolViewModel.cs` | Extended state machine: two-point branch in Idle and AnchorPlaced cases | VERIFIED | Contains "AnchorLine is null" discriminator (line 326), "Click vertex (or a line)" (line 53), "Click to set baseline direction" (lines 159, 327), "Guid.Empty, Guid.Empty" (line 274) |
| `MathGaze/ViewModels/PdfCanvasViewModel.cs` | Two-point ghost preview anchored at vertex | VERIFIED | Contains `_toolVm.AnchorPt.HasValue` (lines 345, 378), `ghostCenterPx` computed variable (line 334, assigned at lines 340, 349) |
| `MathGaze/ViewModels/GeometryLayerViewModel.cs` | Readout guard for Guid.Empty protractors | VERIFIED | Contains `obj.Line1Id != Guid.Empty` at line 416 inside DrawProtractor, within the canvas.Save()/Restore() scope |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| ToolViewModel.HandleCanvasClick (Protractor, Idle) | AnchorLine == null sentinel | else branch when TryHitObject returns non-LineObject | WIRED | ToolViewModel.cs: hit is LineObject → two-line path; else → AnchorPt set, AnchorLine stays null |
| DrawGhostProtractor | AnchorPt vertex position | _toolVm.AnchorPt.HasValue branch | WIRED | PdfCanvasViewModel.cs line 345: else if (_toolVm.AnchorPt.HasValue) derives ghostCenterPx via PageToScreen |
| DrawProtractor | readout suppression | obj.Line1Id != Guid.Empty guard | WIRED | GeometryLayerViewModel.cs line 416: guard is the condition on the IsPracticeMode block; two-point protractors pass Guid.Empty which short-circuits the readout |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| ToolViewModel (two-point Idle) | AnchorPt | mapper.ScreenToPage(screenPx) from live mouse event | Yes — screen coordinates from user click | FLOWING |
| ToolViewModel (two-point AnchorPlaced) | baselineAngleDeg | Math.Atan2(screenPx.Y - anchorScreen.Y, screenPx.X - anchorScreen.X) | Yes — computed from two real click positions | FLOWING |
| DrawGhostProtractor | ghostCenterPx | _coordinateMapper.PageToScreen(anchorPt.xPt, anchorPt.yPt) | Yes — derived from stored AnchorPt via live CoordinateMapper | FLOWING |
| GeometryLayerViewModel (readout guard) | obj.Line1Id | ProtractorObject.Line1Id set at construction with Guid.Empty | Yes — guard evaluates actual stored value at render time | FLOWING |

### Behavioral Spot-Checks

Step 7b: SKIPPED — requires running WPF application with gaze/click simulation. All key behaviors are verified through static code analysis; runtime checks routed to human verification.

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|---------|
| PROT-07 | 05-01-PLAN.md | User can place a protractor via two-point click (click vertex then a point on one arm) without needing pre-drawn lines | SATISFIED (code complete; runtime verification human-needed) | ToolViewModel two-point branch present; ProtractorObject(Guid.Empty, Guid.Empty) construction confirmed; all 6 must-have truths verified |

### Anti-Patterns Found

No anti-patterns detected in any of the three modified files. No TODO, FIXME, placeholder, or empty-return patterns found. No hardcoded stub patterns. All new code branches contain substantive logic (coordinate math, state transitions, rendering).

### Human Verification Required

All six must-have truths are verified at the static code level. The implementation is complete and correctly wired. However, five runtime behaviours require a human tester with the running app:

#### 1. Ghost Preview Visual Correctness

**Test:** Activate Protractor mode, click an empty area of a PDF canvas. Move the mouse. Watch the ghost rendering.
**Expected:** The ghost protractor arc stays pinned at the click point (vertex), does not float with the cursor. A dashed line connects the vertex to the cursor. The arc rotates as the cursor moves. Status bar reads "Click to set baseline direction".
**Why human:** Ghost rendering is driven by Paint() on the SkiaSharp canvas — cannot be verified by static analysis of the drawing logic alone.

#### 2. Two-Point Placement Correctness

**Test:** After the ghost appears (click 1 done), click a second point in a different direction from the vertex.
**Expected:** Protractor appears centred exactly at click 1. Baseline faces toward click 2. Right-rail rotate/flip/style buttons are immediately active. Status shows "Protractor placed".
**Why human:** Angle math correctness (Math.Atan2 direction, screen-space vs PDF-space) and visual alignment require runtime observation.

#### 3. Two-Line Path Regression

**Test:** Draw two LineObjects on the canvas. Activate Protractor, click Line 1, then click Line 2.
**Expected:** Protractor auto-places at the intersection of the two lines with baseline aligned to Line 1 — identical to Phase 3 behaviour.
**Why human:** Requires placed LineObjects, live hit-testing, and visual confirmation that the intersection placement is correct.

#### 4. Practice Mode Readout Suppression

**Test:** Place a two-point protractor. Toggle into Practice Mode (chip in top bar).
**Expected:** No angle readout (no inner arc, no "N deg" label) appears inside the two-point protractor. A two-line protractor placed in the same session does show the readout.
**Why human:** Requires visual inspection of the canvas render in Practice Mode with both types of protractor present.

#### 5. Save/Restore Round-Trip

**Test:** Place a two-point protractor, then close and reopen the app with the same PDF.
**Expected:** The protractor reappears at the correct position and baseline angle. The sidecar JSON contains Line1Id and Line2Id as "00000000-0000-0000-0000-000000000000". No crash on restore.
**Why human:** Requires running the app, closing it, and reopening it; also requires inspecting the sidecar JSON file on disk.

### Build Status

`dotnet` SDK was not available in the verification environment. Build verification (0 errors, no new warnings) must be confirmed by the developer. The SUMMARY.md self-check records Build: 0 errors, no new warnings as PASSED at commit time (commits 8af53d6, 2e1f48e).

The three files modified contain no syntax indicators of build failure: all new code follows existing patterns (switch expression arms, `using var` SKPaint locals, standard C# nullable patterns). No new NuGet dependencies, no new using directives, no new classes.

### Gaps Summary

No gaps. All six must-have truths are verified at the static code level. The implementation matches the plan exactly with no deviations. PROT-07 is satisfied in code. Status is `human_needed` because five runtime behaviours (ghost visual, placement correctness, regression, readout suppression, save/restore) cannot be confirmed without running the app.

---

_Verified: 2026-05-28T17:40:24Z_
_Verifier: Claude (gsd-verifier)_
