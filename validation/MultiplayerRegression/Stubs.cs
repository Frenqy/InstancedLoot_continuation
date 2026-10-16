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
        public int GetInstanceID() => GetHashCode();
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
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>() => gameObject.GetComponentsInChildren<T>();
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour
    {
        public object StartCoroutine(IEnumerator routine) { Scheduler.Start(routine); return routine; }
    }
    public class GameObject : Object
    {
        public string name = "Chest1(Clone)";
        public readonly Transform transform = new();
        private readonly List<Component> components = new();
        public T GetComponent<T>() => components.Where(c => c).OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() => components.Where(c => c).OfType<T>().ToArray();
        public T[] GetComponentsInChildren<T>() => components.Where(c => c).OfType<T>().ToArray();
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
        public float fade;
        public void GetPropertyBlock(MaterialPropertyBlock block) => block.Fade = fade;
        public void SetPropertyBlock(MaterialPropertyBlock block) => fade = block.Fade;
    }
    public class Light : Behaviour { }
    public static class Shader { public static int PropertyToID(string name) => 1; }
    public static class Time { public static float realtimeSinceStartup; }
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
    public static class NetworkServer { public static bool active; }
    public class NetworkWriter
    {
        public readonly MemoryStream stream = new();
        public void Write(int value) => new BinaryWriter(stream).Write(value);
        public void Write(NetworkInstanceId value) => new BinaryWriter(stream).Write(value.Value);
    }
    public class NetworkReader(byte[] bytes)
    {
        private readonly BinaryReader reader = new(new MemoryStream(bytes));
        public int ReadInt32() => reader.ReadInt32();
        public NetworkInstanceId ReadNetworkId() => new(reader.ReadUInt32());
    }
}
namespace RoR2
{
    public class PlayerCharacterMasterController : UnityEngine.Component { public NetworkUser networkUser; }
    public class NetworkUser { public UnityEngine.Networking.NetworkConnection connectionToClient; }
    public class CharacterMaster : UnityEngine.Component { public PlayerCharacterMasterController playerCharacterMasterController; }
    public class CharacterBody : UnityEngine.Component { public CharacterMaster master; }
    public class Interactor : UnityEngine.Component { }
    public class GenericPickupController : UnityEngine.Component { }
    public class PickupDef { public struct GrantContext { public bool shouldDestroy; } }
    public enum Interactability { Disabled, Available }
    public interface IInteractable { }
    public class PurchaseInteraction : UnityEngine.Component
    {
        public bool available = true;
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
        public Xoroshiro128Plus rng;
        public object Networkpickup;
        public bool Networkhidden;
        public MultiShopController serverMultiShopController;
        public void UpdatePickupDisplayAndAnimations() { }
    }
    public class MultiShopController : UnityEngine.Component
    {
        private static uint nextTerminalId = 1000;
        public UnityEngine.GameObject[] terminalGameObjects = [];
        public bool available;
        public bool Networkavailable { get => available; set => available = value; }
        public int CreateTerminalsCalls;
        // Matches installed build 25921972, independently checked by the DLL audit.
        private void Awake() { if (UnityEngine.Networking.NetworkServer.active) CreateTerminals(); }
        public int cost;
        public int Networkcost { get => cost; set => cost = value; }
        public Xoroshiro128Plus rng = new(0);
        public void CreateTerminals()
        {
            CreateTerminalsCalls++;
            terminalGameObjects = [CreateTerminal(), CreateTerminal(), CreateTerminal()];
            foreach (var terminal in terminalGameObjects)
                terminal.GetComponent<ShopTerminalBehavior>().serverMultiShopController = this;
        }

