using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class BeatCircle : DiscoProgram
    {
        public override bool UseRandomTickOffset => false;

        public Color CircleColor, OtherColor;
        public Vector3 Centre;
        public bool Circular;
        public float BaseRadius;
        public float BlendDst;
        public float BeatVel;
        public float BeatRecovery;
        public float VelRecovery;
        public int BeatInterval;

        private float vel;
        private float radius;
        private float radiusAdd;

        public BeatCircle(ProgramDef def) : base(def) { }

        public override void Init()
        {
            CircleColor = Def.Get("circleColor", Color.red);
            OtherColor = Def.Get("otherColor", Color.white);

            BeatInterval = Def.Get("beatInterval", 30);

            Circular = Def.Get("circular", true);

            BaseRadius = Def.Get("baseRadius", 0.5f);
            BlendDst = Def.Get("blendDistance", 0.5f);
            BeatVel = Def.Get("beatVelocity", 0.35f);
            BeatRecovery = Def.Get("beatRecovery", 0.86f);
            VelRecovery = Def.Get("velocityRecovery", 0.78f);
        }

        public override void Tick()
        {
            base.Tick();
            Centre = !Circular ? DJStand.FloorBounds.CenterCell.ToVector3() : DJStand.FloorBounds.CenterCell.ToVector3Shifted();

            if (TickCounter % BeatInterval == 0)
            {
                vel = BeatVel;
            }

            radiusAdd += vel;
            radius = BaseRadius + radiusAdd;

            vel *= VelRecovery;
            radiusAdd *= BeatRecovery;
        }

        public override Color ColorFor(IntVec3 cell)
        {
            float dstFromCentre = !Circular ? (cell - Centre.ToIntVec3()).LengthManhattan : (cell.ToVector3Shifted() - Centre).magnitude;
            float dstFromTarget = Mathf.Abs(radius - dstFromCentre);
            float lerp = DistanceToLerp(dstFromTarget);
            return Color.Lerp(CircleColor, OtherColor, lerp);
        }

        public virtual float DistanceToLerp(float dst)
        {
            if (dst <= radius)
                return 0f;
            if (dst <= radius + BlendDst)
                return Mathf.Clamp01((dst - radius) / BlendDst);
            return 1f;
        }
    }
}
