# Pickup grant IL structure regression

Run with .NET 8 or newer from the repository root:

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release
```

This executable links the actual production `PickupGrantIL` emitter and uses
MonoMod.Utils 21.12.13.1 / Mono.Cecil to transform synthetic methods. It checks
both struct value and address loads, a captured local index other than 5,
callback ordering, the by-reference callback parameter, preservation of original
field reads, and an explicit failure when the pattern is absent.
The default run checks 15 structural assertions.

Optionally supply an installed game's real assembly for 7 more structural
assertions against that assembly's `GenericPickupController.AttemptGrant`:

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
```

No local game path is required by the project. The transformation is in memory;
the supplied assembly is never modified. The callback is not executed. These
checks establish the emitted instruction structure, not runtime execution,
compatibility with a different game build, transport, or multiplayer behavior.
Do not pass a stripped GameLibs assembly as runtime IL evidence.
