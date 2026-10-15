# Pickup, animation and command cube IL regression

Run with .NET 8 or newer from the repository root:

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release
```

This executable links the actual production `PickupGrantIL`, `AnimationPrefabIL` and `CommandCubeIL` emitters and uses
MonoMod.Utils 21.12.13.1 / Mono.Cecil to transform synthetic methods. It checks
both struct value and address loads, a captured local index other than 5,
callback ordering, the by-reference callback parameter, preservation of original
field reads, and an explicit failure when the pattern is absent.
The default run checks 15 pickup structural assertions. It also transforms four
synthetic animation spawn sites, checks that each returned Transform remains for
the original pop while the callback receives self and the captured bool flag,
and rejects four missing/changed-pattern cases. These pickup/animation callbacks are never executed.
The harness does not load Unity or reference the main mod project.

The command cube checks reproduce the inherited invalid injector, which duplicates
the GameObject before Spawn but lets incoming branches skip that duplication. Both
the bounded stack verifier and actual generated CLR methods reject the old injector.
The corrected production emitter runs through `ILContext.Invoke`, including MonoMod's
label conversion, and must redirect every incoming branch/switch target to dup while
preserving all other edges. Six conditional, switch, unconditional and fallthrough
paths execute with captured callbacks through `RuntimeILReferenceBag`; they must spawn
and observe exactly the same object, with one callback after Spawn. Six changed
Spawn signatures/patterns must fail explicitly. The tests use MonoMod's Cecil generator
for in-memory CLR execution because its old DynamicMethod backend relies on runtime
internals unavailable on .NET 8; the generator environment override is process-local
and restored afterwards.

Optionally supply an installed game's real assembly for 7 more pickup structural
assertions against that assembly's `GenericPickupController.AttemptGrant`,
animation emitter checks against `AnimationEvents.CreatePrefab`, complete command cube
stack/type/control-flow checks and its old-injector negative control, and an audit of
the registered Release IL hook patterns. Run this mode from the repository root:

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
```

`InstalledHookAudit` reads the current `HookManager` and registered ObjectHandler
sources to identify IL subscriptions, excluding comments and DEBUG blocks. It
fails when a registered hook lacks an audit entry, an entry is no longer
registered, or a required pattern cannot be found. Except for the linked pickup,
animation and command cube emitters, it checks the expected cursor patterns rather than
running each complete production injector. Command cube runs its actual production
injector and checks the whole method, including branch merges and call argument types.
The verifier deliberately supports only the instructions needed by the method and fails
on unknown instructions, unreachable instructions or exception regions. It is not a general CLR verifier.
Keep these explicit audit patterns
in sync when modifying hook implementations or the registration syntax.

On 2026-10-15, the run with installed game 1.5.0 / Steam build 25921972 passed all
17 registered Release pattern checks, 15 synthetic plus 7 installed pickup
assertions, and the four real animation return-site checks. The tested `RoR2.dll` SHA256 was
`48F2758CCCBE1A5EFBA88CFFE0A68631953F489A71A92726572C15A4C57FF90C`.
A subsequent user run nevertheless failed installing the old command cube hook with
InvalidProgramException. The original Spawn-name-only audit missed the branch that
skipped dup. The corrected emitter and new negative control now check that same real
method: 48 transformed instructions, one conditional edge and no exception regions.
The earlier 7 installed pickup assertions used old build 21587608; these are
separate evidence, not a runtime test of the updated game. Detailed environment
and remaining coverage gaps are in [DEVELOPMENT.md](../../docs/DEVELOPMENT.md).

No local game path is required by the project. The transformation is in memory;
the supplied assembly is never modified. Real game methods are not executed; only
synthetic command cube methods execute with replacement GameObject/NetworkServer types.
These checks establish selected cursor matches, emitted instruction structure and
the synthetic CLR behavior, not corrected Unity/Mono hook installation, game runtime execution, compatibility with another
game build, transport, or multiplayer behavior. Pool ownership lifecycles and
the picker's direct-grant asset/event usage are outside these checks.
Do not pass a stripped GameLibs assembly as runtime IL evidence.
