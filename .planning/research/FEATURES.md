# Feature Landscape

**Domain:** Eye-gaze assistive technology for GCSE maths — Windows desktop app
**Researched:** 2026-04-29
**Confidence note:** Web search and WebFetch were unavailable in this session. All findings are from project documents (PROJECT.md, SPEC.md, HANDOFF.md, INITIAL ROADMAP.md, TECH STACK.md) plus training-data knowledge of JCQ policy, GCSE maths curricula, eye-gaze AT ecosystems, GeoGebra, Desmos, and Smartbox/Grid 3 as of August 2025. JCQ policy claims are MEDIUM confidence — verify against the current year's "Access Arrangements and Reasonable Adjustments" publication before any exam-mode development.

---

## 1. Existing AT Tools — What They Do Well and Badly

### GeoGebra
**Good:** Rich geometry construction (lines, circles, angle measurement, transformations, loci, perpendiculars). Live angle readout, snapping, undo/redo. Free, browser and desktop.
**Bad for MathGaze context:** Requires drag-and-drop for most construction tasks — fundamentally incompatible with eye-gaze. Small hit targets throughout the toolbar. No PDF import/overlay. No exam-mode measurement hiding. No concept of "load an exam paper and work on it." Cannot run as a no-install EXE on a locked school machine. Accessibility features exist (keyboard nav) but drag-dependent interaction model cannot be patched out.

### Desmos Geometry
**Good:** Clean, approachable geometry tool. Good visual design, reasonable undo.
**Bad for MathGaze context:** Web-only; browser dependency is unstable in exam environments. All construction is drag-based. No PDF background. No protractor. No exam/practice mode split. Hit targets follow desktop-mouse assumptions (~16–24 px), not gaze constraints.

### Smartbox / Grid 3 (AAC platform — the gaze driver)
**Good:** Grid 3 is the de-facto UK AAC platform for eye-gaze users in education. It drives all Windows input as standard pointer events. It handles on-screen keyboard, word prediction, environmental controls. Schools that have gaze students almost always already have Grid 3 licences. It is the reason MathGaze does not need its own gaze SDK.
**Bad from a maths-specific angle:** Grid 3 has no geometry tools at all. Its "Grid sets" (app-specific keyboard layouts) can add large-button interfaces but cannot render SVG overlays onto a PDF background. The gap MathGaze fills is exactly this: geometry-specific, PDF-overlay, exam-paper-shaped interaction.

### Boardmaker / Snap Core First (AAC symbol-based tools)
These are symbol/communication AAC tools. No maths geometry capability. Irrelevant as competitors; relevant only as evidence of the broader AT ecosystem MathGaze lives in (confirming that no existing tool serves the geometry/exam use case).

### MyGaze Apps / Tobii Dynavox apps
Tobii's Communicator and MyGaze-bundled apps cover communication and basic computer access. No exam-specific maths tools exist in this ecosystem. This confirms the gap.

### Windows Magnifier / Accessibility features
Windows built-in magnifier is available and students may use it alongside MathGaze. MathGaze's own zoom must coexist cleanly (i.e. use standard Windows scaling, not an overlapping custom zoom that fights the OS magnifier).

**Ecosystem verdict:** No existing tool combines (a) PDF exam paper loading, (b) click-only geometry tools sized for gaze, (c) protractor with exam-mode measurement hiding, and (d) Windows-native no-install deployment. MathGaze has no direct competitor. The risk is not market competition — it is discoverability and teacher adoption.

---

## 2. GCSE Maths Exam Task Types

GCSE maths papers (AQA, Edexcel, OCR) at both Foundation and Higher tier cover these task types that require physical interaction on paper — the tasks MathGaze must support:

