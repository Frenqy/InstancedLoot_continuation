using InstancedLoot;
using InstancedLoot.Components;
using InstancedLoot.Hooks;
using InstancedLoot.Networking;
using InstancedLoot.ObjectInstancing;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

int assertions = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }
uint nextId = 100;
GameObject Obj(string name = "Chest1(Clone)")
{
    var target = new GameObject { name = name };
    target.AddComponent<NetworkIdentity>().netId = new(++nextId);
    Util.Objects[target.GetComponent<NetworkIdentity>().netId] = target;
    return target;
}
void Index(GameObject target)
{
    if (!target.GetComponent<NetworkIdentity>()) target.AddComponent<NetworkIdentity>().netId = new(++nextId);
    Util.Objects[target.GetComponent<NetworkIdentity>().netId] = target;
}
var plugin = new InstancedLoot.InstancedLoot();
var p1 = Obj("p1").AddComponent<PlayerCharacterMasterController>();
var p2 = Obj("p2").AddComponent<PlayerCharacterMasterController>();
PlayerCharacterMasterController.instances.AddRange([p1, p2]);
Interactor Actor(PlayerCharacterMasterController player)
{
    var target = Obj("actor");
    var body = target.AddComponent<CharacterBody>();
    body.master = Obj("master").AddComponent<CharacterMaster>();
    body.master.playerCharacterMasterController = player;
    player.body = body;
    return target.AddComponent<Interactor>();
}
var actor1 = Actor(p1);
var actor2 = Actor(p2);
var life = new ObjectLifecycleHooks();
var buy = new PurchaseInteractionHandler();
var interact = new InteractorHandler();
life.RegisterHooks(); buy.RegisterHooks(); interact.RegisterHooks();
NetworkServer.active = true;
GameObject Chest()
{
    var target = Obj();
    target.AddComponent<ChestBehavior>();
    target.AddComponent<PurchaseInteraction>().cost = 700;
    target.AddComponent<Renderer>();
    return target;
}
var source = Chest();
var created = new List<GameObject>();
source.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => { var copy = Chest(); created.Add(copy); return copy; } };
int sourceStarts = 0, cloneStarts = 0;
On.RoR2.ChestBehavior.Fire(source.GetComponent<ChestBehavior>(), () => { sourceStarts++; source.GetComponent<ChestBehavior>().rng.State = 431; });
Check(PendingOwnership.IsPending(source) && !InstanceAccessPolicy.CanInteract(source, actor1), "Source initialization blocks purchase until complete");
plugin.Objects.Pump();
Check(sourceStarts == 1 && created.Count == 1, "One native source Start and N-1 native entities");
var clone = created.Single();
source.GetComponent<PurchaseInteraction>().cost = 999;
source.GetComponent<ChestBehavior>().currentPickup = "consumed-source";
On.RoR2.ChestBehavior.Fire(clone.GetComponent<ChestBehavior>(), () => cloneStarts++);
Check(cloneStarts == 0, "Copy never rerolls via native Chest.Start");
Check(clone.GetComponent<ChestBehavior>().currentPickup.Equals("item1") && clone.GetComponent<ChestBehavior>().rng.State == 431,
    "Copy applies immutable initialized pickup and RNG snapshot after source changes");
Check(clone.GetComponent<PurchaseInteraction>().cost == 700 && clone.GetComponent<PurchaseInteraction>().CostWrites == 1,
    "Final source money price copied through native SyncVar setter");
Check(source.GetComponent<InstanceHandler>().Owner == p1 && clone.GetComponent<InstanceHandler>().Owner == p2,
    "Each independently consumable entity has one distinct owner");
