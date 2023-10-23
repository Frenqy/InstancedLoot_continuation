using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class ShrineChanceHandler : AbstractObjectHandler
{
    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;
    public override string[] HandledSources => new[] { ItemSource.ShrineChance };

    public override bool IsValidForObject(string source, GameObject gameObject)
    {
        return gameObject.GetComponent<ShrineChanceBehavior>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ShrineChanceBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }
}