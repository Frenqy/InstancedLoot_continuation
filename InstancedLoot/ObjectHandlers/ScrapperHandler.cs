using InstancedLoot.Enums;
using InstancedLoot.Hooks;

namespace InstancedLoot.ObjectHandlers;

public class ScrapperHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } = { ObjectType.Scrapper };
    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.None;

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ScrapperControllerHandler>();
    }
}