using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Enums;
using InstancedLoot.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace InstancedLoot.Components;

public class InstanceHandler : InstancedLootBehaviour
{
    //Players that can currently interact with this object instance
    public HashSet<PlayerCharacterMasterController> Players;
    //If ObjectInstanceMode is CopyObject, player for which the object instance was originally instanced
    public PlayerCharacterMasterController OrigPlayer;
    //Mode of instancing used for the object
    public ObjectInstanceMode ObjectInstanceMode = ObjectInstanceMode.InstancedObject;
    //Set of InstanceHandlers for which this object was instanced.
    //If ObjectInstanceMode is not CopyObject, contains only the current handler.
    public InstanceHandler[] LinkedHandlers;
    //If ObjectInstanceMode is CopyObject, contains the object this is a copy of.
    //Used when instancing to copy information from the source object.
    public GameObject SourceObject;
    //Set of all players that can interact with any of the object's instances
    public HashSet<PlayerCharacterMasterController> AllPlayers;

    public static List<InstanceHandler> Instances = new();
    
    public void Awake()
    {
        AllPlayers = new();
        LinkedHandlers = new [] {this};
        Instances.Add(this);
    }

    public void OnDestroy()
    {
        var fadeBehavior = GetComponent<FadeBehavior>();
        
        if (fadeBehavior)
            Destroy(fadeBehavior);

        Instances.Remove(this);
    }

    public void SetLinkedHandlers(IEnumerable<InstanceHandler> handlers, bool sync = true)
    {
        LinkedHandlers = handlers.ToArray();
        if(sync)
            SyncPlayers();
    }

    public void SetPlayers(IEnumerable<PlayerCharacterMasterController> players, bool sync = true)
    {
        Players = new HashSet<PlayerCharacterMasterController>(players);
        if(sync)
            SyncPlayers();
    }

    public void RemovePlayer(PlayerCharacterMasterController player, bool sync = true)
    {
        if (Players == null)
            return;
        Players.Remove(player);
        if(sync)
            SyncPlayers();
    }
    
    public void AddPlayer(PlayerCharacterMasterController player, bool sync = true)
    {
        if (Players == null)
            Players = new();
        Players.Add(player);
        if(sync)
            SyncPlayers();
    }

    public void SyncPlayers()
    {
        if (NetworkServer.active)
        {
            new SyncInstanceHandlerSet(LinkedHandlers).Send(NetworkDestination.Clients);
        }
        
        UpdateVisuals();
    }

    public void SyncToPlayer(PlayerCharacterMasterController player)
    {
        if (NetworkServer.active)
        {
            if (player == null || player.networkUser == null || player.networkUser.connectionToClient == null)
                return;
            //TODO: Am I doing this right? Is there a better way to handle this?
            new SyncInstanceHandlerSet(LinkedHandlers).Send(player.networkUser.connectionToClient);
        }
    }

    public void SyncToConnection(NetworkConnection connection)
    {
        if (NetworkServer.active)
        {
            new SyncInstanceHandlerSet(LinkedHandlers).Send(connection);
        }
    }

    public void UpdateVisuals()
    {
        AllPlayers.Clear();
        AllPlayers.UnionWith(Players);
        foreach (var instanceHandler in LinkedHandlers)
        {
            AllPlayers.UnionWith(instanceHandler.Players);
        }
        
        FadeBehavior.Attach(gameObject);
        
        // var localPlayer = PlayerCharacterMasterController.instances[0]; // Seems hacky to me, but it's recommended?
        // var fadeBehavior = GetComponent<FadeBehavior>();
        //
        // if (Players.Contains(localPlayer) && fadeBehavior)
        //     Destroy(fadeBehavior);
        //
        // if (!Players.Contains(localPlayer))
        //     FadeBehavior.Attach(gameObject);
    }

    public bool IsObjectInstancedFor(PlayerCharacterMasterController player)
    {
        switch (ObjectInstanceMode)
        {
            case ObjectInstanceMode.CopyObject: return OrigPlayer == player;
            case ObjectInstanceMode.InstancedObject: return Players.Contains(player);
        }

        return true;
    }
    
    public bool IsInstancedFor(PlayerCharacterMasterController player)
    {
        return Players.Contains(player);
    }

    public bool IsInstancedForInteractor(Interactor interactor)
    {
        PlayerCharacterMasterController player = interactor.GetComponent<PlayerCharacterMasterController>();
        if (player)
            return IsInstancedFor(player);
        return false;
    }
}