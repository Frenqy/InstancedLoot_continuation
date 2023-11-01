using System;
using System.Collections.Generic;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public abstract class AbstractObjectHandler
{
    
    private static Dictionary<string, SpawnCard> _SpawnCardsForPrefabName;

    public static Dictionary<string, SpawnCard> SpawnCardsForPrefabName
    {
        get
        {
            if(_SpawnCardsForPrefabName == null)
                GenerateSpawnCards();
            return _SpawnCardsForPrefabName;
        }
    }
    public static void GenerateSpawnCards()
    {
        var spawnCards = Resources.FindObjectsOfTypeAll<InteractableSpawnCard>();
        if (_SpawnCardsForPrefabName == null) _SpawnCardsForPrefabName = new();

        foreach (var spawnCard in spawnCards)
        {
            string name = spawnCard.prefab.name;
            if(!_SpawnCardsForPrefabName.ContainsKey(name))
                _SpawnCardsForPrefabName.Add(name, spawnCard);
        }
    }
    
    protected ObjectHandlerManager Manager;
    protected InstancedLoot Plugin => Manager.Plugin;

    public AbstractObjectHandler()
    {
    }
    
    /// <summary>
    /// Register hook handlers and other events here
    /// </summary>
    public virtual void Init(ObjectHandlerManager manager)
    {
        Manager = manager;
    }
    
    public abstract string[] HandledObjectTypes { get; }
    public abstract ObjectInstanceMode ObjectInstanceMode { get; }
    public virtual bool CanObjectBeOwned => false;

    public virtual bool IsValidForObject(string objectType, GameObject gameObject)
    {
        return true;
    }

    public virtual void InstanceObject(string objectType, GameObject gameObject, PlayerCharacterMasterController[] players)
    {
        InstanceHandler[] instanceHandlers;
        PlayerCharacterMasterController[] primaryPlayers;
        if (ObjectInstanceMode == ObjectInstanceMode.InstancedObject)
        {
            instanceHandlers = new InstanceHandler[1];
            primaryPlayers = players;
        }
        else if (ObjectInstanceMode == ObjectInstanceMode.CopyObject)
        {
            instanceHandlers = new InstanceHandler[players.Length];

            for (int i = 1; i < players.Length; i++)
            {
                GameObject newInstance = CloneObject(objectType, gameObject);
                instanceHandlers[i] = InstanceSingleObjectFrom(gameObject, newInstance, new[]
                {
                    players[i]
                });
            }

            primaryPlayers = new[] { players[0] };
        }
        else
        {
            throw new InvalidOperationException("Object handler doesn't support instancing objects (?)");
        }

        InstanceHandler primary =
            instanceHandlers[0] = InstanceSingleObjectFrom(gameObject, gameObject, primaryPlayers);

        foreach (var instanceHandler in instanceHandlers)
        {
            instanceHandler.SetLinkedHandlers(instanceHandlers, false);
        }

        primary.SyncPlayers();

        foreach (var instanceHandler in instanceHandlers)
        {
            instanceHandler.UpdateVisuals();
        }
    }

    public virtual GameObject CloneObject(string objectType, GameObject gameObject)
    {
        GameObject clone = null;

        SpawnCard spawnCard = null;

        //Try to use exact SpawnCard used to create object
        if (gameObject.GetComponent<SpawnCardTracker>() is var spawnCardTracker && spawnCardTracker != null)
        {
            spawnCard = spawnCardTracker.SpawnCard;
        }

        //Fall back to scanning SpawnCards
        if (spawnCard == null)
        {
            string name = gameObject.name.Replace("(Cloned)", "");

            SpawnCardsForPrefabName.TryGetValue(name, out spawnCard);
        }

        if (spawnCard != null)
        {
            DirectorSpawnRequest spawnRequest = new(spawnCard, null, new Xoroshiro128Plus(0));
            SpawnCard.SpawnResult spawnResult = spawnCard.DoSpawn(gameObject.transform.position,
                gameObject.transform.rotation, spawnRequest);

            clone = spawnResult.spawnedInstance;

            if (clone != null)
            {
                clone.transform.position = gameObject.transform.position;
                clone.transform.rotation = gameObject.transform.rotation;
                clone.transform.localScale = gameObject.transform.localScale;
            }
        }

        return clone;
    }

    public virtual InstanceHandler InstanceSingleObjectFrom(GameObject source, GameObject target,
        PlayerCharacterMasterController[] players)
    {
        InstanceHandler instanceHandler = target.AddComponent<InstanceHandler>();
        instanceHandler.SetPlayers(players, false);
        if (ObjectInstanceMode == ObjectInstanceMode.CopyObject)
        {
            instanceHandler.OrigPlayer = players[0];
            instanceHandler.SourceObject = source;

            InstanceInfoTracker instanceInfoTracker = source.GetComponent<InstanceInfoTracker>();
            if (instanceInfoTracker != null)
            {
                instanceInfoTracker.Info.AttachTo(target);
            }
        }
        instanceHandler.ObjectInstanceMode = ObjectInstanceMode;
        return instanceHandler;
    }
}