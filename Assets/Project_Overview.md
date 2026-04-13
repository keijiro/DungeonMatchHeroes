This technical documentation provides a comprehensive overview of the Unity project, covering its core mechanics, architecture, and systems.

# 1. Project Description
This project is a **Match-3 RPG Prototype** built with Unity 6 and URP. It combines grid-based puzzle mechanics with turn-based combat. Players interact with a 7x7 grid to perform actions like attacking, healing, and shielding to defeat waves of enemies. The project features a stylized 2D aesthetic with visual feedback powered by custom URP features and an event-driven combat system.

# 2. Gameplay Flow / User Loop
1.  **Initialization**: The `Main.unity` scene loads, initializing the `GridManager` and `CombatManager`. A wave of enemies is spawned.
2.  **Player Input**: The player clicks on blocks in the **bottom row** of the 7x7 grid.
3.  **Grid Resolution**:
    *   The clicked block is destroyed, triggering gravity and refilling the grid.
    *   Falling blocks create matches (3 or more in a row/column).
    *   Matching "Ska" (gray) blocks provides no effect but clears space.
    *   Matching functional blocks (Sword, Shield, Magic, etc.) adds actions to the `CombatManager` queue.
4.  **Combat Execution**: 
    *   The `CombatManager` processes the action queue sequentially.
    *   Player actions (Attack, Heal, etc.) trigger character animations and affect enemy stats.
    *   Enemies attack on independent timers, adding their actions to the end of the queue.
5.  **Progression/Reset**: Defeating all enemies spawns a new wave. If the player's HP reaches zero, the prototype resets party stats and starts a new wave.

# 3. Architecture
The project follows a **Manager-Pattern** combined with an **Asynchronous Event Queue** for combat.

### Core Managers
*   `GridManager`: Owns the puzzle logic, grid state, and input detection. It communicates to the `CombatManager` via method calls when matches occur.
*   `CombatManager`: A Singleton that manages party stats, enemy lifecycle, and the combat execution queue.

### Design Patterns
*   **Singleton**: Used by `CombatManager` for easy access from the grid and enemy units.
*   **Command/Queue**: Combat actions are encapsulated in `CombatAction` objects and processed via a `LinkedList` queue to ensure animations and logic stay synchronized.
*   **Coroutine-based Sequencing**: Extensive use of Coroutines for grid gravity, matching animations, and combat timing.

`Location: Assets/Scripts`

# 4. Game Systems & Domain Concepts

### Puzzle System
*   `GridManager`: Handles a 7x7 grid using a 2D array of `BlockType`.
*   **Weighted Randomness**: Block spawning is governed by `typeWeights` (e.g., Swords are common, Keys are rare).
*   **Ska Mechanic**: "Ska" blocks are filler blocks that appear more frequently during manual destruction to challenge the player's matching strategy.

### Combat System
*   `EnemyUnit`: Individual AI components that use simple timers to "queue" attacks.
*   **Damage Logic**: Standard attacks are blocked by `Shield` stats; Magic attacks bypass shields and hit HP directly.
*   **AOE Logic**: Magic blocks trigger area-of-effect damage to all active enemies.

### Visual Feedback System
*   `CameraShake`: Provides haptic-like visual feedback on block destruction and landing.
*   **Notification Layer**: A dynamic UI system that spawns floating text at world-to-screen coordinates when matches occur.

# 5. Scene Overview
The project currently utilizes a single main scene:
*   `Main.unity`: Contains the `GridManager`, `CombatManager`, `Main Camera` (with URP 2D Renderer), and the `UI` document.
*   **Flow Constraints**: There is no level transition system; the game loop persists within this scene via infinite enemy wave spawning.

# 6. UI System
The project uses **UI Toolkit (UITK)** for its interface.

### UI Structure
*   `Main.uxml`: Defines the layout, including the `hud-bottom` for stats and a `notification-layer` for floating text.
*   `Main.uss`: Handles styling, utilizing a "Pirata One" font for a stylized look.

### Binding & Logic
*   The `CombatManager` manually queries the `UIDocument` in `SetupUI()` using `Q<T>` queries.
*   **Dynamic Notifications**: Floating labels are created programmatically, added to the `notification-layer`, and animated via Coroutines that interpolate `translate` and `opacity` properties.

`Location: Assets/UI`

# 7. Asset & Data Model
*   **Prefabs**: Enemies (Fighter, Mage, Tank) and FX (Shockwaves, Particles) are stored as prefabs for dynamic instantiation.
*   **Sprites**: Organized by category (Blocks, Characters, FX, Backgrounds).
*   **Rendering**: Uses URP with a custom `2D Renderer`. Includes a `EightColorFeature` for post-processing/palette effects.
*   **Animations**: Character actions (Idle, Attack) are driven by `AnimatorControllers` using `Triggers`.

`Location: Assets/Prefabs`, `Assets/Sprites`, `Assets/URP`

# 8. Notes, Caveats & Gotchas
*   **Interaction Rule**: Players can ONLY click blocks in the bottom row (`y = 0`). This is a hardcoded constraint in `GridManager.HandleClick`.
*   **Ska Logic**: The `manualSkaRate` is set to 1.0 by default, meaning manual clicks almost always produce a gray "Ska" block to fill the gap, forcing players to rely on gravity for matches.
*   **Queue Priority**: The `CombatManager` uses `AddFirst` for player actions and `AddLast` for enemy actions, effectively giving the player's immediate matches priority over the enemy's scheduled attacks.
*   **Zombie Mage**: Enemies named "ZombieMage" automatically enable `IsMagic`, causing their attacks to ignore the player's Shield stat.