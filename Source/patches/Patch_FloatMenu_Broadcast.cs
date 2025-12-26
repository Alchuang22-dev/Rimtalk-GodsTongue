using System.Collections.Generic;
using HarmonyLib;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Cache = RimTalk.Data.Cache;

namespace RimTalk.Broadcast.Patches;

#if V1_5
[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ChoicesAtFor))]
#else
[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
#endif
public static class Patch_FloatMenu_Broadcast
{
    private const int ClickRadiusCells = 1;

#if V1_5
    [HarmonyPostfix]
    public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> __result)
    {
        TryAddBroadcastOption(__result, pawn, clickPos);
    }
#else
    [HarmonyPostfix]
    public static void Postfix(
        List<Pawn> selectedPawns,
        Vector3 clickPos,
        FloatMenuContext context,
        ref List<FloatMenuOption> __result)
    {
        Pawn pawn = (selectedPawns is { Count: 1 }) ? selectedPawns[0] : null;
        TryAddBroadcastOption(__result, pawn, clickPos);
    }
#endif

    private static void TryAddBroadcastOption(List<FloatMenuOption> result, Pawn selectedPawn, Vector3 clickPos)
    {
        if (result == null) return;
        if (!Settings.Get().AllowCustomConversation) return;

        if (selectedPawn == null || selectedPawn.Drafted) return;
        if (!selectedPawn.Spawned || selectedPawn.Dead) return;

        Map map = selectedPawn.Map;
        IntVec3 clickCell = IntVec3.FromVector3(clickPos);

        HashSet<Pawn> processed = new();

        for (int dx = -ClickRadiusCells; dx <= ClickRadiusCells; dx++)
        for (int dz = -ClickRadiusCells; dz <= ClickRadiusCells; dz++)
        {
            IntVec3 curCell = clickCell + new IntVec3(dx, 0, dz);
            if (!curCell.InBounds(map)) continue;

            var things = map.thingGrid.ThingsListAt(curCell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is not Pawn hitPawn) continue;
                if (!processed.Add(hitPawn)) continue;

                if (TryResolveInitiatorAndOrigin(selectedPawn, hitPawn, out var initiator, out var origin))
                {
                    AddBroadcastOption(result, initiator, origin);
                }
            }
        }
    }

    // 对齐 core 的拆分逻辑：User->Pawn 或 Pawn->Pawn :contentReference[oaicite:13]{index=13}
    private static bool TryResolveInitiatorAndOrigin(Pawn selectedPawn, Pawn hitPawn, out Pawn initiator, out Pawn origin)
    {
        initiator = null;
        origin = null;

        if (selectedPawn == null || hitPawn == null) return false;

        // User->Pawn：点击自己 pawn 时，initiator=playerPawn，origin=selectedPawn
        if (hitPawn == selectedPawn)
        {
            if (Settings.Get().PlayerDialogueMode == Settings.PlayerDialogueMode.Disabled)
                return false;

            var playerPawn = Cache.GetPlayer();
            if (playerPawn == null) return false;

            initiator = playerPawn;
            origin = selectedPawn;
            return true;
        }

        // Pawn->Pawn：initiator=selectedPawn，origin=hitPawn（走到 hitPawn 后以其为中心广播）
        if (!selectedPawn.IsTalkEligible()) return false;
        if (!selectedPawn.CanReach(hitPawn, PathEndMode.Touch, Danger.None)) return false;

        initiator = selectedPawn;
        origin = hitPawn;
        return true;
    }

    private static void AddBroadcastOption(List<FloatMenuOption> result, Pawn initiator, Pawn origin)
    {
        string label = initiator.IsPlayer()
            ? "RimTalk.FloatMenu.BroadcastAsPlayer".Translate()
            : "RimTalk.FloatMenu.BroadcastAsPawn".Translate(initiator.LabelShortCap);

        result.Add(new FloatMenuOption(
            label,
            () => Find.WindowStack.Add(new BroadcastDialogueWindow(initiator, origin)),
            MenuOptionPriority.Default,
            null,
            origin
        ));
    }
}
