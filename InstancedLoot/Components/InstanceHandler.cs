using System.Collections.Generic;
using InstancedLoot.Enums;
using InstancedLoot.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Components;

public class InstanceHandler : InstancedLootBehaviour
{
    //Players that can currently interact with this object instance
    public HashSet<PlayerCharacterMasterController> Players = new();
    //If ObjectInstanceMode is CopyObject, player for which the object instance was originally instanced
    public PlayerCharacterMasterController OrigPlayer;
    //Mode of instancing used for the object
    public ObjectInstanceMode ObjectInstanceMode => sharedInfo?.ObjectInstanceMode ?? ObjectInstanceMode.None;
    //Set of InstanceHandlers for which this object was instanced.
    //If ObjectInstanceMode is not CopyObject, contains only the current handler.
    public List<InstanceHandler> LinkedHandlers => sharedInfo?.LinkedHandlers;
    //If ObjectInstanceMode is CopyObject, contains the object this is a copy of.
    //Used when instancing to copy information from the source object.
    public GameObject SourceObject => sharedInfo?.SourceObject;
    //Set of all players that can interact with any of the object's instances
    public HashSet<PlayerCharacterMasterController> AllPlayers => sharedInfo.AllPlayers;
    public HashSet<PlayerCharacterMasterController> AllOrigPlayers => sharedInfo.AllOrigPlayers;

    private SharedInstanceInfo sharedInfo;
    public SharedInstanceInfo SharedInfo
    {
        get => sharedInfo;
        set
        {
            if (sharedInfo == value) return;
            
            if (sharedInfo != null)
            {
                sharedInfo.LinkedHandlers.Remove(this);
                sharedInfo.RecalculateAllPlayers();
            }

            sharedInfo = value;
            
            if (sharedInfo != null)
            {
                sharedInfo.LinkedHandlers.Add(this);
                sharedInfo.AllPlayers.UnionWith(Players); //Don't have to recalculate when adding
                if (OrigPlayer)
                    sharedInfo.AllOrigPlayers.Add(OrigPlayer);
            }
        }
    }

    public class SharedInstanceInfo
    {
        //Set of InstanceHandlers for which this object was instanced.
        //If ObjectInstanceMode is not CopyObject, contains only the current handler.
        public List<InstanceHandler> LinkedHandlers = new();
        //If ObjectInstanceMode is CopyObject, contains the object this is a copy of.
        //Used when instancing to copy transform from the source object.
        public GameObject SourceObject;
        //Set of all players that can interact with any of the object's instances
        public readonly HashSet<PlayerCharacterMasterController> AllPlayers = new();
        public readonly HashSet<PlayerCharacterMasterController> AllOrigPlayers = new();
        //Mode of instancing used for the object
        public ObjectInstanceMode ObjectInstanceMode;

        public void RecalculateAllPlayers()
        {
            AllPlayers.Clear();
            AllOrigPlayers.Clear();
            foreach (var instanceHandler in LinkedHandlers)
            {
                AllPlayers.UnionWith(instanceHandler.Players);
                if(instanceHandler.OrigPlayer)
                    AllOrigPlayers.Add(instanceHandler.OrigPlayer);
            }
        }

        public void SyncTo(NetworkConnection connection)
        {
            if (NetworkServer.active) new SyncInstances(this).Send(connection);
        }

        public void SyncToAll()
        {
            if (NetworkServer.active) new SyncInstances(this).Send(NetworkDestination.Clients);
        }
    }

    public static List<InstanceHandler> Instances = new();
    
    public void Awake()
    {
        Instances.Add(this);
        FadeBehavior.Attach(gameObject);
    }

    public void OnDestroy()
    {
        var fadeBehavior = GetComponent<FadeBehavior>();
        
        if (fadeBehavior)
            Destroy(fadeBehavior);

        Instances.Remove(this);
    }

    public void SetPlayers(IEnumerable<PlayerCharacterMasterController> players, bool sync = true)
    {
        Players = [..players];
        if(sync)
            SyncPlayers();
    }

    public void RemovePlayer(PlayerCharacterMasterController player, bool sync = true)
    {
        Players.Remove(player);
        if(sync)
            SyncPlayers();
    }
    
    public void AddPlayer(PlayerCharacterMasterController player, bool sync = true)
    {
        Players.Add(player);
        if(sync)
            SyncPlayers();
    }

    public void SyncPlayers()
    {
        sharedInfo.RecalculateAllPlayers();
        
        if (NetworkServer.active)
        {
            sharedInfo.SyncToAll();
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
            sharedInfo.SyncTo(player.networkUser.connectionToClient);
        }
    }

    public void SyncToConnection(NetworkConnection connection)
    {
        if (NetworkServer.active) sharedInfo.SyncTo(connection);
    }

    public void UpdateVisuals()
    {
        FadeBehavior.Attach(gameObject);
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