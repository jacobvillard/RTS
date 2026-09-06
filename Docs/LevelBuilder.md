# Level creation tools

Open **Tools > RTS > Level Builder**. The Scene, Tilemaps, Troops and Buildings tabs share one window. Tools are disabled during Play Mode. Existing battle scenes and the Template scene are the starting point; no battle layout is automatically overwritten.

## New enemies

Added `AI_Officer`, `AI_Pikemen`, `AI_Skirmisher`, `AI_Dragoon` and `AI_Bannermen` under `Assets/Prefabs/Units`.

These use the existing player role stats and artwork with AI team/layers, AI emblems and no player selection handlers. The officer has EnemyOfficerCommander and OfficerCommandController; the bannerman retains MoraleAura; the dragoon uses the existing AI infantry dismount reference. Enemy officers now wait until deployment ends before issuing commands. Shared combat code already implements the pikeman, skirmisher and dragoon roles.

The existing AI_Archers prefab is the project's ranged/musket enemy counterpart despite its old name. Existing infantry, cavalry and scout enemy prefabs remain available. Missing new enemy prefabs are installed on editor import; existing assets are preserved.

## Recommended workflow

1. In **Scene**, choose a saved battle or Template as the base and use **Create level copy**. This copies to a new filename and opens the copy. Use a numeric name for compatibility with the current campaign loader and save system.
2. Edit the matching level settings in the same tab. The current database already has numeric entries 1-40. A scene without an entry displays a warning; it does not silently inherit a new campaign identity.
3. In **Tilemaps**, scan the active scene, select a layer, and drag a tile asset into Paint tile. Enable the Scene-view brush, or set a rectangular region and fill it. A second tile plus seed/noise controls gives repeatable terrain variation. Erase mode removes cells. Escape stops the brush.
4. Review each tilemap's surface rule. `Rivers_3` defaults to Water; mountain/object layers default to Solid. Trees and ground remain Decoration. Layer-name guesses are editable and should be checked against the actual artwork.
5. Water creates existing **SeethroughObstacle** prefabs; Solid creates existing **CastleWall** prefabs. A separate passable/bridge tilemap excludes occupied bridge cells. Do not use the source map itself as its exception map. Explicitly sync after using Unity's external Tile Palette or changing a bridge layer; painting/filling through this window can sync automatically.
6. In **Troops**, select any AI/player prefab, choose formation dimensions, spacing and rotation, then click in Scene view or use the position fields. The preview shows deployment points. Existing troop overlap is rejected by default; this is a spacing check, not a navigation or budget test.
7. In **Buildings**, choose an existing environment/building prefab or drag in another prefab asset. Click to place it. Enable the CastleWall footprint for solid buildings; set its local width/height to match the blocked area. Keep doors/open passages outside the footprint. Disable it for walkable objectives or buildings that already contain their own blocker.
8. Existing scene objects can receive a footprint with **Add footprint to selected scene object**. Existing artwork stays intact. Objects painted on the Objects tilemap can instead use its Solid rule.
9. Return to **Scene**, validate, register the scene in Build Settings if needed, build navigation, and save. Playtest bridge crossings, blocked walls, troop AI and objectives.

## Undo and scope

- Region painting and its overlays undo together. Each formation/building batch supports Undo.
- Overlay sync replaces only that tilemap's `__LevelBuilderOverlays` child. It does not remove manually placed obstacle objects. Existing manual obstacles may overlap generated ones: review them before baking.
- Adjacent cells are grouped into horizontal strips. Bridge gaps remain open. Automatic overlays support rectangular XY maps at cell Z=0; fill regions are limited to 65,536 cells.
- Obstacle sprites retain their geometry for this project's NavMeshPlus renderer collection, but are transparent so they do not cover artwork. Solid footprints retain colliders; water obstacles retain the existing see-through prefab behaviour.
- Surface rules are window settings. Rescan when opening another scene and review guessed rules. Geometry already generated remains saved in the scene. This is not a live runtime generator.
- Scene copying and Build Settings registration are project operations, separate from scene Undo. Copying refuses to overwrite an existing level. Registration preserves the existing startup order.
- Navigation rebuilding uses the scene's existing NavMeshPlus configuration. The validator detects missing surface/data, not every gameplay path problem.

## Validation

**Run level builder self-checks** in the Scene tab (also Tools > RTS > Validate Level Builder Tools) creates a temporary scene and checks enemy prefab wiring, AI dragoon dismount, water/solid behaviour, bridge exceptions, negative cell coordinates, repeated sync without duplication, tile/overlay Undo, formation positioning and building colliders.

Results are written to `Docs/LevelBuilderChecks.txt`. Compilation and editor checks do not establish combat balance or correct movement on every existing map; those checks remain with the user.
