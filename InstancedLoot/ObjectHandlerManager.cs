using System;
using System.Collections.Generic;
using InstancedLoot.ObjectHandlers;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public class ObjectHandlerManager
{
    public readonly Dictionary<Type, AbstractObjectHandler> ObjectHandlers = new();
    public readonly Dictionary<string, AbstractObjectHandler> HandlersForSource = new();
    public readonly InstancedLoot Plugin;

    public ObjectHandlerManager(InstancedLoot pluginInstance)
    {
        Plugin = pluginInstance;

        RegisterHandler<ChestHandler>();
        RegisterHandler<ShrineChanceHandler>();
        RegisterHandler<RouletteChestHandler>();
    }

    public void RegisterHandler<T>() where T : AbstractObjectHandler, new()
    {
        var instance = new T();
        instance.Init(this);
        ObjectHandlers[typeof(T)] = instance;
        foreach (var source in instance.HandledSources)
        {
            HandlersForSource[source] = instance;
        }
    }

    public T GetHandler<T>() where T : AbstractObjectHandler
    {
        return (T)ObjectHandlers[typeof(T)];
    }

    public void InstanceObject(string source, GameObject gameObject, PlayerCharacterMasterController[] players)
    {
        HandlersForSource[source].InstanceObject(source, gameObject, players);
    }

    public bool CanInstanceObject(string source, GameObject gameObject)
    {
        return HandlersForSource.TryGetValue(source, out var objectHandler)
               && objectHandler.IsValidForObject(source, gameObject);
    }
}