using InstancedLoot;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using InstancedLoot.Networking;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

int assertions = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    assertions++;
}
GameObject Obj(uint id, bool player = false)
{
    var obj = new GameObject();
    obj.AddComponent<NetworkIdentity>().netId = new(id);
    if (player) obj.AddComponent<PlayerCharacterMasterController>();
    Util.Objects[new(id)] = obj;
    return obj;
}
SyncInstances Message(uint source, params (uint target, uint owner, uint[] players)[] entries)
{
    var writer = new NetworkWriter();
    writer.Write((int)ObjectInstanceMode.CopyObject);
    writer.Write(new NetworkInstanceId(source));
    writer.Write(entries.Length);
    foreach (var (target, owner, players) in entries)
    {
        writer.Write(new NetworkInstanceId(target));
        writer.Write(new NetworkInstanceId(owner));
        writer.Write(players.Length);
        foreach (uint player in players) writer.Write(new NetworkInstanceId(player));
    }
    var message = new SyncInstances();
    message.Deserialize(new(writer.stream.ToArray()));
    return message;
}
int PendingCount() => ((System.Collections.ICollection)typeof(SyncInstances).GetField("PendingMessages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null)).Count;
var p1 = Obj(1, true).GetComponent<PlayerCharacterMasterController>();
var p2 = Obj(2, true).GetComponent<PlayerCharacterMasterController>();

// The source must arrive even when every target/player already exists. 100 frames
// exceeds the old frame-based limit, while remaining within the new real-time limit.
var target = Obj(10);
Message(20, (10, 1, [1])).OnReceived();
for (int i = 0; i < 100; i++) Scheduler.Tick();
Check(!target.GetComponent<InstanceHandler>(), "Do not accept missing source");
var source = Obj(20);
source.transform.position = 42;
Scheduler.Tick();
var handler = target.GetComponent<InstanceHandler>();
Check(handler && handler.Players.Contains(p1) && target.transform.position == 42, "Resolve source after target");
Check(PendingCount() == 0, "Completed snapshot removed");

// A new snapshot can resolve before an old snapshot that still needs player 99.
Message(0, (10, 1, [1, 99])).OnReceived();
Message(0, (10, 2, [2])).OnReceived();
Obj(99, true);
Scheduler.Tick();
Check(handler.Players.SetEquals([p2]) && handler.OrigPlayer == p2, "Older coroutine cannot restore stale permissions");
Check(PendingCount() == 0, "Superseded snapshot removed");
var roundTrip = new NetworkWriter();
new SyncInstances(handler.SharedInfo).Serialize(roundTrip);
Check(roundTrip.stream.Length == 28, "Original binary field layout remains unchanged");
var decoded = new SyncInstances();
decoded.Deserialize(new(roundTrip.stream.ToArray()));
decoded.OnReceived();
Check(handler.Players.SetEquals([p2]) && handler.OrigPlayer == p2, "Production serializer and reader round trip");
Message(0, (10, 0, [])).OnReceived();
Check(handler.OrigPlayer == null && handler.Players.Count == 0, "Clear previous owner and eligibility");

// Recheck already resolved objects; do not use the cached, now-destroyed target.
var vanishing = Obj(30);
var delayed = Message(0, (30, 1, [1]), (31, 2, [2]));
delayed.OnReceived();
UnityEngine.Object.Destroy(vanishing);
Obj(31);
Scheduler.Tick();
Check(!vanishing.GetComponent<InstanceHandler>(), "Do not apply to destroyed cached target");
Scheduler.Tick(31);
Check(PendingCount() == 0 && InstancedLoot.InstancedLoot.FailedSyncs.Count == 1, "Timeout removes pending message");

Message(0, (40, 1, [1])).OnReceived();
Run.instance = new();
Scheduler.Tick();
Check(PendingCount() == 0, "Run transition cancels pending message");
Message(0, (41, 1, [1])).OnReceived();
InstancedLoot.InstancedLoot.Instance.enabled = false;
Scheduler.Tick();
Check(PendingCount() == 0, "Disable cancels pending message");
InstancedLoot.InstancedLoot.Instance.enabled = true;
Message(0, (42, 1, [1])).OnReceived();
SyncInstances.ClearPending(); // Production plugin calls this synchronously in OnDisable.
Obj(42);
Scheduler.Tick();
Check(PendingCount() == 0 && !Util.FindNetworkObject(new(42)).GetComponent<InstanceHandler>(), "Disable/reenable in same frame cannot revive old snapshot");
InstancedLoot.InstancedLoot.Instance = new();
Obj(41);
Message(0, (41, 2, [2])).OnReceived();
Check(Util.FindNetworkObject(new(41)).GetComponent<InstanceHandler>().Players.SetEquals([p2]), "Reenabled receiver accepts new snapshot");

// Shared group destruction must remove dead members before future serialization.
var groupSource = Obj(50);
var groupCopy = Obj(51);
Message(50, (50, 1, [1]), (51, 2, [2])).OnReceived();
var first = groupSource.GetComponent<InstanceHandler>();
var second = groupCopy.GetComponent<InstanceHandler>();
var group = first.SharedInfo;
first.OnDestroy();
Check(group.LinkedHandlers.Count == 1 && group.SourceObject == null && group.AllPlayers.SetEquals([p2]), "Destroy removes source and aggregated eligibility");
new SyncInstances(group).Serialize(new NetworkWriter());
second.OnDestroy();
Check(group.LinkedHandlers.Count == 0 && group.AllPlayers.Count == 0, "Destroy complete group");

// Clone initialization copies the final source cost via the network property. It
// must not establish a runtime price link, so shrines can change independently.
var objectHandler = new CopyHandler();
objectHandler.Init(new());
var pricedSource = Obj(60);
var pricedCopy = Obj(61);
var sourcePurchase = pricedSource.AddComponent<PurchaseInteraction>();
sourcePurchase.cost = 735;
sourcePurchase.costType = 7;
sourcePurchase.rng = new(123UL);
var copyPurchase = pricedCopy.AddComponent<PurchaseInteraction>();
var copyRenderer = pricedCopy.AddComponent<Renderer>();
copyPurchase.cost = 25;
objectHandler.InstanceSingleObjectFrom(pricedSource, pricedSource, [p1]).SharedInfo = new() { SourceObject = pricedSource, ObjectInstanceMode = ObjectInstanceMode.CopyObject };
objectHandler.InstanceSingleObjectFrom(pricedSource, pricedCopy, [p2]);
Check(copyPurchase.cost == 735 && copyPurchase.costType == 7 && copyPurchase.CostWrites == 1 && copyPurchase.CostTypeWrites == 1, "Copy final cost with SyncVar setters");
Check(sourcePurchase.CostWrites == 0 && !ReferenceEquals(copyPurchase.rng, sourcePurchase.rng) && copyPurchase.rng.State == 123, "Preserve source and independently copy RNG");
sourcePurchase.cost = 999;
Check(copyPurchase.cost == 735, "No runtime lock between copy prices");

// Direct server purchase calls also enforce eligibility, not just UI prompts.
var hooks = new PurchaseInteractionHandler();
hooks.RegisterHooks();
Interactor Actor(PlayerCharacterMasterController player)
{
    var actor = new GameObject();
    var body = actor.AddComponent<CharacterBody>();
    body.master = new GameObject().AddComponent<CharacterMaster>();
    body.master.playerCharacterMasterController = player;
    return actor.AddComponent<Interactor>();
}
NetworkServer.active = true;
On.RoR2.PurchaseInteraction.Buy(copyPurchase, Actor(p1));
Check(On.RoR2.PurchaseInteraction.Purchases == 0, "Reject other player's copy before purchase callback");
On.RoR2.PurchaseInteraction.Buy(copyPurchase, Actor(p2));
Check(On.RoR2.PurchaseInteraction.Purchases == 1, "Allow eligible player");
hooks.UnregisterHooks();
NetworkServer.active = false;

var pickupObject = Obj(70);
var pickup = pickupObject.AddComponent<GenericPickupController>();
var body1 = Actor(p1).GetComponent<CharacterBody>();
var body2 = Actor(p2).GetComponent<CharacterBody>();
Check(PickupGrantPolicy.CanGrant(pickup, body1), "Vanilla pickup retains original grant behavior");
var pickupHandler = pickupObject.AddComponent<InstanceHandler>();
pickupHandler.SetPlayers([p1, p2], false);
pickupHandler.SharedInfo = new() { ObjectInstanceMode = ObjectInstanceMode.InstancedObject };
var context = new PickupDef.GrantContext { shouldDestroy = false };
PickupGrantPolicy.OnGranted(ref context, pickup, body1);
Check(pickupHandler.Players.SetEquals([p1, p2]) && !context.shouldDestroy, "Failed native grant preserves all eligibility");
context.shouldDestroy = true;
PickupGrantPolicy.OnGranted(ref context, pickup, body1);
Check(pickupHandler.Players.SetEquals([p2]) && !context.shouldDestroy, "First successful grant preserves shared pickup in native context");
Check(!PickupGrantPolicy.CanGrant(pickup, body1) && PickupGrantPolicy.CanGrant(pickup, body2), "Reject repeated grants before native grant logic");
Check(!PickupGrantPolicy.CanGrant(pickup, null), "Instanced pickup rejects missing player body");
context.shouldDestroy = true;
PickupGrantPolicy.OnGranted(ref context, pickup, body2);
Check(pickupHandler.Players.Count == 0 && context.shouldDestroy, "Last successful grant allows native destruction");

// Dither visibility must change when permissions change while viewing with the
// same camera/player, and split-screen cameras retain their own player lookup.
var fade = pricedCopy.GetComponent<FadeBehavior>();
var camera1 = new CameraRigController { targetBody = Actor(p1).GetComponent<CharacterBody>() };
var camera2 = new CameraRigController { targetBody = Actor(p2).GetComponent<CharacterBody>() };
Check(fade.GetFadeLevelForCameraRigController(camera2) == 1f, "Eligible camera starts visible");
pricedCopy.GetComponent<InstanceHandler>().RemovePlayer(p2);
Check(fade.GetFadeLevelForCameraRigController(camera2) == fade.FadeLevel, "Same-camera eligibility update invalidates dither state");
pricedCopy.GetComponent<InstanceHandler>().SetPlayers([p1]);
Check(fade.GetFadeLevelForCameraRigController(camera1) == 1f && fade.GetFadeLevelForCameraRigController(camera2) == fade.FadeLevel, "Split-screen camera eligibility remains separate");
fade.RefreshForPreCull(p1);
Check(!copyRenderer.enabled, "Other player's model and hologram renderers are hidden");
fade.RefreshForPreCull(p2);
Check(copyRenderer.enabled, "Own model is restored for second camera");
fade.RefreshForPreCull(p1);
pricedCopy.GetComponent<InstanceHandler>().OnDestroy();
fade.OnDestroy();
Check(copyRenderer.enabled && copyRenderer.fade == 1f, "Unloading restores renderers after group has detached");

Console.WriteLine($"PASS: {assertions} assertions against linked production sources (stub runtime, no real game or transport test).");

sealed class CopyHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes => [];
    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;
}
