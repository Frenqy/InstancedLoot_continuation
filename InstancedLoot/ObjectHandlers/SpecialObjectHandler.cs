using InstancedLoot.Components;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

//Used by special sources that only instance the object and do not drop items
public class SpecialObjectHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.ShrineBlood, ObjectType.ShrineRestack,
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ShrineBloodBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<ShrineRestackBehaviorHandler>();
    }

    public override InstanceHandler InstanceSingleObjectFrom(GameObject source, GameObject target,
        PlayerCharacterMasterController[] players)
    {
        if (target.GetComponent<ShrineRestackBehavior>() is var targetShrine && targetShrine != null)
        {
            var sourceShrine = source.GetComponent<ShrineRestackBehavior>();

            targetShrine.rng = new Xoroshiro128Plus(sourceShrine.rng);
        }
        
        return base.InstanceSingleObjectFrom(source, target, players);
    }
}