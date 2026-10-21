using System.Collections.Generic;
using System.Linq;
using RoR2;

namespace InstancedLoot.Components;

/// <summary>A native entity belongs to exactly one player for its entire lifetime.</summary>
public sealed class InstanceHandler : InstancedLootBehaviour
{
    public static readonly List<InstanceHandler> Instances = new();
    public PlayerCharacterMasterController Owner { get; private set; }
    public int GroupId { get; private set; }
    public int Revision { get; private set; }
    public IEnumerable<InstanceHandler> LinkedHandlers => Instances.Where(handler => handler && handler.GroupId == GroupId);
    public IEnumerable<PlayerCharacterMasterController> AllPlayers => LinkedHandlers.Select(handler => handler.Owner).Where(player => player).Distinct();
    public HashSet<PlayerCharacterMasterController> Players => Owner ? new() { Owner } : new();
    public PlayerCharacterMasterController OrigPlayer => Owner;

    public void Awake() { Instances.Add(this); }
    public void Assign(PlayerCharacterMasterController owner, int groupId, int revision = 1)
    {
        Owner = owner;
        GroupId = groupId;
        Revision = revision;
        PendingOwnership.Resolve(gameObject);
        FadeBehavior.Attach(gameObject).Refresh();
    }
    public bool IsInstancedFor(PlayerCharacterMasterController player) => player && Owner == player;
    public bool IsObjectInstancedFor(PlayerCharacterMasterController player) => IsInstancedFor(player);
    public bool IsInstancedForInteractor(Interactor interactor) => InstanceAccessPolicy.CanInteract(this, interactor);
    public void OnDestroy()
    {
        Instances.Remove(this);
        var fade = GetComponent<FadeBehavior>();
        if (fade) Destroy(fade);
    }
}
