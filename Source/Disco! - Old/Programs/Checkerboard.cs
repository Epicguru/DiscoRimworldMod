using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class Checkerboard : DiscoProgram
    {
        public Color ColorA, ColorB;
        public int SwapInterval;
        public int SwapTime;

        private float lerp;
        private int counter;
        private bool flipFlop;

        public Checkerboard(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            ColorA = Def.Get("colorA", Color.yellow);
            ColorB = Def.Get("colorB", Color.red);
            SwapInterval = Def.Get("swapInterval", 20);
            SwapTime = Def.Get("swapTime", 10);
        }

        public override void Tick()
        {
            base.Tick();

            counter++;
            if (counter > SwapInterval)
            {
                lerp = Mathf.Clamp01((counter - SwapInterval) / (float)SwapTime);
                if(counter > SwapInterval + SwapTime)
                {
                    counter = 0;
                    lerp = 0f;
                    flipFlop = !flipFlop;
                }
            }
            else
            {
                lerp = 0f;
            }
        }

        public override Color ColorFor(IntVec3 cell)
        {
            bool isEven = (cell.x + cell.z) % 2 == 0;
            float l = flipFlop ? lerp : 1f - lerp;
            float p = isEven ? l : 1f - l;
            return Color.Lerp(ColorA, ColorB, p);
        }
    }
}
