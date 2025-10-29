# Type Index

This index lists public runtime types discovered under `Assets/_Project/Scripts`.

## MonoBehaviours

- Core/Authoring
  - `ExtendInputManager` — `_Project/Scripts/Core/Authoring/Extend/ExtendInputManager.cs`
  - `CrosshairExtendManager` — `_Project/Scripts/Core/Authoring/Extend/CrosshairExtendManager.cs`
  - `CubeSelectionManager` — `_Project/Scripts/Core/Authoring/Selection/CubeSelectionManager.cs`
  - `InteractableProxy` — `_Project/Scripts/Core/Authoring/Selection/InteractableProxy.cs`
  - `InteractableCubeMarker` — `_Project/Scripts/Core/Authoring/Selection/InteractableCubeMarker.cs`
  - `GlobalDebugController` — `_Project/Scripts/Core/Authoring/Tools/Debug/GlobalDebugController.cs`
  - `InputModeCoordinator` — `_Project/Scripts/Core/Authoring/Tools/AutoFix/InputModeCoordinator.cs`
  - `RaycastDebugger` — `_Project/Scripts/Core/Authoring/Tools/Debug/RaycastDebugger.cs`
  - `InputModeDebugTool` — `_Project/Scripts/Core/Authoring/Tools/Debug/InputModeDebugTool.cs`
  - `SelectionDebugTool` — `_Project/Scripts/Core/Authoring/Tools/Debug/SelectionDebugTool.cs`
  - `SelectionAutoFix` — `_Project/Scripts/Core/Authoring/Tools/AutoFix/SelectionAutoFix.cs`
  - `SelectionTester` — `_Project/Scripts/Core/Authoring/Tools/Tester/SelectionTester.cs`
  - `StageManagerTester` — `_Project/Scripts/Core/Authoring/Tools/Tester/StageManagerTester.cs`
  - `CubeLayoutColliderGenerator` — `_Project/Scripts/Core/Authoring/Layout/CubeLayoutColliderGenerator.cs`
  - `CubeLayoutGameObjectSpawner` — `_Project/Scripts/Core/Authoring/Layout/CubeLayoutGameObjectSpawner.cs`
  - `ProximityColliderActivator` — `_Project/Scripts/Core/Authoring/Layout/ProximityColliderActivator.cs`
  - `ExtendSettingsAuthoring` — `_Project/Scripts/Core/Authoring/Extend/ExtendSettingsAuthoring.cs`
  - `CubeLayoutSpawnerAuthoring` — `_Project/Scripts/Core/Authoring/Layout/CubeLayoutSpawnerAuthoring.cs`
  - `StageManager` — `_Project/Scripts/Core/Authoring/Stage/StageManager.cs`
  - `ColliderGeneratorBridge` — `_Project/Scripts/Core/Authoring/Level/ColliderGeneratorBridge.cs`
  - `ColliderBridgeBootstrap` — `_Project/Scripts/Core/Authoring/Level/ColliderBridgeBootstrap.cs`
  - `NextLevelTester` — `_Project/Scripts/Core/Authoring/Level/NextLevelTester.cs`
  - `CrosshairUI` — `_Project/Scripts/Core/Authoring/UI/CrosshairUI.cs`
  - `CubeMaterialChecker` — `_Project/Scripts/Core/Authoring/Tools/Debug/CubeMaterialChecker.cs`
  - `ProxyDiagnosticTool` — `_Project/Scripts/Core/Authoring/Tools/Debug/ProxyDiagnosticTool.cs`
  - `FreeFlyCamera` — `_Project/Scripts/Core/Authoring/FreeFlyCamera.cs`
- Cube/Authoring
  - `RotatingCubeAuthoring` — `_Project/Scripts/Cube/Authoring/RotatingCubeAuthoring.cs`
- Spawning/Authoring
  - `SpawnerAuthoring` — `_Project/Scripts/Spawning/Authoring/SpawnerAuthoring.cs`
- UI/Authoring
  - `NarrationTrigger` — `_Project/Scripts/UI/Authoring/NarrationTrigger.cs`
  - `NarrationManager` — `_Project/Scripts/UI/Authoring/NarrationManager.cs`
- Player
  - `FirstPersonController` — `_Project/Scripts/Player/Authoring/FirstPersonController.cs`

## ScriptableObjects

- `StageConfig` — `_Project/Scripts/Core/Authoring/Stage/StageConfig.cs`
- `LevelSequence` — `_Project/Scripts/Core/Authoring/Level/LevelSequence.cs`
- `CubeLayout` — `_Project/Scripts/Core/Authoring/Layout/CubeLayout.cs`
- `NarrationDatabase` — `_Project/Scripts/UI/Authoring/NarrationDatabase.cs`

## Bakers

- `ExtendSettingsBaker` — `_Project/Scripts/Core/Authoring/Extend/ExtendSettingsAuthoring.cs`
- `LevelSequenceBaker` — `_Project/Scripts/Core/Authoring/Level/LevelSequenceAuthoring.cs`
- `CubeLayoutSpawnerBaker` — `_Project/Scripts/Core/Authoring/Layout/CubeLayoutSpawnerAuthoring.cs`
- `SpawnerAuthoring.Baker` — `_Project/Scripts/Spawning/Authoring/SpawnerAuthoring.cs`

