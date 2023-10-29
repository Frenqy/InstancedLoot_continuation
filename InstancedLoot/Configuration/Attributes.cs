using System;

namespace InstancedLoot.Configuration.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ObjectTypeDescriptionAttribute : Attribute
{
    public string Description;
    public ObjectTypeDescriptionAttribute(string description)
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