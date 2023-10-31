using System.Collections;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class MultiShopHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes => new[]
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

    public override void InstanceObject(string objectType, GameObject gameObject, PlayerCharacterMasterController[] players)
    {
        base.InstanceObject(objectType, gameObject, players);
        
        LateInstanceShop(objectType, gameObject);
    }
    
    public override InstanceHandler InstanceSingleObjectFrom(GameObject source, GameObject target,
        PlayerCharacterMasterController[] players)
    {
        if (source == target)
        {
            MultiShopController multiShopController = source.GetComponent<MultiShopController>();
    
            if (multiShopController != null)
            {
                foreach (var terminalGameObject in multiShopController.terminalGameObjects)
                {
                    InstanceSingleObjectFrom(terminalGameObject, terminalGameObject, players);
                }
            }
        }
        
        InstanceHandler instanceHandler = base.InstanceSingleObjectFrom(source, target, players);
    
        return instanceHandler;
    }

    internal void LateInstanceShop(string objectType, GameObject gameObject)
    {
        InstanceHandler handler = gameObject.GetComponent<InstanceHandler>();

        handler.StartCoroutine(Coroutine(handler));
        return;

        IEnumerator Coroutine(InstanceHandler primaryHandler)
        {
            yield return 0;

            InstanceHandler[] handlers = primaryHandler.LinkedHandlers;
            MultiShopController[] shops = handlers.Select(handler => handler.GetComponent<MultiShopController>()).ToArray();

            int debugCounter = 10;
            while (!shops.All(shop => shop.terminalGameObjects.Length > 0))
            {
                yield return 0;
                debugCounter--;
                if (debugCounter <= 0)
                {
                    Plugin._logger.LogError("Error instantiating MultiShopController - An instance doesn't have terminals after 10 ticks.");
                    yield break;
                }
            }

            InstanceHandler[][] terminals = shops.Select(shop =>
                    shop._terminalGameObjects.Select(terminal => terminal.GetComponent<InstanceHandler>()).ToArray())
                .ToArray();
            
            for (int i = 0; i < terminals[0].Length; i++)
            {
                InstanceHandler[] linkedHandlers = new InstanceHandler[terminals.Length];
                for (int instanceIndex = 0; instanceIndex < terminals.Length; instanceIndex++)
                {
                    linkedHandlers[instanceIndex] = terminals[instanceIndex][i];
                    terminals[instanceIndex][i].LinkedHandlers = linkedHandlers;
                }
                
                for (int instanceIndex = 0; instanceIndex < terminals.Length; instanceIndex++)
                {
                    terminals[instanceIndex][i].UpdateVisuals();
                }
            }
            
            primaryHandler.SyncPlayers();
        }
    }
}