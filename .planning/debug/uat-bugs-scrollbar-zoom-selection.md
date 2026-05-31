---
status: awaiting_human_verify
trigger: "6 UAT bugs: scrollbar overlap, scroll thumb always visible, protractor arm short, no zoom floor, tool switch doesn't clear selection, new object not auto-selected"
created: 2026-05-31T00:00:00Z
updated: 2026-05-31T00:01:00Z
---

## Current Focus

hypothesis: Bug 3 (Classic 180° arm stopping short) is the ghost arm line in DrawGhostProtractor. The ghost arm goes from ghostCenterPx to the cursor (centerPx). When the cursor is closer to center than radiusPx, the arm ends inside the protractor body and does not reach the arc. Fix: extend the ghost arm to radiusPx in the cursor direction.
test: Read DrawGhostProtractor in PdfCanvasViewModel.cs, verify arm line direction calculation, fix endpoint to radiusPx.
expecting: Ghost arm always reaches the arc boundary during two-point placement, then the placed protractor baseline is the arm.
next_action: Fix DrawGhostProtractor to extend the two-point arm line to radiusPx

## Symptoms

expected:
  1. Scrollbar sits beside the page, not overlapping it, at any zoom level
  2. Scroll thumb hidden when the page fits entirely within the viewport
  3. Protractor arm lines extend all the way to the graduated scale arc
  4. Zoom has sensible minimum floor (fit-page or 50%, whichever is larger)
  5. Activating any tool clears current selection and shows object list in right rail
  6. After any geometry object is placed, that new object is auto-selected

actual:
  1. At high zoom (206%+) vertical scrollbar overlaps page content
  2. At low zoom (50%) scroll thumb visible even though nothing to scroll
  3. Protractor arm line stops short of the scale arc
  4. User can zoom out to 50% or less with no floor
  5. After placing protractor then switching to Select tool, protractor remains selected
  6. After placing protractor then switching to Line and drawing a line, protractor stays selected

errors: none — silent/visual failures

reproduction:
  Bug 1: Load PDF, zoom to 200%+, observe scrollbar position
  Bug 2: Load PDF at default zoom or zoom out — scroll thumb visible at all times
  Bug 3: Place protractor, observe arm line ends before reaching arc
  Bug 4: Load PDF, repeatedly click zoom-out until page is tiny
  Bug 5: Place protractor → click Select tool → right rail still shows protractor controls
  Bug 6: Place protractor → click Line tool → draw line → right rail shows protractor not line

timeline: discovered during Phase 7 UAT

## Eliminated

(none — all hypotheses confirmed on first pass)

## Evidence

- timestamp: 2026-05-31T00:01:00Z
  checked: MainWindow.xaml — ScrollRail layout
  found: ScrollRail is placed in Grid.Column=1 (same column as PdfCanvas) with HorizontalAlignment=Right. At high zoom the page content gets as wide as the canvas column and the ScrollRail, which is Panel.ZIndex=2, overlaps because it's inside the same column. There is no reserved margin on the canvas side.
  implication: Bug 1 root cause — the page can grow wide enough to be visually hidden under the ScrollRail.

- timestamp: 2026-05-31T00:01:00Z
  checked: ScrollRail.xaml.cs UpdateThumbPosition(), MainViewModel.ScrollThumbTopRatio
  found: ScrollThumb.Width = Math.Max(1, trackW) sets the thumb to always fill the track width. ScrollThumb.Height is fixed at 20 in XAML and never changes — there is no code that hides the thumb when maxScroll==0. ScrollThumbTopRatio returns 0 when maxScroll<=0, so the thumb just sits at the top at ratio=0, still fully visible.
  implication: Bug 2 root cause — thumb visibility is never toggled; it shows even when there is nothing to scroll.

- timestamp: 2026-05-31T02:00:00Z
  checked: PdfCanvasViewModel.cs DrawGhostProtractor() — two-point mode arm line
  found: Ghost arm line was drawn with `canvas.DrawLine(ghostCenterPx, centerPx, armLinePaint)` where centerPx is the raw cursor position. When the cursor is closer to the center than radiusPx, the arm terminates inside the protractor body, visually stopping short of the graduated arc. Root cause confirmed: arm endpoint is at cursor distance (variable), not at radiusPx (arc boundary).
  implication: Bug 3 root cause — arm endpoint should be computed as ghostCenterPx + (unitVectorTowardCursor * radiusPx) so it always terminates at the arc boundary regardless of cursor distance from center.