## DOTS IComponentData

- Core/Selection
  - `InteractableCubeTag` — `_Project/Scripts/Core/Components/Selection/InteractableCubeTag.cs`
  - `SelectionState` — `_Project/Scripts/Core/Components/Selection/SelectionState.cs`
  - `HighlightState` — `_Project/Scripts/Core/Components/Selection/HighlightState.cs`
  - `ProxyReference` — `_Project/Scripts/Core/Components/Selection/ProxyReference.cs`
- Core/Extend
  - `ExtendSettings` — `_Project/Scripts/Core/Components/Extend/ExtendSettings.cs`
  - `ExtendExecutionRequest` — `_Project/Scripts/Core/Authoring/Extend/ExtendInputManager.cs`
  - `ExtendPreview` — `_Project/Scripts/Core/Components/Extend/ExtendPreview.cs`
  - `ExtendableTag` — `_Project/Scripts/Core/Components/Extend/ExtendableTag.cs`
  - `ExtendChainData` — `_Project/Scripts/Core/Components/Extend/ExtendChainData.cs`
  - `NeedsCollider` — `_Project/Scripts/Core/Components/Extend/NeedsCollider.cs`
  - `ColliderReference` — `_Project/Scripts/Core/Components/Extend/NeedsCollider.cs`
  - `OccupiedCubeMap` — `_Project/Scripts/Core/Components/Extend/OccupiedCubeMap.cs`
  - `CubeGridPosition` — `_Project/Scripts/Core/Components/Extend/OccupiedCubeMap.cs`
- Core/Layout
  - `CubeLayoutSpawner` — `_Project/Scripts/Core/Authoring/Layout/CubeLayoutSpawnerAuthoring.cs`
  - `LayoutController` — `_Project/Scripts/Core/Components/Layout/LayoutController.cs`
  - `PositionLerp` — `_Project/Scripts/Core/Components/Layout/PositionLerp.cs`
  - `SpawnIndex` — `_Project/Scripts/Core/Components/Layout/SpawnIndex.cs`
- Core/Stage
  - `StageCubeTag` — `_Project/Scripts/Core/Components/Stage/StageCubeTag.cs`
  - `GenerateCollidersRequest` — `_Project/Scripts/Core/Components/Level/GenerateCollidersRequest.cs`
  - `NextLevelRequest` — `_Project/Scripts/Core/Components/Level/NextLevelRequest.cs`
  - `LevelSequenceRuntime` — `_Project/Scripts/Core/Authoring/Level/LevelSequenceAuthoring.cs`
- Cube
  - `CubeTypeTag` — `_Project/Scripts/Core/Components/Cube/CubeTypeTag.cs`
  - `RotatingCube` — `_Project/Scripts/Cube/RotatingCube.cs`
  - `RotationSpeed` — `_Project/Scripts/Cube/RotationSpeed.cs`
  - `PlanetTag` — `_Project/Scripts/Cube/Components/PlanetTag.cs`
  - `RingTag` — `_Project/Scripts/Cube/Components/RingTag.cs`
- Spawning
  - `Spawner` — `_Project/Scripts/Spawning/Spawner.cs`

## DOTS Systems

- ISystem (struct)
  - `LevelProgressionSystem` — `_Project/Scripts/Core/Systems/Level/LevelProgressionSystem.cs`
  - `CubeLayoutSpawnSystem` — `_Project/Scripts/Core/Systems/Layout/CubeLayoutSpawnSystem.cs`
  - `OccupiedCubeMapSystem` — `_Project/Scripts/Core/Systems/Extend/OccupiedCubeMapSystem.cs`
  - `RotatingSystem` — `_Project/Scripts/Cube/Systems/RotatingSystem.cs`
  - `HighlightRenderSystem` — `_Project/Scripts/Core/Systems/Selection/HighlightRenderSystem.cs`
  - `PositionLerpSystem` — `_Project/Scripts/Core/Systems/Layout/PositionLerpSystem.cs`
  - `PlanetRotationSystem` — `_Project/Scripts/Cube/Systems/PlanetRotationSystem.cs`
  - `RingRotationSystem` — `_Project/Scripts/Cube/Systems/RingRotationSystem.cs`
  - `SpawningSystem` — `_Project/Scripts/Spawning/System/SpawningSystem.cs`
  - `LayoutApplySystem` — `_Project/Scripts/Core/Systems/Layout/LayoutApplySystem.cs`
  - `LayoutToggleSystem` — `_Project/Scripts/Core/Systems/Layout/LayoutToggleSystem.cs`

- SystemBase (class)
  - `ExtendExecutionSystem` — `_Project/Scripts/Core/Systems/Extend/ExtendExecutionSystem.cs`
  - `ExtendPreviewSystem` — `_Project/Scripts/Core/Systems/Extend/ExtendPreviewSystem.cs`

## Editor

- `GridBrushWindow` (EditorWindow) — `_Project/Scripts/Editor/GridBrush/GridBrushWindow.cs`

---

Notes
- This index is generated manually from a quick scan and may evolve as files change.
- Next step: per-module API pages with field/property details and usage examples.
