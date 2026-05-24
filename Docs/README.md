# Butterfly House Documentation

This folder is the current documentation source of truth for the production closeout stage.

## Current Documents

- [Project Overview](PROJECT_OVERVIEW.md): product framing, target platform, current scene, and feature inventory.
- [Build And Run](BUILD_AND_RUN.md): Unity version, PC VR setup, Quest 3 Link workflow, and smoke test steps.
- [System Structure](SYSTEM_STRUCTURE.md): project folders, main scene hierarchy, core prefabs, and runtime systems.
- [Interaction Bindings](InteractionBindings.md): player-facing controls and interactables in the production scene.
- [Cleanup And Standardization](CLEANUP_AND_STANDARDIZATION.md): hierarchy, asset, naming, and documentation cleanup rules.
- [Asset Reference Audit](Asset_Reference_Audit.md): current external dependency snapshot and Unity-side asset cleanup queue.
- [VR Performance Guide](VR_PERFORMANCE_GUIDE.md): profiling workflow, target budgets, rendering settings, and issue triage.
- [Final Release Checklist](FINAL_RELEASE_CHECKLIST.md): Unity Editor, Play Mode, and Quest 3 Link signoff steps.

## Documentation Policy

- All maintained documentation is written in English.
- Historical milestone packets, kickoff guides, and issue-board seeds have been removed from the maintained set because they no longer describe the current production state.
- Any future asset moves or renames must be performed in Unity through `AssetDatabase`, the Project window, or the production cleanup tools, not by moving files directly in the operating system.
