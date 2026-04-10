# Project Overview: 3-Match RPG Prototype

This project is a technical prototype for a hybrid 3-match puzzle and RPG game. It focuses on validating a unique "bottom-row only" interaction mechanic combined with a cluster-based chain reaction system and a "Ska" (dud) block penalty/bonus loop.

## 1. Project Description
The prototype implements a 7x7 grid-based puzzle where the player's influence is restricted to the bottom row. The core experience centers on strategic destruction to trigger cascades. It is designed for PC (Standalone) using Unity 6 and URP's 2D Renderer.
*   **Core Pillars:**
    *   **Bottom-Row Interaction:** Players can only manually destroy blocks on the bottom row (Y=0).
    *   **Cluster Matching:** Beyond standard 3-match lines, matching blocks trigger "cluster" clearing of all adjacent identical types via flood-fill.
    *   **Ska Mechanic:** Manual destructions introduce "Ska" (gray/dud) blocks, while chain reactions produce high-value blocks.
    *   **Chain Reactions:** Gravity-fed cascades are the primary way to score and clear "Ska" blocks via proximity detonation.

## 2. Gameplay Flow / User Loop
1.  **Initialization:** `GridManager` populates a 7x7 grid with weighted random blocks, ensuring no matches exist at the start.
2.  **Player Input:** The player clicks a block in the bottom row (Y=0).
3.  **Manual Destruction:** The clicked block is destroyed. This action is "inefficient" as it triggers a refill with a high probability of "Ska" blocks (determined by `manualSkaRate`).
4.  **Gravity & Refill:** Blocks above fall to fill gaps. New blocks enter from the top.
5.  **Match Detection:** The system checks for horizontal/vertical lines of 3 or more.
6.  **Cluster & Detonation:** 
    *   Identical adjacent blocks are grouped into a "cluster" and cleared.
    *   Any "Ska" blocks adjacent to a clearing cluster are "detonated" and added to that cluster's score.
7.  **Chain Reaction:** If matches occurred, steps 4-6 repeat automatically via coroutines until no matches remain.
8.  **UI Update:** Scores (match counts) for each block type (Sword, Shield, Magic, etc.) are updated in the HUD.

## 3. Architecture
The project uses a centralized manager pattern to handle the grid logic, with the UI decoupled via UI Toolkit (UITK).
*   **Grid Management:** `GridManager` acts as the "Brain," handling state (logical grid), representation (SpriteRenderers), and the game loop (Coroutines).
*   **Input Handling:** Uses the New Input System to perform screen-to-world raycasts, filtered by the Y-coordinate of the hit object.
*   **UI Binding:** The `GridManager` queries the `UIDocument` to update labels by name string, following a simple View-Controller relationship.
*   **Rendering:** Powered by URP 2D Renderer using sprite-based visuals with a layered approach (Base + Icon).

`Location: Assets/Scripts/` (Logic)
`Location: Assets/URP/` (Pipeline)

## 4. Game Systems & Domain Concepts

### Grid & Match System
*   `GridManager`: The core class managing a 2D array of `BlockType` enums and a parallel array of `SpriteRenderer` references.
*   `BlockType`: Enum defining `Sword`, `Shield`, `Magic`, `Heal`, `Gem`, `Key`, and `Ska`.
*   **Matching Logic:** Uses a two-pass approach: first identifying 3-in-a-row triggers, then performing a recursive flood-fill (`FindCluster`) to identify the full connected group.
*   **Extension:** To add new match rules (e.g., T-shapes), modify `CheckAndApplyMatches`. To add new block behaviors, expand the `BlockType` enum and the scoring logic in `CheckAndApplyMatches`.

### Refill & Weight System
*   **Weighted Random:** `GetWeightedRandomType` uses an array of floats to determine spawn probabilities for standard blocks.
*   **Contextual Spawning:** `DecideNewBlockType` distinguishes between "Manual Refill" (high Ska rate) and "Match Refill" (0% Ska rate).
*   `Location: Assets/Scripts/GridManager.cs`

## 5. Scene Overview
*   **Main.unity:** The sole functional scene.
    *   **Camera:** Orthographic, configured for URP 2D.
    *   **UI Object:** Contains `UIDocument` and references the `Main.uxml` and `PanelSettings`.
    *   **GridManager Object:** The root for all dynamically generated block GameObjects. Blocks are instantiated as children of this object at runtime with `BoxCollider2D` for click detection.

## 6. UI System
The project utilizes **UI Toolkit (UITK)** for its HUD.
*   **Structure:** `Main.uxml` defines the layout with specific `Label` names (`SwordCount`, `ShieldCount`, etc.).
*   **Styling:** `Main.uss` handles positioning and visual style.
*   **Logic:** `GridManager.UpdateUI()` finds labels using `rootVisualElement.Q<Label>("Name")` and updates their text property based on the `matchCounts` array.
*   **How to Modify:** Open `Main.uxml` in the UI Builder. Adding a new resource requires adding a Label with a unique name and updating the `matchCounts` array indexing in `GridManager.cs`.

`Location: Assets/UI/`

## 7. Asset & Data Model
*   **Sprites:** Uses a layered approach with `Block_Base.png` for the background and various `Icon_*.png` for the block type indicators.
*   **Configurables:**
    *   `typeWeights`: Inspector-editable array in `GridManager` for balancing drop rates.
    *   `manualSkaRate`: Slider (0-1) in `GridManager` to tune the penalty for manual clicks.
    *   `iconSprites`: Array of sprites mapped to the `BlockType` enum index.
*   **Block Colors:** Hardcoded mapping within `GridManager.GetColor(BlockType)` used to tint the base sprite.

## 8. Notes, Caveats & Gotchas
*   **Bottom Row Restriction:** Input is strictly ignored if the clicked block is not in the bottom row (index 0).
*   **Ska Scoring:** "Ska" blocks do not have their own counter. Instead, they act as score multipliers for whatever cluster triggered their destruction.
*   **Physics Dependency:** Click detection relies on `BoxCollider2D` and `Physics2D.GetRayIntersection`. If blocks are moved or scaled, ensure the colliders remain reachable by the raycast.
*   **Coroutine Safety:** The `isProcessing` flag prevents multiple simultaneous click inputs while the grid is animating or calculating chains.
*   **Coordinate System:** The grid's logical (0,0) is the bottom-left. World positions are calculated centered around the `GridManager`'s transform.