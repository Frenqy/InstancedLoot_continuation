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
var cloneSource = Obj(59);
cloneSource.transform.position = 100;
cloneSource.transform.rotation = 205;
cloneSource.transform.localScale = 3;
var spawnCard = new InteractableSpawnCard();
cloneSource.AddComponent<SpawnCardTracker>().SpawnCard = spawnCard;
var cloned = objectHandler.CloneObject("Chest1", cloneSource);
Check(cloned && spawnCard.LastRequest != null, "Production CloneObject reaches SpawnCard with a usable request");
Check(spawnCard.LastRequest.placementRule.placementMode == DirectorPlacementRule.PlacementMode.DirectWithoutRandomRotation,
    "Production clone request suppresses random rotation in native SpawnCard");
Check(spawnCard.LastRequest.placementRule.position == 100 && spawnCard.LastRequest.placementRule.rotation == 205 &&
    spawnCard.LastPosition == 100 && spawnCard.LastRotation == 205, "Clone rule and direct spawn arguments retain source position and rotation");
Check(cloned.transform.position == 100 && cloned.transform.rotation == 205 && cloned.transform.localScale == 3,
    "Clone restores complete source transform after SpawnCard placement");
var pricedSource = Obj(60);
var pricedCopy = Obj(61);
var sourcePurchase = pricedSource.AddComponent<PurchaseInteraction>();
sourcePurchase.cost = 735;
sourcePurchase.costType = 7;
sourcePurchase.rng = new(123UL);
var copyPurchase = pricedCopy.AddComponent<PurchaseInteraction>();
copyPurchase.cost = 25;
var sourceCopyHandler = objectHandler.InstanceSingleObjectFrom(pricedSource, pricedSource, [p1]);
sourceCopyHandler.SharedInfo = new() { SourceObject = pricedSource, ObjectInstanceMode = ObjectInstanceMode.CopyObject };
var cloneCopyHandler = objectHandler.InstanceSingleObjectFrom(pricedSource, pricedCopy, [p2]);
// A CopyObject source can temporarily carry the aggregate player set while a
// snapshot or handler-specific initialization is being rebuilt. Interaction
// eligibility must still follow OrigPlayer, and the clone must remain exclusive.
sourceCopyHandler.SetPlayers([p1, p2], false);
Check(sourceCopyHandler.OrigPlayer == p1 && sourceCopyHandler.IsInstancedFor(p1) &&
    !sourceCopyHandler.IsInstancedFor(p2), "Copy source uses OrigPlayer for interaction permission");
Check(cloneCopyHandler.OrigPlayer == p2 && cloneCopyHandler.IsInstancedFor(p2) &&
    !cloneCopyHandler.IsInstancedFor(p1), "Copy clone uses its own OrigPlayer for interaction permission");
sourceCopyHandler.SetPlayers([p1], false);
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
On.RoR2.PurchaseInteraction.Buy(sourcePurchase, Actor(p2));
Check(On.RoR2.PurchaseInteraction.Purchases == 0, "Reject other player's aggregate CopyObject source");
Check(InstancedLoot.InstancedLoot.Instance._logger.Infos.Count == 0,
    "Interaction diagnostics stay quiet when disabled");
InstancedLoot.InstancedLoot.Instance.InteractionLoggingEnabled = true;
On.RoR2.PurchaseInteraction.Buy(copyPurchase, Actor(p1));
Check(On.RoR2.PurchaseInteraction.Purchases == 0, "Reject other player's copy before purchase callback");
Check(InstancedLoot.InstancedLoot.Instance._logger.Infos.Any(message =>
    message.Contains("netId=") && message.Contains("OrigPlayer=") && message.Contains("Players=") &&
    message.Contains("available=") && message.Contains("buyer=")),
    "Opt-in interaction diagnostics include ownership and buyer state");
On.RoR2.PurchaseInteraction.Buy(copyPurchase, Actor(p2));
Check(On.RoR2.PurchaseInteraction.Purchases == 1, "Allow eligible player");
Check(!copyPurchase.available && sourcePurchase.available,
    "Close purchased handler while retaining the legal other-player copy");
