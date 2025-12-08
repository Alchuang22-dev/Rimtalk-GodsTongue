using HarmonyLib;
using Verse;

namespace RimTalk.BabyInteraction
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("yourname.rimtalk.babyinteraction");
            harmony.PatchAll();
            Log.Message("[RimTalk.BabyInteraction] Harmony patches applied.");
        }
    }
}