# Project Research Summary

**Project:** MathGaze -- native Windows eye-gaze geometry assistant for GCSE maths exams
**Domain:** Assistive technology / geometry annotation tool / accessible Windows desktop app
**Researched:** 2026-04-29
**Confidence:** MEDIUM-HIGH overall

---

## Executive Summary

MathGaze is a native Windows desktop app that loads GCSE exam PDFs and overlays click-only geometry tools (line, circle, protractor, reflection) sized for eye-gaze users driven by Grid 3. No existing tool combines PDF exam loading, gaze-sized targets, Exam Mode measurement hiding for JCQ compliance, and zero-install deployment on locked school machines. The product fills a real gap and the build path is well understood -- the primary technical risks are deployment validation and the protractor placement algorithm, not fundamental unknowns.

**The critical stack decision is already made: WPF + SkiaSharp, not WinUI 3 + Win2D.** WinUI 3 self-contained publish does not bundle the Windows App Runtime Main and Singleton packages. On a managed school machine with Group Policy blocking MSIX sideloading, the app fails with 0x80070005. WPF with SkiaSharp publishes as a single EXE (~80-100 MB, no MSIX, no admin rights, runs from a USB stick). Phase 0 must still validate on the actual target machine before any feature work begins. If Phase 0 passes for WinUI 3 (school IT has pre-provisioned the runtime), the stack can be revisited -- but WPF is the safe default.

The core architectural risk is coordinate space drift. Three spaces -- PDF space (points), canvas space (DIPs), and screen space (physical pixels) -- must stay in sync across zoom, pan, DPI scaling, and page changes. All geometry must be stored in PDF space; a single CoordinateMapper class must own every transform. This must be established before any geometry objects are created. The second major risk is protractor placement and rotation math: accumulated floating-point error and atan2 discontinuities around 180 degrees can silently produce wrong angle readings in Exam Mode where the student cannot see the readout to catch the error. Both risks have clear mitigations and are not novel.

---

## Key Findings

### Recommended Stack

