# Changelog

## Continuation

### 1.5.0 (Unreleased)

- Updated game references for Hallowed Concepts (Risk of Rain 2 1.5.0), MMHOOK and R2API dependencies; pinned the BepInEx 5 compile reference and updated runtime requirements.
- Migrated roulette chest and pickup picker hooks to `UniquePickup`, preserving pickup state and restoring drop ownership context after nested calls or exceptions.
- Copied initialized purchase prices and cost types to instanced interactables through native network setters, with independent purchase RNG state.
- Fixed client ownership synchronization to wait for missing network objects, replace stale pending snapshots and clean up destroyed instance groups.
- Enforced server purchase and pickup eligibility, retained shared pickups for remaining players and synchronized shared picker eligibility.
- Fixed stale fading when ownership changes while preserving visibility for each camera's player.
- Supplied explicit direct placement rules for interactable copies to prevent null placement-rule failures and retain the source rotation.
- Updated animation prefab fading hooks for the current DoSpawnEffect call sites; pooled effect ownership remains unsupported.
- Added isolated production-source and IL structure regression checks.

**Validation:** The Release build and 37 stub assertions passed. Static matching of 17 registered Release IL hook patterns and in-memory pickup/animation emitter structure checks passed against the installed 1.5.0 game build 25921972. Runtime hook installation, host/client gameplay and split screen remain untested. Dedicated support for new DLC4 interactables and the direct pickup-picker grant path has not been added.

### 1.4.0

- Added support for Alloyed Collective DLC.

### 1.3.0

- Switched to NuGet and updated libraries again.

### 1.2.0

- Updated libraries.

## Original

### 1.1.0

- Updated for the Devotion Update.
- **Warning:** This is a cursory update that includes a dirty hotfix for a new issue. Other bugs have not been fixed, the mod might not be stable.

### 1.0.1

- Removed a debugging message from being printed in chat.

### 1.0.0

- Initial release.
