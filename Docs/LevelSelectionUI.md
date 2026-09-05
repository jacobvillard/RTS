# Level selection UI

The implemented screen is in `Assets/Scenes/Main Menu.unity`, under `Main Camera/Canvas/LevelSelect`.

- `Level Cards` contains six reusable, editable card objects and previous/next page buttons.
- Cards read names, thumbnails and medal thresholds from `Assets/Resources/LevelSettingsDatabase.asset`.
- Three stars correspond to the existing bronze, silver and gold time thresholds. Unplayed levels show empty stars.
- Selection uses database entries, independent of hierarchy order. Locked levels and scenes absent from Build Settings cannot launch.
- The existing StartGameBtn and Back button retain their roles with new pixel-style visuals. The existing Play and Back transitions are preserved.
- The reference-image overlay and old detail panels are inactive. The currencies, progress panel and difficulty selector from the reference were not added.
- Decorative shapes, frames, stars, arrows and a sword banner are UI meshes. They do not require generated image assets or special font glyphs.

Open the Main Menu scene and press Play, then the game's Play button. LevelSelect starts inactive so it does not cover the initial menu. To edit its layout outside Play Mode, temporarily activate LevelSelect in the hierarchy; deactivate it before saving the starting scene.

## Editor tools

Under `Tools/RTS`:

- **Install Level Selection Cards** installs the screen or reapplies its presentation. Uses the currently open Main Menu scene. Saves that scene.
- **Capture Missing Level Card Previews** captures numbered battle scenes that exist on disk. It preserves manually assigned previews outside `Assets/UI/LevelPreviews` and refreshes previews generated in that folder. Already-open battle scenes are skipped. Other scene renderers and canvases are temporarily hidden and restored during capture.
- **Validate Level Selection Cards** checks six slots, page boundaries, the partial last page, level labels, invalid selection rejection, star thresholds, renderer components and the existing Start Game callback. Results are written to `Temp/LevelCardsValidation.txt`.

## Content/build notes

The database currently contains 40 levels while only numbered scenes 1-6 exist. Later levels show an uncharted placeholder and stay locked. Level 6 is also locked until it is included in Build Settings and unlocked by progression. This UI change does not alter the build list or existing progression rules.

Unity emits a pre-existing missing-NavMesh warning from units in the Main Menu. That gameplay/scene setup issue is separate from the card UI.
