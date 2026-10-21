# InstancedLoot continuation 2.0

A BepInEx 5 mod for Risk of Rain 2, targeting Hallowed Concepts / game 1.5.0.

Install the same 2.0 version on the host and every client. The plugin GUID remains `com.kuberoot.instancedloot`.

## How it works

`PreferredInstanceMode=InstanceObject` is the only strategy. Each player present when an interactable is allocated receives an independently usable native object. Players pay and open their own chests or choose from their own three-choice shop. Initial goods and costs match, while subsequent prices, cooldowns and consumption stay independent. Dropped items remain ordinary public loot that anybody can collect.

Supported source adapters cover normal/category chests, equipment barrels, lunar/void chests, lockboxes, adaptive/option chests, scavenger sacks, three-choice/equipment/delivery shops, and Shrines of Chance, Blood and Order. Damaged drone and turret purchases have an experimental repair adapter: the native summon belongs to the buyer; special broken prefabs without a SpawnCard fall back to vanilla. Active drone/turret bodies are not cloned.

Printers, cauldrons, cleansing pools, scrappers, lunar shops, unknown/global shrines and item pickup behavior remain vanilla.

## Configuration

Open `BepInEx/config/com.kuberoot.instancedloot.cfg`. Every supported object has its own boolean toggle; the three supported shrines and damaged drone/turret purchases have separate sections. All are enabled by default. A disabled type uses its native shared behavior. Changes affect newly spawned objects.

The old item-sharing, owner-only, preset/alias and chat-command configuration was removed. Back up an old cfg and regenerate it when upgrading, then select the new toggles. See [configuration](docs/CONFIGURATION.md) for the exact keys. The interactable-budget reduction remains enabled by default; money-drop reduction remains disabled by default, under the existing `General - Balance` section.

## Validation and limits

Release builds and production-linked stub regressions pass. The installed game assembly is also checked for the new candidate injection and native lifecycle/payment assumptions. This version has **not yet been tested in an actual host/client game or split-screen run**. Late joining synchronizes current ownership but does not add old object copies. Unknown metadata may briefly display overlapping native objects; server permissions still apply. Pool effects, shaders and native ping highlighting need gameplay verification.

See [development and validation](docs/DEVELOPMENT.md) and [known limits](docs/KNOWN_ISSUES.md). This change does not build or modify release archives.

## Installation

Place the built plugin under `Risk of Rain 2/BepInEx/plugins` with the dependencies listed in manifest.json.

Original project: [KubeRoot/ror2-instancedloot](https://gitlab.com/KubeRoot/ror2-instancedloot). Original distribution: [KubeRoot/InstancedLoot](https://thunderstore.io/package/KubeRoot/InstancedLoot/). Continuation: [GitHub](https://github.com/Frenqy/InstancedLoot_continuation).
