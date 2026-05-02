---
phase: 01-foundation
plan: 05
subsystem: infra
tags: [dotnet-publish, self-contained, single-file, wpf, skia, pdfium, deployment]

# Dependency graph
requires:
  - phase: 01-foundation plan 04
    provides: Fully wired WPF app with PDF rendering, page nav, zoom, open command

provides:
  - Self-contained single-file EXE (MathGaze.exe) bundling .NET 9 runtime, WPF, SkiaSharp, and PDFium
  - Verified xcopy-deployable artifact that launches on Windows 10/11 without .NET pre-installed
  - CORE-04 satisfied: no admin install, no runtime dependency on school machine

affects: [phase-02-geometry-core, phase-03-annotation, all future phases needing deployment artifact]

# Tech tracking
tech-stack:
  added:
    - "dotnet publish --self-contained true -p:PublishSingleFile=true (bundling strategy)"
    - "IncludeNativeLibrariesForSelfExtract=true (PDFium DLL bundled into single EXE)"
    - ".gitignore excluding publish/, bin/, obj/, PDB sidecar files"
  patterns:
    - "All publish properties baked into .csproj PropertyGroup (no per-run flags needed)"
    - "PDFium extracts to %TEMP%\\.net\\MathGaze\\[hash]\\ on first run — expected behaviour, no admin"

key-files:
  created:
    - ".gitignore — excludes publish/ output, bin/, obj/, third-party PDB files"
  modified:
    - "MathGaze/MathGaze.csproj — all self-contained publish properties present and verified"

key-decisions:
  - "Accept pdfium.dll extracted to %TEMP% on first run — %TEMP% is per-user writable, no UAC needed"
  - "Publish properties baked into .csproj so dotnet publish with no extra flags is sufficient"
  - "Companion pdfium.dll not needed in publish/ folder — IncludeNativeLibrariesForSelfExtract=true bundles it"
  - "SmartScreen blue warning on unsigned EXE accepted — student clicks More info then Run anyway, no admin"

patterns-established:
  - "Self-contained publish: dotnet publish MathGaze/MathGaze.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/"
  - "Output: single MathGaze.exe ~149.6 MB; no loose DLLs; pdfium bundled via %TEMP% extraction"

requirements-completed: [CORE-04]

# Metrics
duration: ~30min
completed: 2026-04-30
---

# Phase 1 Plan 05: Deployment Verification Summary

**Self-contained 149.6 MB MathGaze.exe produced via dotnet publish, bundling .NET 9 runtime + SkiaSharp + PDFium — launches from USB on Windows 10/11 without .NET installed, no admin rights, approved by user**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-04-30
- **Completed:** 2026-04-30
- **Tasks:** 2 (Task 1: publish + verify; Task 2: deployment checkpoint — human-approved)
- **Files modified:** 2 (.gitignore created, MathGaze.csproj verified)

## Accomplishments

- All self-contained publish properties confirmed present in MathGaze.csproj: `RuntimeIdentifier`, `SelfContained`, `PublishSingleFile`, `IncludeNativeLibrariesForSelfExtract`, `DocnetRuntime`
- `dotnet publish` produced `publish/MathGaze.exe` at 149.6 MB — .NET 9 runtime, WPF, SkiaSharp, and PDFium all bundled
- No loose runtime DLLs in publish output; PDFium extracted to `%TEMP%\.net\MathGaze\[hash]\` on first run (expected, no admin needed)
- User approved deployment checkpoint: EXE launched successfully, PDF rendered on canvas
- `.gitignore` added to exclude publish/, bin/, obj/, and third-party PDB sidecar files from version control
- Phase 1 all success criteria satisfied: single EXE from USB, PDF renders, page nav, zoom, CoordinateMapper 32 tests pass

## Task Commits

Each task was committed atomically:

1. **Task 1: Produce self-contained single-file publish and verify output** - `d685a77` (chore)
2. **Task 2: Deployment checkpoint** - N/A (human-verify checkpoint, no code change)

**Plan metadata:** (this commit — docs: complete deployment verification)

## Files Created/Modified

- `MathGaze/MathGaze.csproj` — verified all publish properties present; no changes needed (all properties were already correct from prior plan work)
- `.gitignore` — excludes publish/ folder (self-contained EXE), bin/, obj/, third-party PDB sidecar files

## Decisions Made

- **Publish properties baked into .csproj:** `RuntimeIdentifier`, `SelfContained`, `PublishSingleFile`, `IncludeNativeLibrariesForSelfExtract`, and `DocnetRuntime` all live in the project file. Running `dotnet publish MathGaze/MathGaze.csproj -c Release -o publish/` is sufficient with no extra CLI flags.
- **PDFium companion DLL not needed:** `IncludeNativeLibrariesForSelfExtract=true` causes the .NET single-file host to bundle pdfium.dll inside MathGaze.exe and extract it to `%TEMP%\.net\MathGaze\[bundle-hash]\` on first run. This is xcopy-deployable and does not require admin rights. Fallback (companion DLL) was not needed.
- **SmartScreen accepted as normal:** Unsigned EXE shows blue SmartScreen warning on first run. Student clicks "More info" then "Run anyway". No admin rights required to dismiss. Documented for school IT staff.
- **Threat T-05-01 accepted:** %TEMP% DLL extraction path is content-addressable by bundle hash. Attacker needing to know hash pre-launch and having write to the current user's %TEMP% already has code execution as that user — below practical threat model for a school tool.

## Deviations from Plan

None — plan executed exactly as written. All required publish properties were already present in MathGaze.csproj from prior plan work. The publish command succeeded first time. Deployment checkpoint was approved by the user.

## Issues Encountered

None — publish completed without errors. PDFium bundled correctly on first attempt (no fallback to companion DLL needed). EXE launched and PDF rendered as expected.

## User Setup Required

None — no external service configuration required.

**School IT note:** MathGaze.exe is an unsigned EXE. On first launch, Windows SmartScreen shows a blue "Windows protected your PC" dialog. Click "More info" then "Run anyway". This requires no admin rights. Subsequent launches are instant with no warning.

## Phase 1 Final Success Criteria

All Phase 1 ROADMAP success criteria satisfied:

| Criterion | Status | Verified in |
|-----------|--------|-------------|
| Single EXE launches on Windows 10/11 from USB without install/admin | PASS | Plan 05 deployment checkpoint |
| PDF opens and renders on canvas | PASS | Plan 04 checkpoint |
| Page navigation works | PASS | Plan 04 checkpoint |
| Zoom in/out updates view | PASS | Plan 04 checkpoint |
| CoordinateMapper unit tests pass at all zoom × DPI combinations | PASS | Plan 01 (32 tests) |

**Phase 1 is complete.**

## Next Phase Readiness

Phase 2 — Geometry Core — can begin immediately. The foundation is solid:

- WPF + SkiaSharp canvas renders PDF pages as bitmap background layer
- Coordinate system (CoordinateMapper) handles zoom and DPI scaling correctly — geometry drawn on top will map correctly to PDF coordinates
- Self-contained deployment artifact confirmed — geometry tools will be included in the same publish
- No blockers for Phase 2

Phase 2 will add: line drawing (2-click), circle drawing (2-click), protractor overlay, and the geometry state model persisted as JSON sidecar alongside the PDF.

---
*Phase: 01-foundation*
*Completed: 2026-04-30*
