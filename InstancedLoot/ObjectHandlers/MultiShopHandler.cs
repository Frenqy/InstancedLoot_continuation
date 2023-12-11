using System.Collections;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

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
            {
                foreach (var terminalGameObject in multiShopController.terminalGameObjects)
                {
                    InstanceSingleObjectFrom(terminalGameObject, terminalGameObject, players);
                    
                    if(instanceInfoTracker != null)
                        instanceInfoTracker.Info.AttachTo(terminalGameObject);
                }
            }

            if (shopTerminalBehavior != null)
            {
                instanceHandler.SharedInfo = new()
                {
                    SourceObject = target,
                    ObjectInstanceMode = ObjectInstanceMode,
                };
            }
        }
        else
        {
            MultiShopController targetMultiShopController = target.GetComponent<MultiShopController>();
            if (targetMultiShopController != null)
            {
                MultiShopController sourceMultiShopController = source.GetComponent<MultiShopController>();

                targetMultiShopController.rng = new(0); //Temporary RNG
                targetMultiShopController.CreateTerminals();
                targetMultiShopController.Networkcost = sourceMultiShopController.Networkcost;
                targetMultiShopController.rng = new Xoroshiro128Plus(sourceMultiShopController.rng);

                var sourceTerminalGameObjects = sourceMultiShopController._terminalGameObjects;
                var targetTerminalGameObjects = targetMultiShopController._terminalGameObjects;

                for (int i = 0; i < targetTerminalGameObjects.Length; i++)
                {
                    AwaitObjectFor(targetTerminalGameObjects[i],
                        new AwaitedObjectInfo
                        {
                            SourceObject = sourceTerminalGameObjects[i],
                            Players = players
                        });
                }
            }
            
            ShopTerminalBehavior targetShopTerminalBehavior = target.GetComponent<ShopTerminalBehavior>();
            if (targetShopTerminalBehavior != null)
            {
                ShopTerminalBehavior sourceShopTerminalBehavior = source.GetComponent<ShopTerminalBehavior>();

                targetShopTerminalBehavior.hasStarted = true;
                targetShopTerminalBehavior.rng = new Xoroshiro128Plus(sourceShopTerminalBehavior.rng);
                targetShopTerminalBehavior.NetworkpickupIndex = sourceShopTerminalBehavior.NetworkpickupIndex;
                targetShopTerminalBehavior.Networkhidden = sourceShopTerminalBehavior.Networkhidden;
            }

            PurchaseInteraction targetPurchaseInteraction = target.GetComponent<PurchaseInteraction>();
            if (targetPurchaseInteraction != null)
            {
                PurchaseInteraction sourcePurchaseInteraction = source.GetComponent<PurchaseInteraction>();

                targetPurchaseInteraction.rng = sourcePurchaseInteraction.rng;
                targetPurchaseInteraction.Networkcost = sourcePurchaseInteraction.Networkcost;
            }
        }

        return instanceHandler;
    }
}