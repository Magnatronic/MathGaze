---
status: partial
phase: 04-answer-layer
source: [04-VERIFICATION.md]
started: 2026-05-27T00:00:00Z
updated: 2026-05-27T00:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Text label placement
expected: Copy text to clipboard, activate Text tool (click T button in tool rail), click canvas — a Consolas 14pt text label appears at the clicked position
result: [pending]

### 2. Empty clipboard toast
expected: Activate Text tool with empty clipboard, click canvas — status bar shows "Copy text first, then click to place" and no object is placed
result: [pending]

### 3. TextObject nudge
expected: Click a placed text label to select it (right rail shows "Text" type and "Move" nudge label) — nudge buttons move the label; Delete removes it
result: [pending]

### 4. Auto-save sidecar creation
expected: After placing any geometry, inspect the PDF directory — `{pdfname}.pdf.mathgaze.json` exists, contains `"$type"` discriminator fields and all geometry objects
result: [pending]

### 5. Session restore
expected: Close and reopen the same PDF — all geometry objects (including text labels) restore silently with no prompt or dialog
result: [pending]

### 6. Corrupt sidecar resilience
expected: Hand-edit `{pdf}.mathgaze.json` to break JSON syntax (e.g. delete a closing brace), then open the PDF — app opens clean with no crash and no error dialog
result: [pending]

## Summary

total: 6
passed: 0
issues: 0
pending: 6
skipped: 0
blocked: 0

## Gaps