Check(!InstanceAccessPolicy.CanInteract(clone, actor1) && InstanceAccessPolicy.CanInteract(clone, actor2), "Body/master ownership resolves correctly");
int paid1 = 0, paid2 = 0;
On.RoR2.PurchaseInteraction.Buy(source.GetComponent<PurchaseInteraction>(), actor2, () => paid1++);
Check(paid1 == 0, "Direct unauthorized purchase cannot pay or invoke purchase events");
On.RoR2.PurchaseInteraction.Buy(source.GetComponent<PurchaseInteraction>(), actor1, () => { paid1++; source.GetComponent<PurchaseInteraction>().available = false; });
Check(paid1 == 1 && clone.GetComponent<PurchaseInteraction>().available, "First player consumes source without consuming the other entity");
On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => { paid2++; clone.GetComponent<PurchaseInteraction>().available = false; });
Check(paid2 == 1, "Second player still pays and consumes their own native object");
On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => paid2++);
Check(paid2 == 1, "Unavailable duplicate purchase rejected");
clone.GetComponent<PurchaseInteraction>().available = true;
On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () =>
{
    paid2++;
    On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => paid2 += 99);
});
Check(paid2 == 2, "Same-entity purchase reentry is rejected");
try { On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => throw new InvalidOperationException()); } catch (InvalidOperationException) { }
On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => paid2++);
Check(paid2 == 3, "Exception releases purchase reentry guard");
On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => clone.GetComponent<PurchaseInteraction>().available = false);
clone.GetComponent<PurchaseInteraction>().available = true;
On.RoR2.PurchaseInteraction.Buy(clone.GetComponent<PurchaseInteraction>(), actor2, () => paid2++);
Check(paid2 == 4 && clone.GetComponent<InstanceHandler>().Owner == p2, "Native shrine refresh preserves owner and supports subsequent purchases");
On.RoR2.Interactor.Perform(actor1, clone);
On.RoR2.Interactor.Perform(actor2, clone);
Check(On.RoR2.Interactor.Interactions == 1, "Server PerformInteraction guards all native IInteractable components");

GameObject Shop()
{
    var root = Obj("TripleShop(Clone)");
    var controller = root.AddComponent<MultiShopController>(); // Native Awake creates terminals once.
    foreach (var terminal in controller.terminalGameObjects) Index(terminal);
    return root;
}
var shopSource = Shop();
var sourceShop = shopSource.GetComponent<MultiShopController>();
sourceShop.Networkcost = 444;
sourceShop.Networkavailable = true;
for (int i = 0; i < 3; i++)
{
    var terminal = sourceShop.terminalGameObjects[i];
    terminal.GetComponent<ShopTerminalBehavior>().Networkpickup = "choice" + i;
    terminal.GetComponent<PurchaseInteraction>().cost = 444;
}
GameObject shopCopy = null;
shopSource.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => shopCopy = Shop() };
On.RoR2.MultiShopController.Fire(sourceShop, () => { });
Check(sourceShop.terminalGameObjects.All(PendingOwnership.IsPending), "Unready source shop gates every terminal, not only the root");
int premature = 0;
On.RoR2.PurchaseInteraction.Buy(sourceShop.terminalGameObjects[0].GetComponent<PurchaseInteraction>(), actor1, () => premature++);
plugin.Objects.Pump();
Check(premature == 0 && shopCopy == null, "Cannot buy or capture shop before terminal Start completes");
foreach (var terminal in sourceShop.terminalGameObjects)
    On.RoR2.ShopTerminalBehavior.Fire(terminal.GetComponent<ShopTerminalBehavior>(), () => terminal.GetComponent<ShopTerminalBehavior>().hasStarted = true);
plugin.Objects.Pump();
Check(shopCopy && sourceShop.Creates == 1 && shopCopy.GetComponent<MultiShopController>().Creates == 1, "Exactly one Awake terminal group per native shop entity");
int terminalRerolls = 0;
foreach (var terminal in shopCopy.GetComponent<MultiShopController>().terminalGameObjects)
    On.RoR2.ShopTerminalBehavior.Fire(terminal.GetComponent<ShopTerminalBehavior>(), () => terminalRerolls++);
Check(terminalRerolls == 0, "Copy terminals skip native Start even when they start before their controller");
On.RoR2.MultiShopController.Fire(shopCopy.GetComponent<MultiShopController>(), () => throw new Exception("Copy controller native Start reran"));
var copyShop = shopCopy.GetComponent<MultiShopController>();
Check(copyShop.terminalGameObjects.Select(t => t.GetComponent<ShopTerminalBehavior>().Networkpickup).SequenceEqual(new[] { "choice0", "choice1", "choice2" }), "Three choices preserve exact source goods");
Check(copyShop.terminalGameObjects.All(t => t.GetComponent<PurchaseInteraction>().cost == 444 && t.GetComponent<InstanceHandler>().Owner == p2), "Every copied shop terminal receives matching price and exclusive owner");
Check(sourceShop.terminalGameObjects[1].GetComponent<InstanceHandler>().GroupId == copyShop.terminalGameObjects[1].GetComponent<InstanceHandler>().GroupId &&
      sourceShop.terminalGameObjects[0].GetComponent<InstanceHandler>().GroupId != copyShop.terminalGameObjects[1].GetComponent<InstanceHandler>().GroupId,
    "Corresponding terminal slots share a visual group, different slots remain distinct");