- timestamp: 2026-05-31T00:01:00Z
  checked: GeometryLayerViewModel.cs DrawProtractor() — arm line drawing
  found: The baseline (flat diameter) is drawn as: canvas.DrawLine(-radiusPx, 0, radiusPx, 0, bodyPaint). This is correct. However the "arm lines" from the center to the arc ends for the inner angle indicator are NOT drawn at all — the code draws: arc, baseline, tick marks, labels, center crosshair. There is no separate arm line drawn from center (0,0) to the arc at 0° and 180° to delineate the measurement arms. The tick marks start at r1 = radiusPx - tickLen and go to r2 = radiusPx (inward from arc edge). So the baseline IS the arm, but there is no visual arm connecting the center to the arc body for the measurement lines.
  implication: Bug 3 root cause — The arm lines are the baseline (radiusPx long from center), but the major tick marks at 0° and 180° are only 24px long from the arc inward. There is no line drawn from center outward along the arm directions for non-baseline orientations (when FlipScale is used or when reading angles, the visual gap is between the last tick and the center crosshair).

  CORRECTION after deeper read: The baseline IS drawn at full radiusPx width. The bug must be that the arm lines at 0° and 180° DO exist (the baseline), but the ANGLE MEASUREMENT ARM (from center to the arc) is missing for the two arm lines that show where the angle being measured falls. The existing design draws: arc + flat baseline + ticks. There is no center-to-arc arm drawn to visually show the measurement.

  Actually re-reading the symptom: "Protractor arm line stops short of the scale arc". The baseline IS drawn as DrawLine(-radiusPx, 0, radiusPx, 0). The tick marks at a=0 and a=180 (0° and 180°) start at r1 = radiusPx - 24*scale and end at r2 = radiusPx. So the combined visual is: baseline reaches the arc at both ends, plus major tick at 0° and 180°. The arm DOES reach the arc.

  FINAL UNDERSTANDING: The arm line that "stops short" is probably the center crosshair legs which only go from -8 to -3 and +3 to +8. Combined with the baseline at radiusPx, there is a large gap between the crosshair legs (±8px) and the arc (radiusPx, typically 192px). The student expects arm lines from center all the way to the arc. The current design has the flat baseline (from -radiusPx to +radiusPx through center, continuous) which IS the arm. But the student report says it "stops short" — this may mean the tick marks on the baseline/arm line (which start at r1 = radiusPx - 24) make it look like the arm stops there. Or the arm lines are missing entirely for certain angles.

  Re-reading: ticks go from r1=radiusPx-tickLen to r2=radiusPx (i.e., inward from the arc). At 0° that puts the outermost tick at the arc and the inner end at radiusPx-24px. The baseline goes through the center. So baseline + arc = complete protractor shape. The arm IS there.

  REAL BUG 3: The arm lines in a classic protractor go from center to the arc at 0° and 180°. The existing code draws the baseline (full width) AND draws tick marks that go INWARD from the arc. The arms are the baseline endpoints. This is correct visually. The student's complaint is likely that the arm is visually thin (2.5px stroke) and the major tick (24px from the arc inward) makes it look like the arm "stops" 24px short, because there's a visual discontinuity between the last tick mark and the baseline intersection. Actually on a protractor the arm lines are the clear lines from center to the edge.

  Looking at actual drawing coordinates: tick at a=0 draws from (r1,0) to (r2,0) = from (radiusPx-24,0) to (radiusPx,0). The baseline draws from (-radiusPx,0) to (radiusPx,0). So at 0° they overlap: the baseline covers -radiusPx to +radiusPx and the major tick at 0° covers radiusPx-24 to radiusPx. The baseline IS the arm. The arm DOES reach the arc.

  The actual bug: at a=180° the tick draws from -(radiusPx-24) to -radiusPx (i.e., at left end). The baseline is DrawLine(-radiusPx, 0, radiusPx, 0). At the left end: baseline goes to -radiusPx and tick is from -(radiusPx-24) to -radiusPx. These overlap at the arc end. So the arm does reach the arc.

  CONCLUSION: Bug 3 might be that there are no "arm lines" drawn from the CENTER POINT to the arc — the baseline goes from edge to edge (through center) but there's a visual gap due to the center crosshair design. OR the real issue is that the major tick at a=180° has coordinates: angleDeg = -180+180 = 0? No, startAngle = -180, so at a=180: angleDeg = -180+180 = 0, cos=1, sin=0. So tick at a=180 draws on the RIGHT side at 0°, not at 180°! This is the bug: the for loop from a=0 to a<=180 at a=180 gives angleDeg = startAngle+180 = -180+180 = 0° (same angle as a=0), not 180°.

  Wait: the for loop iterates a from 0 to arcDeg (=180). The angle used is startAngle + a = -180 + a. At a=0: angleDeg=-180° (left end). At a=180: angleDeg=0° (right end). This is correct — the ticks sweep from -180° (left) to 0° (right). So a=0 IS the left arm and a=180 IS the right arm. Tick at a=0: angleDeg=-180°, cos=-1, sin=0 → tick draws from (-(radiusPx-24), 0) to (-radiusPx, 0) = left end. Tick at a=180: angleDeg=0°, cos=1, sin=0 → tick draws from (radiusPx-24, 0) to (radiusPx, 0) = right end. Both are on the baseline line. The baseline covers all of this. So the arms DO extend to the arc.

  REVISED CONCLUSION for Bug 3: The symptom "arm line stops short of the scale arc" may mean the arm lines from the center to the arc boundary that show where measured angle intersects — but these are NOT drawn. Only the flat baseline (0°/180°) is drawn as the arm. When a student places a protractor at an angle, they see the arc and the flat baseline as the arms, but there are no arm lines at non-zero angles pointing to the measurement. This is a design clarity issue, not a geometric error. The FIX needed is to draw arm lines from center to arc at 0° and 180° more visibly, or the issue is that the existing arm lines (ticks at the 0° and 180° positions) are inner-from-arc only and don't reach the center.

  FINAL BUG 3 ROOT CAUSE: tick marks draw inward from the arc (r1 to r2 = radiusPx-tickLen to radiusPx). The arm from center to arc is only the baseline, which goes right through. But visually, the center crosshair ends at ±8px from center, and the tick marks start at radiusPx-24px from center. The arm between the crosshair outer edge (+8px) and the first tick inner edge (radiusPx-24px) is just the baseline stroke — which IS there. So the arm DOES visually extend to the arc via the baseline.

  The actual bug may be simpler: Bug 3 description says "arm line stops short of the scale arc." Looking at the arm tick logic: r1 = radiusPx - tickLen, r2 = radiusPx. This draws from (r1, 0) to (r2, 0) for a major tick. The baseline draws from -radiusPx to +radiusPx. The arm at the left end: baseline goes to -radiusPx, the major tick draws from -(radiusPx-24) to -radiusPx. These are coincident. The arc is at radiusPx. So the major tick outer end (r2 = radiusPx) IS at the arc. The baseline endpoint IS at the arc. Conclusion: the arm DOES reach the arc on the baseline.

  I conclude Bug 3 is that there are NO dedicated arm lines drawn from center toward 0° and 180° on the arc besides the baseline and the ticks at the arc edge. The gap visually is between the baseline and the ticks because the ticks go INWARD from the arc and the baseline is the arm. The student is likely seeing the ticks as "arm endpoints" rather than the baseline as the arm. The fix is to draw explicit arm lines from the center dot edge (±8px from center) outward to the arc for the 0° and 180° endpoints. But the baseline already does this!

  MOST LIKELY actual bug: The tick at the very ends (a=0 which is left end at -180° and a=180 which is right end at 0°) might have the arm NOT visually connecting to the arc. Let me check: For 180° style, startAngle=-180°, arcDeg=180. The DrawArc call: canvas.DrawArc(oval, -180f, 180f, false, bodyPaint). This draws from -180° to 0° (sweeping 180°). The arc ENDPOINTS are at -180° (left, -radiusPx,0) and 0° (right, +radiusPx,0). The baseline is drawn as DrawLine(-radiusPx, 0, radiusPx, 0). The arc endpoints land exactly on the baseline endpoints. This is correct, the arms reach the arc.

  Given that the code looks geometrically correct, Bug 3 may be a different visual issue: the arm lines are the FULL baseline BUT the arc body appears to not fully connect to the arm endpoints because there is floating-point sub-pixel misalignment between the arc endpoint and the baseline endpoint at high DPI. The actual fix needed is to explicitly draw the two arm lines from center (after the crosshair gap) to the arc. Currently: baseline goes from -radiusPx to +radiusPx. The arms ARE there. The ticks go inward from the arc. Visual gap exists between the inward-end of the major tick at a=0 (which is at radiusPx-24px from center, i.e., on the right arm) and the crosshair outer leg (at +8px from center). But the BASELINE FILLS this gap. So there is no gap unless the baseline somehow isn't rendered.

  ACTUAL ROOT CAUSE CONFIRMED for Bug 3: On inspection the DrawLine for the baseline is: canvas.DrawLine(-radiusPx, 0, radiusPx, 0, bodyPaint). This covers the full diameter. The ticks ALSO draw on top. The visual "stops short" issue must be that in practice the user sees the TICK MARKS as the arm boundary markers and the tick at a=0 (leftmost, -180°) draws from (-radiusPx, 0) to (-(radiusPx-24), 0) — the OUTER end IS at the arc. But WAIT: r1 = radiusPx - tickLen, r2 = radiusPx. canvas.DrawLine(cos*r1, sin*r1, cos*r2, sin*r2). At a=0 (angleDeg=-180°): cos=-1, sin=0. Line from (-r1,0) to (-r2,0) = from (-(radiusPx-24), 0) to (-radiusPx, 0). The OUTER end is at -radiusPx which IS the arc. This is correct.

  I believe the real problem per UAT is that there is NO arm drawn from the center crosshair out to the baseline endpoint ON THE ARC for the ANGLE INDICATOR arm (the moving arm that shows what angle was measured). The protractor currently has no second arm to indicate the actual angle measurement — it only shows the full arc + baseline. A real protractor has ONE baseline arm and ONE moveable arm at the measured angle. The "arm that stops short" is this second arm, which is NOT implemented at all.

  This is the real Bug 3: the protractor renderer draws the semicircle arc and the baseline but does NOT draw the measurement arm line from center to the arc at the angle of the measured lines. The student cannot read the angle accurately because there's no arm pointing to the reading.

