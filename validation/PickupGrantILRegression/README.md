# Current IL/lifecycle audit and historical regressions

```powershell
dotnet run --project validation/PickupGrantILRegression --configuration Release
# Optional installed, unstripped DLL; read/transformed only in memory
dotnet run --project validation/PickupGrantILRegression --configuration Release -- "C:/path/to/Risk of Rain 2_Data/Managed/RoR2.dll"
```

Run from the repository root with .NET 8 or later. Current 2.0 production sources linked here are **InteractorCandidateIL** and **AnimationPrefabIL**. Candidate tests invoke the real emitter through ILContext.Invoke and execute synthetic CLR raycast/overlap paths with incoming branches, captured callback, original target/interactor/search flag and out IInteractable. A missing site fails explicitly. The installed method is actually transformed; both acquisition sites, callback arguments and unrelated branch targets are checked. Installed game methods are not executed.

Production animation checks cover four synthetic/installed return sites and four changed-pattern negative controls; they inspect structure.

With a game DLL, InstalledHookAudit identifies current Release subscriptions and fails on an unhandled registered hook: **7 active IL pattern checks** pass. Most are cursor checks, not full transformations. NativeLifecycleAudit separately passes **10 lifecycle/payment assertions**, covering Awake terminals, readiness, repair summoner/destroy/upgrade behavior, activator money payment/Executive Card callback, Blood/Order scope and multi-interface PerformInteraction.

## Historical fixtures are not 2.0 runtime hooks

Historical/PickupGrantIL.cs and Historical/CommandCubeIL.cs preserve pre-2.0 emitters solely for existing regressions and negative controls. They are **not compiled into or installed by the 2.0 plugin**. The output's 22 pickup structure assertions (15 synthetic + 7 installed), six command cube CLR paths and old invalid-injector controls are historical evidence, not current item-sharing support. Prior game failure and fixture history are in docs/HISTORY_1_X.md. Historical audit entries absent from current registration are skipped; active missing entries still fail.

Installed input on 2026-10-16: game 1.5.0 / build 25921972, RoR2.dll SHA256:

```text
48F2758CCCBE1A5EFBA88CFFE0A68631953F489A71A92726572C15A4C57FF90C
```

These tests do not install Mono hooks, run real game methods or sessions, render split-screen views or establish another build's compatibility. The given DLL is never modified. Stripped GameLibs are not runtime-method evidence.
