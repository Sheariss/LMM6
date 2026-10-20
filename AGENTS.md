# Project instructions

Never change my files without first showing the proposed changes in chat and receiving my explicit approval. Default to recommendations and code snippets in chat. A request to diagnose or fix an issue does not authorize direct edits.

## UI icons

- Render all UI icons using image assets, preferably UI Toolkit Image elements.
- Never use text characters, Unicode symbols, emoji, or icon fonts as icons.
- This includes arrows, close/minimize/maximize controls, plus/minus actions, menu indicators, and status symbols.
- Use existing project icon assets where available.
- Give icons explicit dimensions and preserve their aspect ratio.
- Keep meaningful text labels and tooltips separate from the icon image.
- Icons must remain visually consistent when accessibility settings change fonts.

## Change packages and commits

- Show proposed file changes in chat and obtain explicit approval before editing.
- Approval to implement a fix or change package also authorizes one local commit
  after its available validation is complete.
- Keep each commit focused on one logical fix or package.
- Stage only files belonging to that approved package. Do not include unrelated
  changes without explicit approval.
- Use plain-language commit titles describing the resulting change.
- Include a short commit body explaining what changed, why, validation performed,
  and any outstanding limitations.
- Report the commit hash and validation results in chat.
- Do not push, amend commits, or rewrite history without explicit approval.
