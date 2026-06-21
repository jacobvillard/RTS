# Level Specifications 6-41

## Project Conventions

- Scenes should keep the current numeric naming convention: `6`, `7`, etc.
- Per-level player budget should be entered in `Assets/Resources/LevelSettingsDatabase.asset`.
- Existing level setting fields cover `officer`, `Scout`, `Pikemen`, `Skirmishers`, `Grenadiers`, and `Bannermen`. Add a `Dragoons` bool and remove/ignore `Grenadiers` when convenient.
- The current `UnitPlacer` only exposes Infantry, Cavalry, and Musket. Player-facing unlocks for Scouts, Pikemen, Bannermen, Officers, Dragoons, and Skirmishers need shop/button support before those units can be placed by the player.
- Enemy formations can be placed with `Tools/RTS/Formation Placer`. Extend its default mappings as new enemy/player prefabs are created.
- Use grid coordinates as `(x,y)` from bottom-left, `1..18`. Player deployment is normally bottom rows.
- Use `TilemapForestZone` for forests, `CapturableBuilding` for capturable POIs, `TeamWideBuildingBuff` for strategic building buffs, `MedicalBuilding` for healing POIs, `MoraleAura` for bannermen, and `CannonEmplacement` for cannons.
- Existing normal win condition is enemy wipe. Level 41 needs a survival/wave win script if it should end by timer instead.
- Enemy behaviour can use `EnemyTacticalCommander` with `captureBuildings`, `takeCannons`, and configured choke point transforms.

## Unit Symbol Key

- `I` Infantry
- `M` Musket
- `C` Cavalry
- `S` Scout
- `P` Pikemen
- `B` Bannermen
- `O` Officer
- `D` Dragoon
- `X` Skirmisher

## Campaign Overview Table

| Level | Name | Player Budget | Main Lesson | Primary Map Shape | Key POIs |
|---:|---|---:|---|---|---|
| 6 | Greywall Lane | 550 | First Scout use | Central lane with forest pockets | 2 small morale buildings |
| 7 | Pinewatch Road | 575 | Forest control | Road with forest strips | 1 central building |
| 8 | Briarfield | 600 | Hidden cavalry threat | Open centre with forest hooks | 1 medic |
| 9 | Old Hunter's Trail | 600 | Narrow forest lanes | Three woodland paths | 1 small building |
| 10 | Speargate Common | 625 | Enemy pikemen | Open common with blockers | 1 central building |
| 11 | Harper's Bridge | 650 | Pikemen defence | Single bridge chokepoint | None |
| 12 | Whiteford Town | 700 | Town combined arms | Village blocks and forest | Medic, small building |
| 13 | Ashen Keep | 725 | Break supported centre | Keep and wall gaps | Keep morale |
| 14 | North Orchard | 725 | Anti-cavalry setup | Orchard side lanes | 2 small buildings |
| 15 | Rose Banner Fields | 750 | Enemy morale support | Open field with farm cover | Enemy bannerman |
| 16 | Crownfield Square | 800 | Player bannermen | Town square | Medic, 2 buildings |
| 17 | Redwater Crossing | 825 | River chokepoint | Single bridge | 2 bridge buildings |
| 18 | The Two Fords | 850 | Split crossing | Two fords | 2 ford buildings |
| 19 | Lowbank Village | 875 | River town fight | Village crossing | Medic, building |
| 20 | Banner Road | 900 | Enemy officer | Road with cover | Central keep-lite |
| 21 | Kingsway Green | 950 | Player officers | Open multi-front field | 2 buildings |
| 22 | Old Iron Yard | 975 | First cannon | Three industrial lanes | Neutral cannon |
| 23 | Powder Mill | 1000 | Cannon defence | Protected cannon lane | Enemy cannon, medic |
| 24 | Ironwood Road | 1025 | Avoid cannon lanes | Long road with forest flanks | Cannon, 2 buildings |
| 25 | Dragoon Ford | 1050 | Enemy dragoons | Two crossing river | 2 crossing buildings |
| 26 | Blackstone Gate | 1100 | Player dragoons | Wall with two openings | 2 gate buildings |
| 27 | Northgate Bastion | 1125 | Fort assault | Compact fort | Medic, cannon, keep |
| 28 | The Foundry Yard | 1125 | Obstacle fighting | Small lane maze | Medic, 2 buildings |
| 29 | Coalbrook Keep | 1175 | Heavy defence | Keep-backed approach | Keep, optional cannon |
| 30 | Mistwood Rifles | 1200 | Enemy skirmishers | Forest lanes | Medic, building |
| 31 | Mistwood Ford | 1250 | Player skirmishers | Forested crossings | 2 crossing buildings |
| 32 | The Three Roads | 1275 | Route choice | Three routes | 3 buildings, medic |
| 33 | Crown Road East | 1300 | Combined support | Road, forests, town | Keep-lite, medic |
| 34 | Greyfort Approach | 1325 | Siege approach | Forest village to wall | Outer buildings, keep |
| 35 | Emberwick Keep | 1400 | Full enemy toolkit | Fort, forest, cannon lane | Keep, cannon, medic |
| 36 | Dawn at Redwater | 1250 | Fast river remix | Wide bridge and ford | 1 central building |
| 37 | Foxwood Return | 1350 | Forest finale | Three forest clearings | Medic, building |
| 38 | King's Orchard | 1400 | Morale finale | Orchard lanes | Keep, buildings, medic |
| 39 | The Iron Crown | 1500 | Siege finale | Two-gate fort | Cannon, keep, medic |
| 40 | The Royal Road | 1600 | Final mastery | Full combined map | Keep, cannon, medic, building |
| 41 | The Last Redoubt | 1800 | Extreme defence | Fort defence versus waves | Cannon, medic, keep |

