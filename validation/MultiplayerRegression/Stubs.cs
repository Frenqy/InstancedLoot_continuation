// A small deterministic Unity/RoR2 boundary. This does not emulate game IL, transport,
// rendering, or real SyncVars; production files are linked by the project above.
#pragma warning disable CS0067, CS9113 // Boundary members are intentionally only read by production code.
using System.Collections;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static implicit operator bool(Object value) => value is not null && !value.destroyed;
        public static bool operator !(Object value) => !((bool)value);
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b) || (!((bool)a) && !((bool)b));
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        public static void Destroy(Object value) { if (value) value.destroyed = true; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public string name => gameObject.name;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>() => gameObject.GetComponentsInChildren<T>();
        public T[] GetComponents<T>() => gameObject.GetComponentsInChildren<T>();
        public T GetComponentInParent<T>() { for (var current = gameObject; current != null; current = current.Parent) { var value = current.GetComponent<T>(); if (value is not null) return value; } return default; }
    }
    public class Behaviour : Component { public bool enabled = true; public bool isActiveAndEnabled => enabled; }
    public class MonoBehaviour : Behaviour
    {
        public object StartCoroutine(IEnumerator routine) { Scheduler.Start(routine); return routine; }
    }
    public class GameObject : Object
    {
        public string name = "Chest1(Clone)";
        public GameObject Parent;
        public readonly List<GameObject> Children = new();
        public readonly Transform transform = new();
        private readonly List<Component> components = new();
        public T GetComponent<T>() => components.Where(c => c).OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() => components.Where(c => c).OfType<T>().ToArray();
        public bool TryGetComponent<T>(out T value) { value = GetComponent<T>(); return value is not null; }
        public T[] GetComponentsInChildren<T>() => components.Where(c => c).OfType<T>().Concat(Children.SelectMany(child => child.GetComponentsInChildren<T>())).ToArray();
        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            components.Add(component);
            component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(component, null);
            return component;
        }
    }
    public class Transform { public int position, rotation, localScale; public GameObject gameObject; }
    public class Material { public void EnableKeyword(string keyword) { } }
    public class MaterialPropertyBlock { public float Fade; public void SetFloat(int id, float value) => Fade = value; }
    public class Renderer : Behaviour
    {
        public Material[] materials = [];
        public float fade; public bool forceRenderingOff;
        public void GetPropertyBlock(MaterialPropertyBlock block) => block.Fade = fade;
        public void SetPropertyBlock(MaterialPropertyBlock block) => fade = block.Fade;
    }
    public class Light : Behaviour { }
    public static class Shader { public static int PropertyToID(string name) => 1; }
    public static class Time { public static float realtimeSinceStartup, unscaledTime; }
    public static class Resources { public static T[] FindObjectsOfTypeAll<T>() => []; }
    public static class Scheduler
    {
        public static readonly List<IEnumerator> Pending = new();
        public static void Start(IEnumerator routine) { if (routine.MoveNext()) Pending.Add(routine); }
        public static void Tick(float seconds = 0.01f)
        {
            Time.realtimeSinceStartup += seconds;
            foreach (var routine in Pending.ToArray()) if (!routine.MoveNext()) Pending.Remove(routine);
        }
    }
}
namespace UnityEngine.Networking
{
    public readonly record struct NetworkInstanceId(uint Value)
    {
        public static NetworkInstanceId Invalid => new(0);
    }
    public class NetworkIdentity : UnityEngine.Component { public NetworkInstanceId netId; }
    public class NetworkConnection { }
    public static class NetworkServer { public static bool active; public static void Destroy(UnityEngine.GameObject target) { foreach (var c in target.GetComponents<UnityEngine.Component>()) UnityEngine.Object.Destroy(c); UnityEngine.Object.Destroy(target); } }
    public class NetworkWriter
    {
        public readonly MemoryStream stream = new();
        public void Write(string value) => new BinaryWriter(stream).Write(value);
        public void Write(int value) => new BinaryWriter(stream).Write(value);
        public void Write(NetworkInstanceId value) => new BinaryWriter(stream).Write(value.Value);
    }
    public class NetworkReader(byte[] bytes)
    {
        private readonly BinaryReader reader = new(new MemoryStream(bytes));
        public string ReadString() => reader.ReadString();
        public int ReadInt32() => reader.ReadInt32();
        public NetworkInstanceId ReadNetworkId() => new(reader.ReadUInt32());
    }
}
namespace RoR2
{
    public class PlayerCharacterMasterController : UnityEngine.Component { public static readonly List<PlayerCharacterMasterController> instances = new(); public CharacterBody body; public NetworkUser networkUser; }
    public class NetworkUser { public UnityEngine.Networking.NetworkConnection connectionToClient; }
    public class CharacterMaster : UnityEngine.Component { public PlayerCharacterMasterController playerCharacterMasterController; }
    public class CharacterBody : UnityEngine.Component { public CharacterMaster master; }
    public class Interactor : UnityEngine.Component { }
    public class GenericPickupController : UnityEngine.Component { }
    public class PickupDef { public struct GrantContext { public bool shouldDestroy; } }
    public enum Interactability { Disabled, ConditionsNotMet, Available }
    public interface IInteractable { Interactability GetInteractability(Interactor actor); bool ShouldIgnoreSpherecastForInteractibility(Interactor actor); void OnInteractionBegin(Interactor actor); }
    public class PurchaseInteraction : UnityEngine.Behaviour, IInteractable
    {
        public bool available = true;
        public bool Networkavailable { get => available; set => available = value; }
        public Interactability GetInteractability(Interactor actor) => available ? Interactability.Available : Interactability.Disabled;
        public bool ShouldIgnoreSpherecastForInteractibility(Interactor actor) => false;
        public void OnInteractionBegin(Interactor actor) { }
        public int cost, costType, CostWrites, CostTypeWrites;
        public Xoroshiro128Plus rng;
        public int Networkcost { get => cost; set { cost = value; CostWrites++; } }
        public int NetworkcostType { get => costType; set { costType = value; CostTypeWrites++; } }
    }
    public class Xoroshiro128Plus
    {
        public ulong State;
        public Xoroshiro128Plus(ulong seed) { State = seed; }
        public Xoroshiro128Plus(Xoroshiro128Plus other) { State = other.State; }
    }
    public class ShopTerminalBehavior : UnityEngine.Component
    {
        public bool hasStarted;
        public MultiShopController serverMultiShopController;
        public Xoroshiro128Plus rng;
        public object Networkpickup;
        public bool Networkhidden;
        public void UpdatePickupDisplayAndAnimations() { }
    }
    public class MultiShopController : UnityEngine.Component
    {
        public UnityEngine.GameObject[] terminalGameObjects = [];
        public int cost;
        public bool Networkavailable = true;
        public int Creates;
        public void Awake() { CreateTerminals(); }
        public int Networkcost { get => cost; set => cost = value; }
        public Xoroshiro128Plus rng = new(0);
        public void CreateTerminals()
        {
            Creates++;
            terminalGameObjects = [CreateTerminal(), CreateTerminal(), CreateTerminal()];
            foreach (var terminal in terminalGameObjects) terminal.GetComponent<ShopTerminalBehavior>().serverMultiShopController = this;
        }

