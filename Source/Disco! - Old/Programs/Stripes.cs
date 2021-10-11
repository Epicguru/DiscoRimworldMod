using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class Stripes : DiscoProgram
    {
        public int EveryX = 2;
        public int ShiftInterval = 20;
        public int ShiftDirection = 1;
        public bool Horizontal = false;

        private int offset;
        private Color[] colors;

        public Stripes(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            EveryX = Def.Get("everyX", 2);
            if (EveryX < 1)
                EveryX = 1;
            ShiftInterval = Def.Get("shiftInterval", 20);
            ShiftDirection = Def.Get("shiftDirection", 1);
            Horizontal = Def.Get("horizontal", true);
            colors = Def.Get<Color[]>("colors");
        }

        public override void Tick()
        {
            base.Tick();

            if (TickCounter % ShiftInterval == 0)
            {
                offset += ShiftDirection;
            }

            if (colors == null || colors.Length < 2)
            {
                Core.Error("Null colors or less than 2 colors! Removing.");
                Remove();
            }
        }

        public override Color ColorFor(IntVec3 cell)
        {
            int coord = Horizontal ? cell.z : cell.x;
            coord += offset;
            if (coord % EveryX != 0)
                return default;

            return colors[(coord / EveryX) % colors.Length];
        }
    }
}
