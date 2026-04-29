# Domain Pitfalls

**Domain:** Eye-gaze assistive geometry tool — native Windows exam app (WinUI 3 / Win2D)
**Researched:** 2026-04-29
**Confidence:** MEDIUM-HIGH (knowledge cutoff Aug 2025; no live web search available; grounded in WinUI 3, Win2D, WPF, assistive tech, and geometry app domain knowledge)

---

## Critical Pitfalls

Mistakes that cause rewrites or make the app unusable for the student.

---

### Pitfall 1: WinUI 3 Self-Contained EXE on a Managed School Machine

**What goes wrong:**
WinUI 3 (Windows App SDK) requires the Windows App Runtime to be installed on the target machine. Publishing as "self-contained" packages the .NET runtime and app binaries, but it does NOT bundle the Windows App Runtime. On a fresh or locked-down school machine that has never had a Windows App SDK app installed, the EXE silently fails, crashes on launch, or shows an unhelpful error dialog.

Additionally, even with bootstrapper mode (WindowsPackageType=None), early versions of Windows App SDK had bugs where the self-contained bootstrapper tried to write to HKLM or program files, requiring elevation — which students do not have.

**Why it happens:**
The "self-contained EXE" story in WinUI 3 is newer and less battle-tested than WPF's xcopy-deploy model. Documentation conflates .NET self-contained with Windows App Runtime self-contained; they are separate concerns.

**Consequences:**
The entire Phase 0 validation fails. If discovered late, forces rewrite to WPF + SkiaSharp fallback under time pressure.

**Prevention:**
- Phase 0 MUST validate on an actual managed Windows 10/11 machine with a non-admin account before writing any app code. Bring a USB stick with the EXE; do not assume a VM is representative.
- Test the exact Windows App Runtime version required vs. what is already on school machines (Windows 11 22H2+ has some App Runtime components in-box; Windows 10 often does not).
- If bootstrapper fails without elevation, switch to the WPF + SkiaSharp fallback immediately — do not try to work around it. The fallback path is explicitly planned.
- Keep Phase 0 scope minimal: empty window that launches. That is the entire acceptance test.

**Detection:**
Launch on a clean Windows 10 VM with no developer tools installed. If it does not launch without admin, the plan fails.

**Phase:** Phase 0 — validation milestone, before any feature work.

---

### Pitfall 2: Grid 3 Focus Stealing the Canvas

**What goes wrong:**
Grid 3 (and other AAC / switch-access overlays) is a persistent top-level window. It sends synthesised pointer events to the frontmost non-Grid-3 window. The problem arises when:
- A UI action in MathGaze (e.g., opening a dialog, showing a tooltip, a WinUI 3 flyout) momentarily shifts focus away from the main window.
- Grid 3 then targets the wrong window for subsequent clicks.
- The student's next gaze click fires at nothing, or worse, at the wrong element.

WinUI 3 flyouts, ContentDialog, and MenuFlyout all create transient child windows or popups that can break Grid 3's window targeting model. WPF has the same issue with popups.

**Why it happens:**
Gaze software finds the target window by z-order or foreground window handle. Any popup or secondary HWND breaks the assumption that "frontmost non-overlay window = MathGaze canvas."

**Consequences:**
Clicks misfire. In an exam context this is catastrophic — a student could accidentally trigger something irreversible or lose their place.

**Prevention:**
- Design all UI as in-window panels, not flyouts or popups. The Split Rails layout already enforces this — the right rail adapts in-place rather than popping out. Do not add context menus or dropdown flyouts.
- If a modal confirmation is needed (e.g., "Clear all annotations?"), render it as an in-canvas overlay, not a ContentDialog (which creates a separate HWND in WinUI 3).
- Never allow the canvas to lose pointer capture while a tool is mid-action.
- Test every UI transition with Grid 3 open and driving clicks. A developer mouse is not the same test.

**Detection:**
After building any new UI component, test: activate Grid 3, position gaze on a canvas element, trigger the new UI element via gaze, then attempt another canvas click. If the canvas click misfires, the element creates a focus break.

**Phase:** Phase 1 (shell) — establish the no-flyout rule before any UI is built. Phase 5+ (right rail) — enforce when adding selection-aware controls.

