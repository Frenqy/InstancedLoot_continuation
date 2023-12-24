using System;
using System.Linq;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class SceneDirectorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.SceneDirector.Start += IL_SceneDirector_Start;
        IL.RoR2.SceneDirector.PopulateScene += IL_SceneDirector_PopulateScene;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.SceneDirector.Start -= IL_SceneDirector_Start;
        IL.RoR2.SceneDirector.PopulateScene -= IL_SceneDirector_PopulateScene;
    }

    private void IL_SceneDirector_Start(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Run>("get_participatingPlayerCount"));
        cursor.EmitDelegate<Func<int, int>>(playerCount =>
        {
            if (ModConfig.ReduceInteractibleBudget.Value)
                return 1;

            return playerCount;
        });
    }

    private void HookInteractible(ILCursor cursor, string resourceString, Func<ItemDef> itemDefGetter)
    {
        int variableLoopIndex = -1;

        cursor.GotoNext(i => i.MatchLdstr(resourceString));
        cursor.GotoNext(
            i => i.MatchLdloc(out variableLoopIndex),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd());
        cursor.GotoPrev(MoveType.After, i => i.MatchCallOrCallvirt<DirectorCore>("TrySpawnObject"));
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldloc, variableLoopIndex);
        cursor.EmitDelegate<Action<GameObject, int>>((obj, index) =>
        {
            var characterMastersWithItem = CharacterMaster.readOnlyInstancesList
                .Where(master => master.inventory.GetItemCount(itemDefGetter()) > 0).ToArray();
            if (characterMastersWithItem.Length == 0)
                return; // Sanity check
            
            index %= characterMastersWithItem.Length;
            var characterMaster = characterMastersWithItem[index];

            if (characterMaster &&
                characterMaster.playerCharacterMasterController is var player && player)
            {
                InstanceInfoTracker.InstanceOverrideInfo.SetOwner(obj, player);
            }
        });
        
    }

    private void IL_SceneDirector_PopulateScene(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        
        HookInteractible(cursor, "SpawnCards/InteractableSpawnCard/iscLockbox", () => RoR2Content.Items.TreasureCache);
        HookInteractible(cursor, "SpawnCards/InteractableSpawnCard/iscLockboxVoid", () => DLC1Content.Items.TreasureCacheVoid);
        HookInteractible(cursor, "SpawnCards/InteractableSpawnCard/iscFreeChest", () => DLC1Content.Items.FreeChest);
    }
}