## Grid Sketch Legend

- `D` player deployment.
- `F` forest.
- `R` river or blocked water.
- `B` small capturable building.
- `K` keep or major building.
- `H` medic.
- `G` cannon.
- `W` wall or hard blocker.
- `E` visible enemy formation area.
- `?` hidden enemy threat.

### Example Grid Sketch: Level 6

| Row | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 |
|---:|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 18 | . | . | . | . | . | . | . | E | E | E | E | . | . | . | . | . | . | . |
| 17 | . | . | . | . | . | . | . | E | E | E | E | . | . | . | . | . | . | . |
| 16 | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| 15 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 14 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 13 | . | F | F | F | F | F | . | . | E | E | . | . | F | F | F | F | F | . |
| 12 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 11 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 10 | . | F | F | ? | B | F | . | . | . | . | . | . | F | B | F | F | F | . |
| 09 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 08 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 07 | . | F | F | F | F | F | . | . | . | . | . | . | F | F | F | F | F | . |
| 06 | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| 05 | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . | . |
| 04 | . | . | . | D | D | D | D | D | D | D | D | D | D | D | D | . | . | . |
| 03 | . | . | . | D | D | D | D | D | D | D | D | D | D | D | D | . | . | . |
| 02 | . | . | . | D | D | D | D | D | D | D | D | D | D | D | D | . | . | . |
| 01 | . | . | . | D | D | D | D | D | D | D | D | D | D | D | D | . | . | . |

## Level Specs

### 6. Greywall Lane

- Player budget: 550.
- Enemy strength: about 300.
- Enemy composition: 3 Infantry, 1 Musket, 1 hidden Infantry.
- Deployment zone: player rows `y=1..4`, `x=4..15`.
- Layout: central lane from `(8,1)` to `(11,18)`. Forest pockets at `(2..6,7..12)` and `(13..17,7..12)`. Small morale buildings at `(5,10)` and `(14,10)`.
- POIs: two small buildings with `CapturableBuilding + TeamWideBuildingBuff`, radius 5.
- Enemy plan: main group holds upper lane around `(9,13)`. Hidden Infantry waits in left forest near `(4,10)`.
- Scripted behaviour: none beyond forest concealment. This should be a forgiving Scout tutorial.
- Unlocks: Scouts should be available from this level onward.
- Required code/data: enable `Scout` for scene `6` in `LevelSettingsDatabase`. Shop support for Scout if not already added.

### 7. Pinewatch Road

