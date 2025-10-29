# Core Components API

DOTS `IComponentData` types used across Core systems.

---

## Selection

### InteractableCubeTag
- Path: `_Project/Scripts/Core/Components/Selection/InteractableCubeTag.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| InteractionType | int | 0=selectable, 1=extend root, 2=special |

### SelectionState
- Path: `_Project/Scripts/Core/Components/Selection/SelectionState.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| IsSelected | int | 0/1 selection flag |
| SelectTime | float | Timestamp used by highlight animation |

### HighlightState
- Path: `_Project/Scripts/Core/Components/Selection/HighlightState.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| Intensity | float | 0..1 recommended (system clamps/fades) |
| Color | float4 | RGBA, used for emission when URP define enabled |
| AnimTime | float | Accumulator for pulse animation |

### ProxyReference
- Path: `_Project/Scripts/Core/Components/Selection/ProxyReference.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| GameObjectInstanceID | int | Linked proxy GameObject instance id |

---

## Extend

### ExtendSettings (singleton)
- Path: `_Project/Scripts/Core/Components/Extend/ExtendSettings.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| CubePrefab | Entity | Prefab entity used for extend |
| CubeSize | float | Grid unit length |
| ApplyInstanceColor | bool | Write URP per-instance color |
| DefaultColor | float4 | Default spawn color |
| AutoAddCollider | bool | Add `BoxCollider` via GO bridge |
| ColliderActiveRadius | float | Enable colliders within radius |
| ColliderDeactivateHysteresis | float | Hysteresis distance to avoid flicker |

### ExtendPreview
- Path: `_Project/Scripts/Core/Components/Extend/ExtendPreview.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| PreviewLength | int | Desired length (cubes) |
| PreviewDirection | int3 | ±X/±Y/±Z |
| IsValid | bool | Whether the preview is valid after collision checks |
| ValidLength | int | Max feasible length after validation |

### ExtendExecutionRequest
- Path: `_Project/Scripts/Core/Authoring/Extend/ExtendInputManager.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| Direction | int3 | Extend direction |
| Length | int | Requested length |
| ChainID | int | Session/chain identifier |
| StartIndex | int | Progressive generation start index (0=from first) |

### ExtendableTag
- Path: `_Project/Scripts/Core/Components/Extend/ExtendableTag.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| MaxExtendLength | int | Hard cap per root |
| CurrentExtensions | int | Active chains count |
| AllowMultipleChains | bool | Allow multiple concurrent chains from root |

### ExtendChainData
- Path: `_Project/Scripts/Core/Components/Extend/ExtendChainData.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| RootEntity | Entity | Chain root entity |
| Direction | int3 | Chain axis |
| IndexInChain | int | 0=root, 1=first, ... |
| ChainID | int | Unique chain id |

### NeedsCollider / ColliderReference
- Path: `_Project/Scripts/Core/Components/Extend/NeedsCollider.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| NeedsCollider.Size | float | Box side length (world units) |
| ColliderReference.GameObjectInstanceID | int | Linked GO collider id |

### OccupiedCubeMap (singleton) / CubeGridPosition
- Path: `_Project/Scripts/Core/Components/Extend/OccupiedCubeMap.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| OccupiedCubeMap.Map | NativeParallelHashMap<int3, Entity> | Spatial hash (parallel-writable) |
| OccupiedCubeMap.IsInitialized | bool | Init flag |
| CubeGridPosition.GridPosition | int3 | Integer grid position |
| CubeGridPosition.IsRegistered | bool | Registered into hash map |

---

## Layout

### CubeLayoutSpawner
- Path: `_Project/Scripts/Core/Authoring/Layout/CubeLayoutSpawnerAuthoring.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| Prefab | Entity | Renderable cube prefab |
| Origin | float3 | World origin for layout |
| CellSize | float | Size multiplier of cell units |
| SpawnPerFrame | int | Per-frame spawn budget |
| SpawnedCount | int | Runtime progress |
| ApplyInstanceColor | int | 0/1 flag |
| EmissionIntensity | float | Emission multiplier |
| RemoveOnComplete | int | Remove spawner when finished |

### CubeCell (buffer)
- Path: `_Project/Scripts/Core/Authoring/Layout/CubeLayoutSpawnerAuthoring.cs`
- Fields

| Name | Type | Description |
|------|------|-------------|
| Coord | int3 | Cell coordinate |
| TypeId | int | Layout-specific type |
| Color | float4 | Per-cell color (optional) |

### LayoutController / PositionLerp / SpawnIndex
- Paths:
  - `_Project/Scripts/Core/Components/Layout/LayoutController.cs`
  - `_Project/Scripts/Core/Components/Layout/PositionLerp.cs`
  - `_Project/Scripts/Core/Components/Layout/SpawnIndex.cs`
- Notes: Drive layout animations/toggles; see systems for behavior.

---

## Stage & Level

### StageCubeTag
- Path: `_Project/Scripts/Core/Components/Stage/StageCubeTag.cs`
- Fields: implementation-dependent simple tag used by `StageManager` to clear stage entities.

### GenerateCollidersRequest / NextLevelRequest
- Paths:
  - `_Project/Scripts/Core/Components/Level/GenerateCollidersRequest.cs`
  - `_Project/Scripts/Core/Components/Level/NextLevelRequest.cs`
- Purpose: Fire-and-forget requests for stage/collider progression.
