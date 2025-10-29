# Core Authoring API

This page documents Core MonoBehaviours and Bakers that bridge authoring-time configuration to DOTS.

All paths are relative to `Assets/_Project/Scripts`.

---

## ExtendInputManager
- Path: `_Project/Scripts/Core/Authoring/Extend/ExtendInputManager.cs`
- Purpose: Keyboard-driven extend input (WASD/QE) to preview and confirm cube chain creation.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| holdThreshold | float | 0.1 | Time you must hold a direction before preview starts |
| extendInterval | float | 0.2 | Interval between preview length increments while holding |
| maxExtendLength | int | 10 | Upper bound for preview length |
| playerController | FirstPersonController | null | Optional controller toggled when `autoDisableMovement` is true |
| autoDisableMovement | bool | false | Legacy movement toggle; prefer `InputModeCoordinator` |
| showDebugLog | bool | false | Print debug logs |

- Lifecycle: Finds selected entity each frame; manages `ExtendPreview`; emits `ExtendExecutionRequest` on release.

---

## CrosshairExtendManager
- Path: `_Project/Scripts/Core/Authoring/Extend/CrosshairExtendManager.cs`
- Purpose: Mouse/crosshair-driven progressive extend with axis snapping and visual preview.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| mainCamera | Camera | Camera.main | Ray origin |
| maxBlocksPerFrame | int | 4 | Cap spawned blocks per frame on confirm |
| maxExtendLength | int | 16 | Safety upper bound on extend length |
| raycastDistance | float | 100 | Max hit distance |
| rayLayerMask | LayerMask | ~0 | Layers for proxy/geometry hit |
| showDebug | bool | true | Draw baseline/ray/ghost previews |
| drawGhostAtEnd | bool | true | Wireframe ghost cubes on preview chain |
| drawBaselineAndRay | bool | true | Visualize baseline and ray |
| useHitNormal | bool | true | Prefer surface normal for axis selection |
| axisSnapAngleDeg | float | 60 | Max angle to snap normal to axis |
| allowScrollAxisCycle | bool | true | Allow wheel to cycle ±X/±Y/±Z before generation |
| preferDragDirection | bool | true | Use mouse drag direction to pick axis |
| dragPickThreshold | float | 6 | Pixels threshold before drag decides axis |

- Behavior: LMB press captures baseline and axis; hold previews; release confirms; RMB cancels or retracts.

---

## CubeSelectionManager
- Path: `_Project/Scripts/Core/Authoring/Selection/CubeSelectionManager.cs`
- Purpose: Raycast-based selection/deselection of interactable cubes via GameObject proxies.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| mainCamera | Camera | Camera.main | Camera used for raycasts |
| raycastDistance | float | 100 | Max selection distance |
| interactableLayer | LayerMask | ~0 | Layers to test in selection raycast |
| showDebugRay | bool | true | Draw yellow ray when clicking |
| highlightIntensity | float | 1.2 | Initial highlight intensity on select |
| highlightColor | Color | (1,1,0,1) | Color used by `HighlightState` |
| showDetailedLog | bool | false | Verbose logging |

- Lifecycle: On LMB, selects via `InteractableProxy`; on ESC, deselects.

---

## InputModeCoordinator
- Path: `_Project/Scripts/Core/Authoring/Tools/AutoFix/InputModeCoordinator.cs`
- Purpose: Central controller for switching between Move and Extend modes and enforcing cursor policy.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| selectionManager | CubeSelectionManager | auto-find | Selection source |
| extendInputManager | ExtendInputManager | auto-find | Keyboard extend (optional) |
| crosshairExtendManager | CrosshairExtendManager | auto-find | Crosshair extend (preferred) |
| playerController | FirstPersonController | auto-find | Movement controller |
| parkourMode | bool | true | Allow moving while selected when using mouse-based extend |
| allowMovementWhileExtending | bool | true | Keep controller enabled in extend mode (mouse) |
| lockCursorInMoveMode | bool | true | Lock/hide cursor during move mode |
| showCursorInExtendMode | bool | true | Show cursor during extend mode (if not `hideCursorOnMouseDragExtend`) |
| hideCursorOnMouseDragExtend | bool | true | Lock/hide cursor while mouse-drag extending |
| showDebugLog | bool | false | Verbose mode |

- Behavior: Chooses extend input type (prefers crosshair); switches modes based on selection; enforces cursor.

---