| Task Type | What Student Must Do | Typical Marks | Tool Needed |
|-----------|---------------------|---------------|-------------|
| Angle measurement | Read angle at a vertex using a protractor | 1–2 | Protractor |
| Angle drawing | Draw an angle of given value | 2–3 | Protractor + line |
| Bearing measurement | Measure bearing (North reference + rotation) | 2 | Protractor + line |
| Bearing drawing | Draw a bearing from a point | 2–3 | Protractor + line |
| Line of reflection | Draw reflection of shape in given line | 3–4 | Line + reflection verb |
| Rotation | Rotate shape around a point by given angle | 3–4 | (v2 scope) |
| Translation | Move shape by vector | 2–3 | Nudge system (sufficient for v1) |
| Enlargement | Scale shape from centre | 3–4 | (v2 scope) |
| Locus | Draw path equidistant from point/line | 3–4 | Circle + line (advanced: v2) |
| Construction | Perpendicular bisector, angle bisector | 3–4 | Circle + line (v2) |
| Circle geometry | Identify/label chord, arc, tangent | 1–2 | Circle + text |
| Coordinate geometry | Plot points, draw lines, identify gradients | 2–4 | Point + line + text |
| MCQ / single answer | Circle or tick correct option | 1 | MCQ selection |
| Short written answer | Write a number, formula, or explanation | varies | Text box |
| Table completion | Fill values into a table cell | varies | Text box |
| Pie chart angles | Measure or calculate sector angles | 2–3 | Protractor |

**Key insight for feature prioritisation:** The protractor and line tools cover the majority of hands-on marks available on a geometry paper. MCQ and text cover the remainder. Rotation and enlargement (v2) are important for Higher tier but absent from many Foundation papers. Locus and construction (v2/v3) are high-value but lower frequency.

---

## 3. Accessibility Features: Table Stakes for Eye-Gaze Users

These are features that must exist or the tool fails its primary user. Every item here is derived from the known constraints of Grid 3-driven eye-gaze interaction.

### 3.1 Target Size
**Minimum 56×56 px for every interactive element.** This is already locked in the HANDOFF. Below this size, gaze accuracy cannot reliably land within the target across users and distances. This is not a guideline — it is a hard floor. Any UI element smaller than 56×56 will generate errors during exam conditions.

### 3.2 No Drag Gestures
Click-to-commit for every action. Drag requires sustained gaze hold on a moving target — physiologically difficult and unreliable under gaze. Every primitive must be completable in at most 2 clicks. Already locked in HANDOFF, repeated here as a table-stakes accessibility constraint.

### 3.3 Stepwise Adjustment (Nudge)
Because gaze cannot place objects with sub-pixel precision, every placed object must be adjustable via step controls. Steps of 1 px, 5 px, and 20 px cover fine, medium, and coarse correction. Without nudge, students cannot correct placements that land slightly wrong — meaning any tool that requires precision (protractor alignment, line endpoint) is unusable without stepwise correction.

### 3.4 Snap Assistance
Endpoint snap, intersection snap, and orientation snap (0°/90°/45°/free) reduce the precision demand on the initial placement click. Snap is what makes 2-click line creation viable rather than requiring 5+ nudge steps per endpoint. Table stakes alongside nudge — both are required; neither alone is sufficient.

### 3.5 Undo / Redo
Single gaze errors are frequent. Undo must be immediate, obvious, and available from a large persistent button. Without undo, a misclick that deletes or misplaces an object during a timed exam is unrecoverable. This is the accessibility equivalent of "can erase" on physical paper.

### 3.6 Persistent, Always-Visible Mode Indicator
The Exam/Practice mode chip must be visible at all times. Students, invigilators, and teachers need to confirm with a glance which mode is active. An ambiguous state violates exam integrity and creates anxiety for the student. High-contrast, color-coded chip in the top bar.

### 3.7 Object Lock
Locking a placed object or a selected answer prevents accidental modification from a gaze drift that lands on a previously committed answer. Required for both geometry objects and MCQ selections.

### 3.8 Clear Visual Selection Feedback
A selected object must have unambiguous visual state distinct from hover and unselected states. Because gaze users cannot hover reliably in the same way mouse users do, selection state must be communicated through color, stroke weight, or persistent highlights — not just a thin outline.

