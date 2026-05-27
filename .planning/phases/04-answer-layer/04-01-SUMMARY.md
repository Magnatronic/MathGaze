---
phase: 04-answer-layer
plan: "01"
subsystem: geometry-model
tags: [textobject, json-serialization, geometry, tdd]
dependency_graph:
  requires:
    - "03-04: ProtractorObject and GeometryService baseline"
  provides:
    - "TextObject model class for clipboard-paste text labels"
    - "GeometryObject polymorphic JSON serialization via [JsonDerivedType]"
    - "GeometryService.NudgeObject TextObject case"
  affects:
    - "04-02: Text tool UX (depends on TextObject existing)"
    - "04-03: Session persistence (depends on [JsonDerivedType] and Id being init-only)"
tech_stack:
  added: []
  patterns:
    - "System.Text.Json [JsonDerivedType] on abstract base for polymorphic serialization"
    - "SKFont.MeasureText with 'using var' for per-click hit-test (not per-frame)"
    - "500-char clipboard truncation for DoS protection"
key_files:
  created:
    - "MathGaze/Core/Geometry/TextObject.cs"
    - "MathGaze.Tests/TextObjectTests.cs"
  modified:
    - "MathGaze/Core/Geometry/GeometryObject.cs"
    - "MathGaze/Services/GeometryService.cs"
decisions:
  - "GeometryObject.Id changed to { get; init; } — enables System.Text.Json round-trip without breaking immutability semantics (Pitfall 1 fix)"
  - "Five [JsonDerivedType] attributes on GeometryObject cover all concrete subclasses including new TextObject; unknown $type values throw JsonException caught by future TryLoadAsync"
  - "TextObject.Draw throws NotSupportedException (not NotImplementedException) — matches established pattern where rendering lives in GeometryLayerViewModel only"
  - "HitTest uses 'using var font = new SKFont(...)' — one allocation per click acceptable; per-frame font is cached in GeometryLayerViewModel (Pitfall 5)"
  - "ContentText truncated at 500 chars in constructor, not at placement site — model enforces invariant regardless of call site (T-04-01)"
metrics:
  duration_minutes: 3
  completed_date: "2026-05-27"
  tasks_completed: 1
  tasks_total: 1
  files_created: 2
  files_modified: 2
---

# Phase 4 Plan 1: TextObject model + GeometryObject serialization foundation Summary

**One-liner:** TextObject sealed class with clipboard-paste semantics plus five `[JsonDerivedType]` attributes on `GeometryObject` enabling polymorphic JSON round-trips including ProtractorObject cross-references.

## What Was Built

### TextObject model (`MathGaze/Core/Geometry/TextObject.cs`)

New sealed class extending `GeometryObject`:
- `ContentText { get; init; }` — immutable clipboard text (D-03), truncated at 500 chars (T-04-01)
- `XPt { get; set; }` / `YPt { get; set; }` — PDF-point coordinates (D-04/D-10), nudge-able
- Parameterless constructor for `System.Text.Json` deserialization
- Valued constructor with 500-char truncation
- `HitTest` using `SKFont.MeasureText` with `using var` disposal — accurate ink bounds for any clipboard content including math symbols
- `GetSnapPoints` returns empty (D-04: no snap points for text)
- `Draw` throws `NotSupportedException` — rendering delegated to `GeometryLayerViewModel` (established pattern)

### GeometryObject serialization foundation (`MathGaze/Core/Geometry/GeometryObject.cs`)

Two changes:
1. `Id` changed from `{ get; }` to `{ get; init; }` — fixes Pitfall 1 (Id was silently skipped by STJ; now round-trips correctly, preventing ProtractorObject Line1Id/Line2Id from becoming Guid.Empty after restore)
2. Five `[JsonDerivedType]` attributes added on the abstract base: `point`, `line`, `circle`, `protractor`, `text` — enables STJ to write and read `$type` discriminator automatically; unknown types throw `JsonException` (T-04-02 mitigation)

### GeometryService NudgeObject extension (`MathGaze/Services/GeometryService.cs`)

Added `case TextObject t:` to the existing switch in `NudgeObject`. Applies `dxPt`/`dyPt` to `XPt`/`YPt` (TEXT-02 requirement).

## Test Results

- 12 new tests in `TextObjectTests.cs` — all pass
- 75 total tests pass (0 regressions)

TDD cycle:
- RED: tests written and committed as `5029c5b` — compile error confirmed (TextObject missing)
- GREEN: implementation committed as `b9fd8f1` — 12/12 new tests pass, 75/75 total pass

## Deviations from Plan

None — plan executed exactly as written.

The test for `Record.Exception` with async signature was noted in the initial RED compile output (xunit.v3 uses `Record.ExceptionAsync` for async). The test was written as a synchronous lambda so there was no actual async issue — the xunit.v3 overload resolution warning resolved once `TextObject` existed and the compiler could pick the correct synchronous overload.

## Known Stubs

None. `TextObject` is a complete model — no stub data, no placeholder properties, no TODO fields. `Draw` throwing `NotSupportedException` is intentional per the established architecture pattern (not a stub).

## Threat Flags

No new threat surface introduced beyond what is documented in the plan's threat model. The `[JsonDerivedType]` whitelist explicitly covers only the 5 known concrete types — unknown `$type` values in sidecar JSON will throw `JsonException` (T-04-02 mitigation correctly implemented).

## Self-Check: PASSED

Files exist:
- `MathGaze/Core/Geometry/TextObject.cs` — FOUND
- `MathGaze/Core/Geometry/GeometryObject.cs` — FOUND (patched)
- `MathGaze/Services/GeometryService.cs` — FOUND (patched)
- `MathGaze.Tests/TextObjectTests.cs` — FOUND

Commits exist:
- `5029c5b` — test(04-01): add failing tests — FOUND
- `b9fd8f1` — feat(04-01): introduce TextObject model — FOUND

Build: 0 errors. Tests: 75/75 pass.