## ExtendSettingsAuthoring / ExtendSettingsBaker
- Path: `_Project/Scripts/Core/Authoring/Extend/ExtendSettingsAuthoring.cs`
- Purpose: Create `ExtendSettings` singleton for extend systems.
- Authoring Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| cubePrefab | GameObject | — | Prefab for spawned cubes (Entities Graphics compatible) |
| cubeSize | float | 1 | Grid unit size |
| defaultColor | Color | (0.8,0.8,1,1) | Default color for spawned cubes |
| applyInstanceColor | bool | true | Write URP instance color if supported |
| autoAddCollider | bool | true | Add BoxCollider for spawned cubes |
| colliderActiveRadius | float | 25 | Enable colliders near player only |
| colliderDeactivateHysteresis | float | 3 | Hysteresis to avoid thrashing |

- Baker Output: `ExtendSettings { CubePrefab, CubeSize, ApplyInstanceColor, DefaultColor, AutoAddCollider, ColliderActiveRadius, ColliderDeactivateHysteresis }`.

---

## CubeLayoutSpawnerAuthoring / CubeLayoutSpawnerBaker
- Path: `_Project/Scripts/Core/Authoring/Layout/CubeLayoutSpawnerAuthoring.cs`
- Purpose: Bake `CubeLayout` to `CubeLayoutSpawner` + `CubeCell` buffer for runtime spawning.
- Authoring Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| cubePrefab | GameObject | — | Entities Graphics compatible prefab |
| layout | CubeLayout | — | Source layout asset |
| spawnPerFrame | int | 2048 | Batch size to avoid stalls |
| applyInstanceColor | bool | true | Enable URP instance properties |
| emissionIntensity | float | 1.2 | Base emission multiplier (URP) |
| removeOnComplete | bool | true | Remove spawner when done |

- Baker Output: `CubeLayoutSpawner` and `DynamicBuffer<CubeCell>`.

---

## CubeLayoutColliderGenerator
- Path: `_Project/Scripts/Core/Authoring/Layout/CubeLayoutColliderGenerator.cs`
- Purpose: Generate merged colliders (box-merged) from `CubeLayout` for character collision.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| layout | CubeLayout | — | Source layout asset |
| colliderType | enum ColliderType | BoxCollider | MeshCollider or BoxCollider (use Box for CharacterController) |
| mergeMode | enum MergeMode | GreedyMerge | Box merge algorithm: None/LayerMerge/GreedyMerge |
| drawDebugGizmos | bool | false | Draw bounds of generated boxes in Scene view |
| convex | bool | false | MeshCollider convex (not recommended for terrain) |
| clearOldColliders | bool | true | Remove pre-existing colliders before generating |

- Notes: Greedy merge dramatically reduces collider count; prefer BoxCollider for CharacterController.

---

## StageConfig (ScriptableObject)
- Path: `_Project/Scripts/Core/Authoring/Stage/StageConfig.cs`
- Purpose: Stage data for `StageManager`.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| stageName | string | "Stage 1" | Label |
| description | string | — | Info text |
| layout | CubeLayout | — | Layout asset |
| cubePrefab | GameObject | — | Render prefab |
| spawnPerFrame | int | 2048 | Spawn batch size |
| applyInstanceColor | bool | true | URP per-instance color |
| emissionIntensity | float | 1.2 | Emission multiplier |
| colliderType | ColliderType | BoxCollider | Collider choice |
| mergeMode | MergeMode | GreedyMerge | Box merge mode |
| playerSpawnPoint | Vector3 | (0,0,0) | Player start |
| cameraPosition | Vector3 | (0,0,0) | Optional camera reset position |
| resetCamera | bool | false | Whether to reset camera |
| transitionDuration | float | 1 | Stage transition duration |
| transitionType | enum TransitionType | Fade | Instant/Fade/Slide |

---

## StageManager
- Path: `_Project/Scripts/Core/Authoring/Stage/StageManager.cs`
- Purpose: Load/unload stages: clear old entities/colliders, spawn new colliders, move player, manage transitions.
- Fields

| Name | Type | Default | Description |
|------|------|---------|-------------|
| stages | List<StageConfig> | [] | Ordered stage list |
| initialStageIndex | int | 0 | -1 means no auto-load |
| colliderGeneratorPrefab | GameObject | — | Must contain `CubeLayoutColliderGenerator` |
| playerTransform | Transform | — | Player to move to spawn point |
| cameraTransform | Transform | — | Camera to optionally reset |
| fadePanel | CanvasGroup | null | Fade UI panel |

- Key Methods: `LoadStage(int)`, `LoadNextStage()`, `LoadPreviousStage()`, `ReloadCurrentStage()`
- Notes: Uses `StageCubeTag` to clear DOTS entities; integrates with collider generator; DOTS spawning hook is noted for integration.
