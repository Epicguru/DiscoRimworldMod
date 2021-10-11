using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class Noise : DiscoProgram
    {
        public float Scale;
        public float Multi;
        public float Add;

        public Noise(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            Scale = Def.Get("scale", 2f);
            if ((int) Scale == Scale)
                Scale += 0.02f;
            Add = Def.Get("add", 0f);
            Multi = Def.Get("multi", 1f);
        }

        public override Color ColorFor(IntVec3 cell)
        {
            float perlin = Mathf.PerlinNoise((cell.x + 0.2451f) * Scale, (cell.z + 0.2451f) * Scale);
            float n = Mathf.Clamp01((Multi * perlin) + Add);
            return new Color(n, n, n, 1);
        }
    }
}