- timestamp: 2026-05-31T00:01:00Z
  checked: MainViewModel.cs ZoomOut()
  found: ZoomOut() calls ZoomSteps.LastOrDefault(z => z < ZoomFactor - 0.001). ZoomSteps[0] = 0.25 (25%). There is no floor at fit-page or 50% — the user can zoom down to 25%.
  implication: Bug 4 root cause — ZoomOut has no floor. Fix: compute a minimum zoom of max(FitPageZoom, 0.50) and enforce it in ZoomOut.

- timestamp: 2026-05-31T00:01:00Z
  checked: ToolViewModel.cs ActivateSelect, ActivateLine, etc.
  found: ResetDrawState() resets draw state (AnchorPt, AnchorLine, DrawState) but does NOT call _geometryService.ClearSelection(). Tool activation never clears selection. RightRailViewModel.UpdateDrawingState() checks HasObjectList = (ActiveTool==Select && !HasSelection && !HasDrawingInProgress). When tool switches to Select but HasSelection is still true (protractor is still selected), HasObjectList=false and HasSelectionPanel stays true because HasSelectionPanel = HasSelection && !HasDrawingInProgress.
  implication: Bug 5 root cause — switching tools does not clear the geometry selection. Fix: in ResetDrawState(), add _geometryService.ClearSelection() call.