- Player budget: 575.
- Enemy strength: about 360.
- Enemy composition: 3 Infantry, 2 Muskets, 1 hidden Cavalry.
- Deployment zone: `y=1..4`, full width except forest edge tiles.
- Layout: straight road up `x=8..11`. Forest strips at `x=1..5` and `x=14..18`, `y=5..16`.
- POIs: neutral small building at `(9,9)` with strategic radius 4.
- Enemy plan: muskets hold road at `(8,13)` and `(11,13)`, Infantry screen at `(9,11)`, hidden Cavalry starts in right forest `(16,10)`.
- Scripted behaviour: hidden Cavalry should only threaten after player advances near centre.
- Required code/data: none beyond Scout availability.

### 8. Briarfield

- Player budget: 600.
- Enemy strength: about 390.
- Enemy composition: 2 Infantry, 2 Muskets, 2 hidden Cavalry.
- Deployment zone: `y=1..4`, `x=3..16`.
- Layout: open centre with forest hooks at `(1..5,8..15)` and `(14..18,8..15)`.
- POIs: medic POI at `(9,8)` with `CapturableBuilding + MedicalBuilding`, radius 3.
- Enemy plan: visible Infantry/Muskets bait the centre. Cavalry hidden in both forests targets unsupported player muskets.
- Scripted behaviour: place Cavalry deep enough that Scouts can reveal before contact.
- Required code/data: none.

### 9. Old Hunter's Trail

- Player budget: 600.
- Enemy strength: about 350.
- Enemy composition: 4 Infantry, 1 Musket, 1 hidden Musket.
- Deployment zone: small lower clearing `(5..14,1..4)`.
- Layout: three narrow forest lanes: left `x=3..5`, centre `x=8..11`, right `x=14..16`, with forest filling gaps.
- POIs: no healing; one small building at `(9,10)` as a control anchor.
- Enemy plan: visible blockers in centre lane, hidden Musket controls right lane.
- Scripted behaviour: short tactical puzzle. Avoid too many enemies; difficulty is route reading.
- Required code/data: none.

### 10. Speargate Common

- Player budget: 625.
- Enemy strength: about 420.
- Enemy composition: 3 Pikemen, 2 Muskets, 2 Infantry.
- Deployment zone: `y=1..4`, full width.
- Layout: open common with two low wall blockers at `(6,9)` and `(12,9)`, leaving flanking gaps.
- POIs: neutral small morale building at `(9,9)`.
- Enemy plan: Pikemen form centre anti-cavalry line; Muskets behind them.
- Scripted behaviour: none. Player should learn cavalry is not the answer here.
- Unlocks: victory unlocks Pikemen.
- Required code/data: enable `Pikemen` from level `11`; add player Pikemen shop support.

### 11. Harper's Bridge

- Player budget: 650.
- Enemy strength: about 430.
- Enemy composition: 4 Infantry, 2 Cavalry, 1 Musket.
- Deployment zone: bottom bank `y=1..4`.
- Layout: river across `y=9..10`, single bridge at `x=8..11`.
- POIs: none, keep the lesson clean.
- Enemy plan: Cavalry tries to cross bridge first, Infantry follows, Musket supports from far bank.
- Scripted behaviour: AI choke point at bridge centre `(9,9)`.
- Required code/data: none once Pikemen are placeable.

### 12. Whiteford Town

- Player budget: 700.
- Enemy strength: about 480.
- Enemy composition: 3 Infantry, 2 Muskets, 1 Cavalry, 1 Pikemen.
- Deployment zone: `y=1..4`, `x=2..17`.
- Layout: village blocks at `(5,8)`, `(9,10)`, `(13,8)`, forest patch `(2..5,12..16)`.
- POIs: medic building at `(6,8)`, small strategic building at `(12,8)`.
- Enemy plan: AI contests town POIs and holds one musket lane.
- Scripted behaviour: `EnemyTacticalCommander.captureBuildings=true`.
- Required code/data: none.

### 13. Ashen Keep

- Player budget: 725.
- Enemy strength: about 520.
- Enemy composition: 4 Infantry, 2 Muskets, 2 Pikemen.
- Deployment zone: lower field `y=1..4`.
- Layout: keep at `(9,13)` with walls at `(6..12,12)` except two gaps.
- POIs: keep uses `CapturableBuilding + TeamWideBuildingBuff`, radius 6, stronger values around `1.08`.
- Enemy plan: supported centre. Muskets behind walls, Pikemen guard gaps.
- Scripted behaviour: AI hold choke points at wall gaps.
- Required code/data: none.

