
using System.Collections;
using System.Reflection;
using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class PurchaseInteractionHandler : AbstractHookHandler
{
    protected FieldInfo Field_PurchaseInteraction_rng =
        typeof(PurchaseInteraction).GetField("rng", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
    public override void RegisterHooks()
    {
        On.RoR2.PurchaseInteraction.Awake += On_PurchaseInteraction_Awake;
        On.RoR2.PurchaseInteraction.GetInteractability += On_PurchaseInteraction_GetInteractability;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.PurchaseInteraction.Awake -= On_PurchaseInteraction_Awake;
        On.RoR2.PurchaseInteraction.GetInteractability -= On_PurchaseInteraction_GetInteractability;
    }

    private void On_PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
    {
        orig(self);
        self.StartCoroutine(HandleDelayedInstancing(self));
    }

    private IEnumerator HandleDelayedInstancing(PurchaseInteraction self)
    {
        yield return 0;
        
        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        
        if (instanceHandler != null && instanceHandler.SourceObject != null && NetworkServer.active)
        {
            Plugin._logger.LogInfo("Testing - Start called on PurchaseInteraction with InstanceHandler");

            PurchaseInteraction source = instanceHandler.SourceObject.GetComponent<PurchaseInteraction>();
            
            Plugin._logger.LogInfo($"Copying Networkcost from {source.Networkcost} overriding {self.Networkcost}");
            
            self.Networkcost = source.Networkcost;
            Field_PurchaseInteraction_rng.SetValue(self, Field_PurchaseInteraction_rng.GetValue(source));
        }
    }

    private Interactability On_PurchaseInteraction_GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
    {
        var interactability = orig(self, activator);

        var body = activator.GetComponent<CharacterBody>();
        if (body)
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceHandler = self.GetComponent<InstanceHandler>();
            if (player && instanceHandler)
            {
                if (!instanceHandler.Players.Contains(player))
                    interactability = Interactability.Disabled;
            }
        }

        return interactability;
    }
}