copyPurchase.available = false;
On.RoR2.PurchaseInteraction.Buy(copyPurchase, Actor(p2));
Check(On.RoR2.PurchaseInteraction.Purchases == 1, "Reject unavailable purchase before callback");
copyPurchase.available = true;
hooks.UnregisterHooks();
NetworkServer.active = false;

// A client must not authorize an object while its source/clone snapshot is still
// waiting for a missing network object. Once the snapshot resolves, the handler is
// installed and normal eligibility resumes.
hooks.RegisterHooks();
var pendingPurchaseObject = Obj(100);
var pendingPurchase = pendingPurchaseObject.AddComponent<PurchaseInteraction>();
Message(200, (100, 1, [1])).OnReceived();
On.RoR2.PurchaseInteraction.Buy(pendingPurchase, Actor(p1));
Check(On.RoR2.PurchaseInteraction.Purchases == 1,
    "Reject client purchase while instance handler snapshot is pending");
Obj(200);
Scheduler.Tick();
Check(pendingPurchaseObject.GetComponent<InstanceHandler>()?.OrigPlayer == p1,
    "Install source/clone handler after all network references resolve");
hooks.UnregisterHooks();

// TripleShop controller copies keep one shared group, while each terminal has
// its own source/clone group.  This mirrors the asynchronous terminal path used
// after a controller clone is spawned.
var multiShop = new InstancedLoot.ObjectHandlers.MultiShopHandler();
multiShop.Init(new ObjectHandlerManager());
var multiSource = Obj(80);
var sourceController = multiSource.AddComponent<MultiShopController>();
sourceController.CreateTerminals();
var multiSourceHandler = multiShop.InstanceSingleObjectFrom(multiSource, multiSource, [p1]);
multiSourceHandler.SharedInfo = new() { SourceObject = multiSource, ObjectInstanceMode = ObjectInstanceMode.CopyObject };
var sourceTerminalHandlers = sourceController.terminalGameObjects
    .Select(terminal => terminal.GetComponent<InstanceHandler>()).ToArray();
Check(sourceTerminalHandlers.Length == 3 && sourceTerminalHandlers.All(handler => handler != null) &&
    sourceTerminalHandlers.Select(handler => handler.SharedInfo).Distinct().Count() == 3,
    "Each three-choice source terminal has its own SharedInfo group");
Check(sourceTerminalHandlers.All(handler => handler.OrigPlayer == p1 && handler.IsInstancedFor(p1) &&
    !handler.IsInstancedFor(p2)), "Three-choice source terminals belong only to first player");

var multiClone = Obj(81);
var cloneController = multiClone.AddComponent<MultiShopController>();
multiShop.InstanceSingleObjectFrom(multiSource, multiClone, [p2]);
var cloneTerminals = cloneController.terminalGameObjects;
foreach (var cloneTerminal in cloneTerminals)
    multiShop.HandleAwaitedObject(cloneTerminal);
var cloneTerminalHandlers = cloneTerminals.Select(terminal => terminal.GetComponent<InstanceHandler>()).ToArray();
Check(multiClone.GetComponent<InstanceHandler>().SharedInfo == multiSourceHandler.SharedInfo &&
    multiClone.GetComponent<InstanceHandler>().OrigPlayer == p2,
    "Three-choice controller clone shares only controller state");
Check(cloneTerminalHandlers.Length == 3 &&
    cloneTerminalHandlers.Zip(sourceTerminalHandlers).All(pair =>
        pair.First.SharedInfo == pair.Second.SharedInfo &&
        pair.First.SharedInfo != multiSourceHandler.SharedInfo &&
        pair.First.OrigPlayer == p2 && pair.First.IsInstancedFor(p2) &&
        !pair.First.IsInstancedFor(p1)),
    "Each three-choice clone terminal keeps matching group and clone permission");
Check(cloneTerminalHandlers.Select(handler => handler.SharedInfo).Distinct().Count() == 3,
    "Three-choice terminal groups remain independent by slot");

// Finalizing a controller must broadcast its controller group and all three
// independent terminal groups so clients can install every terminal handler.
R2API.Networking.Interfaces.Extensions.SentMessages.Clear();
NetworkServer.active = true;
multiShop.FinalizeObject(multiSource);
Check(R2API.Networking.Interfaces.Extensions.SentMessages.Count == 4,
    "Three-choice finalization broadcasts controller and all terminal groups");