### 14. North Orchard

- Player budget: 725.
- Enemy strength: about 520.
- Enemy composition: 3 Cavalry, 3 Infantry, 1 Musket.
- Deployment zone: `y=1..4`, `x=3..16`.
- Layout: orchard forest rows at `(2..6,6..14)` and `(13..17,6..14)`, clear centre.
- POIs: two small buildings at `(6,8)` and `(12,8)`.
- Enemy plan: Cavalry pressure on both flanks; Infantry pushes middle.
- Scripted behaviour: AI side groups start angled toward player musket positions.
- Required code/data: none.

### 15. Rose Banner Fields

- Player budget: 750.
- Enemy strength: about 560.
- Enemy composition: 4 Infantry, 2 Muskets, 1 Bannerman, 1 Cavalry.
- Deployment zone: `y=1..4`.
- Layout: open field with small blocking farm buildings at `(6,9)` and `(12,9)`.
- POIs: no neutral morale POIs; enemy Bannerman is the lesson.
- Enemy plan: Bannerman behind Infantry centre, Muskets behind, Cavalry on one flank.
- Scripted behaviour: none.
- Unlocks: victory unlocks Bannermen.
- Required code/data: enable `Bannermen` from level `16`; add player Bannerman shop support.

### 16. Crownfield Square

- Player budget: 800.
- Enemy strength: about 540.
- Enemy composition: 4 Infantry, 2 Muskets, 1 Cavalry.
- Deployment zone: `y=1..4`, broad.
- Layout: town square at centre with roads in cross shape.
- POIs: medic at `(9,8)`, two small strategic buildings at `(6,10)` and `(12,10)`.
- Enemy plan: balanced army pressures centre. Player learns Bannerman positioning.
- Scripted behaviour: avoid hidden enemies; keep it readable.
- Required code/data: none once Bannerman is placeable.

### 17. Redwater Crossing

- Player budget: 825.
- Enemy strength: about 600.
- Enemy composition: 5 Infantry, 2 Pikemen, 2 Muskets.
- Deployment zone: bottom bank `y=1..4`.
- Layout: river `y=8..11`, one bridge `x=8..11`.
- POIs: small building just below bridge `(9,6)`; enemy small building above `(9,13)`.
- Enemy plan: infantry/pikemen contest bridge, muskets punish clumps.
- Scripted behaviour: AI choke point at bridge.
- Required code/data: none.

### 18. The Two Fords

- Player budget: 850.
- Enemy strength: about 620.
- Enemy composition: 4 Infantry, 2 Muskets, 2 Cavalry, 1 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: river diagonal-ish using blocked water from `(1,9)` to `(18,11)`, crossings at `(5,9)` and `(14,10)`.
- POIs: small building near each ford.
- Enemy plan: split defence, one stronger side with muskets, one weaker side with cavalry counter.
- Scripted behaviour: AI commander can rotate choke points between both fords.
- Required code/data: none.

### 19. Lowbank Village

- Player budget: 875.
- Enemy strength: about 650.
- Enemy composition: 5 Infantry, 2 Muskets, 1 Pikemen, 1 Bannerman.
- Deployment zone: `y=1..4`, `x=2..17`.
- Layout: village around river crossing at `(9,9)`, building clusters at `(6,8)`, `(12,8)`, `(9,12)`.
- POIs: medic at `(7,8)`, strategic building at `(12,8)`.
- Enemy plan: morale-backed centre around upper village.
- Scripted behaviour: AI prioritizes building capture.
- Required code/data: none.

### 20. Banner Road

- Player budget: 900.
- Enemy strength: about 700.
- Enemy composition: 1 Officer, 1 Bannerman, 4 Infantry, 2 Muskets, 1 Cavalry, 1 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: road with alternating building cover, no hills.
- POIs: central keep-lite building `(9,10)` with strong strategic radius.
- Enemy plan: officer-led groups stay organized and attack/capture POI.
- Scripted behaviour: attach `EnemyOfficerCommander` to enemy Officer; `EnemyTacticalCommander.captureBuildings=true`.
- Unlocks: victory unlocks Officers.
- Required code/data: enable `officer` from level `21`; add player Officer shop/support UI hook if not already.

### 21. Kingsway Green

