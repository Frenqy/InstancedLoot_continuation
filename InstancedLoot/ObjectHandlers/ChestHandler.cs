using System.Reflection;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using MonoMod.Utils;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class ChestHandler : AbstractObjectHandler
{
    protected FieldInfo Field_ChestBehavior_rng =
        typeof(ChestBehavior).GetField("rng", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
    public override string[] HandledSources => new string[]
    {
        ItemSource.Chest1, ItemSource.Chest2, ItemSource.GoldChest
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override bool IsValidForObject(string source, GameObject gameObject)
    {
        return gameObject.GetComponent<ChestBehavior>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ChestBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }

    // public override InstanceHandler InstanceSingleObjectFrom(GameObject source, GameObject target, PlayerCharacterMasterController[] players)
    // {
    //     InstanceHandler instanceHandler = base.InstanceSingleObjectFrom(source, target, players);
    //
    //     
    //     
    //     return instanceHandler;
    // }
}