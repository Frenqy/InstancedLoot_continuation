using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class FadeHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.DitherModel.RefreshObstructorsForCamera += IL_DitherModel_RefreshObstructorsForCamera;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.DitherModel.RefreshObstructorsForCamera -= IL_DitherModel_RefreshObstructorsForCamera;
    }

    private void IL_DitherModel_RefreshObstructorsForCamera(ILContext il)
    {
        var cursor = new ILCursor(il);
        
        var labelNoFadeBehavior = il.DefineLabel();
        var labelDone = il.DefineLabel();
        
        var methodGetComponent = typeof(Component).GetMethods(
            BindingFlags.Instance | BindingFlags.Public
        ).First(method =>
            method.Name == nameof(Component.GetComponent)
            && method.IsGenericMethod
        );
        Plugin._logger.LogWarning(methodGetComponent);
        var methodGetComponentFadeBehavior = methodGetComponent.MakeGenericMethod(typeof(FadeBehavior));
        Plugin._logger.LogWarning(methodGetComponentFadeBehavior);

        int ditherModelLoc = -1;
        
        cursor.GotoNext(MoveType.Before,
            i => i.MatchLdloc(out ditherModelLoc),
            i => i.MatchCallOrCallvirt<DitherModel>("UpdateDither"));

        cursor.Emit(OpCodes.Ldloc, ditherModelLoc);
        cursor.Emit(OpCodes.Ldloc, ditherModelLoc);
        
        cursor.Emit(OpCodes.Call, methodGetComponentFadeBehavior);
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldnull);
        cursor.Emit(OpCodes.Ceq);
        cursor.Emit(OpCodes.Brtrue, labelNoFadeBehavior);
        
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit<FadeBehavior>(OpCodes.Call, nameof(FadeBehavior.GetFadeLevelForCameraRigController));
        cursor.Emit(OpCodes.Ldloc, ditherModelLoc);
        cursor.Emit<DitherModel>(OpCodes.Ldfld, "fade");
        cursor.Emit(OpCodes.Mul);
        cursor.Emit<DitherModel>(OpCodes.Stfld, "fade");
        cursor.Emit(OpCodes.Br, labelDone);

        cursor.MarkLabel(labelNoFadeBehavior);
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Pop);
        
        cursor.MarkLabel(labelDone);
        
        // Plugin._logger.LogInfo(il);
    }
}