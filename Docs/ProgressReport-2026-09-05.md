# Battle Boxes progress report

Checkpoint: 5 September 2026. Changes are in the working tree; this task has not committed them.

## Delivered

- Implemented six-card level selection in Main Menu > Main Camera > Canvas > LevelSelect: numbered tabs, previews, selected enlargement, medal stars, locks and page arrows.
- Restyled the existing Start Game and Back objects and added pixel borders, a sword banner and decorative rules. Preserved the existing logo and menu transitions.
- Added database-driven selection, build-scene availability checks, preview captures for levels 2-6 and a separate white pixel-font material.
- Added editor tools to install/reapply the cards, capture previews, validate the cards and capture a Play Mode screenshot.
- Patched first-load shop selection: explicit unit bindings, removal of old selection callbacks, cleanup of owned listeners before rebinding, decorative graphics excluded from click targets, and one layout/graphics refresh after startup.
- Added last-played-level persistence independently of highest progress. Both selectors now open on the current/last played level and its page. Older saves fall back to highest played.

## Verification and limits

Earlier Unity Play Mode checks covered card selection, paging, Back, reopening and launching selected level 3. The editor card validator passed paging, invalid selection, medal thresholds, renderer and Start-callback checks.

The latest shop and current-level changes compiled successfully using Unity's compiler and project response file. Their gameplay confirmation is left to the user. The reported first-load symptom was not independently reproduced before patching; its resolution is therefore not yet verified. The last minor arrow, logo-position and disabled-button presentation changes compiled but were not separately fully retested.

No standalone release build has been validated. AI, combat balance and the general level-creation workflow have not been repaired or certified by this work.

## Main files

- `Assets/Scenes/Main Menu.unity`
- `Assets/#Scripts/UI/LevelCardGraphic.cs`, `LevelSelectionCard.cs`, `LevelSelectionCarousel.cs`
- `Assets/Editor/LevelSelectionCardSetup.cs`
- `Assets/Resources/LevelSettingsDatabase.asset`
- `Assets/UI/LevelCardText.mat` and `Assets/UI/LevelPreviews/`
- `Assets/#Scripts/Units/UnitPlacer.cs`
- `Assets/#Scripts/GameManagement/PersistentGameSettings.cs` and `LevelSelector.cs`

## Best-value next priorities

1. **Complete scene flow.** Build Settings starts at level 1, duplicates level 5 and excludes Main Menu and level 6. LevelLoader advances numerically without a final-demo boundary. The launch router can redirect level 1 to highest played. Resolve startup, replay, Next and menu return together. Acceptance: a standalone build starts correctly and ends safely after the last included battle.
2. **Stabilise one battle.** Check deployment, costs/refunds, pause, victory, defeat, retry and saving on level 1. Investigate persistent-manager/UI ownership where reproducible failures point to it. Acceptance: win and loss both support retry/menu return without stale state.
3. **Validate level setup.** Extend the existing editor workflow with scene-registration, required-reference, navigation and deployment checks; establish a trusted template before adding creation conveniences. Acceptance: create a playable test level without gameplay-code edits.
4. **Fix specific AI failures.** Use open-field, choke and ranged encounters to identify path, target and order stalls. Acceptance: each encounter reliably finishes with understandable enemy behaviour.
5. **Polish a small demo.** Use levels 1-5 as candidates, subject to platform/scope choice. Tune one teaching purpose per battle, objectives and medal times. Defer the full campaign and new mechanics.

These priorities are inspection findings and recommendations, not claims that every risk has been reproduced. Main Menu's existing missing-NavMesh warning and the existing GameOver UI fallback warning also need triage during scene-flow work.

## Economical process

- One bounded outcome per batch; read this checkpoint and only relevant source files.
- User supplies the level, fresh load/retry, actions, expected result and actual result. Use a short recording when visual/input behaviour is hard to describe.
- Agent traces, patches and compiles once. Add focused tests for fragile behaviour, not every small presentation edit.
- User handles gameplay and feel checks. Avoid extended agent-driven UI testing unless diagnosis requires it.
- Save changed files, verification, limitations and the next action after each batch, before starting another subsystem.
- Extra model calls should answer an independent, bounded question. Avoid repeated whole-project audits.
- Treat the available budget as a ceiling rather than a spending target. Start small and expand when results justify it. No calls have used the supplied API key, and its balance has not been verified.

## Immediate user test

1. Fresh-load a battle and switch between affordable unit types before placing anything or opening settings.
2. Repeat after retry and scene change; check for duplicate click sounds or stale selections.
3. Replay an earlier unlocked level, return to level select and restart the game. The selector should show that last-played level rather than highest unlocked.

If these pass, proceed with priority 1. See `DemoActionPlan.md` for the broader milestones and `LevelSelectionUI.md` for editor maintenance.
