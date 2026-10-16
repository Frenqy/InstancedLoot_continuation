using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Networking;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

// Opt-in observations only: never call interaction methods to discover their state.
internal static class InteractionDiagnostics
{
    private static readonly Dictionary<string, string> LastStates = new();
    private static int lines;

    private static string Id(GameObject obj) => obj ? obj.GetComponent<NetworkIdentity>()?.netId.ToString() ?? "none" : "none";
    private static bool Enabled => InstancedLoot.Instance != null && InstancedLoot.Instance.InteractionLoggingEnabled && lines < 512;

    private static void Changed(string key, string state)
    {
        if (LastStates.TryGetValue(key, out string previous) && previous == state) return;
        if (LastStates.Count < 512) LastStates[key] = state;
        lines++;
        InstancedLoot.Instance._logger.LogInfo(state);
        if (lines == 512)
            InstancedLoot.Instance._logger.LogInfo("Selection/pickup diagnostic limit reached (512 lines); restart the game for a new trace.");
    }

    internal static void Clear()
    {
        LastStates.Clear();
        lines = 0;
    }

    private static string State(GameObject target)
    {
        if (!target) return "target=none";
        var handler = target.GetComponent<InstanceHandler>();
        var purchase = target.GetComponent<PurchaseInteraction>();
        string players = handler == null ? "none" : string.Join(",", handler.Players.Select(p => Id(p.gameObject)));
        return $"netId={Id(target)} name={target.name} handler={handler != null} sourceNetId={Id(handler?.SourceObject)} " +
            $"OrigPlayer={Id(handler?.OrigPlayer?.gameObject)} Players=[{players}] pending={SyncInstances.IsPending(target)} " +
            $"available={(purchase ? purchase.available.ToString() : "n/a")}";
    }

    internal static void Selection(Interactor interactor, GameObject selected)
    {
        if (!Enabled) return;
        var body = interactor.GetComponent<CharacterBody>();
        var player = body && body.master ? body.master.playerCharacterMasterController : null;
        string components = selected ? string.Join(",", selected.GetComponents<MonoBehaviour>().OfType<IInteractable>().Select(component => component.GetType().Name)) : "none";
        Changed($"selection:{interactor.GetInstanceID()}",
            $"Selection server={NetworkServer.active} buyer={Id(player?.gameObject)} body={Id(body?.gameObject)} interactables=[{components}] {State(selected)}");
    }

    internal static void Pickup(string action, GenericPickupController pickup, CharacterBody body)
    {
        if (!Enabled) return;
        var player = body && body.master ? body.master.playerCharacterMasterController : null;
        Changed($"pickup:{Id(pickup.gameObject)}:{Id(player?.gameObject)}:{action}",
            $"Pickup {action} server={NetworkServer.active} buyer={Id(player?.gameObject)} {State(pickup.gameObject)}");
    }

    internal static void Prompt(PurchaseInteraction purchase, Interactor interactor, Interactability native, bool allowed)
    {
        if (!Enabled) return;
        var body = interactor ? interactor.GetComponent<CharacterBody>() : null;
        var player = body && body.master ? body.master.playerCharacterMasterController : null;
        Changed($"prompt:{purchase.GetInstanceID()}:{Id(player?.gameObject)}",
            $"Prompt server={NetworkServer.active} buyer={Id(player?.gameObject)} native={native} allowed={allowed} {State(purchase.gameObject)}");
    }
}