var terminalPurchase = sourceShop.terminalGameObjects[0].GetComponent<PurchaseInteraction>();
On.RoR2.PurchaseInteraction.Buy(terminalPurchase, actor1, () => { foreach (var t in sourceShop.terminalGameObjects) t.GetComponent<PurchaseInteraction>().available = false; });
Check(sourceShop.terminalGameObjects.All(t => !t.GetComponent<PurchaseInteraction>().available) && copyShop.terminalGameObjects.All(t => t.GetComponent<PurchaseInteraction>().available), "Native one-of-three closure stays within one player's controller");
terminalPurchase.available = true;
On.RoR2.PurchaseInteraction.Buy(terminalPurchase, actor1, () => { }); // Native Executive Card may leave other terminals available.
Check(terminalPurchase.available, "No forced shop closure overrides native legal special mechanics");
sourceShop.terminalGameObjects[1].GetComponent<PurchaseInteraction>().available = true; int nestedSlots = 0;
On.RoR2.PurchaseInteraction.Buy(terminalPurchase, actor1, () => On.RoR2.PurchaseInteraction.Buy(sourceShop.terminalGameObjects[1].GetComponent<PurchaseInteraction>(), actor1, () => nestedSlots++));
Check(nestedSlots == 0, "Reentrant purchases across terminals share the same native-controller transaction guard");

var noCard = Chest();
On.RoR2.ChestBehavior.Fire(noCard.GetComponent<ChestBehavior>(), () => { });
var noCardFade = noCard.GetComponent<FadeBehavior>();
noCardFade.RefreshForPreCull(p1);
plugin.Objects.Pump();
noCardFade.RefreshForPreCull(p1); noCardFade.RefreshForPreRender(p1);
Check(!noCard.GetComponent<InstanceHandler>() && !PendingOwnership.IsPending(noCard) && noCard.GetComponent<Renderer>().enabled && noCard.GetComponent<Renderer>().fade == 1f,
    "Missing SpawnCard restores complete vanilla access and display for all players");
var doomedSource = Chest(); GameObject independent = null;
doomedSource.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => independent = Chest() };
plugin.Objects.InitializeSource(new NativeAdapters.Chest(), doomedSource, "Chest1"); plugin.Objects.Pump();
NetworkServer.Destroy(doomedSource);
Check(plugin.Objects.InitializeCopy(independent) && independent.GetComponent<InstanceHandler>().Owner == p2, "Source destruction cannot cancel another player's immutable copy initialization");
var failedSource = Chest(); GameObject failedCopy = null;
failedSource.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => failedCopy = Chest() };
plugin.Objects.InitializeSource(new FailingAdapter(), failedSource, "Chest1"); plugin.Objects.Pump();
Check(plugin.Objects.InitializeCopy(failedCopy) && !failedCopy, "Failed adapter initialization destroys its network entity instead of leaving unowned loot");
var clearSource = Shop(); GameObject clearCopy = null;
foreach (var t in clearSource.GetComponent<MultiShopController>().terminalGameObjects) t.GetComponent<ShopTerminalBehavior>().hasStarted = true;
clearSource.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => clearCopy = Shop() };
plugin.Objects.InitializeSource(new NativeAdapters.MultiShop(), clearSource, "TripleShop"); plugin.Objects.Pump();
var orphanCandidates = clearCopy.GetComponent<MultiShopController>().terminalGameObjects;
plugin.Objects.Clear();
Check(!clearCopy && orphanCandidates.All(t => !t), "Transition cleanup destroys pending root and every independent network terminal");