NetworkServer.active = false;

// A three-choice shop is one purchase for each owner across all three slots.
// Closing the purchased owner's source terminals must leave the legal clone set
// available to the other player.
hooks.RegisterHooks();
NetworkServer.active = true;
var sourceTerminalPurchase = sourceController.terminalGameObjects[0].GetComponent<PurchaseInteraction>();
var purchasesBeforeReentry = On.RoR2.PurchaseInteraction.Purchases;
On.RoR2.PurchaseInteraction.DuringNativePurchase = (_, actor) =>
{
    On.RoR2.PurchaseInteraction.DuringNativePurchase = null;
    On.RoR2.PurchaseInteraction.Buy(sourceTerminalPurchase, actor);
};
On.RoR2.PurchaseInteraction.Buy(sourceTerminalPurchase, Actor(p1));
Check(On.RoR2.PurchaseInteraction.Purchases == purchasesBeforeReentry + 1,
    "Reentrant purchase in one SharedInfo group is executed only once");
Check(sourceController.terminalGameObjects.All(terminal =>
    !terminal.GetComponent<PurchaseInteraction>().available &&
    terminal.GetComponent<ShopTerminalBehavior>().Networkhidden),
    "One three-choice purchase closes every source terminal for that owner");
Check(cloneTerminals.All(terminal => terminal.GetComponent<PurchaseInteraction>().available &&
    !terminal.GetComponent<ShopTerminalBehavior>().Networkhidden),
    "Three-choice purchase retains every legal client clone");
hooks.UnregisterHooks();
NetworkServer.active = false;
InstancedLoot.InstancedLoot.Instance.InteractionLoggingEnabled = false;

// Interactor candidate filtering must resolve the player through the body/master
// chain carried by the Interactor GameObject. PlayerCharacterMasterController is
// attached to the master object, not to the Interactor itself.
var interactableObject = Obj(62);
var interactableHandler = interactableObject.AddComponent<InstanceHandler>();
interactableHandler.SetPlayers([p2], false);
interactableHandler.OrigPlayer = p2;
interactableHandler.SharedInfo = new() { ObjectInstanceMode = ObjectInstanceMode.CopyObject };
Check(!interactableHandler.IsInstancedForInteractor(Actor(p1)), "Reject ineligible interactor candidate");
Check(interactableHandler.IsInstancedForInteractor(Actor(p2)), "Accept eligible interactor candidate");

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
var fadeObject = Obj(63);
var fadeRenderer = fadeObject.AddComponent<Renderer>();
var fadeHandler = fadeObject.AddComponent<InstanceHandler>();
fadeHandler.SetPlayers([p1, p2], false);
fadeHandler.SharedInfo = new() { ObjectInstanceMode = ObjectInstanceMode.InstancedObject };
var fade = fadeObject.GetComponent<FadeBehavior>();
var camera1 = new CameraRigController { targetBody = Actor(p1).GetComponent<CharacterBody>() };
var camera2 = new CameraRigController { targetBody = Actor(p2).GetComponent<CharacterBody>() };
Check(fade.GetFadeLevelForCameraRigController(camera2) == 1f, "Eligible camera starts visible");
fadeHandler.RemovePlayer(p2);
Check(fade.GetFadeLevelForCameraRigController(camera2) == fade.FadeLevel, "Same-camera eligibility update invalidates dither state");
fadeHandler.SetPlayers([p1]);
Check(fade.GetFadeLevelForCameraRigController(camera1) == 1f && fade.GetFadeLevelForCameraRigController(camera2) == fade.FadeLevel, "Split-screen camera eligibility remains separate");
fade.RefreshForPreRender(p1);
Check(fadeRenderer.fade == 1f, "Own player's instanced object is fully visible");
fade.RefreshForPreRender(p2);
Check(fadeRenderer.fade == fade.FadeLevel, "Other player's instanced object is faded");
fadeHandler.OnDestroy();
fade.OnDestroy();
Check(fadeRenderer.enabled && fadeRenderer.fade == 1f, "Unloading restores renderers after group has detached");

Console.WriteLine($"PASS: {assertions} assertions against linked production sources (stub runtime, no real game or transport test).");

sealed class CopyHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes => [];
    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;
}
