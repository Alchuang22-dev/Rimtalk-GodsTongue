using System.Collections.Generic;
using System.Linq;
using RimTalk.Source.Data;
using RimWorld;
using Verse;
using Cache = RimTalk.Data.Cache;

namespace RimTalk.Broadcast;

public static class BroadcastHelper
{
    // 和 PawnSelector.HearingRange 对齐（10f）:contentReference[oaicite:5]{index=5}
    private const float HearingRange = 10f;

    // 防极端：候选池上限 + 实际发送上限
    public const int DefaultMaxCandidates = 24;
    public const int DefaultMaxTargets = 6;

    public static List<Pawn> SelectBroadcastTargets(
        Pawn origin,
        int maxTargets = DefaultMaxTargets,
        int maxCandidates = DefaultMaxCandidates)
    {
        if (origin == null || origin.Destroyed || !origin.Spawned || origin.Dead)
            return new List<Pawn>();

        Room originRoom = origin.GetRoom();
        var candidates = new List<Pawn>();

        foreach (var p in Cache.Keys)
        {
            if (p == null || p.Destroyed || !p.Spawned || p.Dead) continue;
            if (p == origin) continue;

            // hearing capacity
            float hearing = p.health?.capacities?.GetLevel(PawnCapacityDefOf.Hearing) ?? 0f;
            if (hearing <= 0f) continue;

            // same room (or both outdoors)
            Room r = p.GetRoom();
            bool sameRoom = (r != null && originRoom != null && r == originRoom) ||
                            (r == null && originRoom == null);
            if (!sameRoom) continue;

            float range = HearingRange * hearing;
            if (!p.Position.InHorDistOf(origin.Position, range)) continue;

            // 只选 RimTalk 已缓存且可显示的 pawn（避免后续 ExecuteDialogue 直接 no-op）
            var state = Cache.Get(p);
            if (state == null || !state.CanDisplayTalk()) continue;

            candidates.Add(p);
        }

        if (candidates.Count == 0) return candidates;

        // 候选池限流（先随机打散）
        var shuffled = candidates.OrderBy(_ => Rand.Value).Take(maxCandidates).ToList();

        // 目标抽样
        return shuffled.Take(maxTargets).ToList();
    }
}
