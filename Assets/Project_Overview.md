# Project Overview: RPG Match-3 Battle System

## 1. Project Description
This project is a high-polish RPG Match-3 hybrid built in Unity 6, designed for WebGL. It combines classic grid-clearing mechanics with turn-based combat systems. Players manage a party of three heroes (Fighter, Mage, Tank) by matching blocks on a 7x7 grid to trigger attacks, spells, heals, and defensive maneuvers. The core experience is defined by a "Tactical Match" loop where players must balance offensive matches with defensive "Junk-Cleansing" (removing dead blocks) to survive escalating waves of enemies.

## 2. Gameplay Flow / User Loop
1.  **Title Screen**: The entry point where the user starts the session.
2.  **Main Game Loop**: 
    *   **Matching Phase**: The player interacts with the 7x7 grid. Clicking blocks on the bottom row triggers gravity and match-detection.
    *   **Action Queueing**: Matches generate `CombatAction` objects which are queued in the `CombatManager`.
    *   **Combat Resolution**: The `CombatManager` processes actions sequentially, triggering animations for players and enemies.
    *   **Progression**: Clearing waves grants EXP and occasionally triggers Treasure Chest events.
3.  **Game Over**: Triggered when Player HP reaches zero. Displays final level, waves cleared, and provides an option to restart.

## 3. Architecture
The project follows a **Manager-Centric Pattern** with a heavy emphasis on **Event-Driven UI** and **Command-style Action Queueing**.

*   **Global Access**: `CombatManager` and `AudioManager` utilize the Singleton pattern for easy cross-system access.
*   **Decoupled Logic**: The `GridManager` handles the match-3 logic independently of combat, communicating via the `AddPlayerAction` method in `CombatManager`.
*   **Action Queue**: All combat events (player attacks, enemy actions, heals) are wrapped into `CombatAction` objects and processed through a `LinkedList` queue to ensure visual synchronization and prevent animation overlapping.
*   **Data-Driven Balance**: Combat stats and enemy definitions are stored in `GameBalanceData` (ScriptableObject), allowing for tuning without code changes.

`Location: Assets/Scripts`

## 4. Game Systems & Domain Concepts

### Match-3 & Grid System
Manages a 7x7 grid using a logical array and physical `SpriteRenderer` objects. It supports gravity, refilling, and cluster-based match detection.
*   `GridManager`: The core controller for matching, gravity, and "Junk" (dead block) mechanics.
*   `RowHighlighter`: Provides visual feedback for valid interaction zones (bottom row).
*   `Junk Blocks`: Unique gray blocks that cannot be matched normally; they must be cleared by proximity to other matches.
`Location: Assets/Scripts`

### Combat & Party System
A turn-based execution layer that translates grid matches into RPG actions.
*   `CombatManager`: Orchestrates the flow of battle, wave spawning, and action processing.
*   `EnemyUnit`: Individual AI entities that track their own attack timers and health.
*   `CharacterVisuals`: A dedicated component for handling sprite-based effects like flashes, shakes, and persistent silhouette states.
`Location: Assets/Scripts`

### Progression System
Handles character growth and rewards.
*   `GameResults`: A static container that persists end-of-game stats between scene transitions.
*   `GameBalanceData`: Defines level thresholds, HP scaling, and enemy difficulty budget per wave.
`Location: Assets/Scripts`

## 5. Scene Overview
*   `PersistentSystems`: Contains the `AudioManager` and `SceneTransitionController`. Loaded once and remains across all other scenes.
*   `Title`: The initial entry point with a UITK-based menu.
*   `Main`: The primary gameplay scene containing the 7x7 grid, hero party, and enemy spawn points.
*   `GameOver`: Summary screen that displays performance metrics and transitions back to `Title`.

## 6. UI System
The project uses **UI Toolkit (UITK)** for all menus and HUD elements, styled with USS.
*   **HUD (`Main.uxml`)**: Displays dynamic health/shield bars, EXP labels, and a notification layer for floating combat text.
*   **Binding Logic**: `CombatManager` queries the `VisualElement` tree via `Q<T>` and updates styles (width percentages) or text content based on internal state.
*   **Screen Flow**: `SceneTransitionController` handles scene loading, while specific controllers like `TitleScreenController` and `GameOverScreenController` manage UI-specific interactions.
*   **Extension**: To add a new screen, create a `.uxml` and `.uss` pair and a corresponding Controller script to handle button callbacks and state updates.

`Location: Assets/UI`

## 7. Asset & Data Model
*   **ScriptableObjects**: `GameBalanceData` is the primary source of truth for all numerical balancing, including `EnemyDefinition` structs.
*   **Prefabs**: Enemies are prefabs (e.g., Skeleton, Slime) containing an `Animator` and `EnemyUnit` script.
*   **Materials/Shaders**: Custom shaders (`SpriteOverlay.shader`) are used for hit-flashes and status effects, accessed via `MaterialPropertyBlock` for performance.
*   **Resources**: Critical shared materials like `EnemyOverlay` are located in `Assets/Resources` for runtime loading by `CharacterVisuals`.

## 8. Notes, Caveats & Gotchas
*   **Bottom Row Only**: Players can only click the bottom row (y=0) to trigger clearing; this is a design constraint enforced in `GridManager.HandleClick`.
*   **Queue Priority**: Player actions are inserted at the *head* of the `CombatAction` queue (`AddFirst`) to ensure immediate responsiveness even if enemy actions were previously queued.
*   **Animation Synchronization**: The `CombatManager` uses a `WaitForAnimation` coroutine helper that relies on specific animator state names ("Attack", "Magic"). Renaming animator states will break combat timing.
*   **Junk-Cleansing**: Cleansing a "Junk" block (matching next to it) triggers a unique flag in the refill logic to prevent immediate Junk re-generation, rewarding tactical play.