// Adapters for the explicitly supported shrine and repair lifecycles.
GameObject Repair()
{
    var target = Obj("Drone1Broken(Clone)"); target.AddComponent<PurchaseInteraction>().cost = 889;
    var summon = target.AddComponent<SummonMasterBehavior>(); summon.masterPrefab = Obj("DroneMaster"); summon.destroyAfterSummoning = true;
    return target;
}
var repairSource = Repair(); var sourceSummon = repairSource.GetComponent<SummonMasterBehavior>(); sourceSummon.droneUpgradeCount = 3;
GameObject repairCopy = null;
repairSource.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => repairCopy = Repair() };
int sourceRepairStarts = 0, copyRepairStarts = 0;
On.RoR2.SummonMasterBehavior.Fire(sourceSummon, () => sourceRepairStarts++); plugin.Objects.Pump();
On.RoR2.SummonMasterBehavior.Fire(repairCopy.GetComponent<SummonMasterBehavior>(), () => copyRepairStarts++);
Check(sourceRepairStarts == 1 && copyRepairStarts == 1, "Repair display Start runs once for source and once after copied upgrade state");
var copySummon = repairCopy.GetComponent<SummonMasterBehavior>();
Check(copySummon.droneUpgradeCount == 3 && copySummon.UpgradeWrites == 1 && copySummon.masterPrefab == sourceSummon.masterPrefab && copySummon.destroyAfterSummoning,
    "Damaged repair object preserves master prefab, networked upgrades and destroy-after-summoning policy");
PlayerCharacterMasterController summonedOwner = null;
On.RoR2.PurchaseInteraction.Buy(repairCopy.GetComponent<PurchaseInteraction>(), actor1, () => summonedOwner = p1);
Check(summonedOwner == null, "Other player cannot summon through a foreign repair object");
On.RoR2.PurchaseInteraction.Buy(repairCopy.GetComponent<PurchaseInteraction>(), actor2, () => { summonedOwner = InstanceAccessPolicy.ResolvePlayer(actor2); NetworkServer.Destroy(repairCopy); });
Check(summonedOwner == p2 && repairSource, "Native repair receives its original activator and consumes only its own damaged entity");
var livingDrone = Obj("Drone1Body").AddComponent<SummonMasterBehavior>();
On.RoR2.SummonMasterBehavior.Fire(livingDrone, () => { }); plugin.Objects.Pump();
Check(!livingDrone.GetComponent<InstanceHandler>(), "Active body or non-purchasable summon is never claimed as a repair instance");
var chanceSource = Obj("ShrineChance"); chanceSource.AddComponent<PurchaseInteraction>().cost = 123;
chanceSource.AddComponent<ShrineChanceBehavior>().rng.State = 992;
var chanceCopy = Obj("ShrineChance"); chanceCopy.AddComponent<PurchaseInteraction>(); chanceCopy.AddComponent<ShrineChanceBehavior>();
var shrineAdapter = new NativeAdapters.Shrine(); var chanceState = shrineAdapter.CaptureState(chanceSource);
chanceSource.GetComponent<ShrineChanceBehavior>().rng.State = 1; chanceState(chanceCopy);
Check(chanceCopy.GetComponent<ShrineChanceBehavior>().rng.State == 992 && chanceCopy.GetComponent<ShrineChanceBehavior>().purchaseInteraction == chanceCopy.GetComponent<PurchaseInteraction>(),
    "Chance shrine keeps independent RNG and its own purchase component");