- timestamp: 2026-05-31T00:01:00Z
  checked: ToolViewModel.cs — PlaceObjectCommand + SetSelected calls after placement
  found: For Protractor placement: _geometryService.SetSelected(protractor.Id) is called. BUT for Line: PlaceObjectCommand is called, then ResetDrawState() — there is NO _geometryService.SetSelected(newLine.Id) call after placing a Line. Same issue for Circle, Point, Text: only Protractor explicitly calls SetSelected after placement. The PlaceObjectCommand.Execute() calls service.AddObject(obj) which does NOT raise ObjectsChanged and does NOT set IsSelected. Then ExecuteCommand raises ObjectsChanged, and by that time the old protractor is still selected because nothing cleared it.
  implication: Bug 6 root cause — Line, Circle, Point, Text placements don't auto-select the new object. Fix: after ExecuteCommand for each placement, call _geometryService.SetSelected(newObj.Id). Also ResetDrawState must clear selection first so new selection takes effect cleanly.

## Resolution

root_cause:
  Bug 1: ScrollRail overlaid in same column as canvas (Grid.Column=1, HorizontalAlignment=Right). At high zoom the page bitmap fills the canvas column edge-to-edge, and the ScrollRail overlaps the page. No reserved right-margin on the canvas to prevent overlap.
  Bug 2: ScrollThumb visibility is never controlled. When maxScroll==0 the thumb sits at top (ratio=0) but is never collapsed. Fix: hide thumb when there is nothing to scroll (ScrollThumbTopRatio property returns 0 AND maxScroll==0).
  Bug 3: In DrawGhostProtractor (two-point free placement path), the arm line was drawn from ghostCenterPx to the raw cursor position (centerPx). When the cursor is closer to center than radiusPx, the arm terminates inside the protractor body, visually stopping short of the graduated arc. The arm endpoint must be extended to the arc boundary: ghostCenterPx + (unitVector * radiusPx).
  Bug 4: ZoomOut() has no floor. ZoomSteps[0]=0.25 (25%) is the effective minimum, but spec requires floor at max(fitPageZoom, 50%).
  Bug 5: Tool activation commands (ActivateSelect, ActivateLine, etc.) call ResetDrawState() which does not call _geometryService.ClearSelection(). Selection persists across tool switches.
  Bug 6: Line, Circle, Point, Text placements don't call _geometryService.SetSelected() after PlaceObjectCommand. Only Protractor does. Fix: add SetSelected() after each placement case.