---

### Pitfall 3: Coordinate System Drift Between PDF Space, Canvas Space, and Screen Space

**What goes wrong:**
Three distinct coordinate spaces must stay in sync at all times:
1. **PDF space** — origin at top-left of the PDF page, units are PDF user units (approximately 1/72 inch).
2. **Canvas space** — the Win2D / SkiaSharp drawing surface, in device-independent pixels, with the current zoom and pan offset applied.
3. **Screen/pointer space** — where Grid 3 delivers click events, in physical pixels, subject to DPI scaling.

Common failures:
- A geometry object is placed correctly at zoom 1x but drifts visibly when zoomed in (off-by-one-pixel in the inverse transform).
- Hit-testing uses canvas-space but click events arrive in screen-space, so selections miss by a scale factor.
- After a page change, the PDF layer resets but the geometry layer does not, causing objects to render on the wrong page.
- The protractor's visual centre diverges from its stored mathematical position after several zoom/pan cycles (floating point accumulation).

**Why it happens:**
Each layer has its own local transform. When a click arrives from Grid 3, it is in screen pixels. The developer applies one transform to go to canvas space but forgets the DPI factor, or applies the zoom but not the pan offset. One missed transform silently shifts everything.

**Consequences:**
The student places a line at a precise angle, but the protractor measures a different angle because the two objects live in subtly different coordinate spaces. This is exam-critical.

**Prevention:**
- Define one canonical coordinate space early (recommend: PDF-space as ground truth, measured in PDF user units). All geometry objects store positions in this space only.
- Build explicit, tested conversion functions: `ScreenToCanvas(Point, DpiScale)`, `CanvasToPdf(Point, ZoomLevel, PanOffset)`, and their inverses. These must be the only place coordinate transforms are applied.
- Write unit tests for these transforms at zoom 0.5x, 1x, 1.5x, 2x, and for non-integer DPI (150%, 175%, 200%).
- When rendering, always go from PDF-space → canvas-space via the same pipeline, never hardcode pixel offsets in drawing code.
- On page navigation, clear all canvas-space cached positions and recompute from PDF-space.

**Detection:**
Place a point at a known PDF location. Zoom to 200%. Click the point to select it. The selection highlight must still surround the point exactly. If it is off by more than 1px, the transform pipeline has a bug.

**Phase:** Phase 2 (rendering engine) — establish the transform pipeline before any geometry objects are created.

---

### Pitfall 4: Protractor Rotation Math Accumulated Error and Angle Wrapping

**What goes wrong:**
The protractor is placed at a computed angle derived from the direction vector of the first selected line. Subsequent ±1°/±5° button presses accumulate onto a stored float. Over many adjustments, floating point representation causes the displayed angle to drift from the true angle. Additionally:
- Angle wrapping around 360°/0° is not handled, causing the protractor to jump or report −0.3° instead of 359.7°.
- The baseline angle is computed as `atan2(dy, dx)`, which returns values in (−π, π]. When the line is nearly vertical, small perturbations flip the sign, causing the protractor to snap 180° without the student understanding why.
- Gimbal lock is not a problem for 2D rotation, but the equivalent issue — an angle that passes through a representation discontinuity — causes visible jumps.

**Why it happens:**
Incremental angle accumulation in floating point (`angle += delta`) is fine for small totals but can drift. The real hazard is the initial placement angle derived from `atan2`, which has a discontinuity at ±180° and is sensitive to line direction conventions.

**Consequences:**
The protractor shows 91° when the correct angle is 89°. In Exam Mode the student cannot see the readout to detect the error, making the visual position the only reference — and if the visual position has drifted, the student draws a wrong line of reflection and fails the question.

