---
phase: quick
plan: 260525-kih
type: execute
wave: 1
depends_on: [260525-k0a]
files_modified:
  - MathGaze/Core/SnapEngine.cs
  - MathGaze/ViewModels/ToolViewModel.cs
  - MathGaze.Tests/SnapEngineTests.cs
autonomous: true
requirements: [GAP-14b]

must_haves:
  truths:
    - "Section 3 orientation guide snap (H/V/45°) is entirely removed from SnapEngine"
    - "Endpoint snap (sections 1+2) still fires at up to 20px — unchanged"
    - "All orientation-guide snap tests updated to expect null label"
    - "SnapEngine.Snap signature unchanged — callers unaffected"
  artifacts:
    - path: "MathGaze/Core/SnapEngine.cs"
      provides: "SnapEngine without section 3 orientation guides"
      contains: "SnapThresholdPx"
    - path: "MathGaze.Tests/SnapEngineTests.cs"
      provides: "Tests updated for removed orientation snap"
---

<objective>
GAP-14b: Remove the orientation guide snap section (section 3: H/V/45°) entirely from
SnapEngine. The threshold reduction in GAP-14 (k0a) improved things but orientation guides
still cause accidental snaps. Removing them entirely eliminates silent drift for all canvas
densities.

Sections 1 (endpoint snap) and 2 (intersection snap) are unchanged.
SnapEngine.Snap method signature is unchanged — no callers need updating.

Existing orientation-guide snap tests must be updated to reflect the new behaviour
(expect null label when cursor is only within orientation guide range).
</objective>

<tasks>

<task type="auto">
  <name>Task 1: Remove section 3 from SnapEngine and update tests</name>
  <files>MathGaze/Core/SnapEngine.cs, MathGaze.Tests/SnapEngineTests.cs</files>
  <action>
    1. In SnapEngine.cs:
       - Remove the OrientThresholdPx constant
       - Remove the entire section 3 block (orientation guides comment + if block)
       - Update class XML doc comment to remove reference to orientation guides
    2. In SnapEngineTests.cs:
       - Update tests that previously expected orientation snap labels to expect null
       - Keep the GAP-14 regression test but update its comment to reflect removal
    3. Run all tests — all must pass
  </action>
  <done>
    SnapEngine.cs has no OrientThresholdPx, no section 3.
    All SnapEngineTests pass.
    Build clean.
  </done>
</task>

</tasks>
</content>
</invoke>