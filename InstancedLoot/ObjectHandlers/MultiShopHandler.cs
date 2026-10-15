using System;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class MultiShopHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.TripleShop,
        // ObjectType.TripleShopLarge, // As far as I'm aware, this one's unused.
        ObjectType.TripleShopEquipment,
        ObjectType.FreeChestMultiShop
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override bool CanObjectBeOwned(string objectType)
    {
        if (objectType == ObjectType.FreeChestMultiShop)
            return true;
        
        return base.CanObjectBeOwned(objectType);
    }

    public override bool IsValidForObject(string objectType, GameObject gameObject)
    {
        return gameObject.GetComponent<MultiShopController>() != null ||
               gameObject.GetComponent<ShopTerminalBehavior>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<MultiShopControllerHandler>();
        Plugin.HookManager.RegisterHandler<ShopTerminalBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }

    public override InstanceHandler InstanceSingleObjectFrom(GameObject source, GameObject target,
        PlayerCharacterMasterController[] players)
    {
        InstanceHandler instanceHandler = base.InstanceSingleObjectFrom(source, target, players);
        
        if (source == target)
        {
            InstanceInfoTracker instanceInfoTracker = source.GetComponent<InstanceInfoTracker>();
            MultiShopController multiShopController = source.GetComponent<MultiShopController>();
            ShopTerminalBehavior shopTerminalBehavior = source.GetComponent<ShopTerminalBehavior>();
    
            if (multiShopController != null)
                foreach (var terminalGameObject in multiShopController.terminalGameObjects)
                {
                    // A CopyObject source is owned by the first player only.  The
                    // terminals are separate interactables, so they must use the same
                    // owner as the source instead of accidentally remaining eligible
                    // for every player (which also prevents client candidate filtering
                    // from selecting the wrong terminal).
                    InstanceHandler terminalHandler = base.InstanceSingleObjectFrom(
                        terminalGameObject, terminalGameObject, new[] { instanceHandler.OrigPlayer });

                    // Each terminal has its own copy group.  The controller's shared
                    // group only contains controller copies, while terminal copies are
                    // spawned asynchronously by CreateTerminals and need a source
                    // handler to receive their state and permissions.
                    terminalHandler.SharedInfo = new InstanceHandler.SharedInstanceInfo
                    {
                        SourceObject = terminalGameObject,
                        ObjectInstanceMode = ObjectInstanceMode
                    };
                    
                    if(instanceInfoTracker != null)
                        instanceInfoTracker.Info.AttachTo(terminalGameObject);
                }

            if (shopTerminalBehavior != null)
                instanceHandler.SharedInfo = new InstanceHandler.SharedInstanceInfo
                {
                    SourceObject = target,
                    ObjectInstanceMode = ObjectInstanceMode
                };
        }
        else
        {
            MultiShopController targetMultiShopController = target.GetComponent<MultiShopController>();
            if (targetMultiShopController != null)
            {
                MultiShopController sourceMultiShopController = source.GetComponent<MultiShopController>();

                targetMultiShopController.rng = new Xoroshiro128Plus(0); //Temporary RNG
                targetMultiShopController.CreateTerminals();
                targetMultiShopController.Networkcost = sourceMultiShopController.Networkcost;
                targetMultiShopController.rng = new Xoroshiro128Plus(sourceMultiShopController.rng);

                var sourceTerminalGameObjects = sourceMultiShopController.terminalGameObjects;
                var targetTerminalGameObjects = targetMultiShopController.terminalGameObjects;

                int terminalCount = Math.Min(sourceTerminalGameObjects.Length, targetTerminalGameObjects.Length);
                for (int i = 0; i < terminalCount; i++)
                    AwaitObjectFor(targetTerminalGameObjects[i],
                        new AwaitedObjectInfo
                        {
                            SourceObject = sourceTerminalGameObjects[i],
                            Players = players
                        });
            }
            
            ShopTerminalBehavior targetShopTerminalBehavior = target.GetComponent<ShopTerminalBehavior>();
            if (targetShopTerminalBehavior != null)
            {
                ShopTerminalBehavior sourceShopTerminalBehavior = source.GetComponent<ShopTerminalBehavior>();

                targetShopTerminalBehavior.hasStarted = true;
                targetShopTerminalBehavior.rng = new Xoroshiro128Plus(sourceShopTerminalBehavior.rng);
                targetShopTerminalBehavior.Networkpickup = sourceShopTerminalBehavior.Networkpickup;
                targetShopTerminalBehavior.Networkhidden = sourceShopTerminalBehavior.Networkhidden;
                targetShopTerminalBehavior.UpdatePickupDisplayAndAnimations();
            }

        }

        return instanceHandler;
    }
}