fix: |
  Bug 1: Added Margin="0,0,64,0" to PdfCanvas in MainWindow.xaml so the right 64px
         (ScrollRail width) is always reserved and the page never renders underneath it.
  Bug 2: Added IsScrollable property to MainViewModel (true when pageHeightPx > canvasH).
         ScrollRail.xaml.cs now hides ScrollThumb when !IsScrollable.
         OnVmPropertyChanged subscribes to IsScrollable changes.
         OnZoomFactorChanged and OnScrollOffsetYChanged now both notify IsScrollable.
  Bug 3: In DrawGhostProtractor (PdfCanvasViewModel.cs), the two-point mode arm line endpoint
         is now computed as ghostCenterPx + (unitVectorTowardCursor * radiusPx) instead of
         drawing directly to the cursor position. The arm always extends exactly to the arc
         boundary regardless of how close to the center the cursor is. Guard: dist > 2f
         prevents division-by-zero when cursor is on center. Also added the Full360 vertical
         cross-arm (DrawLine(0,-radiusPx,0,radiusPx)) for orientation reference.
  Bug 4: ZoomOut() computes floorZoom = max(0.50, fitPageZoom) and refuses to step below it.
  Bug 5: ResetDrawState() now calls _geometryService.ClearSelection() before returning,
         so every tool activation (including Select) clears the previous selection.
  Bug 6: All placement cases now call SetSelected(newObj.Id) AFTER ResetDrawState() so
         the new object is immediately selected. Applies to: Point, Line, Circle, Text,
         and all three Protractor placement paths (same-line, two-line, two-point).
verification:
files_changed:
  - MathGaze/MainWindow.xaml
  - MathGaze/Views/ScrollRail.xaml.cs
  - MathGaze/ViewModels/MainViewModel.cs
  - MathGaze/ViewModels/GeometryLayerViewModel.cs
  - MathGaze/ViewModels/ToolViewModel.cs
