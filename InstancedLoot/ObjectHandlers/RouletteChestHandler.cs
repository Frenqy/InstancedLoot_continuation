using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class RouletteChestHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } = { "CasinoChest" };
    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;
    
    public override bool IsValidForObject(string objectType, GameObject gameObject)
    {
        return gameObject.GetComponent<RouletteChestController>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<RouletteChestControllerHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }
}