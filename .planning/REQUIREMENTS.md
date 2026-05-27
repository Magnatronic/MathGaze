# Requirements: MathGaze

**Defined:** 2026-04-29
**Core Value:** A student can complete a GCSE geometry question — measuring angles, drawing lines, selecting answers — using only their eyes, without the app reducing the cognitive challenge of the maths itself.

## v1 Requirements

### Core

- [ ] **CORE-01**: User can open a PDF file from local disk
- [ ] **CORE-02**: User can navigate to any page in the loaded PDF
- [x] **CORE-03**: User can zoom in and out of the PDF view
- [x] **CORE-04**: App runs as a self-contained EXE with no admin install and no pre-installed runtime required

### Geometry Tools

- [x] **GEOM-01**: User can place a Point with one click
- [x] **GEOM-02**: User can draw a Line (segment) with two clicks (click start → click end)
- [x] **GEOM-03**: User can draw a Circle with two clicks (click centre → click radius point)
- [x] **GEOM-04**: User can select any geometry object with one click
- [x] **GEOM-05**: User can nudge a selected object using step controls in the right rail (step sizes: 1 / 5 / 20 px)
- [x] **GEOM-06**: User can delete a selected object via a right-rail action
- [x] **GEOM-07**: User can snap new points to existing object endpoints, line-line intersections, and orientation guides (vertical / horizontal / 45°)

### Protractor

- [x] **PROT-01**: User can activate Protractor mode, click two lines, and have the protractor auto-placed at their intersection with baseline aligned to the first line
- [x] **PROT-02**: User can rotate the placed protractor ±1° and ±5° via right-rail buttons
- [x] **PROT-03**: User can flip the protractor between inner scale (0°→180° left-to-right) and outer scale (180°→0°)
- [ ] **PROT-04**: User can lock the protractor position to prevent accidental nudge
- [x] **PROT-05**: User can choose between 180° classic style and 360° full-circle style (required for bearings questions)
- [x] **PROT-06**: In Practice Mode, protractor shows a live angle readout; in Exam Mode, no numeric value is displayed

### Text

- [x] **TEXT-01**: User can place a text box at a clicked location on the canvas; Grid 3 can type into it via standard Windows text input
- [x] **TEXT-02**: A selected text box responds to nudge controls for repositioning

### Multiple Choice Answers

- [x] **ANS-01**: User can click an answer option region to select it (region highlighted with visual tick indicator)
- [x] **ANS-02**: User can toggle selection to change their answer
- [x] **ANS-03**: User can lock a selected answer to prevent accidental change

### System

- [x] **SYS-01**: User can undo any action and redo previously undone actions
- [x] **SYS-02**: Work is auto-saved to a JSON sidecar file alongside the PDF after every change (no manual save required)
- [x] **SYS-03**: User can resume a previous session by opening the same PDF — all geometry objects restore
- [x] **SYS-04**: User can toggle between Practice Mode (live angle readout shown) and Exam Mode (angle readout hidden) via a chip in the top bar
- [x] **SYS-05**: The current mode indicator (Practice / Exam) is permanently visible in the top bar at all times

## v2 Requirements

### Geometry Tools

- **GEOM-V2-01**: Mark / Highlight tool — draw a freehand highlight over text in the PDF to mark the question the student is working on
- **GEOM-V2-02**: Geometry notation marks — right-angle square, angle arc marker, equal-length tick marks, parallel arrow markers

### Transformations (Computational)

- **TRANS-V2-01**: Computational reflection — student selects a drawn shape + a mirror line; app produces the reflected image as a new polygon
- **TRANS-V2-02**: Rotation — select shape, select centre, rotate by angle
- **TRANS-V2-03**: Translation — select shape, apply vector offset
- **TRANS-V2-04**: Enlargement — select shape, select centre, apply scale factor

### Advanced Constructions

- **CONS-V2-01**: Midpoint tool — click a segment, place its midpoint
- **CONS-V2-02**: Perpendicular line through a point
- **CONS-V2-03**: Parallel line through a point
- **CONS-V2-04**: Angle bisector — place bisector of angle formed by two lines

### Exam Compliance

- **EXAM-V2-01**: Full JCQ-compliant Exam Mode lockdown — verify against current AARA guidance before implementing
- **EXAM-V2-02**: Export annotated PDF for submission

### UX

- **UX-V2-01**: Object locking — lock individual geometry objects to prevent accidental edits
- **UX-V2-02**: Colour coding — assign colour to objects to organise working

## Out of Scope

| Feature | Reason |
|---------|--------|
| In-app keyboard | Grid 3 handles all text input; no duplication needed |
| Cross-platform (iPad, Mac) | Windows-only by design; native stack commitment |
| Drag gestures | Gaze-incompatible; everything is click-to-commit |
| Auto-solving / AI assistance | Exam integrity; app assists process, not answers |
| Real-time collaboration | Single-user, offline tool |
| Computational shape reflection in v1 | Student draws the reflected image manually using Line tool; the cognitive work is the student's |
| PDF shape detection (computer vision) | High complexity, high risk, not required for GCSE tasks |
| Draw tool (freehand) | Gaze-incompatible; accuracy too low for exam work |
| In-app PDF annotation export (v1) | Deferred to v2; JSON sidecar is sufficient for v1 |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CORE-01 | Phase 1 | Pending |
| CORE-02 | Phase 1 | Pending |
| CORE-03 | Phase 1 | Complete |
| CORE-04 | Phase 1 | Complete |
| GEOM-01 | Phase 2 | Complete |
| GEOM-02 | Phase 2 | Complete |
| GEOM-03 | Phase 2 | Complete |
| GEOM-04 | Phase 2 | Complete |
| GEOM-05 | Phase 2 | Complete |
| GEOM-06 | Phase 2 | Complete |
| GEOM-07 | Phase 2 | Complete |
| PROT-01 | Phase 3 | Complete |
| PROT-02 | Phase 3 | Complete |
| PROT-03 | Phase 3 | Complete |
| PROT-04 | Phase 3 | Pending |
| PROT-05 | Phase 3 | Complete |
| PROT-06 | Phase 3 | Complete |
| TEXT-01 | Phase 4 | Complete |
| TEXT-02 | Phase 4 | Complete |
| ANS-01 | Phase 4 | Complete |
| ANS-02 | Phase 4 | Complete |
| ANS-03 | Phase 4 | Complete |
| SYS-01 | Phase 2 | Complete |
| SYS-02 | Phase 4 | Complete |
| SYS-03 | Phase 4 | Complete |
| SYS-04 | Phase 3 | Complete |
| SYS-05 | Phase 3 | Complete |

**Coverage:**
- v1 requirements: 27 total
- Mapped to phases: 27
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-29*
*Last updated: 2026-04-29 after roadmap creation*
