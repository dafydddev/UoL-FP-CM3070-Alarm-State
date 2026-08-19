# Alarm State: Procedural Stealth-Action Roguelite

University of London: CM3070 Final Project (Template 6.2: Procedural Dungeon Generation in Roguelike Games). 

A single-player, 2D, top-down stealth-action roguelite built in Unity 6.3 LTS (C#). The player infiltrates a procedurally generated facility, evades GOAP-driven guards, completes mission objectives, and reaches an exit, over a run of 10, 20, or 30 escalating levels.

<img width="1280" height="725" alt="gameplay scene preview" src="https://github.com/user-attachments/assets/3d4572ad-08df-4d82-82d0-f395432a1952" />

## Playable Build

A WebGL build is available on Itch.io (password: `finalproject`): <https://asdasdasduu.itch.io/cm3070-final-project-roguelike-prototype>

## The Loop

1. Pick a difficulty profile, run length, and layout style in the Play menu.
2. Spend banked points in the Shop on items, upgrades, and skins.
3. Per level: infiltrate -> find keycards -> complete the primary objective -> reach an exit.
4. Guards that lose your trail run for an alarm switch, which broadcasts your last seen position to every guard in earshot.
5. Arrests cost hearts. Losing the last one ends the run and all pending points with it.

---

## Signposting for Markers

The most interesting scripts, grouped by system. Paths are relative to [`Assets/Scripts`](Assets/Scripts).

### 1: Procedural Content Generation Pipeline

| Stage | Script | What it does |
|-------|--------|--------------|
| 0 - Orchestration | [`Generation/Facility/FacilityOrchestrator.cs`](Assets/Scripts/Generation/Facility/FacilityOrchestrator.cs) | Runs the pipeline in order and wires the spawners together. **Best entry point for reading the system.** |
| 1 - Mission | [`Graphs/Missions/MissionGenerator.cs`](Assets/Scripts/Graphs/Missions/MissionGenerator.cs) | Seeded RNG picks a mission type and builds a directed acyclic graph of objectives and their dependencies. |
| 2 - Rooms | [`Graphs/Rooms/RoomGraphGenerator.cs`](Assets/Scripts/Graphs/Rooms/RoomGraphGenerator.cs) | Expands the mission graph into a room graph, adding guard posts, extra exits, and locked doors. |
| 3 - Layout | [`Generation/Tiles/TileLayoutGenerator.cs`](Assets/Scripts/Generation/Tiles/TileLayoutGenerator.cs) | Places the rooms on a tile grid, paints walls and floor, and carves doorways between connected rooms. |
| 4 - Exterior | [`Generation/Terrain/ExteriorGenerator.cs`](Assets/Scripts/Generation/Terrain/ExteriorGenerator.cs) | Layered Perlin noise (fractal Brownian motion) that creates an outside terrain, painted around and between the facility. |
| 5 - Population | The spawners (below) | Instantiates the player, guards, items, doors, objectives, and exits. |

Two layout strategies, chosen per run ([`TileLayoutStyle`](Assets/Scripts/Generation/Tiles/TileLayoutStyle.cs)):

- [`Generation/Tiles/SpineLayout.cs`](Assets/Scripts/Generation/Tiles/SpineLayout.cs): lays the entrance-to-primary path out as a straight spine, hanging each room's subtrees to either side by backtracking search.
- [`Generation/Tiles/RandomWalkLayout.cs`](Assets/Scripts/Generation/Tiles/RandomWalkLayout.cs): a seeded self-avoiding random walk with backtracking, so the facility wanders instead of lining up.

Supporting systems:

- [`Generation/Seeds.cs`](Assets/Scripts/Generation/Seeds.cs): a splitmix-style hash derives one RNG stream per subsystem, per level, from a single master seed.
- [`Run/RunDifficulty.cs`](Assets/Scripts/Run/RunDifficulty.cs): the difficulty profile. Each scaled quantity is a min-max band evaluated along an animation curve against run progress.
- [`Run/RunPerformance.cs`](Assets/Scripts/Run/RunPerformance.cs): a rolling window over recent levels, scored on hearts and alarms, which the room graph uses to add a [supply room](Assets/Scripts/Spawners/SupplyRoomSpawner.cs) or a [pressure room](Assets/Scripts/Spawners/PressureRoomSpawner.cs).
- [`Spawners/`](Assets/Scripts/Spawners): places the player, guards, keycards, doors, objectives, exits, cover and distractions in the generated rooms.

### 2: Guard AI (GOAP + A*)

- [`Guards/GuardAgent.cs`](Assets/Scripts/Guards/GuardAgent.cs): the guard brain. Senses the world, picks the highest-priority applicable goal, plans, and executes, replanning when the goal changes or an action fails.
- [`Guards/Actions/`](Assets/Scripts/Guards/Actions): the actions a guard plans with: patrol, investigate, chase, arrest, and raise the alarm.
- [`Guards/GOAP/GoapPlanner.cs`](Assets/Scripts/Guards/GOAP/GoapPlanner.cs): uniform-cost forward search over the actions' preconditions and effects.
- [`Guards/GuardMemory.cs`](Assets/Scripts/Guards/GuardMemory.cs): what a guard has seen, and the strongest lead it is currently following.
- [`Guards/GuardSenses.cs`](Assets/Scripts/Guards/GuardSenses.cs): view range, facing cone, and hearing, with Bresenham line of sight over the tile grid.
- [`Pathfinding/AStarPathfinder.cs`](Assets/Scripts/Pathfinding/AStarPathfinder.cs): 4-connected A* with a Manhattan heuristic and a binary min-heap frontier.
- [`Guards/GridMotor.cs`](Assets/Scripts/Guards/GridMotor.cs): steps a guard one cell per `N` ticks along its A* route.
- [`Guards/PatrolRouteDeriver.cs`](Assets/Scripts/Guards/PatrolRouteDeriver.cs): derives patrol routes from the room graph's adjacency.
- [`Guards/GuardVisionField.cs`](Assets/Scripts/Guards/GuardVisionField.cs): draws the guards' vision cones in the game view.

### 3: Simulation Core

- [`Simulation/SimulationClock.cs`](Assets/Scripts/Simulation/SimulationClock.cs): a fixed-timestep loop, with an interpolation factor actors render against.
- [`Simulation/Scheduler.cs`](Assets/Scripts/Simulation/Scheduler.cs) and [`Actor.cs`](Assets/Scripts/Simulation/Actor.cs): the tick loop, and the base class for the player and guards it advances.
- [`Simulation/EntryRules.cs`](Assets/Scripts/Simulation/EntryRules.cs): the single authority on whether a cell can be entered, which the pathfinder queries as its walkability test.
- [`Simulation/WorldContext.cs`](Assets/Scripts/Simulation/WorldContext.cs): everything a spawned entity needs from the level it lives in.
- [`Simulation/AlarmState.cs`](Assets/Scripts/Simulation/AlarmState.cs): the facility alarm, its broadcast radius, and the switches that raise and disable it.
- [`Simulation/GameLock.cs`](Assets/Scripts/Simulation/GameLock.cs): stops the simulation while a menu, level transition, or mini game is open.

### 4: Gameplay

- [`Player/PlayerActor.cs`](Assets/Scripts/Player/PlayerActor.cs): grid movement and input, keyboard and click-to-move, on the same tick as the guards.
- [`Player/PlayerInventory.cs`](Assets/Scripts/Player/PlayerInventory.cs): what the player is carrying (e.g. lock pick, health pack, etc.), and the item in the use slot.
- [`Player/PlayerHiding.cs`](Assets/Scripts/Player/PlayerHiding.cs): cover, which hides the player from guards that are not already chasing them.
- [`Player/PlayerDisguise.cs`](Assets/Scripts/Player/PlayerDisguise.cs): a timed disguise the guards cannot see through.
- [`Entities/Objectives/`](Assets/Scripts/Entities/Objectives): the primary and secondary objectives, each with a seeded mini game, and the rewards they leave behind.
- [`Mini Games/`](Assets/Scripts/Mini%20Games): the two mini games. One has the player rotate tiles until a circuit connects, the other has them repeat a sequence of key presses.
- [`Entities/`](Assets/Scripts/Entities): doors, keycards, items, cover, lasers, alarm switches, and exits.
- [`HUD/`](Assets/Scripts/HUD): objectives, hearts, keycards, inventory, alarm, and the minimap.

### 5: Run Structure & Meta-Progression

- [`Run/RunController.cs`](Assets/Scripts/Run/RunController.cs): builds each level and advances the run as the player exits.
- [`Run/RunContext.cs`](Assets/Scripts/Run/RunContext.cs): the current run's difficulty, length, progress, and points.
- [`Menu/ShopMenu.cs`](Assets/Scripts/Menu/ShopMenu.cs), [`PlayMenu.cs`](Assets/Scripts/Menu/PlayMenu.cs), [`ResultsController.cs`](Assets/Scripts/Menu/ResultsController.cs): the shop, run setup, and results screen.
- [`Settings/SaveSystem.cs`](Assets/Scripts/Settings/SaveSystem.cs): the player's banked points, owned items, upgrades, skins, and settings, saved as JSON under `persistentDataPath`.
- [`Analytics/Telemetry.cs`](Assets/Scripts/Analytics/Telemetry.cs): sends the run's progress to Unity Analytics.
- [`Effects/BlueprintSchematic.cs`](Assets/Scripts/Effects/BlueprintSchematic.cs): the generated floor-plan that drifts behind the menu.

## Verification & Tooling

### Editor tools

| Tool | Script | What it does |
|------|--------|--------------|
| Tools -> Mission Graph Editor | [`Editor/GraphEditorWindow.cs`](Assets/Editor/GraphEditorWindow.cs) | Generates a mission and room graph from any settings, and draws both as interactive diagrams. |
| Tools -> Layout Audit | [`Editor/LayoutAudit.cs`](Assets/Editor/LayoutAudit.cs), [`LayoutAuditWindow.cs`](Assets/Editor/LayoutAuditWindow.cs) | Runs the generation pipeline over hundreds of seeds, levels, difficulty profiles and layout styles, and reports structural violations: over-budget doors, unreachable rooms, stacked cells, and non-adjacent doored pairs. |

### Unit tests

[`Assets/Editor/Tests`](Assets/Editor/Tests), run via Window -> General -> Test Runner:

- **Generation:** mission graph shape, room graph key placement / guard posts / adaptive rooms / pressure rooms, tile layout traversability, laser grid layout, seed derivation, shuffle fairness.
- **AI:** GOAP planner, guard memory.
- **Systems:** A* pathfinding, alarm state, GOAP world state, pipe mini game generation, run performance scoring.

## Project Structure

```
Assets/
├── Scripts/
│   ├── Generation/            # Facility orchestration, tile layout strategies, terrain, lasers, seeding
│   ├── Graphs/                # Mission graph and room graph generators
│   ├── Simulation/            # Tick scheduler, clock, entry rules, occupancy, alarm, world context
│   ├── Guards/                # Senses, memory, grid motor, patrol routes, GOAP system
│   ├── Pathfinding/           # A*, navigator, debug drawing
│   ├── Player/                # Actor, inventory, hiding, disguise, health, keyring, skins
│   ├── Entities/              # Doors, keycards, objectives, items, cover, lasers, alarm switches, exits
│   ├── Mini Games/            # Pipe circuit and key sequence mini games
│   ├── Spawners/              # Level population
│   ├── Run/                   # Run context, difficulty profiles, performance tracking, loadout
│   ├── HUD/ Menu/             # In-game HUD (e.g. minimap) and menus (play, shop, pause, results, etc.)
│   ├── Settings/              # Save system, audio/resolution/binding/upgrade/currency settings
│   └── Audio/ Camera/ Effects/ Analytics/
└── Editor/
    ├── GraphEditorWindow.cs   # Mission + room graph visualiser
    ├── LayoutAudit*.cs        # Multi-thousand-seed generation audit
    └── Tests/                 # NUnit test suite
```

## Technology and Tools

- **Engine:** Unity 6.3 LTS (`6000.3.19f1`)
- **Language:** C#
- **Input:** Unity Input System (keyboard, mouse, gamepad)
- **Testing:** Unity Test Framework
- **Analytics:** Unity Services Analytics
- **Version control:** Git + Git LFS (see [`.gitattributes`](.gitattributes))
- **Target:** Windows 11 (primary), WebGL (used for remote user testing)

---

## Asset Attribution

### Sprites & Tiles

**1-Bit Pack, by Kenney**
- Facility tiles, entities, water sprites and shore tiles.
- <https://kenney.nl/assets/1-bit-pack>
- Licence: Creative Commons CC0 1.0, <https://creativecommons.org/publicdomain/zero/1.0/>

**Input Prompts, by Kenney**
- <https://kenney.nl/assets/input-prompts>
- Licence: Creative Commons CC0 1.0, <https://creativecommons.org/publicdomain/zero/1.0/>

**Cursor Pixel Pack, by Kenney**
- <https://kenney.nl/assets/cursor-pixel-pack>
- Licence: Creative Commons CC0 1.0, <https://creativecommons.org/publicdomain/zero/1.0/>

### Fonts

**BoldPixels, by Yuki Pixels**
- <https://yukipixels.itch.io/boldpixels>
- Licence: Creative Commons Attribution-ShareAlike 4.0, <https://creativecommons.org/licenses/by-sa/4.0/deed.en>

### Audio

**Abstraction: Music Loop Bundle, by Tallbeard Studios**
- <https://tallbeard.itch.io/music-loop-bundle>
- Licence: Creative Commons CC0 1.0, <https://creativecommons.org/publicdomain/zero/1.0/>

**Interface Sounds, by Kenney**
- <https://kenney.nl/assets/interface-sounds>
- Licence: Creative Commons CC0 1.0, <https://creativecommons.org/publicdomain/zero/1.0/>

### Project Configuration

**Git Ignore for Unity**
- Pre-made `.gitignore` for Unity projects.
- <https://github.com/github/gitignore/blob/main/Unity.gitignore>
- Licence: Creative Commons CC0 1.0, <https://github.com/github/gitignore/blob/main/LICENSE>

**Git Attributes (LFS) for Unity**
- Pre-made `.gitattributes` for Unity.
- <https://github.com/FrankNine/RepoConfig/blob/master/.gitattributes>
- Licence: MIT, <https://github.com/FrankNine/RepoConfig?tab=MIT>
