# Core Systems API

DOTS runtime systems grouped by domain. Paths are relative to `Assets/_Project/Scripts`.

---

## Extend

### ExtendPreviewSystem (SystemBase)
- Path: `_Project/Scripts/Core/Systems/Extend/ExtendPreviewSystem.cs`
- Schedule: `LateSimulationSystemGroup`
- Requires: `OccupiedCubeMap` singleton (read)
- Reads: `InteractableCubeTag`, `ExtendableTag`, `SelectionState`, `LocalTransform`, `OccupiedCubeMap`
- Writes: `ExtendPreview { IsValid, ValidLength, PreviewLength, PreviewDirection }`
- Notes: Toggled by `ExtendPreviewSystem.PreviewEnabled` static flag.

### ExtendExecutionSystem (SystemBase)
- Path: `_Project/Scripts/Core/Systems/Extend/ExtendExecutionSystem.cs`
- Schedule: `SimulationSystemGroup` (after `ExtendPreviewSystem`)
- Requires: `ExtendSettings` singleton
- Inputs: `ExtendExecutionRequest`
- Effects:
  - Instantiate cubes from `ExtendSettings.CubePrefab`
  - Set `LocalTransform`
  - Add: `CubeTypeTag`, `ExtendChainData`, `CubeGridPosition`, `StageCubeTag`
  - Optional: `NeedsCollider`, `URPMaterialPropertyBaseColor` (when `ApplyInstanceColor`)
- Notes: Validates config; logs when settings/prefab missing.

### OccupiedCubeMapSystem (ISystem)
- Path: `_Project/Scripts/Core/Systems/Extend/OccupiedCubeMapSystem.cs`
- Schedule: `InitializationSystemGroup`
- OnCreate: initialize `OccupiedCubeMap.Map` with capacity; `RequireForUpdate<OccupiedCubeMap>()`
- OnUpdate:
  - `RegisterCubeJob` uses `NativeParallelHashMap<int3, Entity>.ParallelWriter` (Burst) to register positions
  - Cleanup pass removes destroyed entries
- OnDestroy: disposes `OccupiedCubeMap.Map`

---

## Selection & Highlight

### HighlightRenderSystem (ISystem)
- Path: `_Project/Scripts/Core/Systems/Selection/HighlightRenderSystem.cs`
- Attributes: `[BurstCompile]`
- Behavior:
  - Pulse animate `HighlightState.Intensity` when selected; fade out when deselected
  - When `HAS_URP_MATERIAL_PROPERTY` is defined, writes `URPMaterialPropertyEmissionColor` from `HighlightState.Color * Intensity`

### InteractableProxySpawnSystem / ProxySyncSystem
- Paths:
  - `_Project/Scripts/Core/Systems/Selection/InteractableProxySpawnSystem.cs`
  - `_Project/Scripts/Core/Systems/Selection/ProxySyncSystem.cs`
- Behavior:
  - Spawns `InteractableProxy` GameObjects for entities with `InteractableCubeTag`
  - Uses ECB for structural changes (spawn/tag) where applicable
  - Synchronizes proxy transform from `LocalTransform`

---

## Layout

### CubeLayoutSpawnSystem (ISystem)
- Path: `_Project/Scripts/Core/Systems/Layout/CubeLayoutSpawnSystem.cs`
- Attributes: `[BurstCompile]`
- Requires: `CubeLayoutSpawner`; reads `DynamicBuffer<CubeCell>`
- Behavior:
  - Spawns entities in batches (`SpawnPerFrame`) using `EntityCommandBuffer`
  - Tags `StageCubeTag`, adds `CubeGridPosition`, sets `CubeTypeTag`
  - Adds selection-related components for interactable cells (`InteractableCubeTag`, `SelectionState`, `HighlightState`, `ExtendableTag`)
  - Optionally writes URP instance properties (`URPMaterialPropertyBaseColor`, initial `URPMaterialPropertyEmissionColor = 0` for interactables)
  - Removes `CubeLayoutSpawner` upon completion when configured

### PositionLerpSystem / LayoutApplySystem / LayoutToggleSystem
- Paths:
  - `_Project/Scripts/Core/Systems/Layout/PositionLerpSystem.cs`
  - `_Project/Scripts/Core/Systems/Layout/LayoutApplySystem.cs`
  - `_Project/Scripts/Core/Systems/Layout/LayoutToggleSystem.cs`
- Notes: Layout animation and toggle control; avoid Burst in methods using managed arrays or `Input.GetKeyDown`.

---

## Level / Stage

### LevelProgressionSystem (ISystem)
- Path: `_Project/Scripts/Core/Systems/Level/LevelProgressionSystem.cs`
- Behavior: Responds to `NextLevelRequest` / `GenerateCollidersRequest` to orchestrate transitions (see StageManager for authoring side).

---

## Conditional: Entities Graphics / URP

- Define `HAS_URP_MATERIAL_PROPERTY` to enable per-instance properties
- Systems that write URP properties: `HighlightRenderSystem`, `CubeLayoutSpawnSystem`, `ExtendExecutionSystem` (when applicable)
