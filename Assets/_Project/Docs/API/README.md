# DOTS Project API Documentation

## Scope

This documentation describes the runtime APIs (MonoBehaviours, ScriptableObjects, DOTS components, Bakers, and Systems) in `Assets/_Project/Scripts`.

- Audience: developers extending gameplay, systems, and tools
- Version: WIP (incremental) — we will expand module-by-module

## Conventions

- Names of files, classes, systems are shown using backticks (e.g., `StageManager`, `ExtendExecutionSystem`)
- DOTS components implement `IComponentData`
- DOTS systems are either `ISystem` (struct) or `SystemBase` (class)
- Authoring uses the Baker pattern (`Baker<TAuthoring>`)

## Modules

- Core
  - Authoring (input, selection, level, layout, tools)
  - Components (selection, extend, layout, stage, cube)
  - Systems (extend, selection, layout, level)
- Cube
  - Authoring
  - Components
  - Systems
- Spawning
  - Authoring
  - Systems
- UI
  - Authoring
- Editor
  - Grid brush tools

## How to navigate

- Start with Type Index for a searchable list of types and locations
- Each module page (coming next) will have usage, lifecycle, and examples

## Table of Contents

- [Type Index](./TypeIndex.md)
- Core (planned)
  - Authoring (planned)
  - Components (planned)
  - Systems (planned)
- Cube (planned)
- Spawning (planned)
- UI (planned)
- Editor (planned)
