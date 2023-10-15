using System.Linq;
using System.Reflection;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class InteractorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.Interactor.FindBestInteractableObject += IL_Interactor_FindBestInteractableObject;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.Interactor.FindBestInteractableObject -= IL_Interactor_FindBestInteractableObject;
    }

    private void IL_Interactor_FindBestInteractableObject(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        
        var methodGetComponent = typeof(Component).GetMethods(
            BindingFlags.Instance | BindingFlags.Public
        ).First(method =>
            method.Name == nameof(Component.GetComponent)
            && method.IsGenericMethod
        );
        var methodGetComponentFadeBehavior = methodGetComponent.MakeGenericMethod(typeof(InstanceHandler));

        int interactableLocId = -1;
        ILLabel labelIgnoreSpherecast = null;
        ILLabel labelNoInstanceHandler = il.DefineLabel();
        ILLabel labelDone = il.DefineLabel();
        
        cursor.GotoNext(MoveType.After,
            i => i.MatchLdloc(out interactableLocId), // Load interactable
            i => i.MatchLdarg(0), // Load interactor
            i => i.MatchCallOrCallvirt<IInteractable>("ShouldIgnoreSpherecastForInteractibility"), // Check interactable's ShouldIgnoreSpherecastForInteractibility
            i => i.MatchBrtrue(out labelIgnoreSpherecast)); // Skip processing interactable if true

        cursor.Emit(OpCodes.Ldloc, interactableLocId);
        cursor.Emit(OpCodes.Call, methodGetComponentFadeBehavior);
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldnull);
        cursor.Emit(OpCodes.Ceq);
        cursor.Emit(OpCodes.Brtrue, labelNoInstanceHandler);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit<InstanceHandler>(OpCodes.Call, "IsInstancedForInteractor");
        cursor.Emit(OpCodes.Brtrue, labelDone);
        cursor.Emit(OpCodes.Br, labelIgnoreSpherecast);

        cursor.MarkLabel(labelNoInstanceHandler);
        cursor.Emit(OpCodes.Pop);

        cursor.MarkLabel(labelDone);
    }
}