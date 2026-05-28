# Roadmap: MathGaze

## Overview

Four phases deliver a GCSE geometry tool a student can drive entirely with their eyes. Phase 1 validates deployment before writing a single feature line — one afternoon of testing that eliminates the largest project risk. Phase 2 builds every geometry tool (Point, Line, Circle, Snap, Select, Undo) on top of a tested coordinate pipeline. Phase 3 adds the protractor — the highest-value differentiator — with full Practice/Exam mode switching baked in as infrastructure. Phase 4 completes the answer layer: text boxes, MCQ selection, auto-save, and session resume.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Foundation** - Deployment validation spike + WPF shell + PDF rendering + CoordinateMapper
- [x] **Phase 2: Geometry Core** - All geometry tools (Point, Line, Circle, Snap, Select, Nudge, Delete, Undo) — gap closure in progress (completed 2026-05-03)
- [x] **Phase 3: Protractor** - 2-click protractor placement, controls, and Practice/Exam mode infrastructure (completed 2026-05-25)
- [x] **Phase 4: Answer Layer** - Text boxes, MCQ selection, auto-save, and session resume (completed 2026-05-27)
- [ ] **Phase 5: Angle Measurement** - Two-point protractor placement so students can measure pre-drawn angles on exam papers without drawing lines first

## Phase Details

### Phase 1: Foundation
**Goal**: The app loads a GCSE exam PDF and runs from a USB stick on a school machine with no install
**Depends on**: Nothing (first phase)
**Requirements**: CORE-01, CORE-02, CORE-03, CORE-04
**Success Criteria** (what must be TRUE):
  1. The single EXE launches on a clean Windows 10/11 machine from a USB stick without any install step or admin rights
  2. User can open a PDF from local disk and see it rendered on the canvas
  3. User can navigate to any page in a multi-page PDF
  4. User can zoom in and out and the PDF view updates correctly
  5. CoordinateMapper unit tests pass at zoom 0.5×/1×/1.5×/2× and DPI 100/125/150/200%
**Plans**: 5 plans
Plans:
- [x] 01-01-PLAN.md — Project scaffold + CoordinateMapper + 32 unit tests (CORE-04 foundation)
- [x] 01-02-PLAN.md — 3-column WPF shell: TopBar, ToolRail stubs, PdfCanvas, ScrollRail, RightRail (visual)
- [x] 01-03-PLAN.md — PDF rendering pipeline: IPdfService, DocnetPdfService, PdfCanvasViewModel, PaintSurface
- [x] 01-04-PLAN.md — Interactive wiring: all TopBar + ScrollRail commands in MainViewModel (CORE-01, CORE-02, CORE-03)
- [x] 01-05-PLAN.md — Self-contained publish + deployment verification (CORE-04)
**UI hint**: yes

### Phase 2: Geometry Core
**Goal**: Users can place, select, adjust, and delete geometry objects on top of the PDF using only clicks
**Depends on**: Phase 1
**Requirements**: GEOM-01, GEOM-02, GEOM-03, GEOM-04, GEOM-05, GEOM-06, GEOM-07, SYS-01
**Success Criteria** (what must be TRUE):
  1. User can place a Point with one click, a Line with two clicks, and a Circle with two clicks
  2. User can click any placed object to select it; the right rail shows controls for that object
  3. User can nudge a selected object in 1 px / 5 px / 20 px steps from the right rail
  4. User can delete a selected object via a right-rail action
  5. Snap feedback appears when a new point is near an endpoint, line-line intersection, or orientation guide; committing the click snaps to the detected position
  6. User can undo any action and redo previously undone actions
**Plans**: 13 plans
Plans:
- [x] 02-01-PLAN.md — Geometry object model (PointObject, LineObject, CircleObject) + hit-test math + unit tests
- [x] 02-02-PLAN.md — Command pattern (IGeometryCommand, 4 commands) + IGeometryService + UndoService + DI registration
- [x] 02-03-PLAN.md — Tool interaction layer: ToolViewModel state machine, SnapEngine, canvas mouse wiring, DPI fix, ghost preview, ToolRail bindings
- [x] 02-04-PLAN.md — GeometryLayerViewModel: renders all committed objects (Point/Line/Circle) with selection and sub-point indicators
- [x] 02-05-PLAN.md — Right rail: RightRailViewModel, RightRail.xaml (nudge block, delete, undo/redo), replaces RightRailPlaceholder
- [x] 02-06-PLAN.md — GAP CLOSURE: coordinate offset fix (bitmap scale + DPI) and nudge Y-axis inversion fix (GAP-1, GAP-2, GAP-3)
- [x] 02-07-PLAN.md — GAP CLOSURE: right rail design language and step selector active highlight (GAP-4, GAP-5)
- [x] 02-08-PLAN.md — GAP CLOSURE: Delete button hover readable + step button active-hover cobalt fix (GAP-8, GAP-9)
- [x] 02-09-PLAN.md — GAP CLOSURE: geometry objects cleared on new PDF open (GAP-10)
- [x] 02-10-PLAN.md — GAP CLOSURE: DPI call-order race fix + continuous snap ring during mid-draw (GAP-6, GAP-7)
- [x] 02-11-PLAN.md — GAP CLOSURE: CoordinateMapper sync race fix + SnapEngine horizontal alignment unit tests (GAP-11)
- [x] 02-12-PLAN.md — GAP CLOSURE: horizontal orientation snap suppression fix (GAP-12)
- [x] 02-13-PLAN.md — GAP CLOSURE: geometry cleared on page navigation (GAP-13)
**UI hint**: yes

