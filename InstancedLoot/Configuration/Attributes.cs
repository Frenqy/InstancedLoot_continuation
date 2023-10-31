using System;
using InstancedLoot.Enums;

namespace InstancedLoot.Configuration.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class DescriptionAttribute : Attribute
{
    public string Description;
    public DescriptionAttribute(string description)
    {
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class ObjectTypeAliasesAttribute : Attribute
{
    public string[] Aliases;
    public ObjectTypeAliasesAttribute(string[] aliases)
    {
        Aliases = aliases;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class ObjectTypeDisableInstanceModesAttribute : Attribute
{
    public InstanceMode[] DisabledInstanceModes;
    public ObjectTypeDisableInstanceModesAttribute(InstanceMode[] disabledInstanceModes)
    {
        DisabledInstanceModes = disabledInstanceModes;
    }
}