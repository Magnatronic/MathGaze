---
phase: 04-answer-layer
plan: "02"
subsystem: text-tool-ux
tags: [text-tool, clipboard, skia-rendering, right-rail, tool-mode]
dependency_graph:
  requires:
    - "04-01: TextObject model + GeometryObject serialization foundation"
  provides:
    - "ToolMode.Text enum value + ActivateTextCommand + clipboard placement handler"
    - "DrawTextLabel method in GeometryLayerViewModel (Consolas 14pt, selection rect)"
    - "RightRailViewModel TextObject => 'Text' selection type arm"
    - "ToolRail.xaml Text button wired to ActivateTextCommand"
  affects:
    - "04-03: Session persistence (TextObject now fully renderable + selectable)"
tech_stack:
  added: []
  patterns:
    - "System.Windows.Clipboard.ContainsText/GetText on STA thread (synchronous, inside MouseDown handler)"
    - "SKFont-based DrawText overload (Phase 3 pattern) — avoids CS0618 on SKPaint.TextSize"
    - "SKFont.MeasureText for ink-bounds-based selection rect with 4px padding"
    - "Cached readonly SKPaint + SKFont fields in GeometryLayerViewModel — no per-frame allocation"
key_files:
  created: []
  modified:
    - "MathGaze/ViewModels/ToolViewModel.cs"
    - "MathGaze/Views/ToolRail.xaml"
    - "MathGaze/ViewModels/GeometryLayerViewModel.cs"
    - "MathGaze/ViewModels/RightRailViewModel.cs"
decisions:
  - "Clipboard read kept synchronous on STA thread inside HandleCanvasClick (MouseDown handler) — no async/Task.Run wrapper; COMException would result if moved off STA (Pitfall 4 / T-04-06)"
  - "DrawTextLabel baseline placed at PageToScreen(XPt, YPt); selection rect uses _textFont.MeasureText ink bounds + 4px padding on all sides for visible gaze-friendly highlight"
  - "NudgeLabel wildcard _ => 'Move' already covers TextObject correctly per D-06; no additional arm needed in RightRailViewModel"
  - "Consolas font with SKTypeface.FromFamilyName fallback to SKTypeface.Default — guarantees font loads on any Windows machine regardless of font availability"
metrics:
  duration_minutes: 5
  completed_date: "2026-05-27"
  tasks_completed: 2
  tasks_total: 2
  files_created: 0
  files_modified: 4
---

# Phase 4 Plan 2: Text tool UX — clipboard placement + SkiaSharp rendering Summary

**One-liner:** End-to-end Text tool wired via ToolRail button → clipboard read on STA thread → TextObject placement → Consolas 14pt SkiaSharp rendering with cobalt selection rect → right rail nudge/delete working automatically.

## What Was Built

### ToolViewModel — Text mode activation and clipboard placement (`MathGaze/ViewModels/ToolViewModel.cs`)

Three changes:

1. `ToolMode.Text` added to the enum after `Protractor`
2. `ActivateTextCommand` (`[RelayCommand]` on `ActivateText()`) — resets draw state, sets `ActiveTool = ToolMode.Text`, sets status "Copy text, then click canvas"
3. `case (ToolMode.Text, DrawState.Idle):` switch arm in `HandleCanvasClick`:
   - Calls `System.Windows.Clipboard.ContainsText()` synchronously on the STA UI thread (MouseDown handler — always STA)
   - If empty/whitespace: sets `StatusMessage = "Copy text first, then click to place"`, breaks without creating any object
   - If text present: converts click position to PDF-space via `mapper.ScreenToPage`, executes `PlaceObjectCommand(new TextObject(clipText, xPt, yPt))` through the geometry service

### ToolRail.xaml — Text button wired (`MathGaze/Views/ToolRail.xaml`)

- Removed `(Phase 4)` comment and placeholder tooltip text
- Added `Command="{Binding ActivateTextCommand}"` and `ToolTip="Text tool"` to the existing Text button element