        private static UnityEngine.GameObject CreateTerminal()
        {
            var terminal = new UnityEngine.GameObject { name = "ShopTerminal" };
            var shopTerminal = terminal.AddComponent<ShopTerminalBehavior>();
            shopTerminal.rng = new Xoroshiro128Plus(42);
            terminal.AddComponent<PurchaseInteraction>();
            return terminal;
        }
    }
    public class Run : UnityEngine.Object { public static Run instance = new(); public UnityEngine.Networking.NetworkInstanceId netId = new(777); public int stageClearCount; }
    public static class Util
    {
        public static readonly Dictionary<UnityEngine.Networking.NetworkInstanceId, UnityEngine.GameObject> Objects = new();
        public static UnityEngine.GameObject FindNetworkObject(UnityEngine.Networking.NetworkInstanceId id) => Objects.TryGetValue(id, out var obj) && obj ? obj : null;
    }
    public class SpawnCard : UnityEngine.Object
    {
        public UnityEngine.GameObject prefab;
        public DirectorSpawnRequest LastRequest;
        public Func<UnityEngine.GameObject> Factory;
        public int LastPosition, LastRotation;
        public struct SpawnResult { public UnityEngine.GameObject spawnedInstance; }
        public virtual SpawnResult DoSpawn(int position, int rotation, DirectorSpawnRequest request)
        {
            // The installed 1.5 InteractableSpawnCard dereferences this rule. Deliberately
            // reject null so the production CloneObject call cannot hide that regression.
            _ = request.placementRule.placementMode;
            LastRequest = request;
            LastPosition = position;
            LastRotation = rotation;
            var instance = Factory == null ? new UnityEngine.GameObject() : Factory();
            // Native orientToFloor may alter the transform before CloneObject restores it.
            instance.transform.position = -1;
            instance.transform.rotation = -1;
            instance.transform.localScale = -1;
            return new() { spawnedInstance = instance };
        }
    }
    public class InteractableSpawnCard : SpawnCard { }
    public class DirectorPlacementRule
    {
        public enum PlacementMode { Direct, DirectWithoutRandomRotation }
        public PlacementMode placementMode;
        public int position, rotation;
    }
    public class DirectorSpawnRequest(SpawnCard card, DirectorPlacementRule rule, Xoroshiro128Plus rng)
    {
        public readonly DirectorPlacementRule placementRule = rule;
    }
    public class CameraRigController : UnityEngine.Object { public CharacterBody targetBody; }
    public class SceneCamera
    {
        public CameraRigController cameraRigController;
        public static event Action<SceneCamera> onSceneCameraPreCull, onSceneCameraPreRender, onSceneCameraPostRender;
    }
    public class DitherModel : UnityEngine.Component { public UnityEngine.Renderer[] renderers = []; }
    public class ModelLocator : UnityEngine.Component { public UnityEngine.Transform modelTransform; }
    public class CostHologramContent : UnityEngine.Component { public UnityEngine.Component targetTextMesh; }
    public class Highlight : UnityEngine.Behaviour { }
}
namespace R2API.Networking
{
    public enum NetworkDestination { Clients }
}
namespace R2API.Networking.Interfaces
{
    public interface INetMessage
    {
        void Serialize(UnityEngine.Networking.NetworkWriter writer);
        void Deserialize(UnityEngine.Networking.NetworkReader reader);
        void OnReceived();
    }
    public static class Extensions
    {
        public static readonly List<byte[]> Sent = new();
        public static void Send(this INetMessage message, R2API.Networking.NetworkDestination destination) { var writer = new UnityEngine.Networking.NetworkWriter(); message.Serialize(writer); Sent.Add(writer.stream.ToArray()); }
        public static void Send(this INetMessage message, UnityEngine.Networking.NetworkConnection connection) { }
    }
}

