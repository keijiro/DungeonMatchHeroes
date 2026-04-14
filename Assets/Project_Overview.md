# Project Overview: Match-3 Dungeon Crawler Prototype

## 1. Project Description
This project is a 2D match-3 dungeon crawler prototype where puzzle mechanics directly drive turn-based combat. Players interact with a 7x7 grid to perform actions like attacking, healing, and shielding to defeat waves of monsters. The core experience centers on strategic block management—specifically the "Ska" (gray) blocks which serve as both a penalty for manual destruction and a combo multiplier.

## 2. Gameplay Flow / User Loop
1.  **Boot/Initialization**: The `Main` scene loads, initializing the `GridManager` and spawning the first wave of enemies via `CombatManager`.
2.  **Input Phase**: The player clicks on blocks in the bottom row of the 7x7 grid to manually destroy them.
3.  **Puzzle Resolution**:
    *   Manual destruction turns the clicked block into a "Ska" (dead) block.
    *   Gravity pulls blocks down; new blocks (often Ska) refill from the top.
    *   Matching 3+ blocks triggers a combat action and consumes adjacent Ska blocks to boost power.
4.  **Combat Execution**: Triggered matches are queued as `CombatAction` objects. The `CombatManager` processes this queue, triggering character animations, visual effects, and damage calculations.
5.  **Enemy Turn**: Enemies have individual timers (`AttackInterval`) that automatically add attack actions to the combat queue.
6.  **Progression/Loop**: Defeating all enemies triggers a new wave spawn. If the party's HP hits zero, the game resets state for the prototype loop.

## 3. Architecture
The project follows a manager-driven architecture with a centralized event queue for combat to ensure animations and effects don't overlap.

### Combat Management
*   `CombatManager`: A Singleton that manages the party stats (HP, Shield, EXP), handles the combat queue, and coordinates animations between players and enemies.
*   `CombatAction`: A data class representing a pending action (Type, Value, Source).
*   **Design Pattern**: Command Pattern (Queue-based). Actions are added to a `LinkedList<CombatAction>` and processed sequentially via a Coroutine-based `QueueProcessor`.
`Location: Assets/Scripts/CombatManager.cs`

### Grid & Match Logic
*   `GridManager`: Controls the 7x7 board, handles Unity Input System events, and implements gravity and matching logic.
*   **Design Pattern**: Observer-lite. It directly calls `CombatManager.Instance.AddPlayerAction` when matches are detected.
`Location: Assets/Scripts/GridManager.cs`

### Visual Feedback System
*   `CharacterVisuals`: Handles sprite-based effects like flashing (via `MaterialPropertyBlock`) and screen shake.
*   `CameraShake`: A simple utility for camera-based impact feedback.
`Location: Assets/Scripts/CharacterVisuals.cs`, `Assets/Scripts/CameraShake.cs`

## 4. Game Systems & Domain Concepts

### Match-3 Combat System
Matches of different block types correspond to specific character actions:
*   `Sword`: Fighter Attack (Single Target)
*   `Magic`: Mage Attack (AOE)
*   `Heal`: Party HP Restore
*   `Shield`: Tank Shield (Blocks physical damage)
*   `Gem/Key`: Experience and progression bonuses
*   `Ska`: Dead blocks that do nothing alone but increase the `matchCount` when adjacent to a valid match.

### Enemy AI
*   `EnemyUnit`: Simple timer-based AI. Each enemy instance tracks its own attack cooldown and adds `EnemyAttack` actions to the `CombatManager` queue.
*   `ZombieMage` (Variant): Uses a specific `IsMagic` flag to bypass player shields.
`Location: Assets/Scripts/EnemyUnit.cs`

### Audio System
*   `AudioManager`: Centralized SE management with support for pitch randomization to prevent fatigue during rapid matches.
`Location: Assets/Scripts/AudioManager.cs`

## 5. Scene Overview
*   **Main**: The primary gameplay scene. It contains the `GridManager`, `CombatManager`, `MainCamera` (with shake component), and the `UI` root.
*   **Scene Flow**: Currently a single-scene loop. `CombatManager.SpawnWave()` handles the transition between combat encounters by resetting enemy lists and spawning new prefabs at designated `EnemySpawnPoints`.

## 6. UI System
The project uses **UI Toolkit (UITK)** for its interface.
*   `Main.uxml`: Defines the layout, including the HP bar, Shield/EXP labels, and a `notification-layer`.
*   `Main.uss`: Handles the styling and the "notification-label" class used for floating combat text.
*   **Binding**: `CombatManager` manually queries the `VisualElement` tree in `Awake()` and updates values in `UpdateUI()`.
*   **Floating Text**: Generated dynamically via `RuntimePanelUtils.CameraTransformWorldToPanel` to convert grid world positions to UI screen space.
`Location: Assets/UI/`

## 7. Asset & Data Model
*   **Prefabs**:
    *   `Characters/Monster/`: Contains prefabs for Golem, Orc, Skeleton, Slime, and ZombieMage.
    *   `FX/`: Contains `ShockwaveFX` and `MatchDestroyFX` particle systems.
*   **Scriptable Content**: The project relies on inspector-assigned arrays in `CombatManager` and `GridManager` for enemy pools and block weights.
*   **Shaders**: Uses a custom `SpriteOverlay.shader` to allow the `CharacterVisuals` script to perform color flashes without creating new material instances.
`Location: Assets/Prefabs/`, `Assets/Shaders/`

## 8. Notes, Caveats & Gotchas
*   **Ska Block Mechanics**: Manual clicks on the bottom row *always* create a Ska block (controlled by `manualSkaRate`). This is a design choice to force combos rather than simple single-clicking.
*   **Animation Synchronization**: `CombatManager` uses a helper `WaitForAnimation` coroutine. If you add new animations, ensure the `Animator` state names match the strings passed in `CombatManager.ExecuteAction`.
*   **Layering**: Sprite sorting is critical; Icons are on Order 1, while Block Bases are on Order 0.
*   **UI Resolution**: The floating notification labels depend on the `notification-layer` being the full size of the screen to map world-to-panel coordinates correctly.