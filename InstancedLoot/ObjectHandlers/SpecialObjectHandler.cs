using InstancedLoot.Enums;
using InstancedLoot.Hooks;

namespace InstancedLoot.ObjectHandlers;

//Used by special sources that only instance the object and do not drop items
public class SpecialObjectHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.ShrineBlood, ObjectType.ShrineRestack,
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ShrineBloodBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<ShrineRestackBehaviorHandler>();
    }
}