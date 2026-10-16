using UnityEngine;

namespace InstancedLoot.Components;

/// <summary>Known replica awaiting a complete initialization/owner snapshot.</summary>
public sealed class PendingOwnership : InstancedLootBehaviour
{
    public bool Resolved;
    public static void Mark(GameObject target)
    {
        if (!target.GetComponent<PendingOwnership>()) target.AddComponent<PendingOwnership>();
        FadeBehavior.Attach(target).Refresh();
    }
    public static bool IsPending(GameObject target)
    {
        var marker = target ? target.GetComponent<PendingOwnership>() : null;
        return marker && !marker.Resolved;
    }
    public static void Resolve(GameObject target)
    {
        var marker = target.GetComponent<PendingOwnership>();
        if (!marker) return;
        marker.Resolved = true;
        Destroy(marker);
        var fade = target.GetComponent<FadeBehavior>();
        if (fade) fade.Refresh();
    }
}