- Player budget: 950.
- Enemy strength: about 680.
- Enemy composition: 5 Infantry, 2 Cavalry, 2 Muskets.
- Deployment zone: `y=1..4`, full.
- Layout: open green split by two small forests `(3..6,7..12)` and `(13..16,7..12)`.
- POIs: two small buildings at `(6,10)`, `(12,10)`.
- Enemy plan: fast multi-front pressure.
- Scripted behaviour: player Officer tutorial; no cannons yet.
- Required code/data: none once Officer is placeable.

### 22. Old Iron Yard

- Player budget: 975.
- Enemy strength: about 700.
- Enemy composition: 5 Infantry, 2 Muskets, 1 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: industrial yard obstacles create three lanes.
- POIs: neutral cannon at `(9,9)`, small strategic building at `(9,12)`.
- Enemy plan: AI tries to crew cannon with infantry-style unit.
- Scripted behaviour: `EnemyTacticalCommander.takeCannons=true`.
- Required code/data: verify `CannonEmplacement` owner/building setup and crew radius.

### 23. Powder Mill

- Player budget: 1000.
- Enemy strength: about 760.
- Enemy composition: 5 Infantry, 3 Muskets, 1 Pikemen, cannon crew.
- Deployment zone: `y=1..4`.
- Layout: cannon lane through centre, walls/buildings at `(6,9)`, `(12,9)`.
- POIs: enemy-owned cannon at `(9,13)`, medic at `(5,8)`.
- Enemy plan: cannon protected by Infantry and Muskets.
- Scripted behaviour: cannon starts usable by AI via owner building or AI-owned capturable.
- Required code/data: none if cannon ownership works.

### 24. Ironwood Road

- Player budget: 1025.
- Enemy strength: about 800.
- Enemy composition: 4 Infantry, 2 Muskets, 2 Pikemen, 1 Cavalry, 1 cannon.
- Deployment zone: `y=1..4`.
- Layout: long straight centre road under cannon LOS; forests on both sides.
- POIs: cannon at `(9,14)`, small buildings in side forests `(4,10)`, `(15,10)`.
- Enemy plan: cannon punishes direct centre push.
- Scripted behaviour: encourage flank movement through forests/buildings.
- Required code/data: none.

### 25. Dragoon Ford

- Player budget: 1050.
- Enemy strength: about 840.
- Enemy composition: 3 Dragoons, 3 Infantry, 2 Muskets, 1 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: river with two crossings, central building at `(9,9)`.
- POIs: neutral strategic building by each crossing.
- Enemy plan: Dragoons rapidly reinforce whichever crossing player attacks, then dismount.
- Scripted behaviour: AI choke points at both fords; Dragoons begin behind centre.
- Unlocks: victory unlocks Dragoons.
- Required code/data: add `Dragoons` bool to `LevelSettingsDatabase`; add player Dragoon shop support.

### 26. Blackstone Gate

- Player budget: 1100.
- Enemy strength: about 780.
- Enemy composition: 5 Infantry, 3 Muskets, 2 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: gate/wall line at `y=11`, openings at `x=5` and `x=13`.
- POIs: buildings just inside gate `(6,12)`, `(12,12)`.
- Enemy plan: static gate defence; player uses Dragoons to rapidly take/hold openings.
- Scripted behaviour: none.
- Required code/data: none once Dragoon is placeable.

### 27. Northgate Bastion

- Player budget: 1125.
- Enemy strength: about 860.
- Enemy composition: 5 Infantry, 3 Muskets, 2 Pikemen, 1 Bannerman.
- Deployment zone: bottom approach `y=1..4`.
- Layout: compact fort with walls around `(5..14,9..16)`, two entrances.
- POIs: medic outside at `(9,7)`, cannon inside at `(9,13)`, keep at `(9,15)`.
- Enemy plan: hold fort, use cannon and morale.
- Scripted behaviour: AI holds entrance choke points.
- Required code/data: none.

### 28. The Foundry Yard

- Player budget: 1125.
- Enemy strength: about 840.
- Enemy composition: 4 Infantry, 2 Cavalry, 2 Muskets, 2 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: wall/building maze with small lanes, no huge open centre.
- POIs: small strategic buildings at `(5,9)`, `(13,9)`, medic at `(9,7)`.
- Enemy plan: pressure multiple lanes. Dragoons are useful reinforcements.
- Scripted behaviour: AI choke points in lanes.
- Required code/data: none.

