using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public class InstanceTracker : MonoBehaviour
{
    public SortedSet<PlayerCharacterMasterController> Players;
}