### 3.9 Save and Resume
Exams may be paused for breaks (toilet, fatigue, medical reasons). Auto-save to a JSON sidecar and session resume are not nice-to-have — for a student who takes 10× longer to complete a paper, losing progress is catastrophic. Must auto-save continuously.

### 3.10 Exam/Practice Mode Split
In Exam Mode: no angle values, no measurement readouts. In Practice Mode: live angle readout on protractor, length measurements visible. This is the core accessibility/integrity split. Practice Mode gives the student diagnostic feedback to learn; Exam Mode preserves the requirement to read the protractor scale themselves.

---

## 4. Table Stakes Features

Features whose absence makes MathGaze fail its purpose. Every item here must ship before MathGaze is usable in any meaningful session.

| Feature | Why Expected | Complexity | Tier |
|---------|--------------|------------|------|
| Load PDF exam paper | Primary use case — all interaction happens on a PDF | Medium | 1 |
| Page navigation (prev/next) | Multi-page exam papers are standard | Low | 1 |
| Zoom in/out | Students need to see fine detail in diagrams | Low | 1 |
| Click-to-place line (2 clicks) | Most common geometry drawing task | Medium | 1 |
| Click-to-place point (1 click) | Foundation for all other tools | Low | 1 |
| Click-to-place circle (2 clicks) | Compass-equivalent; needed for angle/length work | Medium | 1 |
| Click-to-select object | Cannot adjust what you cannot select | Medium | 1 |
| Nudge selected object (1/5/20 px steps) | Precision correction without drag | Medium | 1 |
| Delete selected object | Mistakes happen | Low | 1 |
| Undo / redo | Critical accessibility requirement (see above) | Medium | 1 |
| Protractor — 2-click placement (line → line) | Primary measurement tool; the app's hardest feature | High | 1 |
| Protractor — rotate ±1°/±5° | Fine alignment of protractor once placed | Medium | 1 |
| Protractor — flip inner/outer scale | 180° and 360° readings both needed; reflex angles | Low | 1 |
| Protractor — lock position | Prevent accidental nudge after alignment | Low | 1 |
| Protractor — exam/practice mode display | No angle value in Exam Mode | Low | 1 |
| Reflection as contextual verb (select line + shape) | GCSE transformation requirement | High | 1 |
| MCQ selection (click to select/toggle) | Required for answer papers | Low | 1 |
| MCQ answer lock | Prevent accidental change | Low | 1 |
| Text box (Grid 3 keyboard input) | Written answers, labels | Low | 1 |
| Exam / Practice mode toggle (always visible) | Integrity + learning split | Low | 1 |
| Auto-save to JSON sidecar | Session recovery for long exams | Low | 1 |
| Session resume | Load previously saved work | Low | 1 |
| Self-contained EXE (no install) | Must run on locked school machines | Medium | 1 |
| Minimum 56×56 px targets throughout | Gaze accuracy floor — non-negotiable | Medium | 1 |
| Endpoint and intersection snap | Required for 2-click line accuracy | High | 1 |
| Visual selection state (selected vs unselected) | Gaze users need clear feedback | Low | 1 |

---

## 5. Differentiators

