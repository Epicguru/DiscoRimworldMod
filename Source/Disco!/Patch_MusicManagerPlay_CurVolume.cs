using HarmonyLib;
using RimWorld;
using Verse;

namespace Disco
{
    [HarmonyPatch(typeof(MusicManagerPlay), "get_CurVolume")]
    static class Patch_MusicManagerPlay_CurVolume
    {
        [TweakValue("RimForge", 0, 20)]
        public static float TurnSpeed = 10f;

        static void Postfix(ref float __result)
        {
            var comp = Find.CurrentMap?.GetComponent<DiscoTracker>();
            if (comp == null)
                return;

            if(comp.AnyActiveStand())
                __result = 0f;
        }
    }
}