        private static UnityEngine.GameObject CreateTerminal()
        {
            var terminal = new UnityEngine.GameObject { name = "ShopTerminal" };
            terminal.AddComponent<UnityEngine.Networking.NetworkIdentity>().netId = new(nextTerminalId++);
            var shopTerminal = terminal.AddComponent<ShopTerminalBehavior>();
            shopTerminal.rng = new Xoroshiro128Plus(42);
            terminal.AddComponent<PurchaseInteraction>();
            return terminal;
        }
    }
    public class Run : UnityEngine.Object { public static Run instance = new(); }
    public static class Util
    {
        public static readonly Dictionary<UnityEngine.Networking.NetworkInstanceId, UnityEngine.GameObject> Objects = new();
        public static UnityEngine.GameObject FindNetworkObject(UnityEngine.Networking.NetworkInstanceId id) => Objects.TryGetValue(id, out var obj) && obj ? obj : null;
    }
    public class SpawnCard : UnityEngine.Object
    {
        public UnityEngine.GameObject prefab;
        public DirectorSpawnRequest LastRequest;
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
            var instance = new UnityEngine.GameObject();
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
        public static event Action<SceneCamera> onSceneCameraPreCull, onSceneCameraPreRender;
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
        public static readonly List<INetMessage> SentMessages = new();
        public static void Send(this INetMessage message, R2API.Networking.NetworkDestination destination) => SentMessages.Add(message);
        public static void Send(this INetMessage message, UnityEngine.Networking.NetworkConnection connection) => SentMessages.Add(message);
    }
}
namespace InstancedLoot.Enums
{
    public enum ObjectInstanceMode { None, InstancedObject, CopyObject }
    public static class ObjectType
    {
        public const string TripleShop = "TripleShop";
        public const string TripleShopEquipment = "TripleShopEquipment";
        public const string FreeChestMultiShop = "FreeChestMultiShop";
    }
}
namespace InstancedLoot
{
    public class Logger { public readonly List<string> Errors = new(); public readonly List<string> Infos = new(); public void LogError(string error) => Errors.Add(error); public void LogInfo(string info) => Infos.Add(info); }
    public class InstancedLoot : UnityEngine.MonoBehaviour
    {
        public static InstancedLoot Instance = new();
        public static readonly List<Networking.SyncInstances.InstanceHandlerEntry[]> FailedSyncs = new();
        public readonly Logger _logger = new();
        public readonly HookManager HookManager = new();
        public bool InteractionLoggingEnabled;
    }
    public class ObjectHandlerManager
    {
        public InstancedLoot Plugin = InstancedLoot.Instance;
        public void RegisterAwaitedObject(UnityEngine.GameObject target, AbstractObjectHandler handler) { }
    }
    public class HookManager
    {
        public void RegisterHandler<T>() where T : new() { }
    }
}
namespace InstancedLoot.Components
{
    public class InstancedLootBehaviour : UnityEngine.MonoBehaviour { }
    public class SpawnCardTracker : UnityEngine.Component { public RoR2.SpawnCard SpawnCard; }
    public class InstanceInfoTracker : UnityEngine.Component
    {
        public InstanceOverrideInfo Info = new();
        public class InstanceOverrideInfo
        {
            public void AttachTo(UnityEngine.GameObject target) { }
            public static void SetOwner(UnityEngine.GameObject target, RoR2.PlayerCharacterMasterController player) { }
        }
    }
}
namespace InstancedLoot.Hooks
{
    public abstract class AbstractHookHandler
    {
        public abstract void RegisterHooks();
        public abstract void UnregisterHooks();
    }
    public sealed class MultiShopControllerHandler : AbstractHookHandler
    {
        public override void RegisterHooks() { }
        public override void UnregisterHooks() { }
    }
    public sealed class ShopTerminalBehaviorHandler : AbstractHookHandler
    {
        public override void RegisterHooks() { }
        public override void UnregisterHooks() { }
    }
}
namespace On.RoR2
{
    public static class PurchaseInteraction
    {
        public delegate global::RoR2.Interactability orig_GetInteractability(global::RoR2.PurchaseInteraction self, global::RoR2.Interactor activator);
        public delegate void orig_OnInteractionBegin(global::RoR2.PurchaseInteraction self, global::RoR2.Interactor activator);
        public static event Func<orig_GetInteractability, global::RoR2.PurchaseInteraction, global::RoR2.Interactor, global::RoR2.Interactability> GetInteractability;
        public static event Action<orig_OnInteractionBegin, global::RoR2.PurchaseInteraction, global::RoR2.Interactor> OnInteractionBegin;
        public static int Purchases;
        public static Action<global::RoR2.PurchaseInteraction, global::RoR2.Interactor> DuringNativePurchase;
        public static Action<global::RoR2.PurchaseInteraction> NativeOutcome;
        public static global::RoR2.Interactability Prompt(global::RoR2.PurchaseInteraction purchase, global::RoR2.Interactor interactor) =>
            GetInteractability((_, _) => global::RoR2.Interactability.Available, purchase, interactor);
        public static void Buy(global::RoR2.PurchaseInteraction purchase, global::RoR2.Interactor interactor)
        {
            OnInteractionBegin((_, _) =>
            {
                DuringNativePurchase?.Invoke(purchase, interactor);
                Purchases++;
                  if (NativeOutcome != null) NativeOutcome(purchase);
                  else purchase.available = false;
            }, purchase, interactor);
        }
    }
}
