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

## Package proposal summaries

- End every change-package proposal with a "Proposed file organization" section.
- List every file to be created, modified, moved, renamed, or deleted.
- Include the filename, full project path, operation, and brief purpose.
- Explicitly identify any new folders.
- List required prefab, scene, catalog, and Inspector wiring separately.
- Present this summary before requesting implementation approval.
- Do not silently change approved paths or expand the package scope.

## State ownership

- Every package proposal must identify which component owns its state.
- Explain whether that state survives window closure, saving/loading, and account
  changes. Explicitly identify session-only state and missing persistence wiring.

## Unity asset references

- Preserve existing GUIDs and .meta files when moving or renaming Unity assets.
- Include required new .meta files in the package commit.

## Implementation completion reports

- After implementation and commit, report the actual files changed, required
  manual setup, validation results, remaining limitations, and commit hash.
- Include a detailed manual testing guide for behavior changes, with precise,
  numbered Unity actions, the starting scene/account/state, and the expected
  result for each step. Cover relevant alternative actions and failure cases.
- Clearly distinguish checks actually performed from tests the user still needs
  to run. Do not claim Unity compilation or Play Mode validation without evidence.
- When Inspector wiring is required, provide exact setup instructions: scene or
  prefab path, target GameObject, component, field name, and the asset or object
  to assign. Specify whether to work in Edit Mode or Prefab Mode and whether to
  save the scene or apply prefab changes. Complete setup before testing.
- For documentation-only packages, explicitly state that Unity testing and
  Inspector setup are not required instead of inventing runtime test steps.