### 29. Coalbrook Keep

- Player budget: 1175.
- Enemy strength: about 920.
- Enemy composition: 5 Infantry, 4 Muskets, 3 Pikemen, 1 Bannerman.
- Deployment zone: `y=1..4`.
- Layout: keep-backed upper defence with bridge/road approach.
- POIs: keep at `(9,14)`, small building at `(5,10)`, cannon optional at `(13,12)`.
- Enemy plan: strong supported line; player needs combined arms.
- Scripted behaviour: avoid hidden forest complexity here.
- Required code/data: none.

### 30. Mistwood Rifles

- Player budget: 1200.
- Enemy strength: about 900.
- Enemy composition: 4 Skirmishers, 3 Infantry, 2 Muskets, 1 Officer.
- Deployment zone: lower clearing `y=1..4`.
- Layout: heavy forest patches with two open lanes.
- POIs: small building in centre `(9,9)`, medic near player side `(9,6)`.
- Enemy plan: Skirmishers harass from forests while Infantry holds lane.
- Scripted behaviour: hidden Skirmisher starts in both forests.
- Unlocks: victory unlocks Skirmishers.
- Required code/data: enable `Skirmishers` from level `31`; add player Skirmisher shop support.

### 31. Mistwood Ford

- Player budget: 1250.
- Enemy strength: about 900.
- Enemy composition: 3 Skirmishers, 3 Infantry, 2 Muskets, 2 Cavalry.
- Deployment zone: `y=1..4`.
- Layout: forested river crossing with ford at `(6,9)` and bridge at `(13,10)`.
- POIs: small buildings near both crossings.
- Enemy plan: contest forests and crossings.
- Scripted behaviour: player Skirmisher tutorial; give readable forest lanes.
- Required code/data: none once Skirmisher is placeable.

### 32. The Three Roads

- Player budget: 1275.
- Enemy strength: about 960.
- Enemy composition: 5 Infantry, 3 Muskets, 2 Pikemen, 2 Cavalry.
- Deployment zone: `y=1..4`.
- Layout: left forest road, open centre road, right building road.
- POIs: one strategic building per route, centre medic.
- Enemy plan: distributed defence; AI reinforces closest route through commander.
- Scripted behaviour: configure three AI choke points.
- Required code/data: none.

### 33. Crown Road East

- Player budget: 1300.
- Enemy strength: about 1000.
- Enemy composition: 1 Officer, 1 Bannerman, 5 Infantry, 3 Muskets, 2 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: broad road with two side forests and central town anchor.
- POIs: central keep-lite building and medic behind it.
- Enemy plan: support-stacked enemy line. Player uses own Officer/Bannerman tools.
- Scripted behaviour: enemy Officer uses `EnemyOfficerCommander`.
- Required code/data: none.

### 34. Greyfort Approach

- Player budget: 1325.
- Enemy strength: about 1040.
- Enemy composition: 5 Infantry, 3 Muskets, 2 Pikemen, 2 Skirmishers, 1 Cavalry.
- Deployment zone: `y=1..4`.
- Layout: forests and village buildings leading to defended wall line.
- POIs: two outer buildings, one inner keep.
- Enemy plan: skirmishers harass approach; main force holds wall gaps.
- Scripted behaviour: AI choke points at gaps.
- Required code/data: none.

### 35. Emberwick Keep

- Player budget: 1400.
- Enemy strength: about 1150.
- Enemy composition: 1 Officer, 1 Bannerman, 2 Dragoons, 3 Pikemen, 4 Muskets, 5 Infantry, 2 Skirmishers, 1 cannon.
- Deployment zone: `y=1..4`.
- Layout: full toolkit defensive map: walls, forests, central keep, cannon lane.
- POIs: keep `(9,14)`, cannon `(9,12)`, medic `(5,8)`, side building `(14,9)`.
- Enemy plan: combined defence with support targets.
- Scripted behaviour: enemy commander takes cannon/buildings and holds gaps.
- Required code/data: none.

### 36. Dawn at Redwater