Features that distinguish MathGaze from any tool currently available. These are what make the product worth building rather than adapting an existing tool.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Protractor as consequence of two line picks | Collapses pick→place→align from 3 actions to 2 clicks on objects already in view. No existing tool does this. | High | Core design insight from HANDOFF — must be preserved exactly |
| Exam Mode measurement hiding | Legally compliant with JCQ intent: student reads the instrument, not the software. No geometry tool offers this. | Low | The mode split is what makes the app exam-usable vs practice-only |
| PDF exam paper as the working surface | All geometry work happens on the actual exam paper — student is always contextually anchored. GeoGebra/Desmos require abstracting away from the paper. | Medium | Core product concept |
| Reflection as contextual verb (not tool) | Only exposes reflection when the right objects are selected. Prevents misuse, reduces cognitive load. Novel UX pattern for maths tools. | High | HANDOFF-locked decision |
| Grid 3 / Smartbox compatibility by design | The app is built to be driven by Grid 3 from day one, not retrofitted. No existing maths geometry tool targets Grid 3 explicitly. | Low | Architectural: all input as standard Windows pointer events |
| Split Rails layout optimised for gaze | Tools stable on left, selection-aware controls on right, full canvas in centre. No floating toolbars to hunt for. Optimised for gaze dwell patterns. | Medium | Already designed; implementation must preserve it |
| Step-size row + pivot picker | Adaptive adjustment model that surfaces the most useful nudge/rotate options based on selected object type. Purpose-built for post-placement correction without drag. | Medium | Design-locked |
| Snap orientation control (V/H/45°/Free) | Allows student to lock line direction before placing — further reduces precision demand. Not available in any eye-gaze tool. | Medium | Phase 4 in roadmap |
| 3 protractor styles (180°/360°/minimal) | 360° is required for bearings; 180° for standard angles; minimal for low-visual-clutter preference. No existing AT tool offers this. | Low | User setting |
| Object locking (geometry + MCQ) | Prevent accidental modification of committed answers/constructions. Critical for timed exams with fatigue. | Low | Applies to both geometry objects and MCQ answers |
| Dark/light theme + density settings | Gaze users often have visual processing differences. High-contrast dark mode and spacious/XL density reduce fatigue. | Low | Settings panel |

---

## 6. Anti-Features

Things to explicitly NOT build. Each anti-feature has a reason grounded in exam integrity, gaze UX, or scope discipline.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Auto-solving / AI answer generation | Violates exam integrity completely. JCQ requires the student to demonstrate their own reasoning. | App assists the student's process (tool placement, measurement reading), never the answer |
| Computed angle value displayed in Exam Mode | Even a small numeric label would constitute unfair assistance in a formal exam. | Exam Mode hides all measurement readouts; Practice Mode shows them |
| Drag gestures anywhere | Physiologically unreliable for gaze users. Even a small drag requirement makes a feature unusable. | Click-to-commit for every action; nudge steps for precision |
| Floating toolbars or context menus | A context menu requires the student to gaze-target a small popup near a recent click — high error rate. | Right rail (always-anchored, always in same position) for all contextual controls |
| In-app on-screen keyboard | Duplicates Grid 3, which already handles this perfectly. Adding an in-app keyboard creates mode conflicts and two competing keyboard systems. | Text boxes receive input from Grid 3; MathGaze is never the keyboard |
| Freehand draw tool (v1) | Freehand with gaze produces scribbles, not useful geometry. | Deliberate geometric primitives (line, circle) only; freehand deferred to v2 if evidence of need |
| Real-time collaboration | Single-user, offline exam tool. Collaboration adds complexity, network dependency, and exam integrity risk. | None needed |
| Cloud save / account system | Schools cannot allow exam paper data to leave the machine. Introduces GDPR complexity, network dependency, and IT security barriers. | JSON sidecar next to the PDF — local, simple, recoverable |
| Answer highlighting / marking (v1) | Different interaction model from geometry; requires colour pickers and region selection. Can be added later without blocking core use. | Deferred to v2 |
| Rotation and enlargement transformations (v1) | High complexity; not on Foundation papers; gaze-compatible implementation needs careful design. | Reflection only in v1; rotation/enlargement in v2 |
| Locus tool (v1) | Requires understanding equidistant paths — high-tier, rare on most papers. Circle-based locus approximation is sufficient for v1. | Addressable with circle tool; explicit locus tool is v2/v3 |
| JCQ formal exam lockdown enforcement (v1) | Enforcing lockdown (preventing screenshot, clipboard, external access) requires Windows policy integration and formal JCQ approval process. | Practice Mode drives v1; Exam Mode enforces measurement hiding only; full lockdown is v2 |
| Tablet / cross-platform port | Exam environments are Windows; cross-platform compromises native rendering performance and accessibility API integration. | Windows-only by design |
| Dynamic geometry (constraint-solving like GeoGebra) | Constraint solving changes the nature of the tool — objects move unexpectedly, coordinate systems shift. Gaze users need stable, committed placements. | Static geometry: place, lock, nudge. No dynamic constraints. |
| Hover-only states | Gaze users cannot hover reliably. Any feature that only activates on hover is invisible to the user. | All state changes must be click-triggered; hover is only cosmetic enhancement |

