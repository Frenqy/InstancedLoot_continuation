# Multiplayer regression harness

Run from the repository root with .NET 8 or newer:

```powershell
dotnet run --project validation/MultiplayerRegression --configuration Release
```

This dependency-free executable links the actual production `SyncInstances`,
`InstanceHandler`, `AbstractObjectHandler`, `PurchaseInteractionHandler`,
`PickupGrantPolicy` and `FadeBehavior` sources. A deterministic stub boundary controls spawn timing,
coroutine scheduling, game-object destruction, camera ownership and purchase callbacks.
Assertions cover delayed source resolution beyond 40 frames, superseded pending
snapshots, destroyed references, timeout/run/disable cleanup, group destruction,
cost setter use and independent RNG, server purchase eligibility, failed/successful/
last-player pickup grants, repeated-grant rejection and camera state.

The stubs model destroyed Unity objects as false/null for overloaded equality;
they do not run actual Unity Object lifetimes or MonoBehaviour callbacks. Tests
invoke relevant lifecycle methods and advance coroutine scheduling explicitly.
They deliberately do not emulate real UNet serialization/SyncVar dirty bits,
R2API transport, RoR2 method IL, actual rendering or two installed game clients.
Passing this harness does not establish game-version or multiplayer compatibility.
