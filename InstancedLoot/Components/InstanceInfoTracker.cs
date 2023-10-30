using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

public class InstanceInfoTracker : MonoBehaviour
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

        public InstanceOverrideInfo(string source = null, PlayerCharacterMasterController owner = null, ItemIndex sourceItemIndex = ItemIndex.None, PlayerCharacterMasterController[] playerOverride = null)
        {
            ObjectType = source;
            Owner = owner;
            SourceItemIndex = sourceItemIndex;
            PlayerOverride = playerOverride;
        }

        public void AttachTo(GameObject obj)
        {
            if (obj.GetComponent<InstanceInfoTracker>() != null) return;

            InstanceInfoTracker instanceInfoTracker = obj.AddComponent<InstanceInfoTracker>();
            instanceInfoTracker.Info = this;
        }

        public static void SetOwner(GameObject obj, PlayerCharacterMasterController newOwner)
        {
            InstanceInfoTracker instanceInfoTracker =
                obj.GetComponent<InstanceInfoTracker>() ?? obj.AddComponent<InstanceInfoTracker>();

            instanceInfoTracker.Info.Owner = newOwner;
        }
    }
}