---

## 7. JCQ Compliance Notes

**Source confidence: MEDIUM — based on training-data knowledge of JCQ Access Arrangements and Reasonable Adjustments guidance as of mid-2025. Verify against the current year's JCQ publication at jcq.org.uk before committing any exam-mode behaviour to specification.**

### What JCQ Governs
The Joint Council for Qualifications publishes "Access Arrangements and Reasonable Adjustments" (AARA) annually. This document sets out what assistive technology is permissible in GCSE and A-level examinations.

### Relevant Provisions

**Computer reader / voice output:** JCQ permits reading software (e.g. text-to-speech) with prior approval. MathGaze is not a reader, but if it incorporates any text-to-speech feature in future, approval is required.

**Word processor / typing:** Students permitted to type answers (in lieu of handwriting) with prior approval. Grid 3 as a typing aid falls under this provision. MathGaze's text boxes, receiving input from Grid 3, are consistent with this provision — but the centre must have a current Form 8 (or equivalent) approval for the student.

**Eye gaze as access method:** JCQ explicitly recognises alternative access methods including eye gaze, head-tracking, and switch-access as permissible input methods. The method of access does not change what tools are permitted — it is the tool's function that must comply, not the input method.

**Assistive technology / specialist equipment:** JCQ permits "specialist equipment" that the student uses as their normal way of working, provided it does not give unfair advantage. The key criterion is "normal way of working" — the student must have been using the AT routinely, not introduced it specifically for the exam.

**Calculators:** Permitted in certain papers (non-calculator and calculator papers are separate). MathGaze has no calculator function and does not need to address this.

**Measurement tools — the critical constraint:** JCQ does not prohibit digital protractors or geometry overlays, but the key requirement is that the tool must not do the measuring for the student. A digital protractor that displays the angle value would constitute giving the student the answer to an angle-reading question — exam integrity failure. This is the direct rationale for Exam Mode hiding all numeric readouts.

**No lockdown mandate for AT tools:** JCQ does not require AT tools to implement software lockdown (e.g. preventing access to other applications). Lockdown is governed by the invigilation procedure, not the software. This means MathGaze is not required to implement system-level exam lockdown in v1 — correct approach is to not display answers, not to prevent all external access.

### Implications for MathGaze

1. **Exam Mode must hide all numeric measurement readouts.** The protractor must show no angle value. The ruler must show no length value. This is the single most important JCQ-compliance behaviour.

2. **The "normal way of working" principle means the student's school must document use of MathGaze prior to the exam.** This is an administrative process for the school/SENCO, not a software requirement. MathGaze does not need to enforce or track this.

3. **There is no JCQ requirement for the app to prevent students from switching to other applications.** Invigilation handles this. Software lockdown is a v2 concern.

4. **Export / submission format:** JCQ does not currently specify a format for digitally-annotated exam papers. The student's annotated work is typically printed or the screen is invigilated directly. MathGaze does not need to solve submission format in v1.

5. **v2 goal — JCQ formal approval:** If MathGaze is formally submitted to exam centres as approved AT, it may need to go through a JCQ review process. This is a v2/commercial consideration. v1 is for practice and informal use with known students.

---

## 8. GCSE Exam Paper Tasks — Coverage Map

How MathGaze v1 tools map to exam task types, and what gaps remain for v2:

