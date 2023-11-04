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
        
        RegisterHandler<DevelopmentHooksHandler>();

        // RegisterHandler<PrinterTargetHandler>();
        // RegisterHandler<ScrapperControllerHandler>();
        
        // RegisterHandler<CommandTargetHandler>();
        // RegisterHandler<CommandHandler>();
        // RegisterHandler<PickupDropletOnCollisionOverrideHandler>();
        
        RegisterHandler<SceneDirectorHandler>();
        RegisterHandler<SpawnCardHandler>();
        // RegisterHandler<EventFunctionsHandler>();
        
        RegisterHandler<PingHandler>();
        RegisterHandler<DitherModelHandler>();
        RegisterHandler<HologramProjectorHandler>();
        
        RegisterHandler<GenericPickupControllerHandler>();
        RegisterHandler<PickupPickerControllerHandler>();
        RegisterHandler<PickupDropletControllerHandler>();
        RegisterHandler<CommandArtifactManagerHandler>();
        
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