### GeometryLayerViewModel — TextObject rendering (`MathGaze/ViewModels/GeometryLayerViewModel.cs`)

Three new cached readonly fields (declared alongside existing paint cache — no per-frame allocation):
- `_textPaint` — `SKPaintStyle.Fill`, BrushInk colour (0x1A1A2E, alpha 220), antialiased
- `_textSelectedBorderPaint` — `SKPaintStyle.Stroke`, BrushAccent cobalt (0x3B6FD4), 1.5px stroke, antialiased
- `_textFont` — `SKFont` Consolas 14pt with `SKTypeface.Default` fallback

New `case TextObject text:` arm in `DrawObject` switch dispatches to `DrawTextLabel`.

New `DrawTextLabel` private method:
- Guards on `string.IsNullOrEmpty(text.ContentText)`
- Converts `(XPt, YPt)` to screen via `mapper.PageToScreen`
- Calls `canvas.DrawText(text.ContentText, x, y, SKTextAlign.Left, _textFont, _textPaint)` — SKFont-based API per Phase 3 pattern
- When selected: calls `_textFont.MeasureText(text.ContentText, out SKRect bounds)` for ink bounds, draws cobalt bounding rect with 4px padding via `_textSelectedBorderPaint`

All three fields disposed in `Dispose()` alongside existing paint disposals.

### RightRailViewModel — TextObject selection type (`MathGaze/ViewModels/RightRailViewModel.cs`)

`TextObject => "Text"` arm added to the `SelectedObjectType` switch expression — right rail displays "Text" as the object type when a TextObject is selected. The existing `NudgeLabel` wildcard `_ => "Move"` already handles TextObject correctly per D-06 (no text-specific nudge label needed). `CanNudge()` and `CanDelete()` already cover TextObject via `is not null` check — no changes needed.

## Test Results

No new automated tests in this plan (UX wiring plan — rendering and command routing covered by Plan 01's TextObject model tests). Manual verification:

- Build: 0 errors, 6 expected NU1701 warnings (SkiaSharp/OpenTK compat shim)
- Grep checks: all acceptance criteria confirmed present in modified files

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None. The Text tool is fully wired end-to-end:
- Button activates Text mode (no stub)
- Clipboard read places TextObject (no stub data)
- SkiaSharp renders from actual `ContentText` field (no placeholder)
- Right rail shows nudge/delete for selected TextObject (no stub controls)

## Threat Flags

No new threat surface beyond what is documented in the plan's threat model:
- T-04-04 (Clipboard text as injection vector): accepted — text rendered by SkiaSharp as visual label only, never parsed or executed
- T-04-05 (DoS via long clipboard content): mitigated in Plan 01 — TextObject constructor truncates at 500 chars
- T-04-06 (Clipboard off STA thread): mitigated — STA comment explicit in code; call is synchronous inside MouseDown handler

## Self-Check: PASSED

Files modified exist:
- `MathGaze/ViewModels/ToolViewModel.cs` — FOUND (contains `ToolMode.Text`, `ActivateText`, `case (ToolMode.Text, DrawState.Idle):`, `Clipboard.ContainsText`, `new TextObject(clipText, xPt, yPt)`)
- `MathGaze/Views/ToolRail.xaml` — FOUND (contains `ActivateTextCommand` binding)
- `MathGaze/ViewModels/GeometryLayerViewModel.cs` — FOUND (contains `_textPaint`, `_textSelectedBorderPaint`, `_textFont`, `case TextObject text:`, `DrawTextLabel`, `_textFont.MeasureText`, `_textPaint.Dispose()`)
- `MathGaze/ViewModels/RightRailViewModel.cs` — FOUND (contains `TextObject => "Text"`)

Commits:
- `011f6c4` — feat(04-02): wire Text tool — ToolMode.Text + ActivateText + clipboard placement — FOUND
- `dd827b7` — feat(04-02): TextObject SkiaSharp rendering + RightRail selection type — FOUND

Build: 0 errors confirmed.