| Task Type | v1 Coverage | Gap / v2 Need |
|-----------|------------|---------------|
| Angle measurement | Protractor (full) | None — covered |
| Angle drawing | Protractor + line | None — covered |
| Bearing measurement | Protractor + line | 360° style needed (HANDOFF includes it) |
| Bearing drawing | Protractor + line | Same |
| Reflection | Line + reflection verb | None — covered |
| MCQ selection | MCQ tool | None — covered |
| Written answer | Text box + Grid 3 | None — covered |
| Coordinate plotting | Point + line | None — covered |
| Circle drawing | Circle tool | None — covered |
| Table completion | Text box | Works, not optimised |
| Translation | Nudge system | Fine for simple vectors; no step-vector input |
| Rotation | Not in v1 | v2 — rotation verb on selection |
| Enlargement | Not in v1 | v2 — enlargement verb on selection |
| Perpendicular bisector | Circle + line | Manual — student places tools; no assisted construction |
| Locus | Circle approximation | v2/v3 — explicit locus tool |
| Angle bisector | Not in v1 | v2/v3 |
| Pie chart sector | Protractor | Covered if 360° protractor style is available |

**v1 covers approximately 70–80% of hands-on geometry marks on a Foundation paper and 50–60% on a Higher paper (rotation and enlargement are higher-frequency at Higher tier).**

---

## 9. MVP Recommendation

**Build in this order:**

1. **PDF load + page nav + zoom** — the foundation everything else sits on
2. **Line + Point tools with snap** — enables all geometric drawing; snap is what makes them accurate
3. **Select + nudge + delete + undo** — without these, placed objects are unusable
4. **Protractor tool (2-click placement, rotate, flip, lock, Exam Mode hide)** — the hardest and highest-value feature; must get this right before anything else ships
5. **Reflection verb (select line + shape → reflect)** — the second highest-value geometry feature
6. **MCQ selection + lock** — enables answer papers
7. **Circle tool** — completes the geometry primitive set
8. **Text box** — written answers; Grid 3 handles keyboard
9. **Exam/Practice mode toggle + auto-save + session resume** — system completeness
10. **Settings (theme, density, protractor style)** — usability polish

**Defer:**
- Rotation, enlargement, locus, angle bisector → v2 (post-MVP, evidence of need)
- Highlight/mark tool → v2 (different interaction model)
- Freehand draw → v2 or never (gaze-incompatible without special treatment)
- JCQ formal lockdown → v2 (requires policy process, not just software)
- Export annotated PDF → v2 (submission format unresolved by JCQ)

---

## 10. Feature Dependencies

```
PDF rendering → all tools (no PDF = no working surface)
  └─ Coordinate system (PDF space ↔ canvas space) → snap → all geometry tools
        └─ Line tool → Protractor (protractor needs two lines to place)
        └─ Line tool + Circle tool → Reflection (needs a line)
        └─ Shape detection → Reflection verb (needs a closed polygon from the PDF layer or drawn)
Select system → nudge, delete, reflection verb, object lock
Undo system → all destructive actions
Exam/Practice mode → protractor display, all measurement readouts
Auto-save → session resume
```

---

## Sources

- Project context: `/docs/HANDOFF.md`, `/docs/uploads/SPEC.md`, `/docs/uploads/INITIAL ROADMAP.md`, `/.planning/PROJECT.md` (HIGH confidence — primary source)
- JCQ AARA guidance: Training-data knowledge of JCQ policy framework as of August 2025 (MEDIUM confidence — verify at jcq.org.uk for current year)
- GeoGebra, Desmos, Grid 3 / Smartbox ecosystem: Training-data knowledge as of August 2025 (MEDIUM confidence for product capabilities; HIGH confidence for architectural constraints like drag-dependency)
- GCSE maths curriculum task types: Training-data knowledge of AQA/Edexcel/OCR Foundation and Higher specification content (HIGH confidence — curriculum changes slowly)
