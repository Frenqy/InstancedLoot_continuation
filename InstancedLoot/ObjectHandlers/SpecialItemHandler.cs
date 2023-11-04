using InstancedLoot.Enums;
using InstancedLoot.Hooks;

namespace InstancedLoot.ObjectHandlers;

//Used by special sources that do not have an actual object to instance
public class SpecialItemHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.Sacrifice, ObjectType.HuntersTricorn
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.None;

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<SacrificeArtifactManagerHandler>();
        Plugin.HookManager.RegisterHandler<BossHunterHandler>();
    }
}