---
phase: 04-answer-layer
plan: "03"
subsystem: session-persistence
tags: [session, auto-save, sidecar, json, system-text-json, dependency-injection]
dependency_graph:
  requires:
    - "04-01: GeometryObject [JsonDerivedType] attributes + Id as init-only"
    - "04-02: TextObject fully renderable (complete object graph for serialization)"
  provides:
    - "ISessionService interface: SetPdfPath, TrySaveAsync(pageOverride), TryLoadAsync"
    - "SessionService singleton: subscribes to ObjectsChanged; writes {pdf}.mathgaze.json"
    - "SidecarModel: CurrentPage + Objects for JSON round-trip"
    - "MainViewModel: restore on PDF open; page-nav save before Reset"
    - "App.xaml.cs DI registration with lazy Func<int> to break circular dependency"
  affects:
    - "All geometry tools (lines, circles, protractor, text): changes now auto-persist"
tech_stack:
  added: []
  patterns:
    - "Func<int> injected into SessionService to lazily resolve CurrentPage from MainViewModel — breaks DI circular dependency without service locator"
    - "Named event handler (OnObjectsChanged) for clean unsubscribe in Dispose() — Phase 2 pattern"
    - "Fire-and-forget async void for ObjectsChanged event handler (save failure is silent by design)"
    - "pageOverride parameter on TrySaveAsync — caller (OnCurrentPageChanged) passes the old page number before CurrentPage updates"
    - "AddObject (no ObjectsChanged) + ObjectsChanged_ForceRaise() for batch restore without save loop"
    - "System.Text.Json WriteIndented=false for compact sidecar files"
key_files:
  created:
    - "MathGaze/Services/ISessionService.cs"
    - "MathGaze/Services/SessionService.cs"
  modified:
    - "MathGaze/ViewModels/MainViewModel.cs"
    - "MathGaze/App.xaml.cs"
decisions:
  - "Func<int> lambda injected into SessionService breaks the circular DI dependency (SessionService needs CurrentPage from MainViewModel; MainViewModel depends on ISessionService) — resolved via lazy factory in App.xaml.cs"
  - "pageOverride parameter on TrySaveAsync allows OnCurrentPageChanged to record the old page before Reset — avoids incorrect sidecar with new page + empty objects"
  - "SidecarModel placed in SessionService.cs (same file as implementation) — it is a DTO used only by SessionService; no need for a separate file"
  - "ANS-01, ANS-02, ANS-03 deferred to v2 per D-08 — no AnswerObject, AnswerMode, or MCQ code anywhere in Phase 4"
  - "_lastSavedPage field tracks the page being left during navigation so TrySaveAsync receives the correct old page number even though CommunityToolkit partial void fires after the property setter"
metrics:
  duration_minutes: 8
  completed_date: "2026-05-27"
  tasks_completed: 2
  tasks_total: 2
  files_created: 2
  files_modified: 2
---

# Phase 4 Plan 3: JSON sidecar auto-save and session restore Summary

**One-liner:** `SessionService` singleton subscribes to `ObjectsChanged` and writes `{pdf}.mathgaze.json` on every geometry change; `MainViewModel` restores all objects and navigates to the saved page silently on PDF open, with page-nav save before Reset to prevent cross-page data loss.

## What Was Built

### ISessionService interface (`MathGaze/Services/ISessionService.cs`)

Thin interface with three members:
- `SetPdfPath(string? pdfPath)` — registers the active PDF path (null on close)
- `TrySaveAsync(string pdfPath, int? pageOverride = null)` — writes sidecar; swallows `IOException`/`UnauthorizedAccessException` silently; `pageOverride` lets callers record the old page during navigation
- `TryLoadAsync(string pdfPath)` — returns `SidecarModel?`; returns null if sidecar missing or corrupt

### SidecarModel + SessionService (`MathGaze/Services/SessionService.cs`)

`SidecarModel` sealed class:
- `CurrentPage { get; set; }` — page the student was on
- `Objects { get; set; }` — all geometry objects; polymorphic via `[JsonDerivedType]` added in Plan 01

`SessionService` sealed class implementing `ISessionService, IDisposable`:
- Constructor injects `IGeometryService` and `Func<int> getCurrentPage` — the Func breaks the circular DI dependency with `MainViewModel`
- Subscribes to `_geometryService.ObjectsChanged += OnObjectsChanged` using a named method (Phase 2 pattern — enables clean unsubscription)
- `OnObjectsChanged`: fire-and-forget `async void` handler; calls `TrySaveAsync`; no-ops if `_pdfPath` is null
- `TrySaveAsync`: snapshots `Objects.ToList()` + resolves page via `pageOverride ?? _getCurrentPage()`; clears `IsSelected` on all objects (transient state not persisted); serializes with `WriteIndented = false`; catches `IOException or UnauthorizedAccessException` silently (Pitfall 6 / T-04-08)
- `TryLoadAsync`: checks `File.Exists`; deserializes; catch-all returns null (D-13 / T-04-07)
- `Dispose`: unsubscribes `ObjectsChanged -= OnObjectsChanged`