**Prevention:**
- Store the protractor's absolute rotation as a `double` in degrees, normalised to [0, 360) after every update. Use a `NormalizeAngle` function everywhere.
- For the initial placement angle from a line: use the direction of the line consistently (always Start→End, never reversed). Document this convention in code. If the student's first click is near the end point rather than the start, snap the direction convention, not the angle.
- Do not accumulate rotations by repeatedly adding `delta`. Instead, store `baseAngle` (set at placement) and `adjustmentDelta` (incremented by button presses), and compute `displayAngle = NormalizeAngle(baseAngle + adjustmentDelta)`. This keeps drift bounded to the last adjustment only.
- The live angle readout in Practice Mode provides continuous feedback — implement this early to catch placement bugs before exam use.
- Test: place protractor on a horizontal line (0°), a vertical line (90°), a line near −180°/180°. Verify no jumps.

**Detection:**
Place a protractor on a line at 179° to horizontal. Press ±1° through the 180°/0° boundary. The protractor should smoothly cross 180° and display 181° then 182°, not jump to −178°.

**Phase:** Phase 5 (protractor tool) — get the math right before any UI for it is built.

---

### Pitfall 5: DPI Scaling on School Monitors Breaking Touch Targets

**What goes wrong:**
Windows DPI scaling means a "56px" button specified at 96 DPI becomes a different physical size at 125% or 150% scaling. The problem has two directions:
- On a high-DPI monitor at 150% scaling, a Win2D canvas element drawn at 56 logical pixels is 84 physical pixels — fine.
- But if the developer hardcodes canvas hit-testing tolerances in physical pixels (e.g., `hitRadius = 56`), at 150% DPI the effective hit zone shrinks to 37 logical pixels, which is too small for eye gaze.

WinUI 3 automatically scales UI elements (buttons, sliders) for DPI. Win2D's `CanvasControl` draws in device-independent pixels by default, but the `PointerPoint` coordinates from click events may arrive in physical pixels depending on how the pointer event is handled, particularly with Grid 3 injecting events.

**Why it happens:**
WinUI 3 and Win2D use different units internally. The `PointerRoutedEventArgs` position is in logical pixels (DIPs) within WinUI 3, but when Grid 3 injects a `SendInput`-style event, Windows reports it in screen physical pixels before the DPI scaling transform is applied. The developer must confirm which unit they are receiving.

**Consequences:**
Hits miss. The student gazes at a line and clicks, but the hit-test radius is wrong so nothing selects. This is invisible to a developer testing on a 100% DPI laptop.

**Prevention:**
- At startup, read `DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel` (WinUI 3) and store as a DPI scale factor.
- Perform all hit-testing in logical pixels (DIPs). Convert incoming pointer positions from physical to logical before any hit test: `logicalPoint = physicalPoint / dpiScale`.
- Define all minimum target sizes as logical pixels (56 logical px minimum). Let the framework and DPI scaling handle the physical rendering.
- Test on a machine set to 125% and 150% DPI. The gaze target acceptance test ("every target ≥56px") should be measured in logical pixels at the target DPI, not by inspecting physical pixel dimensions.
- Do not use `CanvasControl.Dpi` for pointer math — it is a render DPI, not the input DPI.

**Detection:**
On a 150% DPI machine, click on the outer edge of a rail button. It must register. If it misses consistently on the outer 25% of the button, hit-test coordinate conversion is wrong.

**Phase:** Phase 1 (shell and DPI constants), Phase 3 (hit-testing implementation).

---

### Pitfall 6: Snap Threshold That Is "Grippy" or "Dead Zone" Instead of "Magnetic"

**What goes wrong:**
Snap systems fail in two opposite ways:
1. **Too grippy (sticky):** The snap radius is too large. The student's click is pulled to a snap target they did not intend. Lines snap to nearby intersections when the student was aiming for open canvas. For eye-gaze users who have natural gaze jitter of 20–50px, a snap radius above ~30px at 100% zoom causes constant misfires.
2. **Too small (dead zone):** The snap radius is so small that the student's gaze, which has jitter, never consistently lands in the snap zone. Endpoint snapping becomes unreliable. The student tries to close a shape and the line refuses to snap to the starting point.

The additional failure mode: snap behaviour is not consistent across zoom levels. At 100% zoom a snap radius of 20px may feel right. At 200% zoom the same 20px radius now covers half a cm on the rendered geometry — far too small in screen terms but the objects are physically further apart on screen.

**Why it happens:**
Snap radii are defined in one coordinate space and tested at one zoom level. No one tests at multiple zoom levels with a gaze input device.

