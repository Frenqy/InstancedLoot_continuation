using InstancedLoot.Enums;
using InstancedLoot.Hooks;

namespace InstancedLoot.ObjectHandlers;

public class PrinterHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.Duplicator, ObjectType.DuplicatorLarge, ObjectType.DuplicatorWild, ObjectType.DuplicatorMilitary,
        ObjectType.LunarCauldronWhiteToGreen, ObjectType.LunarCauldronGreenToRed, ObjectType.LunarCauldronRedToWhite,
        ObjectType.ShrineCleanse
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.None;

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ShopTerminalBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }
}