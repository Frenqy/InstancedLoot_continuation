using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Hologram;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InstancedLoot.Hooks;

public class HologramProjectorHandler : AbstractHookHandler
{
    private MethodInfo Method_ReadOnlyCollection_PlayerCharacterMasterController_get_Item =
        typeof(ReadOnlyCollection<PlayerCharacterMasterController>).GetMethod("get_Item",
            BindingFlags.Instance | BindingFlags.Public);
    
    public override void RegisterHooks()
    {
        IL.RoR2.Hologram.HologramProjector.FindViewer += IL_HologramProjector_FindViewer;
        // IL.RoR2.Hologram.HologramProjector.Update += IL_HologramProjector_Update;
        On.RoR2.Hologram.HologramProjector.BuildHologram += On_HologramProjector_BuildHologram;
        On.RoR2.Hologram.HologramProjector.DestroyHologram += On_HologramProjector_DestroyHologram;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.Hologram.HologramProjector.FindViewer -= IL_HologramProjector_FindViewer;
        // IL.RoR2.Hologram.HologramProjector.Update -= IL_HologramProjector_Update;
        On.RoR2.Hologram.HologramProjector.BuildHologram -= On_HologramProjector_BuildHologram;
        On.RoR2.Hologram.HologramProjector.DestroyHologram -= On_HologramProjector_DestroyHologram;
    }

    private void On_HologramProjector_BuildHologram(On.RoR2.Hologram.HologramProjector.orig_BuildHologram orig, HologramProjector self)
    {
        orig(self);

        FadeBehavior fadeBehavior = self.GetComponent<FadeBehavior>();
        if (fadeBehavior != null)
        {
            fadeBehavior.RefreshComponentLists();
        }
    }

    private void On_HologramProjector_DestroyHologram(On.RoR2.Hologram.HologramProjector.orig_DestroyHologram orig, HologramProjector self)
    {
        orig(self);

        FadeBehavior fadeBehavior = self.GetComponent<FadeBehavior>();
        if (fadeBehavior != null)
        {
            fadeBehavior.RefreshComponentLists();
        }
    }

    private void IL_HologramProjector_FindViewer(ILContext il)
    {
        if (Plugin == null || !Plugin.enabled) return;
        
        ILCursor cursor = new ILCursor(il);
        
        var methodGetComponent = typeof(Component).GetMethods(
            BindingFlags.Instance | BindingFlags.Public
        ).First(method =>
            method.Name == nameof(Component.GetComponent)
            && method.IsGenericMethod
        );
        var methodGetComponentInstanceHandler = methodGetComponent.MakeGenericMethod(typeof(InstanceHandler));

        ILLabel labelIgnoreSpherecast = il.DefineLabel();
        ILLabel labelNoInstanceHandler = il.DefineLabel();
        ILLabel labelDone = il.DefineLabel();

        int playersVariableId = -1;
        int indexVariableId = -1;

        cursor.GotoNext(MoveType.Before,
            i => i.MatchLdloc(out playersVariableId),
            i => i.MatchLdloc(out indexVariableId),
            i => i.MatchCallOrCallvirt(Method_ReadOnlyCollection_PlayerCharacterMasterController_get_Item)
            );

        cursor.Emit(OpCodes.Ldarg_0);
        foreach (var incomingLabel in cursor.IncomingLabels)
        {
            incomingLabel.Target = cursor.Prev;
        }
        cursor.Emit(OpCodes.Call, methodGetComponentInstanceHandler);
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldnull);
        cursor.Emit(OpCodes.Ceq);
        cursor.Emit(OpCodes.Brtrue, labelNoInstanceHandler);

        cursor.Emit(OpCodes.Ldloc, playersVariableId);
        cursor.Emit(OpCodes.Ldloc, indexVariableId);
        cursor.Emit<ReadOnlyCollection<PlayerCharacterMasterController>>(OpCodes.Callvirt, "get_Item");
        cursor.Emit<InstanceHandler>(OpCodes.Call, "IsObjectInstancedFor");
        cursor.Emit(OpCodes.Brtrue, labelDone);
        cursor.Emit(OpCodes.Ldc_I4_0);
        cursor.Emit(OpCodes.Br, labelIgnoreSpherecast);

        cursor.MarkLabel(labelNoInstanceHandler);
        cursor.Emit(OpCodes.Pop);

        cursor.MarkLabel(labelDone);
        
        cursor.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<Object>("op_Implicit"),
            i => i.MatchBrfalse(out _));
        cursor.Index++;
        cursor.MarkLabel(labelIgnoreSpherecast);
        
    }

    private void IL_HologramProjector_Update(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        int varShouldDisplayHologram = -1;

        cursor.GotoNext(i => i.MatchCallOrCallvirt<IHologramContentProvider>("ShouldDisplayHologram"),
            i => i.MatchStloc(out varShouldDisplayHologram));

        cursor.GotoNext(MoveType.AfterLabel, i => i.MatchLdloc(varShouldDisplayHologram), i => i.MatchBrfalse(out _));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<HologramProjector>>(hologramProjector =>
        {
            
        });
    }
}