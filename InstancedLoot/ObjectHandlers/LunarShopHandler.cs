using InstancedLoot.Enums;

namespace InstancedLoot.ObjectHandlers;

public class LunarShopHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.LunarShopTerminal
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.InstancedObject;
}