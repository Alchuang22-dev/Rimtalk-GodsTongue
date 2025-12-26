using System.Collections.Generic;
using RimTalk.Data;
using RimTalk.Service;
using RimTalk.Source.Data;
using Verse;
using Cache = RimTalk.Data.Cache;

namespace RimTalk.Broadcast;

public static class BroadcastDialogueService
{
    public static int MaxTargets = BroadcastHelper.DefaultMaxTargets;
    public static int MaxCandidates = BroadcastHelper.DefaultMaxCandidates;

    public static void ExecuteBroadcast(Pawn initiator, Pawn origin, string message)
    {
        if (initiator == null || initiator.Destroyed) return;
        if (origin == null || origin.Destroyed) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        var targets = BroadcastHelper.SelectBroadcastTargets(origin, MaxTargets, MaxCandidates);
        if (targets.Count == 0) return;

        // 让 pawn “说出来”且只说一次：
        // - 对第一个目标走 ExecuteDialogue：会生成 initiator 自己的 User 气泡（非玩家时）:contentReference[oaicite:9]{index=9}
        // - 对剩余目标只加 TalkRequest（避免重复自说话）
        Pawn first = targets[0];
        CustomDialogueService.ExecuteDialogue(initiator, first, message);

        for (int i = 1; i < targets.Count; i++)
        {
            var t = targets[i];
            var rs = Cache.Get(t);
            if (rs != null && rs.CanDisplayTalk())
            {
                rs.AddTalkRequest(message, initiator, TalkType.User);
            }
        }
    }
}
