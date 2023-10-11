using System.Collections.Generic;
using InstancedLoot.Enums;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public class InstanceOverride : MonoBehaviour
{
    public InstanceOverrideInfo Info;

    public ItemSource ItemSource => Info.ItemSource;
    public PlayerCharacterMasterController Owner => Info.Owner;
    public ItemIndex SourceItemIndex => Info.SourceItemIndex;

    public struct InstanceOverrideInfo
    {
        public ItemSource ItemSource;

        public PlayerCharacterMasterController Owner;

        //If ItemSource is SpecificItem
        public ItemIndex SourceItemIndex;

        public InstanceOverrideInfo(ItemSource source, PlayerCharacterMasterController owner)
        {
            ItemSource = source;
            Owner = owner;
        }

        public InstanceOverrideInfo(ItemSource source, PlayerCharacterMasterController owner, ItemIndex sourceItemIndex) : this(source, owner)
        {
            SourceItemIndex = sourceItemIndex;
        }

        public void AttachTo(GameObject obj)
        {
            if (obj.GetComponent<InstanceOverride>() != null) return;

            InstanceOverride instanceOverride = obj.AddComponent<InstanceOverride>();
            instanceOverride.Info = this;
        }
    }
}