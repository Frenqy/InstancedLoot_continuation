using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using InstancedLoot.Enums;

namespace InstancedLoot.Configuration;

public class AcceptableValuesInstanceMode : AcceptableValueBase
{
    public AcceptableValuesInstanceMode(ICollection<InstanceMode> acceptedValues) : base(typeof(InstanceMode))
    {
        AcceptableValues = new SortedSet<InstanceMode>(acceptedValues);
    }

    public SortedSet<InstanceMode> AcceptableValues;

    public override object Clamp(object value)
    {
        if (IsValid(value)) return value;
        return InstanceMode.Default;
    }

    public override bool IsValid(object value)
    {
        return AcceptableValues.Contains((InstanceMode)value);
    }

    public override string ToDescriptionString() => "# Acceptable values: " +
                                                    string.Join(", ",
                                                        AcceptableValues.Select(x => x.ToString()).ToArray());
}