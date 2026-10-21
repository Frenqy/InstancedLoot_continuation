namespace InstancedLoot.Enums;

// Object instancing is the only allocation strategy in 2.0. Per-type booleans
// select vanilla behavior; there are no item-sharing or owner-only modes.
public enum InstanceMode { InstanceObject }
