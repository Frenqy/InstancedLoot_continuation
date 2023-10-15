using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

public class InstanceInfoTracker : MonoBehaviour
{
    public InstanceOverrideInfo Info;

    public string ItemSource => Info.ItemSource;
    public PlayerCharacterMasterController Owner => Info.Owner;
    public ItemIndex SourceItemIndex => Info.SourceItemIndex;

    public struct InstanceOverrideInfo
    {
        public string ItemSource;

        public PlayerCharacterMasterController Owner;

        //If ItemSource is SpecificItem
        public ItemIndex SourceItemIndex;

        public InstanceOverrideInfo(string source = null, PlayerCharacterMasterController owner = null, ItemIndex sourceItemIndex = ItemIndex.None)
        {
            ItemSource = source;
            Owner = owner;
            SourceItemIndex = sourceItemIndex;
        }

        public void AttachTo(GameObject obj)
        {
            if (obj.GetComponent<InstanceInfoTracker>() != null) return;

            InstanceInfoTracker instanceInfoTracker = obj.AddComponent<InstanceInfoTracker>();
            instanceInfoTracker.Info = this;
        }
    }
}