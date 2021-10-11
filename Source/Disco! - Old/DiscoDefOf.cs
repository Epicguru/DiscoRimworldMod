using RimWorld;
using Verse;

namespace Disco
{
    [DefOf]
    public static class DiscoDefOf
    {
        static DiscoDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DiscoDefOf));
        }

        public static ShaderTypeDef Transparent;
        public static TerrainDef DSC_DiscoFloor;
        public static ThoughtDef DSC_AttendedDiscoThought;
        public static TaleDef DSC_AttendedDiscoTale;
        public static GatheringDef DSC_DiscoGathering;
        public static JobDef DSC_Job_StandAtDJPlatform;
        public static JobDef DSC_Job_Dance_Breakdance;
    }
}
