using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class OptionChestHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.VoidTriple, ObjectType.LockboxVoid,
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override bool IsValidForObject(string objectType, GameObject gameObject)
    {
        return gameObject.GetComponent<OptionChestBehavior>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<OptionChestBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }
    
}