WPF (.NET 9, self-contained publish) is the deployment-safe choice. SkiaSharp 3.x (SKXamlCanvas from SkiaSharp.Views.WPF) provides GPU-accelerated 2D canvas -- the direct WPF equivalent of Win2D. PDFiumSharp or Docnet.Core wraps PDFium (Chrome PDF engine) with no OS registration. CommunityToolkit.Mvvm 8.x drives MVVM via source generators and is platform-agnostic, so a future WinUI 3 migration requires no ViewModel rewrites. Publish command: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` produces one EXE with zero installer requirements.

Note: .NET 9 is STS (supported to May 2026). Migrate to .NET 10 LTS (released November 2025) at first opportunity.

**Core technologies:**
- **.NET 9 + WPF**: UI shell and runtime -- zero external runtime dependencies, xcopy-deployable, excellent UIA accessibility
- **SkiaSharp 3.x**: GPU-accelerated 2D canvas -- SKXamlCanvas fires PaintSurface per frame; production-proven in JetBrains Rider
- **PDFiumSharp / Docnet.Core**: PDF rendering -- PDFium engine, no OS registration, handles all GCSE exam PDF edge cases
- **CommunityToolkit.Mvvm 8.x**: MVVM -- source-generator-based, platform-agnostic, WinUI 3 compatible for future migration
- **System.Text.Json** (inbox): JSON serialisation for session sidecar -- no extra dependency

**Do NOT use:** WinUI 3 + Win2D (MSIX deployment blocker), WinForms (no hardware-accelerated canvas), MAUI (cross-platform overhead), WPF + WriteableBitmap only (CPU rendering stutters at 60 FPS).

---

### Expected Features

MathGaze v1 covers approximately 70-80% of hands-on geometry marks on a Foundation GCSE paper and 50-60% on Higher tier. Uncovered Higher-tier tasks (rotation, enlargement) are explicitly deferred to v2.

**Must have (table stakes):**
- PDF load, page navigation, zoom
- Line tool (2-click), Point tool, Circle tool (2-click) with endpoint/intersection snap
- Select, nudge (1/5/20 px steps), delete, undo/redo
- Protractor: 2-click placement (line-to-line), rotate +/-1/5 degrees, flip inner/outer scale, lock, Exam Mode hide
- Reflection verb (contextual: select line + shape -- not a standalone tool)
- MCQ selection + lock; Text box (Grid 3 handles keyboard input)
- Exam/Practice mode toggle (always visible, high-contrast chip)
- Auto-save to JSON sidecar, session resume
- Self-contained single EXE, no install, no admin rights
- All interactive targets minimum 56x56 px logical pixels
- No drag gestures -- click-to-commit for every action

**Should have (differentiators):**
- Protractor placed as consequence of two line picks (core design insight -- no separate placement step)
- Exam Mode measurement hiding for JCQ compliance (no existing geometry AT tool does this)
- PDF exam paper as the working surface (student never leaves the paper)
- Split Rails layout: tools left, selection-aware controls right, full canvas centre
- Step-size row + pivot picker for post-placement correction without drag
- Snap orientation control (V/H/45/Free) to lock line direction before placing
- 3 protractor styles (180/360/minimal) -- 360 required for bearings
- Object locking for geometry objects and MCQ answers
- Dark/light theme + density settings

**Defer to v2+:**
- Rotation and enlargement transformations
- Explicit locus tool (circle approximation sufficient for v1)
- Angle bisector, perpendicular bisector as assisted constructions
- JCQ formal exam lockdown enforcement
- Export annotated PDF
- Freehand draw tool
- Highlight/mark tool

**Hard anti-features (never build):**
- Auto-solving or AI answer generation (exam integrity violation)
- Computed angle value visible in Exam Mode
- Floating toolbars, context menus, flyouts (breaks Grid 3 focus targeting)
- In-app on-screen keyboard (conflicts with Grid 3)
- Cloud save or account system (GDPR, school IT blockers)
- Dynamic geometry constraint-solving (objects must stay where placed)
- Hover-only states (gaze users cannot hover reliably)

---

### Architecture Approach

Retained-mode geometry store with immediate-mode draw loop: geometry objects persist in GeometryStore (stored in PDF space), and RenderService redraws everything each frame via SkiaSharp SKXamlCanvas. A single CoordinateMapper owns all space transforms. Tool interaction state lives in InputController and ToolStateMachine, never in ViewModels. All geometry mutations go through the UndoStack command pattern. This matches Paint.NET architecture and makes undo correct by construction.

**Critical build order (respect this dependency graph):**

1. CoordinateMapper (pure math, no dependencies -- build and unit-test first)
2. GeometryObject base + PointObject + LineObject
3. GeometryStore + UndoStack
4. PDFService + bitmap rendering
5. RenderService / Draw loop on SKXamlCanvas (first visible output)
6. InputController + ToolStateMachine + HitTester (Select + Line tool only)
7. MVVM ViewModels + WPF shell (MainWindow, LeftRail, RightRail)
8. SnapEngine integrated into InputController
9. CircleObject + Circle tool
10. ProtractorObject + 2-line pick flow
11. Right-rail NudgeBlock + RotateCommand + Orientation snap
12. Reflection verb (CompositeCommand)
13. TextBoxObject + MCQAnnotation
14. PersistenceService (auto-save + session resume)
15. Exam/Practice mode toggle + Settings + packaging

Items 1-8 are the critical path. Nothing works visually until step 5, nothing is interactive until step 6, and the protractor (step 10) depends on a working SnapEngine (step 8).

**Major components:**

1. CoordinateMapper -- single source of truth for all space transforms
2. GeometryStore -- observable collection of GeometryObject in PDF space; fires ObjectsChanged event
3. InputController + ToolStateMachine -- pointer events to IGeometryCommand objects; runs snap before every committed click
4. SnapEngine -- returns snapped point + type; endpoint snap O(n), intersection snap lazy O(n^2)
5. RenderService -- stateless draw helper; Layer 1 (PDF bitmap), Layer 2 (geometry), Layer 3 (snap feedback)
6. UndoStack -- command pattern, 200-step cap; commands own all geometry mutations
7. PDFService -- loads PDF, renders pages to SKBitmap; 3-page LRU cache
8. PersistenceService -- serialises GeometryStore to JSON sidecar; auto-saves on every mutation

**Key patterns:**
- Store all geometry in PDF space; convert to canvas space only at render time
- Commands own all geometry mutations; InputController creates commands, never calls GeometryStore directly
- Tool FSM state is private to InputController; ViewModels only reflect committed state
- Snap arithmetic in PDF space; feedback drawn in canvas space
- No XAML bindings on geometry objects; RenderService reads GeometryStore directly in the draw callback

---

### Critical Pitfalls

1. **WinUI 3 on managed school machines** -- App Runtime Main/Singleton/DDLM packages require MSIX registration; blocked by Group Policy; fails with 0x80070005. Use WPF + SkiaSharp. Phase 0 must test the actual EXE on a real school machine with a non-admin account before any feature code is written.

2. **Coordinate system drift** -- Three spaces (PDF, canvas, screen/DPI) must stay in sync. Hit-testing and pointer events in different spaces causes selections to miss by a scale factor. Build CoordinateMapper first with unit tests at zoom 0.5x/1x/1.5x/2x and DPI 100/125/150/200%.

3. **Protractor rotation math** -- atan2 has a discontinuity at +/-180 degrees; incremental angle accumulation adds float error. In Exam Mode the student cannot catch a wrong angle. Store baseAngle + adjustmentDelta separately; always normalise to [0, 360); test crossing 0/180/360 degree boundaries explicitly.

4. **Grid 3 focus stealing from flyouts/popups** -- Any secondary HWND (ContentDialog, MenuFlyout) breaks Grid 3 window targeting; next click fires at the wrong target. All UI must be in-window panels. No flyouts, no context menus, no ContentDialog -- enforce this rule before any UI is built.

5. **Gaze click double-fire on dwell release** -- Grid 3 dwell mode can generate two pointer events per intended click, completing a 2-click tool in one dwell. Handle all tool actions on PointerPressed (not PointerReleased); add 150ms per-click debounce.

**Additional pitfalls:**
- Snap radius wrong at non-1x zoom: define snap radii in PDF-space, not screen pixels
- Near-parallel line intersection NaN: epsilon guard (determinant < 1e-6 = no intersection)
- DPI scaling breaking hit zones: all tolerances in logical pixels (DIPs), never physical pixels
- Protractor flip transform order: canonical order is translate, scale, flip, rotate; never reorder

---

## Implications for Roadmap

### Phase 0: Deployment Validation
**Rationale:** The entire project depends on deploying to a school machine. One afternoon of testing prevents a potential full rewrite.
**Delivers:** Confirmed deployment story -- WPF EXE launches on clean Windows 10/11 VM, non-admin account, from USB stick with no install.
**Avoids:** Pitfall 1 (WinUI 3 deployment failure discovered late)
**Research flag:** No research needed -- this is a hardware test.

### Phase 1: Project Shell + Input Foundation
**Rationale:** Grid 3 compatibility (no popups, dwell debounce, DPI scale factor) must be baked into the shell before any tools are built.
**Delivers:** WPF shell (TopBar, LeftToolRail, RightRailPanel, empty canvas); Grid 3 pointer event handling with 150ms debounce; DPI scale factor read at startup; no-flyout architectural rule enforced.
**Avoids:** Pitfalls 4 (Grid 3 focus) and 5 (dwell double-fire)
**Research flag:** Standard WPF patterns -- no additional research needed.

### Phase 2: Rendering Engine + PDF Loading
**Rationale:** The coordinate transform pipeline must exist and be tested before any geometry objects are created.
**Delivers:** CoordinateMapper with unit tests at all zoom/DPI combinations; PDFService (load, render to SKBitmap, 3-page cache); RenderService drawing PDF layer on SKXamlCanvas; page navigation; zoom.
**Avoids:** Pitfall 3 (coordinate drift) -- establish the single-transform-class rule here permanently.
**Research flag:** Check PDFiumSharp vs Docnet.Core maintenance status at Phase 2 start.

### Phase 3: Geometry Core (Line, Point, Select, Undo)
**Rationale:** Line and Point are the foundation for all other tools. Select + Nudge + Undo are required before any placed object is usable.
**Delivers:** GeometryObject base; LineObject, PointObject; GeometryStore; UndoStack (command pattern, 200-step cap); HitTester with gaze tolerances (32px select, 12pt PDF-space line hit minimum); Select tool; NudgeBlock (1/5/20px); Delete; Undo/Redo buttons.
**Avoids:** Pitfall 8 (undo memory pressure -- command pattern, not state snapshots)
**Research flag:** Standard command pattern -- no additional research needed.

### Phase 4: Snap System + Circle Tool
**Rationale:** Snap is what makes 2-click placement viable for gaze users. Must be working before the protractor.
**Delivers:** SnapEngine (endpoint O(n), intersection lazy O(n^2), orientation V/H/45); snap radii in PDF-space (24pt default); snap ring visual feedback; CircleObject + Circle tool; epsilon guard for near-parallel intersections.
**Avoids:** Pitfall 6 (snap radius wrong at non-1x zoom), Pitfall 12 (intersection NaN)
**Research flag:** Snap threshold tuning is empirical -- plan calibration session with actual student in Phase 4.

### Phase 5: Protractor Tool
**Rationale:** Hardest feature and highest-value differentiator. Depends on snap (Phase 4) and line objects (Phase 3).
**Delivers:** ProtractorObject; 2-line pick flow; RotateProtractorCommand (+/-1/5 degrees); FlipProtractorCommand; NormalizeAngle utility; 3 styles (180/360/minimal); lock; Exam Mode measurement hiding.
**Avoids:** Pitfall 4 (rotation math discontinuities); Pitfall 15 (flip transforms in wrong order)
**Research flag:** Spike on rendering accurate graduated scale marks in SkiaSharp before full implementation.

### Phase 6: Reflection Verb + MCQ + Text Box
**Rationale:** Completes v1 answer-paper capability. Reflection is second-highest geometry value. MCQ and Text are simpler additions in the same phase.
**Delivers:** Reflection verb (select Line + shape, CompositeCommand producing mirrored polygon); MCQAnnotation (click-to-select, lock); TextBoxObject + Text tool with Grid 3 focus handoff back to canvas.
**Avoids:** Pitfall 11 (text box Grid 3 keyboard focus handoff)
**Research flag:** Scope how shape is defined for reflection verb from HANDOFF.md before Phase 6 planning.

### Phase 7: Persistence + Session + Exam/Practice Mode
**Rationale:** Auto-save and session resume are accessibility requirements. Exam/Practice mode is the JCQ compliance mechanism.
**Delivers:** PersistenceService (JSON sidecar, auto-save on every mutation, resume prompt if sidecar exists); SessionModel; Exam/Practice mode toggle (always-visible high-contrast chip); live angle readout in Practice Mode.
**Avoids:** Pitfall 14 (sidecar overwrite -- always prompt to resume or start fresh)
**Research flag:** Verify current JCQ AARA document at jcq.org.uk before this phase ships to any exam context.

### Phase 8: Settings, Polish, and Packaging
**Rationale:** Settings and accessibility polish complete the product. Final single-EXE build is the Phase 0 promise with all features.
**Delivers:** Dark/light theme; spacious/XL density; protractor style picker; memory validation (<200MB after 300 actions); final single-EXE publish; test on actual school hardware.
**Research flag:** No additional research needed -- implementation and validation only.

---

### Phase Ordering Rationale

- Phase 0 before everything: deployment failure found after Phase 5 requires partial rewrite; one afternoon test eliminates the risk.
- CoordinateMapper before geometry objects: coordinate drift bugs contaminate stored data and are impossible to fix retroactively.
- Snap before protractor: the 2-line pick flow depends on reliable endpoint detection.
- Select/Nudge/Undo before any additional tools: no placed object is usable without correction capability.
- Reflection before MCQ/Text: higher geometric complexity and higher exam value.
- Persistence last among features: auto-save needs all object types defined; one serialisation pass covers all types.

---

### Research Flags

**Phases needing deeper research or spikes:**
- **Phase 2**: Verify PDFiumSharp vs Docnet.Core maintenance status at implementation time.
- **Phase 5**: Spike on rendering accurate graduated scale marks in SkiaSharp before full protractor implementation.
- **Phase 6**: Scope shape definition for reflection verb from HANDOFF.md before planning.

**Phases with standard patterns (skip additional research):**
- **Phase 1**: Standard WPF MVVM with CommunityToolkit.
- **Phase 3**: Command pattern undo, observable collection.
- **Phase 7**: System.Text.Json with plain object graph.
- **Phase 8**: Standard WPF settings and dotnet publish.

---

## Open Questions (Must Answer Before or During Build)

1. Does the target school machine have Windows App SDK components pre-installed? (Phase 0 test answers this.)
2. What Grid 3 dwell configuration does the specific student use? (Affects debounce tuning -- test with actual hardware in Phase 1.)
3. Are the exam PDFs standard A4 portrait with no page rotation? (Validate on actual paper PDFs before Phase 2 ships.)
4. How is shape defined for the reflection verb -- pre-drawn polygon on PDF, or student-constructed lines? (Resolve from HANDOFF.md before Phase 6.)
5. What is the student's snap sensitivity? (24pt PDF-space is the default starting point -- calibrate with actual student in Phase 4.)
6. Current JCQ AARA document: visit jcq.org.uk for the current year before Phase 7 ships to any exam context.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack -- WPF deployment | HIGH | Official Microsoft docs confirm MSIX blocker; WPF self-contained is first-class .NET CLI since .NET Core 3.0 |
| Stack -- SkiaSharp for WPF | HIGH | SKXamlCanvas documented and production-proven (JetBrains Rider, VS Code extension host) |
| Stack -- PDF rendering library | MEDIUM | PDFium engine is solid; PDFiumSharp/Docnet.Core maintenance needs spot-check at implementation time |
| Features -- table stakes | HIGH | Derived from HANDOFF.md, SPEC.md plus GCSE curriculum analysis |
| Features -- JCQ compliance | MEDIUM | Training-data knowledge as of August 2025; verify current year at jcq.org.uk |
| Architecture -- coordinate system | HIGH | Fundamental pattern well-documented across Inkscape, PDF.js, PDFium |
| Architecture -- WPF/SkiaSharp specifics | MEDIUM-HIGH | Pattern standard; exact SKXamlCanvas API surface needs Phase 1 validation |
| Pitfalls -- deployment | HIGH | MSIX blocker documented by Microsoft; confirmed in research |
| Pitfalls -- Grid 3 focus | MEDIUM | Based on general overlay-app focus model; requires hardware validation |
| Pitfalls -- coordinate/math | HIGH | Floating-point angle issues and coordinate space drift are deterministic and well-documented |
| Pitfalls -- snap tuning | MEDIUM | Threshold values are empirical starting points; calibrate with actual student |

**Overall confidence: MEDIUM-HIGH**

The core architecture and stack decisions are grounded in first-class documentation and production precedent. The two MEDIUM areas (JCQ policy and Grid 3 hardware behaviour) require real-world validation but do not change the build order or architecture.

### Gaps to Address

- PDFiumSharp vs Docnet.Core maintenance: check NuGet at Phase 2 start. Fallback is Windows.Data.Pdf (WinRT API, usable from unpackaged WPF with minor boilerplate).
- SkiaSharp 3.x SKXamlCanvas API surface: confirm PaintSurface event signature and DPI handling in Phase 1 spike.
- Reflection verb shape definition: read HANDOFF.md reflection section before Phase 6 planning.
- JCQ current year publication: visit jcq.org.uk before Phase 7 ships to any exam context.

---

## Sources

### Primary (HIGH confidence)
- docs/HANDOFF.md -- Core design decisions (56px targets, click-to-commit, protractor as 2-line consequence, Split Rails layout, reflection as contextual verb)
- docs/uploads/SPEC.md, .planning/PROJECT.md -- Feature scope and deployment requirements
- Microsoft Docs: Windows App SDK deployment architecture -- MSIX package breakdown confirming self-contained does not bundle Main/Singleton packages
- Microsoft Docs: Windows App SDK self-contained deployment -- 0x80070005 error on managed machines documented

### Secondary (MEDIUM confidence)
- JCQ Access Arrangements and Reasonable Adjustments -- training-data knowledge as of August 2025; verify at jcq.org.uk
- SkiaSharp production usage -- training-data knowledge (JetBrains Rider, VS Code extension host)
- Grid 3 / Smartbox event injection model -- synthesised from general overlay-app focus model; requires hardware validation
- GCSE AQA/Edexcel/OCR curriculum task types -- training-data knowledge; curriculum changes slowly

### Tertiary (LOW confidence / needs validation)
- PDFiumSharp / Docnet.Core maintenance status -- check NuGet at implementation time; original PdfiumViewer by pvginkel is archived
- Snap radius tuning values (24pt endpoint, 28px snap search) -- empirical starting points; calibrate with actual student

---

*Research completed: 2026-04-29*
*Ready for roadmap: yes*
