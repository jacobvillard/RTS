# Demo action plan

Created 5 September 2026. Status: planning; implementation has not started.

## Target

Proposed initial scope: a Windows demo with 5 polished battles, using existing levels 1-5 as candidates. Level 6 is optional if it adds a strong final showcase. Confirm platform and scope before release work. Aim for a coherent first session of roughly 15-30 minutes, then adjust from playtests.

The player can launch, select a battle, understand the objective, deploy an army, fight, win or lose, retry, progress, and return later with progress saved. Each milestone below must pass its acceptance gate before moving on.

## Initial repository findings

Static inspection only; visual quality, scene wiring, AI behaviour, and runtime stability still require Unity playtests.

- Unity version: 2022.3.51f1.
- Main Menu, Template, levels 1-6, and AdditveScene exist in Assets/Scenes.
- EditorBuildSettings starts at level 1, lists level 5 twice, and omits Main Menu and level 6. Inspect the existing additive UI flow before choosing the intended entry scene.
- LevelSettingsDatabase contains 40 rows; only rows 1-6 have display names. Later rows exceed the available numbered scenes.
- LevelSelector already supports previews, budget, available units, best times, medals, locks, and pages. It loads numeric scene names directly, bypassing LevelLoader's time reset. Row clicks derive the level number from hierarchy sibling position; verify this against actual scene structure.
- LevelLoader advances by numeric scene name without a demo-end boundary. ApplicationLaunchLevelRouter can redirect to the highest played numeric level; review whether it is attached and appropriate for menu-first startup.
- EnemyTacticalCommander, EnemyOfficerCommander, terrain, capture buildings, healing, morale, and cannons already exist. Their presence does not establish correct gameplay behaviour.
- FormationPlacementWindow provides ASCII prefab placement with Undo. Template.unity exists; a complete validated level creation workflow has not been established.
- Assets/Docs/LevelSpecs_6_41.md is useful campaign design material but contains outdated implementation notes: Dragoons is already a database field, and UnitPlacer already includes expanded unit availability handling.

## 1. Repair level selection and scene flow

- Reproduce the current menu in Play Mode; record screenshots, broken interactions, and intended layout.
- Trace menu/additive scene ownership, selectors, persistent objects, Start, Back, Retry, and Next callbacks.
- Establish a single authoritative set of demo level entries, tied to real scenes; hide unfinished content.
- Fix selection mapping, selected styling, locks, preview/detail updates, and pagination or scrolling as appropriate to the existing design.
- Make level loading use consistent time reset, valid scene checks, and a safe final-demo-level destination.
- Correct build scene registration and startup routing after tracing the current flow.
- Check readability and clipping at agreed supported resolutions.

Acceptance: every visible level selects and loads the correct battle; unavailable content cannot launch; Start/Back/Retry work repeatedly; fresh and existing saves behave sensibly; a standalone build follows the intended startup flow.

## 2. Stabilise one complete battle

- Use level 1 as the reference battle and establish a reproducible baseline.
- Verify deployment boundaries, overlap prevention, costs, refunds, unit availability, and battle start.
- Verify camera/input interactions, selection, orders, pause, speed controls, combat, death, and result timing.
- Verify victory/defeat, best times, medals, unlock rules, save/load, retry, and scene cleanup.
- Resolve the distinction between a played level and a completed/unlocked level; preserve existing save compatibility where needed.
- Fix blockers before adding mechanics. Add focused regression tests for fragile progression and lifecycle logic where practical.

Acceptance: complete win, loss, retry, next-level, and return-to-menu flows repeatedly with no blocking errors, stale units, duplicate managers, or incorrect money/progress.

## 3. Make AI dependable for demo encounters

- Build small repeatable scenarios for open combat, obstacles/chokes, ranged support, and any forest or capture mechanics chosen for the demo.
- Inspect target acquisition, path reachability, attack range, melee congestion, and recovery when targets die or become unreachable.
- Check tactical and officer orders for conflicts, repeated reassignment, and objective priority starvation.
- Ensure AI stops acting outside the battle state and cannot leave an encounter permanently unresolved.
- Expose only encounter settings designers need, such as hold positions and aggression; retain existing systems unless evidence supports replacement.
- Profile representative and maximum intended demo armies before choosing optimisations.

Acceptance: all chosen demo encounters reliably resolve; no reproducible permanent stalls; enemy intent is readable; performance meets a recorded target on agreed hardware.

## 4. Establish a reliable level creation workflow

- Audit Template.unity and produce one trusted template with required runtime objects and references.
- Extend the existing Formation Placer where useful: saved presets, placement preview, mapping validation, and overlap/off-navigation warnings.
- Add a small creation/setup tool to duplicate the template safely, assign level identity/settings, and register intended demo scenes.
- Add validation for missing references, duplicate IDs, invalid scenes, deployment zones, enemy placement, navigation, camera bounds, and required objective data.
- Document terrain painting, navigation baking, formations, budgets, available units, objectives, previews, and test launch.

Acceptance: create a new playable level from the template without editing gameplay code; validation catches deliberately broken setup; verify the new scene in Play Mode.

## 5. Build and balance the demo sequence

- Review existing levels 1-5 and retain the strongest layouts.
- Give each battle one clear teaching purpose, with a gradual increase in tactical demands.
- Suggested sequence: deployment/basic combat; unit matchups; terrain/flanking; one special mechanic; combined challenge. Adapt to the actual existing maps.
- Tune budgets, formations, available units, and medal times through recorded playtests.
- Finish level names, previews, objective text, and concise contextual control hints.
- Add level 6 only after the core sequence passes its quality gates.

Acceptance: a new player can finish the sequence without developer coaching; levels feel distinct; no unexplained difficulty spike; the last result provides a clear demo ending.

## 6. Prepare the distributable demo

- Finish essential feedback, UI readability, audio/options, pause/quit behaviour, and credits/licence review for included assets.
- Produce and test a standalone release candidate with a clean save and an existing save.
- Check supported resolutions, focus loss, loading, repeated retries, and full-sequence completion outside the Editor.
- Record target hardware, frame rate, build version, controls, and known issues.
- Run an external playtest, prioritise blockers and confusion, and repeat affected checks after fixes.

Acceptance: packaged build completes the full demo loop, preserves progress, has no known progression blockers or reproducible crashes, and meets the agreed performance target.

## Scope control and working rhythm

Defer the full 40-level campaign, level 41 waves/survival, additional unit families, and broad AI rewrites unless a chosen demo encounter requires them.

For each step: reproduce the problem, agree the concrete intended behaviour where needed, implement a bounded change, verify it in Unity/build as appropriate, and record the result here. Keep design decisions separate from verified bugs. Do not estimate a release date until milestone 2 establishes the runtime baseline.

Next session: milestone 1, beginning with a visual and wiring audit of the current level selection screen.
