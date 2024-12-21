using System;
using System.Reflection;
using InstancedLoot.Networking;
using MonoMod.RuntimeDetour;
using R2API.Networking;
using RoR2;
using RoR2.Networking;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class DevelopmentHooksHandler : AbstractHookHandler
{
    private static readonly Type syncInstanceHandlerSetType = typeof(SyncInstances);

    private IDetour getNetworkHashDetour;
    
    public override void RegisterHooks()
    {
        On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += On_NetworkManagerSystemSteam_OnClientConnect;
        On.RoR2.PlayerCharacterMasterController.GetDisplayName += On_PlayerCharacterMasterController_DisplayName;
        
        getNetworkHashDetour = new Hook(
            typeof(NetworkingAPI).GetMethod("GetNetworkHash",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
            GetNetworkHash
        );
        NetworkingAPI.RegisterMessageType<SyncInstances>();
    }

    public override void UnregisterHooks()
    {
        On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect -= On_NetworkManagerSystemSteam_OnClientConnect;
        On.RoR2.PlayerCharacterMasterController.GetDisplayName -= On_PlayerCharacterMasterController_DisplayName;
        
        getNetworkHashDetour.Undo();
        getNetworkHashDetour = null;

        // FIXME: This is not working
        //NetworkingAPI.NetMessages.Remove(1337);
    }

    private void On_NetworkManagerSystemSteam_OnClientConnect(On.RoR2.Networking.NetworkManagerSystemSteam.orig_OnClientConnect orig, NetworkManagerSystemSteam self, NetworkConnection conn)
    { }
    
    private int GetNetworkHash(Func<Type, int> orig, Type type)
    {
        if (type == syncInstanceHandlerSetType) return 1337;

        return orig(type);
    }

    private string On_PlayerCharacterMasterController_DisplayName(On.RoR2.PlayerCharacterMasterController.orig_GetDisplayName orig, PlayerCharacterMasterController self)
    {
        string displayName = orig(self);

        if (string.IsNullOrEmpty(displayName))
        {
            int id = PlayerCharacterMasterController.instances.IndexOf(self);

            displayName = $"Player{id}";
        }

        return displayName;
    }
}