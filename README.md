# InstancedLoot_continuation

fork from: https://gitlab.com/KubeRoot/ror2-instancedloot
origin mod: https://thunderstore.io/package/KubeRoot/InstancedLoot/

# InstancedLoot

Highly configurable mod to allow players to loot items and objects separately.

This mod needs to be installed both on clients and the host to function.

NOTE: Please check out the config options in `BepInEx/config/com.kuberoot.instancedloot.cfg` before usage.

[Support me on Ko-fi](https://ko-fi.com/kuberoot)

## Usage notes:

When first opening the mod, by default it will use the `Default` preset, instancing most objects, with `PreferredInstanceMode` set to `InstanceObject`. This way, objects will be instanced, but the items within won't be. What that means is, for example, when a player opens a chest, other players can still open that chest, and the item that drops can be picked up by anybody.

If you wish to change *how* most things are instanced, you should tweak `PreferredInstanceMode` to a mode you prefer - the description for that option includes all available modes, also listed here for convenience:

- `None`: Self-explanatory, this object does not get instanced, nor do items spawned from it
- `Default`: Do not override the preset/alias. If every value in the chain is Default, defaults to None.
- `InstancePreferred`: Use the configuration for PreferredInstanceMode for this entry. Provided for convenience and/or experimentation.
- `InstanceObject`: Spawn multiple copies of the object, one for each player, where each can only be opened by the owning player, and from which items can be picked up by any player
- `InstanceItems`: Keep one copy of the object that can be opened by anybody, but instance the spawned item, such that each player can pick it up independently
- `InstanceBoth`: Spawn multiple copies of the object, like InstanceObject, but also limit the resulting item such that it can only be picked up by the player who earned/bought it
- `InstanceItemForOwnerOnly`: Keep one copy of the object, and limit the resulting item to only be picked up by the player who earned/bought it.
- `InstanceObjectForOwnerOnly`: Keep one copy of the object, and limit opening it to only the owning player. This is only meaningful for objects that inherently belong to a player, like lockboxes. The resulting items are not instanced and can be picked up by any player.
- `InstanceBothForOwnerOnly`: Similar to InstanceObjectForOwnerOnly, but the resulting item can only be picked up by the owning player.  

Another important thing to note is the presence of simple chat commands.

- You can ping an item that is instanced for you and say `uninstance` in the chat to "forfeit" the item, making anybody able to pick it up.

- You can say `uninstanceall` in the chat to "forfeit" all items currently instanced for you.

Those commands also work on pickup pickers, such as command essences. The current chat command format is meant as a placeholder, hopefully I can implement a better system in the future.

## Limitations/issues:

- Changing settings mid-run can result in too many items dropping on the current stage, but everything should work as normal on subsequent stages.

- Certain materials don't support dithering, because of that you might notice some things aren't fading out properly.

- Instancing for drones/turrets isn't implemented - hopefully that will come in a later release.

- You might encounter weird issues with certain things not instancing correctly - when testing I had some issues that I couldn't reproduce. Feel free to report those, especially if you think you know why it happened.

- Interactibles from other mods need explicit support between the mods - I tried to write my mod to make this easy, if you're a mod author looking for compatibility feel free to contact me.

## Features:

- Instancing interactibles, like chests and shrines, so that each player can buy them separately.
- Instancing items, so that each player can pick them up separately.
- Items and objects that are not available for you are invisible or faded out, depending on circumstances.
- An included preset, so you can have sane behavior without having to configure anything manually.
- Configuration options for every "alias" and every object type separately, allowing you to tweak the exact behavior.
- Supports XSplitScreen.

## Installation

Copy the `InstancedLoot` folder to `Risk of Rain 2/BepInEx/plugins`