var orderSource = Obj("ShrineRestack"); orderSource.AddComponent<PurchaseInteraction>(); orderSource.AddComponent<ShrineRestackBehavior>().rng.State = 332;
var orderCopy = Obj("ShrineRestack"); orderCopy.AddComponent<PurchaseInteraction>(); orderCopy.AddComponent<ShrineRestackBehavior>();
shrineAdapter.CaptureState(orderSource)(orderCopy);
Check(orderCopy.GetComponent<ShrineRestackBehavior>().rng.State == 332 && !ReferenceEquals(orderCopy.GetComponent<ShrineRestackBehavior>().rng, orderSource.GetComponent<ShrineRestackBehavior>().rng), "Order shrine RNG is an independent snapshot");
var bloodSource = Obj("ShrineBlood"); bloodSource.AddComponent<PurchaseInteraction>().cost = 75; bloodSource.AddComponent<ShrineBloodBehavior>();
var bloodCopy = Obj("ShrineBlood"); bloodCopy.AddComponent<PurchaseInteraction>(); bloodCopy.AddComponent<ShrineBloodBehavior>();
shrineAdapter.CaptureState(bloodSource)(bloodCopy);
Check(bloodCopy.GetComponent<PurchaseInteraction>().cost == 75 && bloodCopy.GetComponent<ShrineBloodBehavior>().purchaseInteraction == bloodCopy.GetComponent<PurchaseInteraction>(), "Blood shrine copies its cost and uses its own native refresh component");
var optionSource = Obj("VoidTriple"); optionSource.AddComponent<PurchaseInteraction>(); optionSource.AddComponent<OptionChestBehavior>();
var optionCopy = Obj("VoidTriple"); optionCopy.AddComponent<PurchaseInteraction>(); optionCopy.AddComponent<OptionChestBehavior>();
var optionState = new NativeAdapters.OptionChest().CaptureState(optionSource); optionSource.GetComponent<OptionChestBehavior>().generatedPickups.Clear(); optionState(optionCopy);
Check(optionCopy.GetComponent<OptionChestBehavior>().generatedPickups.Count == 3, "Option chest snapshots its choices instead of sharing a mutable list");
var extraOption = Obj("VoidTriple"); extraOption.AddComponent<PurchaseInteraction>(); extraOption.AddComponent<OptionChestBehavior>(); optionState(extraOption);
optionCopy.GetComponent<OptionChestBehavior>().generatedPickups.Clear();
Check(extraOption.GetComponent<OptionChestBehavior>().generatedPickups.Count == 3, "Each option copy gets its own choice list");
var rouletteSource = Obj("CasinoChest"); rouletteSource.AddComponent<PurchaseInteraction>(); rouletteSource.AddComponent<RouletteChestController>().rng.State = 440;
var rouletteCopy = Obj("CasinoChest"); rouletteCopy.AddComponent<PurchaseInteraction>(); rouletteCopy.AddComponent<RouletteChestController>();
new NativeAdapters.Roulette().CaptureState(rouletteSource)(rouletteCopy);
Check(rouletteCopy.GetComponent<RouletteChestController>().rng.State == 440, "Adaptive chest preserves the initialized roulette RNG");
var p3 = Obj("p3").AddComponent<PlayerCharacterMasterController>(); PlayerCharacterMasterController.instances.Add(p3);
var rollbackSource = Chest(); GameObject partialCopy = null; int spawnAttempts = 0;
rollbackSource.AddComponent<SpawnCardTracker>().SpawnCard = new InteractableSpawnCard { Factory = () => ++spawnAttempts == 1 ? partialCopy = Chest() : throw new InvalidOperationException("injected second spawn failure") };
plugin.Objects.InitializeSource(new NativeAdapters.Chest(), rollbackSource, "Chest1"); plugin.Objects.Pump();
Check(!partialCopy && !rollbackSource.GetComponent<InstanceHandler>() && !PendingOwnership.IsPending(rollbackSource), "A later spawn exception rolls back earlier copies and leaves complete vanilla source");
Check(!plugin.Objects.InitializeCopy(partialCopy), "Rolled-back clone cannot later initialize from leftover pending state");
PlayerCharacterMasterController.instances.Remove(p3);
plugin.ModConfig.Enabled = false; var toggledOff = Chest();
On.RoR2.ChestBehavior.Fire(toggledOff.GetComponent<ChestBehavior>(), () => { }); plugin.Objects.Pump();
Check(!PendingOwnership.IsPending(toggledOff) && !toggledOff.GetComponent<InstanceHandler>(), "Disabled object policy retains native source and access"); plugin.ModConfig.Enabled = true;

