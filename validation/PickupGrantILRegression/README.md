# Pickup and animation IL structure regression

Run with .NET 8 or newer from the repository root:

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release
```

This executable links the actual production `PickupGrantIL` and `AnimationPrefabIL` emitters and uses
MonoMod.Utils 21.12.13.1 / Mono.Cecil to transform synthetic methods. It checks
both struct value and address loads, a captured local index other than 5,
callback ordering, the by-reference callback parameter, preservation of original
field reads, and an explicit failure when the pattern is absent.
The default run checks 15 pickup structural assertions. It also transforms four
synthetic animation spawn sites, checks that each returned Transform remains for
the original pop while the callback receives self and the captured bool flag,
and rejects four missing/changed-pattern cases. Callbacks are never executed;
the harness does not load Unity or reference the main mod project.

Optionally supply an installed game's real assembly for 7 more pickup structural
assertions against that assembly's `GenericPickupController.AttemptGrant`,
animation emitter checks against `AnimationEvents.CreatePrefab`, and an audit of
the registered Release IL hook patterns. Run this mode from the repository root:

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
```

`InstalledHookAudit` reads the current `HookManager` and registered ObjectHandler
sources to identify IL subscriptions, excluding comments and DEBUG blocks. It
fails when a registered hook lacks an audit entry, an entry is no longer
registered, or a required pattern cannot be found. Except for the linked pickup
and animation emitters, it checks the expected cursor patterns rather than
running each complete production injector. Keep these explicit audit patterns
in sync when modifying hook implementations or the registration syntax.

On 2026-10-15, the run with installed game 1.5.0 / Steam build 25921972 passed all
17 registered Release pattern checks, 15 synthetic plus 7 installed pickup
assertions, and the four real animation return-site checks. The tested `RoR2.dll` SHA256 was
`48F2758CCCBE1A5EFBA88CFFE0A68631953F489A71A92726572C15A4C57FF90C`.
The earlier 7 installed pickup assertions used old build 21587608; these are
separate evidence, not a runtime test of the updated game. Detailed environment
and remaining coverage gaps are in [DEVELOPMENT.md](../../docs/DEVELOPMENT.md).

No local game path is required by the project. The transformation is in memory;
the supplied assembly is never modified. The callback is not executed. These
checks establish selected cursor matches and emitted instruction structure,
not Unity/Mono hook installation, runtime execution, compatibility with another
game build, transport, or multiplayer behavior. Pool ownership lifecycles and
the picker's direct-grant asset/event usage are outside these checks.
Do not pass a stripped GameLibs assembly as runtime IL evidence.