### MainViewModel changes (`MathGaze/ViewModels/MainViewModel.cs`)

New fields:
- `private readonly ISessionService _sessionService` — injected via constructor
- `private string? _currentPdfPath` — tracks open PDF path for page-nav saves
- `private int _lastSavedPage = 1` — tracks old page number during navigation

Constructor extended with `ISessionService sessionService` parameter.

`OpenFileAsync` additions (inside `Dispatcher.InvokeAsync` after `Reset()`):
- `_sessionService.SetPdfPath(filePath)` + `_currentPdfPath = filePath` + `_lastSavedPage = 1`

After dispatcher block:
- `await _sessionService.TryLoadAsync(filePath)` — if sidecar exists, dispatches to UI thread to call `AddObject` for each object then `ObjectsChanged_ForceRaise()` once; clamps `CurrentPage` to valid range

`OnCurrentPageChanged` additions (before `_geometryService.Reset()`):
- `_ = _sessionService.TrySaveAsync(_currentPdfPath, pageOverride: _lastSavedPage)` — fire-and-forget; saves OLD page state before objects are cleared
- `_lastSavedPage = value` — advances the tracker to the new page

`CloseFile` additions (before `_pdfService.CloseDocument()`):
- `_sessionService.SetPdfPath(null)` + `_currentPdfPath = null` + `_lastSavedPage = 1`

### App.xaml.cs DI registration

```csharp
services.AddSingleton<ISessionService>(sp =>
    new SessionService(
        sp.GetRequiredService<IGeometryService>(),
        () => sp.GetRequiredService<MainViewModel>().CurrentPage));
```

The `Func<int>` lambda resolves `MainViewModel` lazily at save-time, not at registration-time — avoiding the circular constructor dependency.

## Deviations from Plan

None — plan executed exactly as written.

The plan's Task 1 action block documented two alternative approaches for breaking the DI circular dependency (`MainViewModelAccessor` and `Func<int>`). The plan explicitly resolved this by selecting `Func<int>`. The implementation follows that resolution precisely.

## Known Stubs

None. Session persistence is fully wired end-to-end:
- Every `ObjectsChanged` event triggers a sidecar write (no stub — real file I/O)
- PDF open restores from sidecar if it exists (no stub — real deserialization)
- Missing/corrupt sidecar opens clean (no fallback stub data)
- Page navigation saves before clearing (no stub — real `TrySaveAsync` call)

ANS-01/02/03 are intentionally not implemented per D-08. The `requirements:` field in the plan frontmatter documents the deferral — no stub code was added.

## Threat Flags

No new threat surface beyond what is documented in the plan's threat model:
- T-04-07 (Tampering via hand-edited sidecar JSON): mitigated — `[JsonDerivedType]` whitelist on `GeometryObject` covers only 5 known concrete types; unknown `$type` throws `JsonException` caught by `catch { return null; }` in `TryLoadAsync`
- T-04-08 (DoS via read-only directory): mitigated — `catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)` in `TrySaveAsync` swallows silently
- T-04-09 (Information disclosure in sidecar): accepted — student's own geometry work product; no PII or credentials
- T-04-10 (Corrupt Guid values): accepted — `TryLoadAsync` catch-all handles deserialization failure; open clean

## Self-Check: PASSED

Files exist:
- `MathGaze/Services/ISessionService.cs` — FOUND
- `MathGaze/Services/SessionService.cs` — FOUND
- `MathGaze/ViewModels/MainViewModel.cs` — FOUND (patched)
- `MathGaze/App.xaml.cs` — FOUND (patched)

Commits exist:
- `e982507` — feat(04-03): ISessionService interface + SessionService implementation — FOUND
- `c1f8c57` — feat(04-03): wire SessionService into MainViewModel + App DI — FOUND

Verification checks:
- Build: 0 errors (6 expected NU1701 warnings)
- `ObjectsChanged +=` in SessionService — FOUND
- `ObjectsChanged -=` in SessionService — FOUND
- `ISessionService` field + constructor in MainViewModel — FOUND
- `TryLoadAsync` in MainViewModel.OpenFileAsync — FOUND
- `TrySaveAsync` at line 150, `Reset()` at line 156 — save before reset confirmed
- `services.AddSingleton<ISessionService>` in App.xaml.cs — FOUND
- `AnswerObject.cs` does not exist — CONFIRMED (ANS deferral correct)