**Prevention:**
- Define snap radii in PDF-space (content coordinates), not screen pixels. At 100% zoom 20pt in PDF-space maps to roughly 20px on screen; at 200% zoom it maps to 40px, which scales correctly.
- Provide a tunable snap sensitivity setting (low/medium/high) that scales the base snap radius. Do not hardcode one value — the right value depends on the student's gaze accuracy, which varies.
- Distinguish snap types: endpoint snap (very high priority, larger radius), intersection snap (medium priority, medium radius), orientation snap (angle quantisation, always on when toggled). Prioritise by type when multiple candidates are within range.
- Show a snap indicator (a highlighted snap point with a distinct colour) while the user is hovering before committing a click. This gives the student visual confirmation of what will happen before they commit.
- Default snap radius: 24pt in PDF-space for endpoint/intersection. Test this with the actual student before shipping.

**Detection:**
Test with gaze input only (or simulate with a touchpad and deliberate imprecision). Try to close a triangle. If you cannot reliably close it in 3 attempts at 100% zoom, the snap radius is too small.

**Phase:** Phase 4 (precision / snap system).

---

## Moderate Pitfalls

---

### Pitfall 7: Win2D CanvasControl Render Loop Causing Missed Inputs

**What goes wrong:**
Win2D's `CanvasControl` renders on demand via the `Draw` event, which is triggered by `Invalidate()`. If the developer calls `Invalidate()` too aggressively (e.g., on every pointer move event), the render loop becomes the bottleneck and pointer events queue up behind renders. The effective input latency increases from the expected <16ms to 50–100ms. For a gaze user, this lag makes it impossible to judge whether a click has registered.

The opposite problem: calling `Invalidate()` too conservatively means the canvas does not redraw after a state change, and the student sees stale geometry until the next redraw.

**Prevention:**
- Use `CanvasAnimatedControl` only if continuous animation is genuinely needed (it runs a continuous loop at 60fps regardless of input, wasting GPU). For a geometry tool, `CanvasControl` with selective `Invalidate()` calls is correct.
- Call `Invalidate()` exactly once per logical state change (object added, object moved, selection changed), not on pointer move unless a tool has "preview while hovering" behaviour.
- For hover/snap preview, use a throttled invalidation: track the last invalidation timestamp, suppress invalidations that arrive within 16ms of each other.
- Provide immediate visual click feedback (a brief highlight or ripple) using a separate lightweight animation pass, not a full canvas redraw.

**Detection:**
Add a frame counter overlay. At idle, frame count should not increment. On a single click, it should increment 1–2 times. If it increments continuously while the cursor is stationary, renders are being triggered unnecessarily.

**Phase:** Phase 2 (rendering engine), Phase 4 (snap hover preview).

---

### Pitfall 8: Undo History Size Causing Memory Pressure in Long Sessions

**What goes wrong:**
A full GCSE exam paper can have 20+ questions. A student working through the paper accumulates hundreds of geometry objects and potentially thousands of undo steps (each nudge press is an action). If each undo step stores a deep clone of all geometry objects, memory usage grows unbounded.

At 500 undo steps with 50 objects per step, even at 500 bytes per object serialised, that is 12.5 MB just for undo history — manageable. But if objects store rendered bitmaps or image references, this explodes.

**Why it happens:**
Developers reach for "clone entire state per undo step" as the simplest implementation. It is safe but wasteful.

**Prevention:**
- Implement Command pattern undo: each action records what changed and how to reverse it, not a full state snapshot. An `AddObjectCommand` stores only the added object; a `MoveObjectCommand` stores only the delta. Undo = replay inverse command.
- Cap undo history at a configurable maximum (default: 200 steps). Drop the oldest steps when the cap is exceeded. The student is unlikely to need to undo more than 200 steps in a single session.
- Never store rendered bitmaps in undo steps. Geometry objects store vector coordinates only. Re-render on demand.
- JSON sidecar auto-save is separate from undo — save to disk frequently (every 10 actions or every 2 minutes), so a crash does not lose a whole session even if undo is limited.