namespace UnityEngine.SceneManagement
{
    public readonly record struct Scene(string name);
    public static class SceneManager { public static string Name = "golemplains"; public static Scene GetActiveScene() => new(Name); }
}
namespace RoR2
{
    public class ChestBehavior : UnityEngine.Component { public Xoroshiro128Plus rng = new(123); public int dropCount = 1; public object currentPickup = "item1"; }
    public class OptionChestBehavior : UnityEngine.Component { public Xoroshiro128Plus rng = new(123); public List<object> generatedPickups = ["a", "b", "c"]; }
    public class RouletteChestController : UnityEngine.Component { public Xoroshiro128Plus rng = new(123); }
    public class ShrineChanceBehavior : UnityEngine.Component { public Xoroshiro128Plus rng = new(123); public PurchaseInteraction purchaseInteraction; }
    public class ShrineRestackBehavior : UnityEngine.Component { public Xoroshiro128Plus rng = new(123); public PurchaseInteraction purchaseInteraction; }
    public class ShrineBloodBehavior : UnityEngine.Component { public PurchaseInteraction purchaseInteraction; }
    public class SummonMasterBehavior : UnityEngine.Component { public UnityEngine.GameObject masterPrefab; public int droneUpgradeCount, UpgradeWrites; public int NetworkdroneUpgradeCount { get => droneUpgradeCount; set { droneUpgradeCount = value; UpgradeWrites++; } } public bool destroyAfterSummoning; }
}
namespace InstancedLoot
{
    public class Logger { public readonly List<string> Errors = new(); public void LogError(string value) => Errors.Add(value); }
    public class TestConfig { public bool Enabled = true; public bool IsEnabled(string type) => Enabled; }
    public class InstancedLoot : UnityEngine.MonoBehaviour
    {
        public static InstancedLoot Instance;
        public readonly Logger _logger = new();
        public readonly TestConfig ModConfig = new();
        public readonly ObjectInstancing.ObjectInstanceService Objects;
        public InstancedLoot() { Instance = this; Objects = new(this); }
    }
}
namespace InstancedLoot.Components
{
    public class InstancedLootBehaviour : UnityEngine.MonoBehaviour { }
    public class SpawnCardTracker : UnityEngine.Component { public RoR2.SpawnCard SpawnCard; }
}
namespace InstancedLoot.Hooks
{
    public abstract class AbstractHookHandler
    {
        public InstancedLoot Plugin => InstancedLoot.Instance;
        public abstract void RegisterHooks();
        public abstract void UnregisterHooks();
    }
    public static class InteractorCandidateIL { public static void Apply<T>(MonoMod.Cil.ILContext context, T callback) where T : Delegate { } }
}
namespace MonoMod.Cil { public class ILContext { } }
namespace IL.RoR2 { public static class Interactor { public static event Action<MonoMod.Cil.ILContext> FindBestInteractableObject; } }
namespace On.RoR2
{
    public static class Interactor
    {
        public delegate void orig_PerformInteraction(global::RoR2.Interactor self, UnityEngine.GameObject target);
        public static event Action<orig_PerformInteraction, global::RoR2.Interactor, UnityEngine.GameObject> PerformInteraction;
        public static int Interactions;
        public static void Perform(global::RoR2.Interactor actor, UnityEngine.GameObject target) => PerformInteraction((_, _) => Interactions++, actor, target);
    }
    public static class PurchaseInteraction
    {
        public delegate global::RoR2.Interactability orig_GetInteractability(global::RoR2.PurchaseInteraction self, global::RoR2.Interactor actor);
        public delegate void orig_OnInteractionBegin(global::RoR2.PurchaseInteraction self, global::RoR2.Interactor actor);
        public static event Func<orig_GetInteractability, global::RoR2.PurchaseInteraction, global::RoR2.Interactor, global::RoR2.Interactability> GetInteractability;
        public static event Action<orig_OnInteractionBegin, global::RoR2.PurchaseInteraction, global::RoR2.Interactor> OnInteractionBegin;
        public static void Buy(global::RoR2.PurchaseInteraction self, global::RoR2.Interactor actor, Action native)
            => OnInteractionBegin((_, _) => native(), self, actor);
    }
}
namespace On.RoR2 { public static class ChestBehavior {
    public delegate void orig_Start(global::RoR2.ChestBehavior self);
    public static event Action<orig_Start, global::RoR2.ChestBehavior> Start;
    public static void Fire(global::RoR2.ChestBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class OptionChestBehavior {
    public delegate void orig_Start(global::RoR2.OptionChestBehavior self);
    public static event Action<orig_Start, global::RoR2.OptionChestBehavior> Start;
    public static void Fire(global::RoR2.OptionChestBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class MultiShopController {
    public delegate void orig_Start(global::RoR2.MultiShopController self);
    public static event Action<orig_Start, global::RoR2.MultiShopController> Start;
    public static void Fire(global::RoR2.MultiShopController self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class ShopTerminalBehavior {
    public delegate void orig_Start(global::RoR2.ShopTerminalBehavior self);
    public static event Action<orig_Start, global::RoR2.ShopTerminalBehavior> Start;
    public static void Fire(global::RoR2.ShopTerminalBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class RouletteChestController {
    public delegate void orig_Start(global::RoR2.RouletteChestController self);
    public static event Action<orig_Start, global::RoR2.RouletteChestController> Start;
    public static void Fire(global::RoR2.RouletteChestController self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class ShrineChanceBehavior {
    public delegate void orig_Start(global::RoR2.ShrineChanceBehavior self);
    public static event Action<orig_Start, global::RoR2.ShrineChanceBehavior> Start;
    public static void Fire(global::RoR2.ShrineChanceBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class ShrineBloodBehavior {
    public delegate void orig_Start(global::RoR2.ShrineBloodBehavior self);
    public static event Action<orig_Start, global::RoR2.ShrineBloodBehavior> Start;
    public static void Fire(global::RoR2.ShrineBloodBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class ShrineRestackBehavior {
    public delegate void orig_Start(global::RoR2.ShrineRestackBehavior self);
    public static event Action<orig_Start, global::RoR2.ShrineRestackBehavior> Start;
    public static void Fire(global::RoR2.ShrineRestackBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }
namespace On.RoR2 { public static class SummonMasterBehavior {
    public delegate void orig_Start(global::RoR2.SummonMasterBehavior self);
    public static event Action<orig_Start, global::RoR2.SummonMasterBehavior> Start;
    public static void Fire(global::RoR2.SummonMasterBehavior self, Action native) { if (Start == null) native(); else Start(_ => native(), self); }
} }

namespace RoR2 { public class PingerController : UnityEngine.Component { public RoR2.UI.PingIndicator pingIndicator; } }
namespace RoR2.UI { public class PingIndicator : UnityEngine.Component { public UnityEngine.GameObject pingTarget; } }
