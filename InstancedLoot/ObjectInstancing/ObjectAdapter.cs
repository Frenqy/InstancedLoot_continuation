using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectInstancing;

/// <summary>Copies initialized native state; never purchases, consumes, or reopens objects.</summary>
public abstract class ObjectAdapter
{
    public virtual bool IsReady(GameObject source) => true;
    public virtual IEnumerable<GameObject> OwnedObjects(GameObject root) { yield return root; }

    public virtual Action<GameObject> CaptureState(GameObject source)
    {
        var from = source.GetComponent<PurchaseInteraction>();
        if (!from) return _ => { };
        var costType = from.costType;
        int cost = from.cost;
        bool available = from.available;
        var rng = from.rng == null ? null : new Xoroshiro128Plus(from.rng);
        return target =>
        {
            var to = target.GetComponent<PurchaseInteraction>();
            if (!to) throw new InvalidOperationException("Copy is missing PurchaseInteraction.");
            to.NetworkcostType = costType;
            to.Networkcost = cost;
            to.Networkavailable = available;
            if (rng != null) to.rng = new Xoroshiro128Plus(rng);
        };
    }
}
