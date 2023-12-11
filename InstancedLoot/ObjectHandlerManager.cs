using System;
using System.Collections.Generic;
using InstancedLoot.Components;
using InstancedLoot.ObjectHandlers;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public class ObjectHandlerManager
{
    public readonly Dictionary<Type, AbstractObjectHandler> ObjectHandlers = new();
    public readonly Dictionary<string, AbstractObjectHandler> HandlersForObjectType = new();
    public readonly Dictionary<GameObject, AbstractObjectHandler> AwaitedObjects = new();
    public readonly InstancedLoot Plugin;

    public ObjectHandlerManager(InstancedLoot pluginInstance)
    {
        Plugin = pluginInstance;

        RegisterHandler<ChestHandler>();
        RegisterHandler<ShrineChanceHandler>();
        RegisterHandler<RouletteChestHandler>();
        RegisterHandler<MultiShopHandler>();
        RegisterHandler<OptionChestHandler>();
        RegisterHandler<PrinterHandler>();
        RegisterHandler<ScrapperHandler>();
        
        RegisterHandler<SpecialItemHandler>();
        RegisterHandler<SpecialObjectHandler>();
    }

    public void RegisterHandler<T>() where T : AbstractObjectHandler, new()
    {
        var instance = new T();
        instance.Init(this);
        ObjectHandlers[typeof(T)] = instance;
        foreach (var source in instance.HandledObjectTypes)
        {
            HandlersForObjectType[source] = instance;
        }
    }

    public T GetHandler<T>() where T : AbstractObjectHandler
    {
        return (T)ObjectHandlers[typeof(T)];
    }

    public void InstanceObject(string objectType, GameObject gameObject, PlayerCharacterMasterController[] players)
    {
        HandlersForObjectType[objectType].InstanceObject(objectType, gameObject, players);
    }

    public InstanceHandler InstanceSingleObject(string objectType, GameObject sourceGameObject, GameObject targetGameObject, PlayerCharacterMasterController[] players)
    {
        return HandlersForObjectType[objectType].InstanceSingleObjectFrom(sourceGameObject, targetGameObject, players);
    }

    public bool CanInstanceObject(string objectType, GameObject gameObject)
    {
        return HandlersForObjectType.TryGetValue(objectType, out var objectHandler)
               && objectHandler.IsValidForObject(objectType, gameObject);
    }

    public void RegisterAwaitedObject(GameObject gameObject, AbstractObjectHandler owningHandler)
    {
        AwaitedObjects.Add(gameObject, owningHandler);
    }

    public bool HandleAwaitedObject(GameObject gameObject)
    {
        if (AwaitedObjects.TryGetValue(gameObject, out var owningHandler))
        {
            AwaitedObjects.Remove(gameObject);
            owningHandler.HandleAwaitedObject(gameObject);
            return true;
        }

        return false;
    }
}