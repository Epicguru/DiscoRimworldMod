using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class Ripple : DiscoProgram
    {
        public Color LowColor, HighColor;
        public Vector3 Centre;

        public float RadiusChangePerTick = 0.1f;
        public float DespawnAfterRadius = 10;
        public float Radius = 3f;
        public float Thickness = 2f;
        public bool Circular = true;

        public Ripple(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            LowColor = Def.Get("lowColor", new Color(1, 1, 1, 0));
            HighColor = Def.Get("highColor", new Color(1, 1, 1, 1));

            Radius = Def.Get("startRadius", -2f);
            Thickness = Def.Get("thickness", 2f);
            RadiusChangePerTick = Def.Get("radiusChangePerTick", 0.1f);
            DespawnAfterRadius = Def.Get("despawnAfterRadiusReaches", 22);

            Circular = Def.Get("circular", true);
        }

        public override void Tick()
        {
            base.Tick();
            Centre = !Circular ? DJStand.FloorBounds.CenterCell.ToVector3() : DJStand.FloorBounds.CenterCell.ToVector3Shifted();

            Radius += RadiusChangePerTick;
            bool outwards = RadiusChangePerTick >= 0;
            if (outwards && Radius > DespawnAfterRadius)
                Remove();
            else if (!outwards && Radius < DespawnAfterRadius)
                Remove();
        }

        public override Color ColorFor(IntVec3 cell)
        {
            float dstFromCentre = !Circular ? (cell - Centre.ToIntVec3()).LengthManhattan : (cell.ToVector3Shifted() - Centre).magnitude;
            float dstFromTarget = Mathf.Abs(Radius - dstFromCentre);
            float lerp = DistanceToLerp(dstFromTarget);
            return Color.Lerp(LowColor, HighColor, lerp);
        }

        public virtual float DistanceToLerp(float dst)
        {
            float pos = Thickness - dst;
            if (pos < 0f)
                return 0f;
            return pos / Thickness;
        }
    }
}