- Player budget: 1250.
- Enemy strength: about 920 but aggressive.
- Enemy composition: 4 Infantry, 2 Cavalry, 2 Dragoons, 2 Muskets, 1 Officer.
- Deployment zone: `y=1..4`.
- Layout: fast river remix with one wide bridge and one side ford.
- POIs: one central building only.
- Enemy plan: immediate pressure, short 60-90 second battle.
- Scripted behaviour: AI starts closer than usual around `y=12`.
- Required code/data: none.

### 37. Foxwood Return

- Player budget: 1350.
- Enemy strength: about 1050.
- Enemy composition: 4 Skirmishers, 3 Infantry, 2 Cavalry, 2 Muskets, 1 Bannerman.
- Deployment zone: lower forest edge `y=1..4`.
- Layout: heavy forest finale with three clearings.
- POIs: medic in lower clearing, strategic building in centre clearing.
- Enemy plan: Skirmishers and Cavalry use forests to punish unsupported units.
- Scripted behaviour: hidden enemies, but keep each forest threat localized.
- Required code/data: none.

### 38. King's Orchard

- Player budget: 1400.
- Enemy strength: about 1120.
- Enemy composition: 2 Bannermen, 1 Officer, 6 Infantry, 3 Muskets, 2 Pikemen.
- Deployment zone: `y=1..4`.
- Layout: orchard rows create soft lanes; buildings anchor centre.
- POIs: keep at `(9,13)`, small buildings `(5,9)` and `(13,9)`, medic `(9,7)`.
- Enemy plan: morale-heavy defence with layered support.
- Scripted behaviour: target/isolating support is the main lesson.
- Required code/data: none.

### 39. The Iron Crown

- Player budget: 1500.
- Enemy strength: about 1250.
- Enemy composition: 1 Officer, 1 Bannerman, 2 Dragoons, 3 Pikemen, 5 Muskets, 5 Infantry, 1 cannon.
- Deployment zone: `y=1..4`.
- Layout: siege fort with walls at `y=10..15`, two gates, side forest approach.
- POIs: cannon inside `(9,13)`, keep `(9,16)`, medic outside `(5,7)`.
- Enemy plan: fortress defence. Dragoons counter-attack breached gates.
- Scripted behaviour: AI hold gates and take cannon.
- Required code/data: none.

### 40. The Royal Road

- Player budget: 1600.
- Enemy strength: about 1350.
- Enemy composition: full toolkit: Officer, Bannerman, Dragoons, Skirmishers, Pikemen, Muskets, Infantry, Cavalry, Cannon.
- Deployment zone: `y=1..4`, full width.
- Layout: readable final map with river crossing, forest flanks, central road, keep at top.
- POIs: keep `(9,15)`, cannon `(9,12)`, medic `(6,8)`, small building `(13,9)`.
- Enemy plan: combined-arms final. Do not hide everything; let player read the puzzle.
- Scripted behaviour: AI commander enabled for buildings, cannon, choke points.
- Unlocks: campaign clear.
- Required code/data: persistent progression already handles highest level; add campaign-clear UI only if desired.

### 41. The Last Redoubt

- Player budget: 1800.
- Enemy strength: extreme waves, total about 2200-2600 over time.
- Enemy composition: wave-based full toolkit.
- Deployment zone: defended bottom/centre fort `(4..15,1..8)`.
- Layout: player fort with walls/gates, cannon lanes, medic POI inside, forests at side approaches.
- POIs: player-side cannon `(9,6)`, medic `(6,5)`, keep `(9,4)`, small buildings `(4,7)` and `(14,7)`.
- Enemy plan:
  - Wave 1 at `0:00`: Infantry and Muskets from north.
  - Wave 2 at `0:45`: Cavalry from left/right.
  - Wave 3 at `1:30`: Pikemen and Muskets centre.
  - Wave 4 at `2:15`: Dragoons rush weak lanes.
  - Wave 5 at `3:00`: Skirmishers enter through forests.
  - Wave 6 at `3:45`: Officer and Bannermen main army.
  - Wave 7 at `4:30`: final oversized commander wave.
- Win condition: recommended survival timer at 5:00, plus immediate win if final wave is destroyed.
- Required code/data:
  - Add an enemy wave spawner that can instantiate prefabs at timed spawn points and register units.
  - Add survival/final-wave win condition support. Do not use this for normal campaign levels.
  - Consider a level-specific UI timer.
