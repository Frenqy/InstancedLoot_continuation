using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using RoR2;

namespace InstancedLoot.ObjectHandlers;

public class PrinterHandler : AbstractObjectHandler
{
    public override string[] HandledSources => new[]
    {
        ObjectType.Duplicator, ObjectType.DuplicatorLarge, ObjectType.DuplicatorWild, ObjectType.DuplicatorMilitary
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.None;

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ShopTerminalBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }
}