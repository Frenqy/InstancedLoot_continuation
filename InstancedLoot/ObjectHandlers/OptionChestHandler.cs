using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class OptionChestHandler : AbstractObjectHandler
{
    public override string[] HandledSources => new string[]
    {
        ObjectType.VoidTriple, ObjectType.LockboxVoid,
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override bool IsValidForObject(string source, GameObject gameObject)
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