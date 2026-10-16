using System;
using System.Collections.Generic;

namespace InstancedLoot.Hooks;

public sealed class HookManager
{
    public readonly Dictionary<Type, AbstractHookHandler> HookHandlers = new();
    public readonly InstancedLoot Plugin;
    public HookManager(InstancedLoot plugin)
    {
        Plugin = plugin;
        RegisterHandler<ObjectLifecycleHooks>();
        RegisterHandler<PurchaseInteractionHandler>();
        RegisterHandler<InteractorHandler>();
        RegisterHandler<SpawnCardHandler>();
        RegisterHandler<SceneDirectorHandler>();
        RegisterHandler<DeathRewardsHandler>();
        RegisterHandler<DitherModelHandler>();
        RegisterHandler<HologramProjectorHandler>();
        RegisterHandler<AnimationEventsHandler>();
        RegisterHandler<EffectManagerHandler>();
        RegisterHandler<PingHandler>();
    }
    public void RegisterHandler<T>() where T : AbstractHookHandler, new()
    {
        if (HookHandlers.ContainsKey(typeof(T))) return;
        var handler = new T();
        handler.Init(this);
        HookHandlers.Add(typeof(T), handler);
    }
    public T GetHandler<T>() where T : AbstractHookHandler => (T)HookHandlers[typeof(T)];
    public void RegisterHooks() { foreach (var handler in HookHandlers.Values) handler.RegisterHooks(); }
    public void UnregisterHooks() { foreach (var handler in HookHandlers.Values) handler.UnregisterHooks(); }
}
