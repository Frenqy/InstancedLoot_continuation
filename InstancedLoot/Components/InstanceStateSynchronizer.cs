using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

/// <summary>
/// Copies the initialized purchase state that SpawnCard creation does not
/// reproduce. The copy is intentionally a snapshot: later source price/RNG
/// changes must not mutate an already-created copy.
/// </summary>
public static class InstanceStateSynchronizer
{
    public static void CopyPurchaseState(GameObject source, GameObject target)
    {
        if (!source || !target || source == target)
            return;

        PurchaseInteraction sourcePurchase = source.GetComponent<PurchaseInteraction>();
        PurchaseInteraction targetPurchase = target.GetComponent<PurchaseInteraction>();
        if (sourcePurchase == null || targetPurchase == null)
            return;

        targetPurchase.NetworkcostType = sourcePurchase.costType;
        targetPurchase.Networkcost = sourcePurchase.cost;
        if (sourcePurchase.rng != null)
            targetPurchase.rng = new Xoroshiro128Plus(sourcePurchase.rng);
    }
}
