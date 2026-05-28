---
phase: quick-260528-uyf
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - MathGaze/ViewModels/PdfCanvasViewModel.cs
  - MathGaze/ViewModels/GeometryLayerViewModel.cs
autonomous: true
requirements: []
must_haves:
  truths:
    - "Geometry stroke widths, dot radii, font sizes, and tick lengths scale proportionally at all zoom levels"
    - "Ghost preview stroke widths scale with zoom (not just DPI)"
    - "The paint/font cache in GeometryLayerViewModel invalidates when zoom changes, not only when DPI changes"
  artifacts:
    - path: "MathGaze/ViewModels/PdfCanvasViewModel.cs"
      provides: "combinedScale (dpiScale * ZoomFactor) passed to geometry Draw and used in ghost methods"
    - path: "MathGaze/ViewModels/GeometryLayerViewModel.cs"
      provides: "_lastScale field (renamed from _lastDpiScale) invalidates paint cache on zoom AND DPI changes"
  key_links:
    - from: "PdfCanvasViewModel.Paint()"
      to: "GeometryLayerViewModel.Draw()"
      via: "third argument: _dpiScale * _mainVm.ZoomFactor"
      pattern: "_geometryLayer\\.Draw\\(canvas, _coordinateMapper, _dpiScale \\* _mainVm\\.ZoomFactor\\)"
    - from: "PdfCanvasViewModel.DrawGhostPreview()"
      to: "ghost paint StrokeWidth"
      via: "float ds = (float)(_dpiScale * _mainVm.ZoomFactor)"
      pattern: "float ds = \\(float\\)\\(_dpiScale \\* _mainVm\\.ZoomFactor\\)"
    - from: "PdfCanvasViewModel.DrawGhostProtractor()"
      to: "ghost arc/arm paint StrokeWidth"
      via: "float dps = (float)(_dpiScale * _mainVm.ZoomFactor)"
      pattern: "float dps = \\(float\\)\\(_dpiScale \\* _mainVm\\.ZoomFactor\\)"
    - from: "GeometryLayerViewModel.Draw()"
      to: "paint/font size cache guard"
      via: "_lastScale field compared against incoming combined scale"
      pattern: "_lastScale"
---

<objective>
Fix geometry scaling with zoom: geometry sizes (stroke widths, dot radii, font sizes, tick lengths)
currently only scale by _dpiScale which is a constant property of the display monitor. They need to
scale by the combined factor _dpiScale * ZoomFactor so geometry appears proportionally correct at all
zoom levels.

Purpose: When a student zooms in or out, all drawn geometry — lines, circles, protractors, text
labels, ghost previews — must remain proportional to the PDF content, not appear fixed-size.

Output: Three targeted line edits in PdfCanvasViewModel.cs and one field rename in GeometryLayerViewModel.cs.
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/PROJECT.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Pass combinedScale to geometry Draw and ghost methods in PdfCanvasViewModel</name>
  <files>MathGaze/ViewModels/PdfCanvasViewModel.cs</files>
  <action>
Make exactly three targeted substitutions in PdfCanvasViewModel.cs:

1. Line 216 — in Paint(): change
     `_geometryLayer.Draw(canvas, _coordinateMapper, _dpiScale);`
   to
     `_geometryLayer.Draw(canvas, _coordinateMapper, _dpiScale * _mainVm.ZoomFactor);`

2. In DrawGhostPreview(): change
     `float ds = (float)_dpiScale;`
   to
     `float ds = (float)(_dpiScale * _mainVm.ZoomFactor);`

3. In DrawGhostProtractor(): change
     `float dps = (float)_dpiScale;`
   to
     `float dps = (float)(_dpiScale * _mainVm.ZoomFactor);`