### Phase 3: Protractor
**Goal**: Users can measure angles on the PDF using a protractor driven entirely by two clicks on existing lines, with measurement visibility controlled by Practice/Exam mode
**Depends on**: Phase 2
**Requirements**: PROT-01, PROT-02, PROT-03, PROT-04, PROT-05, PROT-06, SYS-04, SYS-05
**Success Criteria** (what must be TRUE):
  1. User activates Protractor mode, clicks two lines, and the protractor appears at their intersection with its baseline aligned to the first line — no separate placement step
  2. User can rotate the placed protractor ±1° and ±5° using right-rail buttons
  3. User can flip the protractor between inner scale (0°→180°) and outer scale (180°→0°)
  4. User can lock the protractor to prevent accidental movement
  5. User can switch between 180° classic style and 360° full-circle style (required for bearings)
  6. In Practice Mode the protractor shows a live angle readout; toggling to Exam Mode hides the numeric value immediately; the mode chip is always visible in the top bar
**Plans**: 4 plans
Plans:
- [x] 03-01-PLAN.md — ProtractorObject model (D-06 fields, DefaultRadiusPt=108) + 3 commands (Rotate/Flip/Style) + GeometryMath.TryLineIntersectPt
- [x] 03-02-PLAN.md — ToolViewModel Protractor state machine (D-01/D-02/D-03) + GeometryService NudgeObject extension + ghost protractor preview
- [x] 03-03-PLAN.md — RightRail ProtractorPanel (rotate ±1°/±5°, flip scale, 180°/360° style toggle) + RightRailViewModel commands
- [x] 03-04-PLAN.md — GeometryLayerViewModel SkiaSharp renderer (arc body, 180 tick marks, labels, readout) + IsPracticeMode wiring
**UI hint**: yes

### Phase 4: Answer Layer
**Goal**: Users can place clipboard-pasted text labels on the PDF and their work persists automatically across sessions via JSON sidecar
**Depends on**: Phase 3
**Requirements**: TEXT-01, TEXT-02, ANS-01, ANS-02, ANS-03, SYS-02, SYS-03
**Success Criteria** (what must be TRUE):
  1. User can activate Text tool, copy text in Grid 3, click canvas to place a text label at that PDF-space position
  2. Empty-clipboard click shows toast "Copy text first, then click to place" and places nothing
  3. A selected text label can be repositioned using nudge controls in the right rail (standard nudge block)
  4. Every geometry change is auto-saved to {pdfPath}.mathgaze.json with no manual save required
  5. Opening the same PDF again restores all geometry objects and text labels silently (no prompt)
  6. Note: ANS-01/02/03 (MCQ answer selection) are deferred to v2 per user decision D-08
**Plans**: 3 plans
Plans:
- [x] 04-01-PLAN.md — TextObject model + GeometryObject.Id init-only + JsonDerivedType annotations + GeometryService NudgeObject TextObject case
- [x] 04-02-PLAN.md — Text tool UX: ToolMode.Text, clipboard placement, SkiaSharp Consolas rendering, RightRail TextObject support
- [x] 04-03-PLAN.md — Session persistence: ISessionService, SessionService (auto-save on ObjectsChanged), restore on PDF open, page-nav save (D-14)
**UI hint**: yes

### Phase 5: Angle Measurement
**Goal**: Students can measure pre-drawn angles on exam papers by placing the protractor via two clicks (vertex + arm direction) — no drawn lines required
**Depends on**: Phase 4
**Requirements**: PROT-07
**Success Criteria** (what must be TRUE):
  1. User activates Protractor mode, clicks a point on the canvas (protractor centres at that point), clicks a second point (baseline aligns toward it) — protractor placed with no drawn lines involved
  2. The two-point path coexists with the existing two-line path — clicking two existing lines still auto-places at their intersection as before
  3. All existing right-rail protractor controls (rotate ±1°/±5°, flip, style, readout) work identically on a two-point-placed protractor
**Plans**: 1 plan
Plans:
- [ ] 05-01-PLAN.md — Two-point protractor placement: ToolViewModel state machine extension, ghost preview vertex anchor, readout suppression for Guid.Empty protractors
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 2/5 | In Progress|  |
| 2. Geometry Core | 13/13 | Complete | 2026-05-25 |
| 3. Protractor | 4/4 | Complete   | 2026-05-25 |
| 4. Answer Layer | 3/3 | Complete   | 2026-05-27 |
| 5. Angle Measurement | 0/1 | Planned | |
