using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace InstancedLoot.Hooks;

public class EffectManagerHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool += IL_EffectManager_SpawnEffect;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool -= IL_EffectManager_SpawnEffect;
    }

    private void IL_EffectManager_SpawnEffect(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<UnityEngine.Object>("Instantiate"));
        
        cursor.Emit(OpCodes.Dup);
        cursor.Emit<AnimationEventsHandler>(OpCodes.Call, nameof(AnimationEventsHandler.HandleEffectCreation));
    }
}