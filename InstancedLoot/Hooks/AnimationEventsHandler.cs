using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class AnimationEventsHandler : AbstractHookHandler
{
    public static AnimationEvents CurrentAnimationEvents;
    
    public override void RegisterHooks()
    {
        On.RoR2.AnimationEvents.CreateEffect += On_AnimationEvents_CreateEffect;

        IL.RoR2.AnimationEvents.CreatePrefab += IL_AnimationEvents_CreatePrefab;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.AnimationEvents.CreateEffect -= On_AnimationEvents_CreateEffect;
        
        IL.RoR2.AnimationEvents.CreatePrefab -= IL_AnimationEvents_CreatePrefab;
    }

    private void On_AnimationEvents_CreateEffect(On.RoR2.AnimationEvents.orig_CreateEffect orig, AnimationEvents self, AnimationEvent animationEvent)
    {
        CurrentAnimationEvents = self;
        orig(self, animationEvent);
        CurrentAnimationEvents = null;
        
        self.GetComponent<FadeBehavior>()?.RefreshNextFrame();
        self.GetComponent<EntityLocator>()?.entity?.GetComponentInParent<FadeBehavior>()?.Refresh();
        
        Plugin._logger.LogDebug($"CreateEffect: {animationEvent.stringParameter}, {animationEvent.objectReferenceParameter}, {animationEvent.intParameter}");
    }

    private void IL_AnimationEvents_CreatePrefab(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<UnityEngine.Object>("Instantiate")))
        {
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit<AnimationEventsHandler>(OpCodes.Call, nameof(HandleObjectCreation));
        }
    }

    public static void HandleEffectCreation(GameObject objectToAdd)
    {
        if(CurrentAnimationEvents != null)
            HandleObjectCreation(objectToAdd, CurrentAnimationEvents);
    }

    public static void HandleObjectCreation(GameObject objectToAdd, AnimationEvents parent)
    {
        InstancedLoot.Instance._logger.LogDebug($"Handling creation of {objectToAdd} on {parent}");
        
        Transform parentTest = objectToAdd.transform;
        Transform parentTransform = parent.transform;
        while (parentTest != parentTransform && parentTest != null)
            parentTest = parentTest.parent;

        FadeBehavior fadeBehavior = parent.GetComponentInParent<FadeBehavior>();
        if (fadeBehavior == null && parent.GetComponent<EntityLocator>() is var entityLocator &&
            entityLocator != null && entityLocator.entity != null)
        {
            fadeBehavior = entityLocator.entity.GetComponentInParent<FadeBehavior>();
        }

        if (fadeBehavior == null)
            return;
        
        if(fadeBehavior != null)
            InstancedLoot.Instance._logger.LogDebug("Found FadeBehavior");
        
        if (parentTest != parentTransform)
        {
            InstancedLoot.Instance._logger.LogDebug("Adding to ExtraGameObjects");
            fadeBehavior.ExtraGameObjects.Add(objectToAdd);
        }
        
        fadeBehavior.Refresh();
    }
}