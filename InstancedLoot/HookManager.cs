using System;
using System.Collections.Generic;

namespace InstancedLoot.Hooks;

public class HookManager
{
    public readonly Dictionary<Type, AbstractHookHandler> HookHandlers = new();

    public readonly InstancedLoot Plugin;

    public HookManager(InstancedLoot pluginInstance)
    {
        Plugin = pluginInstance;

        RegisterHandler<PickupDropletHandler>();
        RegisterHandler<PrinterTargetHandler>();
        RegisterHandler<ScrapperTargetHandler>();
        
        // RegisterHandler<CommandTargetHandler>();
        // RegisterHandler<CommandHandler>();
        // RegisterHandler<PickupDropletOnCollisionOverrideHandler>();
        
        RegisterHandler<GenericPickupControllerHandler>();
        RegisterHandler<PingHandler>();
        RegisterHandler<DitherModelHandler>();
        RegisterHandler<SpawnCardHandler>();
        RegisterHandler<HologramProjectorHandler>();
        RegisterHandler<PickupPickerControllerHandler>();
        RegisterHandler<EventFunctionsHandler>();
        
        // RegisterHandler<InteractorHandler>();
    }

    public void RegisterHandler<T>() where T : AbstractHookHandler, new()
    {
        if (HookHandlers.ContainsKey(typeof(T))) return;
        var instance = new T();
        instance.Init(this);
        HookHandlers[typeof(T)] = instance;
    }

    public T GetHandler<T>() where T : AbstractHookHandler
    {
        return (T)HookHandlers[typeof(T)];
    }

    public void RegisterHooks()
    {
        Plugin._logger.LogWarning("REGISTERING HOOKS");
        foreach (var handler in HookHandlers.Values) handler.RegisterHooks();
    }

    public void UnregisterHooks()
    {
        foreach (var handler in HookHandlers.Values)
        {
            try
            {
                handler.UnregisterHooks();
            }
            catch (Exception e)
            {
                Plugin._logger.LogError($"Error while unloading HookHandler {handler.GetType()}, continuing:\n{e}");
            }
        }
    }
}