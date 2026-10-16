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
non-null clone placement rules and preserved rotation/transform, cost setter use
and independent RNG, CopyObject source/clone permission, independent
three-choice terminal groups, reuse of Awake-spawned terminals and awaited-slot broadcasts, pending client
purchase rejection, native-controlled terminal closure/cooldowns, cross-slot reentry rejection, server purchase eligibility, failed/successful/
last-player pickup grants, repeated-grant rejection and camera state.

The stubs model destroyed Unity objects as false/null for overloaded equality;
they do not run actual Unity Object lifetimes or MonoBehaviour callbacks. Tests
invoke relevant lifecycle methods and advance coroutine scheduling explicitly.
They deliberately do not emulate real UNet serialization/SyncVar dirty bits,
R2API transport, RoR2 method IL, actual rendering or two installed game clients.
Passing this harness does not establish game-version or multiplayer compatibility.

On 2026-10-16, the revised 1.5.4 Release run passed 69 assertions. These include
no second CreateTerminals call, preserved native sibling state, cooldown restoration,
cross-slot reentry, copy visibility after the viewer's own copy is destroyed,
same-camera ownership changes, prompt log deduplication, correct outgoing target IDs
and missing-reference reasons. The earlier 57-assertion run omitted native Awake
and wrongly expected the mod to force-close siblings; it did not validate this lifecycle.
The earlier 1.5.3 run
passed 48; the additional checks cover pending client purchase rejection,
the then-proposed atomic three-choice closure, terminal-group broadcast, same-group reentry,
and opt-in interaction diagnostics. The earlier 1.5.2 run
passed 39; the additional checks cover the CopyObject source/clone permission
split and independent three-choice terminal groups. The earlier 1.5.0 adaptation
record had 33. See
[DEVELOPMENT.md](../../docs/DEVELOPMENT.md) for the separate installed-assembly
IL checks and outstanding game tests.