No other changes. The variable name `ds` and `dps` remain the same — only the initializer changes.
  </action>
  <verify>
    <automated>grep -n "_dpiScale \* _mainVm.ZoomFactor" "MathGaze/ViewModels/PdfCanvasViewModel.cs"</automated>
  </verify>
  <done>Three matches for `_dpiScale * _mainVm.ZoomFactor` appear in PdfCanvasViewModel.cs (Paint, DrawGhostPreview, DrawGhostProtractor)</done>
</task>

<task type="auto">
  <name>Task 2: Rename _lastDpiScale to _lastScale in GeometryLayerViewModel and build</name>
  <files>MathGaze/ViewModels/GeometryLayerViewModel.cs</files>
  <action>
Make exactly two targeted substitutions in GeometryLayerViewModel.cs:

1. Field declaration (line 150) — change
     `private double _lastDpiScale = 0.0;`
   to
     `private double _lastScale = 0.0;`

2. Inside Draw(), the cache guard block — change both occurrences of `_lastDpiScale`:
   a. The comparison:
        `if (Math.Abs(dpiScale - _lastDpiScale) > 0.001)`
      to
        `if (Math.Abs(dpiScale - _lastScale) > 0.001)`
   b. The assignment:
        `_lastDpiScale = dpiScale;`
      to
        `_lastScale = dpiScale;`

Also update the comment above the field from:
  `// DPI scale tracking — updated at the top of Draw() when dpiScale changes.`
  `// _lastDpiScale = 0 forces a first-run update of all paint/font sizes.`
to:
  `// Scale tracking — updated at the top of Draw() when the combined scale (dpiScale * ZoomFactor) changes.`
  `// _lastScale = 0 forces a first-run update of all paint/font sizes.`

The parameter name `dpiScale` in Draw()'s signature stays unchanged (callers pass the combined value positionally).

After edits, build to confirm zero errors:
  C:\dotnet9\dotnet.exe build MathGaze/MathGaze.csproj --no-incremental -v minimal
  </action>
  <verify>
    <automated>C:\dotnet9\dotnet.exe build "C:\Local Docs\Coding\MathGaze\MathGaze\MathGaze.csproj" --no-incremental -v minimal 2>&1 | Select-String -Pattern "error|warning|Build succeeded"</automated>
  </verify>
  <done>Build succeeds with 0 errors. `_lastDpiScale` does not appear anywhere in GeometryLayerViewModel.cs. `_lastScale` appears in the field declaration, comparison, and assignment.</done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| ViewModel internal state | ZoomFactor read from MainViewModel; no external input crosses here |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-uyf-01 | Denial of Service | GeometryLayerViewModel.Draw() | accept | combinedScale approaches 0 only if ZoomFactor=0 which is clamped in MainViewModel ZoomIn/ZoomOut; existing guard `_lastScale=0` forces first-run update harmlessly |
</threat_model>

<verification>
After both tasks:

1. `grep -n "_lastDpiScale" MathGaze/ViewModels/GeometryLayerViewModel.cs` returns no matches.
2. `grep -n "_dpiScale \* _mainVm.ZoomFactor" MathGaze/ViewModels/PdfCanvasViewModel.cs` returns exactly 3 lines.
3. Build passes: `C:\dotnet9\dotnet.exe build MathGaze/MathGaze.csproj --no-incremental -v minimal` → "Build succeeded" with 0 errors.
4. Manual smoke test: open a PDF, zoom in 2×, draw a line — stroke width should visibly increase proportionally.
</verification>

<success_criteria>
- Zero occurrences of `_lastDpiScale` in GeometryLayerViewModel.cs
- Exactly 3 occurrences of `_dpiScale * _mainVm.ZoomFactor` in PdfCanvasViewModel.cs
- `dotnet build` reports Build succeeded with 0 errors
- At zoom 1× and zoom 2×, geometry stroke widths are visually proportional to PDF content size
</success_criteria>

<output>
After completion, create `.planning/quick/260528-uyf-fix-geometry-scaling-with-zoom/260528-uyf-SUMMARY.md`
</output>
