# Changelog

## Continuation

### 1.6.0 (Unreleased)

- Centralized `CopyObject` and `InstancedObject` permission checks in `InstanceAccessPolicy`, including interactor/player resolution used by purchases, pickups, picker choices and client candidate filtering.
- Added a server-side purchase transaction guard and post-native completion consumption for shared `InstancedObject` eligibility; each `CopyObject` remains one-shot through its native availability state.
- Extracted source-to-copy purchase cost, cost type and independent RNG snapshotting into `InstanceStateSynchronizer`; ordinary chest copies and each TripleShop terminal use the same state boundary.

**Validation:** Release build passed with one existing obsolete-API warning. MultiplayerRegression passed 51 assertions, including unified CopyObject/InstancedObject access, shared purchase consumption and TripleShop terminal purchase-state snapshots. The stub does not emulate real RoR2 PayCost, UNet transport or host/client gameplay; chest and TripleShop multiplayer behavior still requires an in-game run.

### 1.5.3 (Unreleased)

- Isolated each TripleShop terminal's source/copy state group and restricted the source terminal to its owning player, so each player keeps an independent three-choice purchase.
- Copied terminal pickup, hidden, RNG and finalized purchase cost state through the awaited terminal path.
- Made every `CopyObject` interaction check follow `OrigPlayer`, preventing an aggregate source `Players` set from authorizing another player's hidden source; `InstancedObject` and shared pickup eligibility retain their per-handler player sets.
- Rechecked server purchase eligibility and native availability before charging or closing an interactable; restored nested shop drop ownership context with `try/finally`.

**Validation:** The Release build passed with one existing obsolete-API warning. MultiplayerRegression passed 48 assertions, including CopyObject source/clone permissions and independent three-choice terminal groups; host/client gameplay and three-choice terminal state, cost, and one-shot purchase behavior still require an in-game multiplayer run.

### 1.5.2 (Unreleased)

- Registered the interactable candidate filter so clients skip overlapping chest copies that belong to another player.
- Resolved the interactor's player through its `CharacterBody` and master, matching the runtime component layout.
- Added an explicit `IInteractable` to `Component` cast before the injected `GetComponent<InstanceHandler>` call.

**Validation:** The Release build and MultiplayerRegression stub passed; the stub now covers eligible and ineligible interactor candidates. The current game 1.5.0 build's `FindBestInteractableObject` has the expected `IInteractable` overlap local and `ShouldIgnoreSpherecastForInteractibility` sequence; the registered IL audit passed 18 patterns and 22 structure assertions. Host/client gameplay remains untested.

### 1.5.1 (Unreleased)

- Fixed the inherited command cube IL hook's invalid stack on the max-option branch by redirecting incoming Spawn labels to the injected GameObject duplication.
- Synchronized plugin, project and manifest versions, and required one mod version increment per subsequent change batch, including documentation-only changes.

**Validation:** A user run of the 1.5.0 code failed during command cube hook installation despite the earlier 17-pattern audit. The corrected production emitter now passes complete command cube stack/type and branch checks against installed game 1.5.0 build 25921972; the old emitter fails that check and produces InvalidProgramException in two synthetic CLR methods. Six generated CLR paths execute the corrected emitter with captured callbacks. Release builds, 37 stub assertions, and the existing pickup/animation checks pass. These checks were recorded with the fix before the version increment; the corrected hook still needs Unity/Mono installation and host/client, Command artifact and split screen retests.

**Version update validation:** The 1.5.1 Release build passed with one existing obsolete-API warning. Plugin, project and manifest versions agree, and the built assembly reports 1.5.1.0. No gameplay logic changed in the version update; the earlier logic regressions were not rerun and no new game tests or package build were performed.

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

**Validation (historical):** The Release build and 37 stub assertions passed. Static matching of 17 registered Release IL hook patterns and in-memory pickup/animation emitter structure checks passed against installed game 1.5.0 build 25921972. A subsequent user run failed during command cube hook installation; see 1.5.1 for the fix and its validation limits. Host/client gameplay and split screen remain untested. Dedicated support for new DLC4 interactables and the direct pickup-picker grant path has not been added.

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
