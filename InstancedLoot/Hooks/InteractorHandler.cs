using InstancedLoot.Components;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public sealed class InteractorHandler : AbstractHookHandler
{
    public delegate bool CandidateFilter(GameObject target, out IInteractable result, Interactor actor, bool spherecast);
    public override void RegisterHooks()
    {
        IL.RoR2.Interactor.FindBestInteractableObject += FilterCandidates;
        On.RoR2.Interactor.PerformInteraction += Perform;
    }
    public override void UnregisterHooks()
    {
        IL.RoR2.Interactor.FindBestInteractableObject -= FilterCandidates;
        On.RoR2.Interactor.PerformInteraction -= Perform;
    }
    private static void FilterCandidates(ILContext il) => InteractorCandidateIL.Apply<CandidateFilter>(il, TryCandidate);
    public static bool TryCandidate(GameObject target, out IInteractable result, Interactor actor, bool spherecast)
    {
        result = null;
        if (!target) return false;
        if (!target.GetComponent<InstanceHandler>() && !PendingOwnership.IsPending(target))
            return target.TryGetComponent(out result); // Exact native path for printers and other unmanaged objects.
        if (!InstanceAccessPolicy.CanInteract(target, actor)) return false;
        foreach (var candidate in target.GetComponents<IInteractable>())
        {
            if (candidate is Behaviour behaviour && !behaviour.isActiveAndEnabled) continue;
            if (spherecast && candidate.ShouldIgnoreSpherecastForInteractibility(actor)) continue;
            var state = candidate.GetInteractability(actor);
            if (state == Interactability.Disabled) continue;
            result = candidate;
            if (state == Interactability.Available) return true;
        }
        return result != null;
    }
    private static void Perform(On.RoR2.Interactor.orig_PerformInteraction orig, Interactor self, GameObject target)
    {
        if (NetworkServer.active && !InstanceAccessPolicy.CanInteract(target, self)) return;
        orig(self, target);
    }
}
