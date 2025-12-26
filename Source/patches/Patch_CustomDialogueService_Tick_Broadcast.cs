using System.Collections.Generic;
using HarmonyLib;
using RimTalk.Service;
using Verse;

namespace RimTalk.Broadcast.Patches;

[HarmonyPatch(typeof(CustomDialogueService), nameof(CustomDialogueService.Tick))]
public static class Patch_CustomDialogueService_Tick_Broadcast
{
    public static void Postfix()
    {
        if (BroadcastPending.Pending.Count == 0) return;

        List<Pawn> toRemove = new();

        foreach (var kv in BroadcastPending.Pending)
        {
            var initiator = kv.Key;
            var pending = kv.Value;

            if (initiator == null || initiator.Destroyed || pending?.Origin == null || pending.Origin.Destroyed)
            {
                toRemove.Add(initiator);
                continue;
            }

            if (!CustomDialogueService.CanTalk(initiator, pending.Origin))
                continue;

            // 到位后执行广播
            BroadcastDialogueService.ExecuteBroadcast(initiator, pending.Origin, pending.Message);
            toRemove.Add(initiator);
        }

        foreach (var p in toRemove)
        {
            if (p != null) BroadcastPending.Pending.Remove(p);
        }
    }
}
