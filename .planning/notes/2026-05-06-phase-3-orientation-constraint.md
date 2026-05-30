---
date: "2026-05-06 10:00"
promoted: false
---

Phase 3 design decision: replace orientation guide proximity snaps (H/V/45°) with explicit right-rail constraint buttons during mid-draw state. After first anchor click, right rail shows Free | Horizontal | Vertical | 45° | (if snapped to line endpoint: Along this line / Perpendicular). Removes false-positive snap labels (screenshot confirmed bug: "snap: horizontal" showed while ghost line was not horizontal — orientation guide was aligning cursor to an existing point's Y, not constraining line direction). Endpoint and intersection snaps stay as-is. Angle input ties into protractor angle logic — design them as one system. Useful for pie charts (anchor at center, constrain to angle, second click = radius).
