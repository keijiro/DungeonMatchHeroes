This project is a minimal Unity 6 (6000.3.10f1) foundation configured for 2D development using the Universal Render Pipeline (URP). It currently serves as a clean slate or "empty" template with UI Toolkit integrated and a 2D rendering path established.

# 1. Project Description
The project is a lightweight 2D application framework. It is designed for developers targeting high-performance 2D graphics on macOS (and potentially other standalone platforms) using Unity's modern UI and rendering stacks. The core pillars are:
*   **Modern 2D Rendering:** Leveraging URP's 2D Renderer for efficient sprite and lighting calculations.
*   **UI Toolkit Integration:** Utilizing the latest retained-mode UI system for responsive and scalable user interfaces.
*   **Unity 6 Ready:** Built on the latest LTS-adjacent version of Unity to utilize the newest engine features.

# 2. Gameplay Flow / User Loop
As the project is in a foundational state, the user loop is currently basic:
1.  **Boot:** The application launches directly into the `Main.unity` scene.
2.  **Initialization:** The URP 2D Renderer initializes the camera and the `UIDocument` component loads the root `Main.uxml` layout.
3.  **Active State:** The user sees a blank screen (defined by the background color of the Camera) with an empty UI overlay.
4.  **Shutdown:** Standard application exit.

# 3. Architecture
The project follows a component-based architecture typical of Unity, but specifically leans into the **UI Toolkit (UITK)** pattern for interface management and **URP** for the graphics pipeline.

*   **Rendering Pipeline:** The project uses the `UniversalRenderPipelineAsset` configured to use a `Renderer2DData` asset. This shifts the engine from a 3D-first approach to one optimized for 2D sorting and lighting.
*   **UI Management:** The UI is managed via `UIDocument` which acts as the entry point for the Visual Tree (`.uxml`). Data binding and event handling are intended to be handled through C# scripts targeting the `VisualElement` API.

`Location: Assets/URP/` (Rendering Configuration)
`Location: Assets/UI/` (Interface Definition)

# 4. Game Systems & Domain Concepts
### Rendering System
*   `UniversalRenderPipelineAsset`: Global settings for the URP.
*   `Renderer2DData`: Specifically configures the pipeline for 2D sorting layers and 2D lights.
*   `VolumeProfile`: Used for global post-processing effects (Bloom, Color Grading, etc.).
`Location: Assets/URP/`

### UI Framework
*   `PanelSettings`: Defines how the UI is scaled and rendered (DPI, atlas settings).
*   `ThemeStyleSheet`: The global USS (Unity Style Sheet) that defines the visual look and feel of UI controls.
*   `VisualTreeAsset`: The XML-based definition of the UI hierarchy.
`Location: Assets/UI/`

# 5. Scene Overview
The project currently contains a single scene:
*   `Main.unity`: The primary entry point. It contains a `Camera` configured for URP and a `UI` GameObject with a `UIDocument` component.
*   **Scene Flow:** No scene transitions are currently implemented.

# 6. UI System
The project uses **UI Toolkit (UITK)** instead of UGUI.
*   **Structure:** The UI is defined in `Main.uxml`. 
*   **Styling:** Global styles are managed via `DefaultTheme.tss` and internal styles within the UXML.
*   **Binding:** Currently, there are no C# scripts bound to the UI. To extend this, a `MonoBehaviour` should be created to query the `UIDocument.rootVisualElement` and attach event callbacks.
*   **Extension:** New screens should be created as `.uxml` files and either swapped in the `UIDocument` or added as children to the root.

# 7. Asset & Data Model
*   **Layouts:** `Main.uxml` stores the hierarchy of the interface.
*   **Settings:** `DefaultPanel.asset` handles the resolution and scaling logic for the UI.
*   **Rendering:** `URP.asset` and `2D Renderer.asset` define the visual constraints.
*   **Naming Convention:** The project uses standard PascalCase for assets and folders.

# 8. Project Structure
The folder structure is organized by system type:
*   `Assets/UI/`: Contains all UI Toolkit assets including layouts, themes, and panel settings.
*   `Assets/URP/`: Contains all Universal Render Pipeline configuration files.
*   `Assets/`: Root contains the main scene and meta-documentation.

# 9. Notes, Caveats & Gotchas
*   **2D Renderer Only:** The URP asset is strictly set to the 2D Renderer. 3D objects using standard URP shaders may not render correctly unless the renderer is changed or a 3D renderer is added to the list.
*   **Legacy Input:** The project is currently configured to use the **Legacy Input Manager**. If upgrading to the New Input System package, the UI Toolkit event system must be updated in the project settings.
*   **Empty UXML:** `Main.uxml` is currently an empty container. Any UI elements must be added via the UI Builder or manual XML editing to become visible.