**Detection:**
Load a PDF. Perform 300 click actions. Check memory usage in Task Manager. It should not exceed 200MB total for the process. If it does, undo history is storing too much.

**Phase:** Phase 8 (undo/redo system).

---

### Pitfall 9: PDF Coordinate Mapping Breaking on Non-A4 or Rotated PDF Pages

**What goes wrong:**
GCSE exam papers are almost always A4 portrait. However:
- Some PDFs have rotated pages (the PDF `Rotate` attribute specifies page orientation in 90° increments separately from the content bounding box).
- Some scanned papers are saved as landscape A4 or non-standard crop sizes.
- The Windows `Windows.Data.Pdf` API reports `PdfPage.Size` in DIP (device-independent pixels at 96 DPI), not PDF user units. The `Rotate` attribute of the PDF page is NOT automatically applied to the reported size — the developer must account for it.

A rotated PDF page rendered without respecting the `Rotate` attribute will display correctly (Windows rotates the bitmap), but the coordinate system is 90° wrong, so geometry objects placed by clicking will appear at the wrong position.

**Prevention:**
- When loading a page via `Windows.Data.Pdf`, read the page's rotation attribute if accessible (the managed wrapper may not expose it directly; use PDFium instead for full access to PDF metadata including rotation).
- Define test cases: load a landscape A4 PDF, load a 90°-rotated PDF page, load a non-standard crop-box PDF. Verify geometry placement is correct on each.
- If using the Windows PDF API for rendering but PDFium for metadata, be aware that the two libraries may report coordinate origins differently (Windows PDF API origin is top-left; PDFium's default is bottom-left in PDF standard coordinates).
- For MVP, document the assumption that exam papers are A4 portrait with no page rotation. Validate this assumption on the actual paper PDFs the student uses.

**Detection:**
Open a test PDF with a known 90° rotation flag. Place a point in the top-right corner of the rendered page. The stored coordinate should match the top-right corner of the PDF page, not the physical top-right of the rotated bitmap.

**Phase:** Phase 2 (PDF rendering setup) — establish the assumption; Phase 6+ if non-standard PDFs appear.

---

### Pitfall 10: Gaze Click Accidental Double-Fire on Dwell Release

**What goes wrong:**
Grid 3 can be configured to trigger click events in multiple ways: dwell (gaze held for N milliseconds), dwell-and-release, or direct click via switch. Depending on the student's configuration, a single intended dwell click may generate two pointer events: one `PointerPressed` at dwell start and one at dwell end, or two `PointerReleased` events. If MathGaze handles both, a two-click tool (like Line) will complete immediately without the student placing the second point.

Additionally, dwell click release is not instant — the Grid 3 pointer moves slightly during the dwell, so the `PointerReleased` position may be 10–30px from the `PointerPressed` position. If MathGaze uses `PointerPressed` for the action, this is fine. If it uses `PointerReleased` (common in mouse-oriented code), the placement position is slightly wrong.

**Prevention:**
- Handle tool clicks on `PointerPressed`, not `PointerReleased`. This is the most consistent event for gaze clicks.
- Add a per-click debounce: after processing a click at position P, ignore any further click events within 150ms unless they are clearly at a different target (>80px away). This absorbs double-fire from dwell systems.
- Document the expected Grid 3 event model and test with the student's actual Grid 3 configuration. Do not assume any particular dwell time or event pair.
- The 56px minimum target size helps here — even with 20–30px of dwell drift, the click is still within the target.

**Detection:**
With Grid 3 in dwell mode, gaze at the canvas and hold for a dwell. Watch how many `PointerPressed` events are logged. If more than one fires per intended click, debounce is needed.

**Phase:** Phase 1 (input abstraction layer) — establish the debounce before any tool logic.

---

### Pitfall 11: Text Box Focus Handoff to Grid 3 Keyboard

**What goes wrong:**
When the student activates the Text tool and clicks to place a text box, the text box must receive keyboard focus so that Grid 3's virtual keyboard can type into it. The sequence requires:
1. MathGaze creates a text box and calls `Focus(FocusState.Programmatic)` on it.
2. Grid 3 detects that a text input field is focused and switches its layout to the AAC keyboard.
3. The student types.
4. The student dismisses the keyboard (a Grid 3 action), and focus must return to the MathGaze canvas without creating a focus break that interrupts the next gaze click.

Step 4 is the common failure: when the Grid 3 keyboard is dismissed, focus can return to an unexpected element, and the next gaze click hits that element instead of the canvas.

**Prevention:**
- After text input is complete, explicitly call `Focus(FocusState.Programmatic)` on the canvas control, not on any button or rail element.
- Do not use a WinUI 3 `TextBox` for text input if it causes a focus transition that Grid 3 misinterprets. Consider using Grid 3 as the keyboard only (which it already is per requirements) and having a custom non-HWND text entry surface that never creates a window-level focus change.
- Test the full text-entry cycle with Grid 3 running before committing to any specific focus management implementation.

**Detection:**
Place a text box. Type via Grid 3 keyboard. Dismiss keyboard. Immediately attempt a click elsewhere on the canvas. If the click misfires or the wrong element receives it, the focus handoff is broken.

**Phase:** Phase 7 (text tool).

---

## Minor Pitfalls

---

### Pitfall 12: Line Intersection Algorithm Numerical Instability

**What goes wrong:**
The standard line-line intersection formula breaks down for near-parallel lines (denominator approaches zero). Near-parallel lines are common on exam papers (two sides of a rectangle that are "almost vertical"). If the snap system tries to snap to the intersection of near-parallel lines, it either divides by near-zero and produces a point far off screen, or the intersection point flickers between NaN and a valid position.

**Prevention:**
- Add an epsilon check in the intersection function: if the determinant is below a threshold (e.g., 1e-6 in PDF-space), report no intersection rather than computing one.
- Visualise intersection snap candidates during development — log when an intersection is detected and what its coordinates are. This makes NaN cases immediately visible.

**Phase:** Phase 4 (snap system).

---

### Pitfall 13: Selection Hit Zone Asymmetry Between Objects

**What goes wrong:**
A line segment has an obvious geometric hit test (distance from point to line, capped to the segment). A circle arc does not — the most natural implementation tests distance to the circumference, but this means the interior of a circle is not selectable. A student looking at a circle assumes clicking inside it selects it. If only the circumference is the hit target (typically a 4px stroke at zoom 1x), they can never reliably select the circle with gaze.

**Prevention:**
- For circles, use a ring-shaped hit zone: distance from click to circumference is within ±16px. This gives a solid hit band around the circle perimeter, which is both geometrically accurate and reliably selectable.
- For lines, use perpendicular distance to the infinite line, clamped to the segment extent, with a minimum hit tolerance of 12pt in PDF-space.
- For the protractor, the entire bounding box is the hit zone (it is a placed overlay, not a geometric object).
- Visualise hit zones during development — draw the hit zone boundary when an object is selected. Remove from release builds.

**Phase:** Phase 3 (geometry core / hit testing).

---

### Pitfall 14: Auto-Save Sidecar Collision When Two Sessions Share a PDF

**What goes wrong:**
The auto-save format is a JSON sidecar named after the PDF (e.g., `exam_paper.json` next to `exam_paper.pdf`). If two students or two school computers share the same PDF file on a network drive, or a student copies the PDF without the sidecar, sessions overwrite each other or load stale data.

**Prevention:**
- Sidecar filename should include a unique session identifier, or prompt if an existing sidecar is found and offer to resume or start fresh.
- For MVP, document the assumption: one student, one PDF, one machine, offline. The single sidecar model is correct under these constraints.
- Never silently overwrite a sidecar. If a sidecar exists when the student loads a PDF, prompt to resume (recommended) or start fresh.

**Phase:** Phase 8 (persistence).

---

### Pitfall 15: Protractor "Flip" Rendering the Wrong Scale After Rotation

**What goes wrong:**
Flipping between the inner and outer protractor scale is typically implemented as a vertical mirror of the protractor image around the baseline. If the protractor has already been rotated, the flip must be applied after the rotation in the transform matrix, or the mirrored image will be mirrored around the wrong axis. A common implementation error is:
- Apply rotation to a pre-flipped bitmap, rather than applying the flip as a final transform.
- This causes the scale markings to read in reverse even when the student expects the outer scale.

**Prevention:**
- Define the canonical transform order: translate to centre, scale, flip (if active), rotate. Never reorder these. Apply as a single composed matrix, not a sequence of independent draw calls.
- Unit test: at 45° rotation, flipped state, the 90° mark on the active scale must point at a specific direction. Assert this in a test.

**Phase:** Phase 5 (protractor tool).

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|---|---|---|
| Phase 0: WinUI 3 validation | App Runtime not pre-installed on school machine | Test on clean Windows 10 VM, non-admin account, before any coding |
| Phase 1: Shell + layout | Grid 3 focus breaks from flyouts | No flyouts, no popups, no ContentDialog — in-window panels only |
| Phase 1: Input layer | Dwell double-fire clicks | Handle on PointerPressed, add 150ms debounce |
| Phase 2: Rendering + PDF | Coordinate space confusion | Define transform pipeline with unit tests before geometry objects |
| Phase 2: PDF loading | Page rotation attribute ignored | Validate on rotated test PDF; document A4-portrait assumption |
| Phase 3: Geometry core | Hit zone too small for gaze on lines | 12pt minimum tolerance in PDF-space for line hit testing |
| Phase 3: Geometry core | Circle interior not selectable | Ring-shaped hit zone around circumference |
| Phase 4: Snap system | Snap radius feels wrong at non-1x zoom | Define snap radii in PDF-space, not screen pixels |
| Phase 4: Snap system | Near-parallel line intersection NaN | Epsilon guard in intersection algorithm |
| Phase 5: Protractor | Angle representation discontinuity at 180° | NormalizeAngle always; test crossing 0°/360° boundary |
| Phase 5: Protractor | Flip transforms in wrong order | Canonical transform order: translate, scale, flip, rotate |
| Phase 6/7: Right rail | Selection-aware UI triggering focus break | Verify each new UI element does not break Grid 3 targeting |
| Phase 7: Text tool | Grid 3 keyboard focus handoff | Programmatic focus return to canvas after text dismissal |
| Phase 8: Undo/redo | Deep-clone undo consuming memory | Command pattern; 200-step cap; never store bitmaps in history |
| Phase 8: Auto-save | Sidecar overwrite on resume | Detect existing sidecar, prompt to resume or start fresh |
| All phases: DPI | Hit zones wrong on 125%/150% DPI | All hit-test tolerances in logical pixels (DIPs), not physical pixels |
| All phases: Performance | Win2D render over-invalidation | Invalidate on state change only; no per-frame loop unless animating |

---

## Sources

- Domain knowledge: WinUI 3 / Windows App SDK deployment model (knowledge cutoff Aug 2025)
- Win2D CanvasControl rendering model and coordinate system behaviour
- Grid 3 / Smartbox event injection model (synthesised pointer events via Windows SendInput)
- WPF and WinUI 3 focus management with overlay applications
- Eye-gaze assistive technology interaction design principles (ISAAC / AAC community practice)
- Geometry app coordinate system pitfalls (GeoGebra architecture discussions, dynamic geometry tool literature)
- Floating point angle representation: standard trigonometry library gotchas (atan2 discontinuity)
- GCSE exam PDF format: A4 portrait assumption grounded in AQA/Edexcel/OCR paper formats

**Confidence by area:**
| Area | Confidence | Notes |
|---|---|---|
| WinUI 3 deployment | MEDIUM | Known issue class; specific App Runtime version behaviour may have changed post-Aug 2025 — Phase 0 test is essential |
| Grid 3 focus management | MEDIUM | Based on general overlay-app focus model; exact Grid 3 event behaviour must be validated with hardware |
| Coordinate system pitfalls | HIGH | Fundamental, well-documented in all 2D graphics tool work |
| Protractor math | HIGH | Standard floating point angle arithmetic — deterministic |
| Snap system tuning | MEDIUM | Threshold values are empirical; numbers given are starting points not guarantees |
| DPI scaling | HIGH | Windows DPI model is stable and well-documented |
| Win2D render loop | HIGH | Documented Win2D CanvasControl vs CanvasAnimatedControl behaviour |