// Both candidate search paths use the same policy, while unmanaged printers retain exact native component selection.
var mixed = Obj(); var disabled = mixed.AddComponent<TestInteractable>(); disabled.enabled = false;
var active = mixed.AddComponent<PurchaseInteraction>(); mixed.AddComponent<InstanceHandler>().Assign(p2, 333);
foreach (bool sphere in new[] { false, true })
{
    Check(!InteractorHandler.TryCandidate(mixed, out _, actor1, sphere), "Foreign raycast/overlap candidate rejected");
    Check(InteractorHandler.TryCandidate(mixed, out var result, actor2, sphere) && ReferenceEquals(result, active), "Disabled picker cannot shadow owned purchase in either search");
}
var printer = Obj("Duplicator"); var nativeFirst = printer.AddComponent<TestInteractable>(); nativeFirst.enabled = false;
printer.AddComponent<PurchaseInteraction>();
Check(InteractorHandler.TryCandidate(printer, out var originalCandidate, actor1, false) && ReferenceEquals(originalCandidate, nativeFirst), "Unmanaged printer keeps native TryGetComponent selection");
int vanillaPurchase = 0;
On.RoR2.PurchaseInteraction.Buy(printer.GetComponent<PurchaseInteraction>(), actor1, () => vanillaPurchase++);
Check(vanillaPurchase == 1, "Unmanaged printer purchase events are untouched");
printer.GetComponent<PurchaseInteraction>().available = false;
On.RoR2.PurchaseInteraction.Buy(printer.GetComponent<PurchaseInteraction>(), actor1, () => vanillaPurchase++);
Check(vanillaPurchase == 2, "Unmanaged purchase remains an exact pass-through even when native availability is false");
var fadeRenderer = clone.GetComponent<Renderer>(); var fade = clone.GetComponent<FadeBehavior>();
fade.RefreshForPreCull(p1); Check(fadeRenderer.forceRenderingOff, "Foreign player model hidden");
fade.RefreshForPreCull(p2); Check(!fadeRenderer.forceRenderingOff, "Next split-screen camera sees its own model");
fade.RefreshForPreCull(p1); Check(fadeRenderer.forceRenderingOff, "Each camera reapplies visibility without aggregate membership gating");
fade.RestoreAfterCamera();
Check(!fadeRenderer.forceRenderingOff && fadeRenderer.enabled, "PostRender restores the native renderer mask");
fadeRenderer.enabled = false; fade.RefreshForPreCull(p2);
Check(!fadeRenderer.enabled && !fadeRenderer.forceRenderingOff, "Owned view never re-enables a natively disabled renderer");
fade.OnDestroy(); Check(!fadeRenderer.enabled && !fadeRenderer.forceRenderingOff, "Unload restores native state rather than enabling every model");

// Nested native roots and terminals never own the same renderer restore state.
var viewRoot = Obj(); viewRoot.AddComponent<InstanceHandler>().Assign(p2, 890);
var viewChild = Obj(); viewChild.Parent = viewRoot; viewRoot.Children.Add(viewChild);
var childRenderer = viewChild.AddComponent<Renderer>(); viewChild.AddComponent<InstanceHandler>().Assign(p2, 891);
var rootView = viewRoot.GetComponent<FadeBehavior>(); var childView = viewChild.GetComponent<FadeBehavior>();
rootView.RefreshForPreCull(p1); childView.RefreshForPreCull(p1);
rootView.RestoreAfterCamera(); childView.RestoreAfterCamera();
Check(!childRenderer.forceRenderingOff, "Nested shop root and terminal views restore each renderer exactly once");
rootView.RefreshForPreCull(p2); childView.RefreshForPreCull(p2);
Check(!childRenderer.forceRenderingOff, "Owner camera does not inherit another nested view's hidden mask");
rootView.RestoreAfterCamera(); childView.RestoreAfterCamera();

var pingerObject = Obj(); var pinger = pingerObject.AddComponent<PingerController>();
pinger.pingIndicator = Obj().AddComponent<RoR2.UI.PingIndicator>(); var pingRenderer = pinger.pingIndicator.gameObject.AddComponent<Renderer>();
pinger.pingIndicator.pingTarget = clone; var pingView = pingerObject.AddComponent<PingerControllerRenderBehaviour>();
pingView.RefreshForPlayer(p1); Check(pingRenderer.forceRenderingOff, "Ping marker hides when the viewer's corresponding purchase is spent");
pingView.RefreshForPlayer(p2); Check(!pingRenderer.forceRenderingOff, "Other split-screen camera keeps its available ping marker");
pingView.RestoreAfterCamera(); Check(!pingRenderer.forceRenderingOff, "Ping marker restores its native mask after camera render");

