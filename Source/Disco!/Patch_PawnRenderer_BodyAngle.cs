using HarmonyLib;
using RimWorld;
using Verse;
#if V15
using LudeonTK;
#endif

namespace Disco
{
    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    static class Patch_PawnRenderer_BodyAngle
    {
        [TweakValue("RimForge", 0, 20)]
        public static float TurnSpeed = 10f;

        static bool Prefix(Pawn ___pawn, ref float __result)
        {
            if (___pawn == null || ___pawn.Dead || ___pawn.Downed)
                return true;

            if (!___pawn.RaceProps.Humanlike)
                return true;

            var jobDef = ___pawn.jobs?.curJob?.def;
            if (jobDef != DiscoDefOf.DSC_Job_Dance_Breakdance)
                return true;

            var post = ___pawn.GetPosture();
            if (post != PawnPosture.Standing)
            {
                float dir = post == PawnPosture.LayingOnGroundFaceUp ? 1f : -1f;
                __result = (___pawn.thingIDNumber % 360) + Find.TickManager.TicksGame * dir * TurnSpeed;
                return false;
            }
            return true;
        }
    }
}
