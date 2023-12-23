using System.Linq;
using InstancedLoot.Components;
using RoR2;
using RoR2.DirectionalSearch;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class ChatHandler : AbstractHookHandler
{
    // private readonly InstanceHandlerSearch instanceHandlerSearch;
    
    // public ChatHandler()
    // {
    //     instanceHandlerSearch = new InstanceHandlerSearch();
    //     instanceHandlerSearch.minAngleFilter = 0f;
    //     instanceHandlerSearch.maxAngleFilter = 10f;
    //     instanceHandlerSearch.minDistanceFilter = 0f;
    //     instanceHandlerSearch.filterByDistinctEntity = false;
    //     instanceHandlerSearch.filterByLoS = true;
    //     instanceHandlerSearch.sortMode = SortMode.DistanceAndAngle;
    // }
    
    public override void RegisterHooks()
    {
        On.RoR2.Chat.CCSay += On_Chat_CCSay;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.Chat.CCSay += On_Chat_CCSay;
    }

    private void On_Chat_CCSay(On.RoR2.Chat.orig_CCSay orig, ConCommandArgs args)
    {
        orig(args);
        
        CharacterBody body = args.senderBody;
        CharacterMaster master = args.senderMaster;
        PlayerCharacterMasterController player = master != null ? master.playerCharacterMasterController : null;

        if (args[0].ToLower() == "uninstance")
        {
            if (body != null && player != null)
            {
                // Ray searchRay = new Ray
                // {
                //     direction = player.bodyInputs.aimDirection,
                //     origin = player.bodyInputs.aimOrigin
                // };
                //
                // searchRay = CameraRigController.ModifyAimRayIfApplicable(searchRay, body.gameObject,
                //     out var extraRaycastDistance);
                //
                // instanceHandlerSearch.searchOrigin = searchRay.origin;
                // instanceHandlerSearch.searchDirection = searchRay.direction;
                // instanceHandlerSearch.maxDistanceFilter = 100f + extraRaycastDistance;
                //
                // instanceHandlerSearch.Player = player;
                //
                // InstanceHandler target =
                //     instanceHandlerSearch.SearchCandidatesForSingleTarget(InstanceHandler.Instances);

                InstanceHandler target = null;
                
                if (player.pingerController is var pingerController && pingerController)
                {
                    if (pingerController.currentPing.active &&
                        pingerController.currentPing.targetGameObject is var obj && obj)
                    {
                        
                        target = obj.GetComponentInParent<InstanceHandler>();
                        if (target == null && obj.GetComponent<EntityLocator>() is var entityLocator &&
                            entityLocator != null && entityLocator.entity != null)
                            target = entityLocator.entity.GetComponentInParent<InstanceHandler>();
                    }
                }

                if (target != null) Plugin.HandleUninstancing(target, player);
            }

            return;
        }

        if (args[0].ToLower() == "uninstanceall")
            if (player != null)
                foreach (var instanceHandler in InstanceHandler.Instances
                             .Where(handler => InstancedLoot.CanUninstance(handler, player)).ToArray())
                    Plugin.HandleUninstancing(instanceHandler, player);
    }

    private class InstanceHandlerSearch : BaseDirectionalSearch<InstanceHandler, InstanceHandlerSearch.InstanceHandlerSearchSelector, InstanceHandlerSearch.InstanceHandlerSearchFilter>
    {
        public PlayerCharacterMasterController Player
        {
            get => candidateFilter.player;
            set => candidateFilter.player = value;
        }
        
        public struct InstanceHandlerSearchSelector : IGenericWorldSearchSelector<InstanceHandler>
        {
            public Transform GetTransform(InstanceHandler source)
            {
                return source.transform;
            }

            public GameObject GetRootObject(InstanceHandler source)
            {
                return source.gameObject;
            }
        }

        public struct InstanceHandlerSearchFilter : IGenericDirectionalSearchFilter<InstanceHandler>
        {
            public PlayerCharacterMasterController player;

            public bool PassesFilter(InstanceHandler instanceHandler)
            {
                return InstancedLoot.CanUninstance(instanceHandler, player);
            }
        }

        public InstanceHandlerSearch() : base(new InstanceHandlerSearchSelector(), new InstanceHandlerSearchFilter())
        {
        }

        public InstanceHandlerSearch(InstanceHandlerSearchSelector selector, InstanceHandlerSearchFilter candidateFilter)
		    : base(selector, candidateFilter)
	    {
	    }
    }
}