// The real production wire format, replay ordering and independent reference resolution.
NetworkServer.active = false;
OwnerSnapshot.Clear();
var epochWire = new NetworkWriter(); new OwnerSnapshot(independent.GetComponent<InstanceHandler>()).Serialize(epochWire);
var epochReader = new NetworkReader(epochWire.stream.ToArray()); epochReader.ReadInt32(); epochReader.ReadNetworkId(); epochReader.ReadInt32(); epochReader.ReadString();
int wireEpoch = epochReader.ReadInt32();
OwnerSnapshot Packet(GameObject target, uint owner, int revision = 1, int version = 2, int stage = 0, int epoch = 0)
{
    var writer = new NetworkWriter(); writer.Write(version); writer.Write(Run.instance.netId); writer.Write(stage); writer.Write(SceneManager.Name); writer.Write(epoch == 0 ? wireEpoch : epoch);
    writer.Write(target.GetComponent<NetworkIdentity>().netId); writer.Write(new NetworkInstanceId(owner)); writer.Write(revision); writer.Write(555);
    var snapshot = new OwnerSnapshot(); snapshot.Deserialize(new(writer.stream.ToArray())); return snapshot;
}
uint Pid(PlayerCharacterMasterController player) => player.GetComponent<NetworkIdentity>().netId.Value;
var delayedTarget = Obj();
Packet(delayedTarget, 999, 1).OnReceived();
Check(OwnerSnapshot.PendingCount == 1 && PendingOwnership.IsPending(delayedTarget) && !InstanceAccessPolicy.CanInteract(delayedTarget, actor1), "Known snapshot with delayed owner gates access and display");
var readyTarget = Obj(); Packet(readyTarget, Pid(p2)).OnReceived();
Check(readyTarget.GetComponent<InstanceHandler>().Owner == p2 && OwnerSnapshot.PendingCount == 1, "One missing owner never blocks independent snapshot");
Packet(delayedTarget, Pid(p2), 3).OnReceived(); Packet(delayedTarget, Pid(p1), 2).OnReceived();
Check(delayedTarget.GetComponent<InstanceHandler>().Owner == p2 && OwnerSnapshot.PendingCount == 0, "Older received revision cannot restore stale ownership");
var writerRound = new NetworkWriter(); new OwnerSnapshot(delayedTarget.GetComponent<InstanceHandler>()).Serialize(writerRound);
var decoded = new OwnerSnapshot(); decoded.Deserialize(new(writerRound.stream.ToArray())); decoded.OnReceived();
Check(delayedTarget.GetComponent<InstanceHandler>().Owner == p2 && writerRound.stream.Length > 28, "Production serializer/reader round trip uses versioned per-entity layout");
var rejected = Obj(); Packet(rejected, Pid(p1), version: 1).OnReceived(); Packet(rejected, Pid(p1), stage: 99).OnReceived();
Check(!rejected.GetComponent<InstanceHandler>(), "Incompatible protocol rejects; future stage waits without attaching ownership");
OwnerSnapshot.Clear();
var vanishing = Obj(); Packet(vanishing, 998).OnReceived(); Check(OwnerSnapshot.PendingCount == 1, "Destroyed-target test is actually waiting for a missing owner"); NetworkServer.Destroy(vanishing);
OwnerSnapshot.Pump(); Check(!vanishing.GetComponent<InstanceHandler>(), "Destroyed target is never attached from a cached reference");
Time.realtimeSinceStartup += 31; OwnerSnapshot.Pump(); Check(OwnerSnapshot.PendingCount == 0 && !PendingOwnership.IsPending(vanishing), "Missing reference timeout releases pending state");
var late = Obj(); Packet(late, 997).OnReceived(); OwnerSnapshot.Clear();
var lateOwner = Obj("late-owner"); lateOwner.GetComponent<NetworkIdentity>().netId = new(997); lateOwner.AddComponent<PlayerCharacterMasterController>(); Util.Objects[new(997)] = lateOwner;
OwnerSnapshot.Pump(); Check(!late.GetComponent<InstanceHandler>(), "Transition clear prevents an old pending snapshot reviving after references arrive");
var sessionTarget = Obj(); Packet(sessionTarget, 996).OnReceived(); SceneManager.Name = "bazaar"; OwnerSnapshot.Pump();
OwnerSnapshot.SceneChanged();
Check(OwnerSnapshot.PendingCount == 0, "Scene transition discards the old scene waiter rather than treating it as a future packet");
OwnerSnapshot.Clear();
Check(!PendingOwnership.IsPending(sessionTarget), "Clear removes temporary marker instead of leaving permanent invisible entities");
SceneManager.Name = "golemplains";
// Snapshot precedes Run and then precedes stage/scene activation.
OwnerSnapshot.Clear();
var earlyRunTarget = Obj(); var savedRun = Run.instance;
var beforeRun = Packet(earlyRunTarget, Pid(p1)); Run.instance = null; beforeRun.OnReceived();
Check(OwnerSnapshot.PendingCount == 1 && !earlyRunTarget.GetComponent<InstanceHandler>(), "Snapshot arriving before Run is queued");
Run.instance = savedRun; OwnerSnapshot.RunChanged();
Check(earlyRunTarget.GetComponent<InstanceHandler>().Owner == p1, "Run arrival resolves the queued snapshot without needing replay");
var earlyStage = Obj(); Packet(earlyStage, Pid(p2), stage: 1, epoch: wireEpoch + 1).OnReceived();
Check(!earlyStage.GetComponent<InstanceHandler>() && OwnerSnapshot.PendingCount == 1, "Incoming stage metadata waits until stage SyncVar arrives");
Run.instance.stageClearCount = 1; OwnerSnapshot.SceneChanged();
Check(earlyStage.GetComponent<InstanceHandler>().Owner == p2, "Future stage snapshot survives transition and resolves");
var reused = Obj(); var reusedId = reused.GetComponent<NetworkIdentity>().netId;
Packet(reused, Pid(p1), stage: 1, epoch: wireEpoch + 1).OnReceived();
OwnerSnapshot.SceneChanged(); NetworkServer.Destroy(reused);
var replacement = Obj(); replacement.GetComponent<NetworkIdentity>().netId = reusedId; Util.Objects[reusedId] = replacement;
Packet(replacement, Pid(p1), stage: 1, epoch: wireEpoch + 1).OnReceived();
Check(!replacement.GetComponent<InstanceHandler>(), "Old scene epoch cannot attach ownership to a reused netId");
Packet(replacement, Pid(p2), stage: 1, epoch: wireEpoch + 2).OnReceived();
Check(replacement.GetComponent<InstanceHandler>().Owner == p2, "New epoch can safely initialize a reused netId");
OwnerSnapshot.Clear();
var timeoutTarget = Obj(); Packet(timeoutTarget, 995, stage: 1).OnReceived();
Check(PendingOwnership.IsPending(timeoutTarget), "Timeout test has a known target with an unresolved owner");
Time.realtimeSinceStartup += 31; OwnerSnapshot.Pump();
Check(!PendingOwnership.IsPending(timeoutTarget), "Timeout releases temporary ownership marker");
// Scene activation can lag the message independently of the stage SyncVar.
OwnerSnapshot.Clear();
var futureScene = Obj(); var savedScene = SceneManager.Name;
SceneManager.Name = "bazaar"; var beforeScene = Packet(futureScene, Pid(p1), stage: 1, epoch: wireEpoch + 3);
SceneManager.Name = savedScene; beforeScene.OnReceived();
Check(OwnerSnapshot.PendingCount == 1 && !futureScene.GetComponent<InstanceHandler>(), "Future scene snapshot waits without touching the current scene");
SceneManager.Name = "bazaar"; OwnerSnapshot.SceneChanged();
Check(futureScene.GetComponent<InstanceHandler>().Owner == p1, "Scene activation resolves an already queued future snapshot");
life.UnregisterHooks(); buy.UnregisterHooks(); interact.UnregisterHooks();
Console.WriteLine($"PASS: {assertions} assertions against the new production lifecycle, adapters, purchase policy, candidate policy, views and owner protocol. Stub boundary; no Unity/transport game test.");

sealed class FailingAdapter : ObjectAdapter { public override Action<GameObject> CaptureState(GameObject source) => _ => throw new InvalidOperationException("injected initialization failure"); }
sealed class TestInteractable : Behaviour, IInteractable
{
    public Interactability GetInteractability(Interactor actor) => Interactability.Available;
    public bool ShouldIgnoreSpherecastForInteractibility(Interactor actor) => false;
    public void OnInteractionBegin(Interactor actor) { }
}
