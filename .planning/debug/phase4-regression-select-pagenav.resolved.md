---
status: awaiting_human_verify
trigger: "Phase 4 regressions: (1) no geometry object can be selected, (2) objects lost on page navigation"
created: 2026-05-27T00:00:00Z
updated: 2026-05-27T00:10:00Z
---

## Current Focus

hypothesis: both root causes confirmed, fixes applied
test: awaiting user verification in running app
expecting: selection works, objects survive round-trip page nav
next_action: user builds and verifies

## Symptoms

expected:
  1. Clicking near any geometry object in Select mode activates it and shows it highlighted in the right rail.
  2. Objects placed on page 1 survive a round-trip to page 2 and back within the same session.

actual:
  1. No object can be selected — clicking in Select mode does nothing (silent failure). Worked before Phase 4.
  2. Objects disappear when navigating between pages. No restore on intra-session page nav.

errors: none — silent failures

reproduction:
  Bug 1: Load PDF, place a Line, switch to Select tool, click near the line. Nothing happens.
  Bug 2: Load PDF, draw on page 1, navigate to page 2, navigate back to page 1. Objects gone.

started: after Phase 4 plans 04-01/04-02/04-03 executed

## Eliminated

- hypothesis: GeometryObject.Id init vs get change broke identity/equality in SetSelected lookup
  evidence: GeometryService.SetSelected uses o.Id == id (Guid value equality) — unchanged by init modifier.
  timestamp: 2026-05-27

- hypothesis: ToolViewModel.HandleCanvasClick Select case broken by Phase 4 Text case addition
  evidence: Select case (ToolMode.Select, DrawState.Idle) is unchanged. HandleSelectClick logic correct.
  timestamp: 2026-05-27

- hypothesis: RightRailViewModel.Refresh broke because TextObject arm was added incorrectly
  evidence: TextObject arm properly added. Refresh() correctly maps all types.
  timestamp: 2026-05-27

- hypothesis: GeometryLayerViewModel draw changes caused event wiring issue
  evidence: No canvas event wiring changed in GeometryLayerViewModel. Only added TextObject draw case.
  timestamp: 2026-05-27

- hypothesis: ToolRail DataContext wiring broken by Phase 4
  evidence: MainWindow.xaml.cs sets ToolRailControl.DataContext = toolViewModel explicitly. Correct.
  timestamp: 2026-05-27

## Evidence

- timestamp: 2026-05-27
  checked: SessionService.TrySaveAsync — line 73-78
  found: |
    var model = new SidecarModel {
        Objects = _geometryService.Objects.ToList(),  // shallow copy — same object references
    };
    foreach (var obj in model.Objects)
        obj.IsSelected = false;  // MUTATES LIVE OBJECTS in GeometryService._objects
  implication: |
    Bug 1 ROOT CAUSE confirmed. ToList() creates a new list but the elements are the same
    GeometryObject instances that live in GeometryService._objects. The foreach then sets
    obj.IsSelected = false on each live object. This fires on every ObjectsChanged event,
    which includes the SetSelected() call that the select-click raises. Sequence:
      1. User clicks near line → SetSelected(id) → obj.IsSelected = true → ObjectsChanged raised
      2. ObjectsChanged → SessionService.OnObjectsChanged → TrySaveAsync()
      3. TrySaveAsync snapshots list (same refs), then foreach clears IsSelected = false on live objs
      4. Canvas repaints — IsSelected is now false — no highlight, right rail shows "NOTHING SELECTED"
    Selection was silently cleared by the save loop on every single select action.

- timestamp: 2026-05-27
  checked: GeometryObject.cs — IsSelected property
  found: public bool IsSelected { get; set; }  // no [JsonIgnore]
  implication: Without [JsonIgnore], the serialiser includes IsSelected in JSON (always false
               because the mutation loop ran). Adding [JsonIgnore] removes the need to mutate
               live objects at all — selection state is simply absent from the sidecar.

- timestamp: 2026-05-27
  checked: MainViewModel.OnCurrentPageChanged — Bug 2 path
  found: |
    _geometryService.Reset() wipes all objects on every page navigation.
    TryLoadAsync is only called in OpenFileAsync — no per-page restore on intra-session nav.
    Additionally: Reset() raises ObjectsChanged → SessionService fires a second TrySaveAsync
    with empty objects, potentially overwriting the good sidecar written for the departing page.
  implication: |
    Bug 2 ROOT CAUSE confirmed. Objects are destroyed on nav and never restored intra-session.
    Fix: in-memory per-page object cache in MainViewModel (Option B). Simpler and more reliable
    than re-reading the sidecar on every page navigation.

## Resolution

root_cause: |
  Bug 1 (selection broken): SessionService.TrySaveAsync called _geometryService.Objects.ToList()
  which creates a shallow list copy — the same GeometryObject instances from GeometryService._objects.
  It then iterated those same references to clear IsSelected. Because ObjectsChanged fires on every
  SetSelected() call, TrySaveAsync ran immediately after every selection action, silently wiping
  IsSelected = false on the live object. Result: selection appeared to work for one frame then
  vanished, or never visibly registered at all.

  Bug 2 (objects lost on page nav): _geometryService.Reset() on every page navigation destroyed
  all objects. TryLoadAsync (the only restore path) is called only on PDF open, so returning to a
  previously-visited page within the same session found an empty geometry service. No intra-session
  per-page restore existed.

fix: |
  Bug 1: Added [System.Text.Json.Serialization.JsonIgnore] to GeometryObject.IsSelected.
  This excludes IsSelected from serialisation entirely, removing any need to touch live objects
  before saving. The foreach mutation loop in SessionService.TrySaveAsync was removed.
  Files: MathGaze/Core/Geometry/GeometryObject.cs, MathGaze/Services/SessionService.cs

  Bug 2: Added Dictionary<int, List<GeometryObject>> _pageObjectCache to MainViewModel.
  In OnCurrentPageChanged: before Reset(), the departing page's objects are snapshotted into
  the cache. After Reset(), the arriving page's objects are restored from cache via AddObject +
  ObjectsChanged_ForceRaise (same pattern as sidecar restore — safe per Pitfall 3).
  Cache is cleared in CloseFile() and at the top of OpenFileAsync() so it never bleeds across
  documents.
  File: MathGaze/ViewModels/MainViewModel.cs

verification: static review — build environment has runtime only, no SDK. All changes verified
  by reading final file state. Logic correctness confirmed by tracing full execution paths.

files_changed:
  - MathGaze/Core/Geometry/GeometryObject.cs
  - MathGaze/Services/SessionService.cs
  - MathGaze/ViewModels/MainViewModel.cs
