# Object-instancing lifecycle regression (2.0)

```powershell
dotnet run --project validation/MultiplayerRegression --configuration Release
```

Run from the repository root with .NET 8 or later. This executable links the **new production** ObjectInstanceService, ObjectAdapter, NativeAdapters, OwnerSnapshot, ObjectLifecycleHooks, PurchaseInteractionHandler, InteractorHandler, InstanceHandler/AccessPolicy/PendingOwnership, FadeBehavior, PingerControllerRenderBehaviour and Utils. Only Unity/RoR2/UNet boundaries are stubbed.

Current result: **82 assertions**. Coverage includes independent native consumption and immutable goods/cost/RNG; controller/terminal Start ordering and one Awake terminal group; native legal shop outcomes and reentry; repair upgrade SyncVar/activator; shrine refresh/state; disabled/unmanaged passthrough; missing SpawnCard and partial generation failure rollback; source destruction, failed copy initialization and terminal cleanup; both candidate paths with multiple components; split-screen/native enabled/nested view restoration; actual wire serialization, delayed and early session references, stale revisions, scene epochs, timeout/clear and netId reuse.

The delegates model boundary outcomes. They do not execute real PayCost, minion summoning, state machines, Unity rendering, real SyncVar dirtiness or packet transport. InteractorCandidateIL is stubbed here; its actual transformation/CLR execution is verified separately in PickupGrantILRegression. No stub pass is a host/client gameplay test.

The former shared-pickup/SharedInfo regression contract is superseded by 2.0 and remains in Git history. The independent purchase regression is retained under the correct contract: player one consuming the source leaves player two's native entity available and consumable. Historical validation records are preserved in docs/HISTORY_1_X.md.
