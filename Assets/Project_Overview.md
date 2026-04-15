# Project Overview: Match-3 RPG Prototype

## 1. Project Description
This project is a 2D Match-3 RPG prototype where players clear blocks on a grid to trigger combat actions. The game blends puzzle mechanics with turn-based combat, featuring a party of three heroes (Fighter, Mage, Tank) defending against waves of monsters. Key features include a physics-based block falling system, combo-based power scaling, and a unified combat queue that handles both player and enemy actions with synchronized visual/audio feedback.

## 2. Gameplay Flow / User Loop
1.  **Boot & Title**: The user starts in `Title.unity`, where the `TitleScreenController` manages the UI.
2.  **Persistent Setup**: The `PersistentSystemsLoader` ensures that the `AudioManager` and other global systems are instantiated across scenes.
3.  **Core Loop (Combat/Puzzle)**: 
    *   The player clicks blocks on the bottom row of a 7x7 grid in `Main.unity`.
    *   `GridManager` processes the removal, gravity, and refill, detecting matches.
    *   Matches generate `CombatAction` events (Attack, Magic, Heal, Shield, etc.).
    *   `CombatManager` queues these actions, playing animations for the heroes and applying effects to enemies.
    *   Enemies automatically queue attacks based on internal timers.
4.  **Wave Progression**: When all enemies are defeated, a new wave is spawned. 
5.  **Game Over**: If the party's HP reaches zero, the stats are reset, and a new wave begins (prototype loop).

## 3. Architecture
The project follows a **Manager-driven Architecture** with a centralized **Event Queue** for combat synchronization.

*   **Entry Point**: The `Main` scene contains the `GridManager` and `CombatManager`, which drive the simulation.
*   **System Interaction**: 
    *   `GridManager` -> `CombatManager`: Sends matched block data to be converted into actions.
    *   `EnemyUnit` -> `CombatManager`: Queues enemy attacks.
    *   `CombatManager` -> `CharacterVisuals`/`Animator`: Controls all unit feedback.
*   **Design Patterns**:
    *   **Singleton**: Used by `CombatManager` and `AudioManager` for global access.
    *   **Command/Queue**: The `CombatAction` class and `eventQueue` in `CombatManager` ensure that animations and damage numbers don't overlap chaotically.
    *   **Observer**: `GridManager.OnBottomRowClicked` allows external systems to react to player input.

Location: `Assets/Scripts`

## 4. Game Systems & Domain Concepts

### Match-3 Puzzle System
*   `GridManager`: Manages the 7x7 logic grid, block types (Sword, Shield, Magic, etc.), and "Ska" (empty/gray) blocks.
*   `BlockType`: Enum defining the affinity of each block (e.g., `Sword` = Physical Attack, `Magic` = AOE Attack).
*   **Extension**: Add new block types to the `BlockType` enum and update the `CombatManager.AddPlayerAction` switch case.
Location: `Assets/Scripts/GridManager.cs`

### Combat & Action System
*   `CombatManager`: The central arbiter. It processes a `LinkedList<CombatAction>` to execute player and enemy turns sequentially.
*   `CombatAction`: A data class carrying the action type, value, and source.
*   **Extension**: Create new `CombatActionType` values to add mechanics like "Stun" or "Buffs."
Location: `Assets/Scripts/CombatManager.cs`

### Unit & Visuals System
*   `EnemyUnit`: Handles individual enemy AI (timer-based attacks) and health.
*   `CharacterVisuals`: A reusable component for sprite flashing and screen-space shaking using `MaterialPropertyBlocks`.
*   **Extension**: Inherit from `EnemyUnit` to create specialized boss logic.
Location: `Assets/Scripts/EnemyUnit.cs`, `Assets/Scripts/CharacterVisuals.cs`

## 5. Scene Overview
*   `Title`: Contains the `TitleScreenController` and UITK-based main menu.
*   `Main`: The primary gameplay scene containing the puzzle grid, combat arena, and URP 2D lighting setup.
*   `PersistentSystems`: A bootstrap scene (often loaded additively or via `PersistentSystemsLoader`) containing the `AudioManager`.

## 6. UI System
The project uses **UI Toolkit (UITK)** for its interface.
*   **Structure**: `Main.uxml` and `Title.uxml` define the layout, styled by `.uss` files.
*   **Binding**: `CombatManager` queries the `UIDocument` in `Awake` using `rootVisualElement.Q<T>()` to bind HP bars, labels, and the notification layer.
*   **Dynamic UI**: `CombatManager.ShowCombatNumber` and `ShowActionNotification` instantiate labels at runtime, converting world positions to panel coordinates using `RuntimePanelUtils.CameraTransformWorldToPanel`.
Location: `Assets/UI`

## 7. Asset & Data Model
*   **Prefabs**: Characters and FX are prefab-based for easy spawning (e.g., `MatchDestroyFX`).
*   **Scriptable Components**: `SEClip` structs in `AudioManager` organize audio data.
*   **Materials**: Uses custom shaders (`SpriteOverlay.shader`) for flash effects, controlled via `CharacterVisuals`.
*   **Resources**: The `EnemyOverlay` material is loaded from `Resources/` to ensure it's available for `CharacterVisuals` at runtime.

## 8. Notes, Caveats & Gotchas
*   **Input Constraint**: In `GridManager`, interaction is hardcoded to only detect clicks on the bottom row (`y=0`).
*   **Ska Blocks**: These blocks act as "dead weight" that can only be cleared by adjacent matches, adding a layer of strategy.
*   **Animation Synchronization**: `CombatManager` uses `WaitForAnimation` coroutines that poll `AnimatorStateInfo`. If animation state names are changed in the `AnimatorController`, these strings must be updated in `CombatManager.cs`.
*   **Z-Axis**: Being a 2D URP project, ensure `Sorting Layers` are used correctly; the grid sits at `SortingOrder 0-1`, while FX usually occupy higher orders.