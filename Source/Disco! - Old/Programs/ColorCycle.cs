using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class ColorCycle : DiscoProgram
    {
        public int FadeTicks;

        private int currentIndex;
        private Color currentColor;
        private int counter;
        private Color[] colors;

        public ColorCycle(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            FadeTicks = Def.Get("transitionTime", 80);
            colors = Def.Get<Color[]>("colors");
        }

        public override void Tick()
        {
            base.Tick();

            counter++;
            if (counter >= FadeTicks)
            {
                counter = 0;
                currentIndex++;
                currentIndex %= colors.Length;
            }
            float p = Mathf.Clamp01((float)counter / FadeTicks);

            Color colorNow = colors[currentIndex];
            int nextIndex = currentIndex == colors.Length - 1 ? 0 : currentIndex + 1;
            Color nextColor = colors[nextIndex];

            currentColor = Color.Lerp(colorNow, nextColor, p);
        }

        public override Color ColorFor(IntVec3 cell)
        {
            return currentColor;
        }
    }
}
