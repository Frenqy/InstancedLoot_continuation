using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

public class InstanceInfoTracker : InstancedLootBehaviour
{
    public InstanceOverrideInfo Info;

    public string ObjectType => Info.ObjectType;
    public PlayerCharacterMasterController Owner => Info.Owner;
    public ItemIndex SourceItemIndex => Info.SourceItemIndex;

    public PlayerCharacterMasterController[] PlayerOverride => Info.PlayerOverride;

    public struct InstanceOverrideInfo
    {
        public string ObjectType;

        public PlayerCharacterMasterController Owner;

        //If ObjectType is SpecificItem
        public ItemIndex SourceItemIndex;

        public PlayerCharacterMasterController[] PlayerOverride;

        public InstanceOverrideInfo(string objectType = null, PlayerCharacterMasterController owner = null, ItemIndex sourceItemIndex = ItemIndex.None, PlayerCharacterMasterController[] playerOverride = null)
        {
            ObjectType = objectType;
            Owner = owner;
            SourceItemIndex = sourceItemIndex;
            PlayerOverride = playerOverride;
        }

        public void AttachTo(GameObject obj)
        {
            InstanceInfoTracker instanceInfoTracker = obj.GetComponent<InstanceInfoTracker>();
            if (instanceInfoTracker != null)
            {
                // if (instanceInfoTracker.ObjectType != null)
                //     return;

                instanceInfoTracker.Info.ObjectType ??= ObjectType;
                instanceInfoTracker.Info.PlayerOverride ??= PlayerOverride;
                if(instanceInfoTracker.Info.SourceItemIndex == ItemIndex.None)
                    instanceInfoTracker.Info.SourceItemIndex = SourceItemIndex;
            }
            else
            {
                instanceInfoTracker = obj.AddComponent<InstanceInfoTracker>();
                instanceInfoTracker.Info = this;
            }
        }

        public static void SetOwner(GameObject obj, PlayerCharacterMasterController newOwner)
        {
            InstanceInfoTracker instanceInfoTracker = obj.GetComponent<InstanceInfoTracker>();

            if (instanceInfoTracker == null)
            {
                instanceInfoTracker = obj.AddComponent<InstanceInfoTracker>();
                instanceInfoTracker.Info.SourceItemIndex = ItemIndex.None;
            }

            instanceInfoTracker.Info.Owner = newOwner;
        }
    }
}