using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class EdgeDistance : DiscoProgram
    {
        public Color EdgeColor, MiddleColor;
        public float SolidDistance;
        public float FadeDistance;

        public EdgeDistance(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            EdgeColor = Def.Get("edgeColor", Color.white);
            MiddleColor = Def.Get("middleColor", new Color(0, 0, 0, 0));
            SolidDistance = Def.Get("solidDistance", 1f);
            FadeDistance = Def.Get("fadeDistance", 3f);
        }

        protected virtual float PostProcessSolidDistance()
        {
            return SolidDistance;
        }

        protected virtual float GetColorLerp(float dst)
        {
            // Where 0 is edge and 1 is middle.

            float solid = PostProcessSolidDistance();

            if (dst < solid)
                return 0f;

            if (dst >= solid + FadeDistance)
                return 1f;

            return (dst - solid) / FadeDistance;
        }

        public override Color ColorFor(IntVec3 cell)
        {
            float dst = DJStand.GetCellDistanceFromEdge(cell);
            return Color.Lerp(EdgeColor, MiddleColor, GetColorLerp(dst));
        }
    }
}
