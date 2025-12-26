using System;              // 为了用 Math.Max
using HarmonyLib;
using RimTalk.Service;     // CustomDialogueService
using RimTalk.Util;        // pawn.IsBaby()
using RimWorld;            // LifeStageDef, HediffDef
using Verse;               // Pawn, Hediff, DefDatabase

namespace RimTalk.BabyInteraction   // 或者改成 RimTalk_GodsTongue，随你
{
    [HarmonyPatch(typeof(CustomDialogueService), nameof(CustomDialogueService.ExecuteDialogue))]
    public static class Patch_CustomDialogueService_ExecuteDialogue
    {
        private const float BabyPlayGainOnTalk = 0.05f;
        private const float ToddlerPlayGainOnTalk = 0.03f;          // 保守值
        private const float ToddlerLonelyReductionOnTalk = 0.01f;   // 保守值

        public static void Postfix(Pawn initiator, Pawn recipient, string message)
        {
            if (initiator == null || recipient == null) return;
            if (!initiator.IsPlayer()) return;

            if (recipient.IsBaby())
            {
                var playNeed = recipient.needs?.play;
                playNeed?.Play(BabyPlayGainOnTalk);
            }

            if (IsToddler(recipient))
            {
                var playNeed = recipient.needs?.play;
                playNeed?.Play(ToddlerPlayGainOnTalk);
                ReduceToddlerLoneliness(recipient);
            }
        }

        /// <summary>
        /// 软检测 Toddlers 的 1–3 岁幼儿阶段。
        /// 如果没装 Toddlers，这里会直接返回 false。
        /// </summary>
        private static bool IsToddler(Pawn pawn)
        {
            if (pawn?.ageTracker?.CurLifeStage == null) return false;

            // DefName 来自 Toddlers 的 LifeStageDef：HumanlikeToddler
            LifeStageDef toddlerStage =
                DefDatabase<LifeStageDef>.GetNamedSilentFail("HumanlikeToddler");
            return toddlerStage != null && pawn.ageTracker.CurLifeStage == toddlerStage;
        }

        /// <summary>
        /// 软依赖 Toddlers 的孤独 Hediff：ToddlerLonely，轻微降低严重度。
        /// </summary>
        private static void ReduceToddlerLoneliness(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;

            // DefName 来自 Hediffs.xml：ToddlerLonely
            HediffDef lonelyDef =
                DefDatabase<HediffDef>.GetNamedSilentFail("ToddlerLonely");
            if (lonelyDef == null) return; // 没装 Toddlers 或被改名时直接跳过

            Hediff lonely = pawn.health.hediffSet.GetFirstHediffOfDef(lonelyDef);
            if (lonely == null) return;

            lonely.Severity = Math.Max(0f, lonely.Severity - ToddlerLonelyReductionOnTalk);
        }
    }
}
