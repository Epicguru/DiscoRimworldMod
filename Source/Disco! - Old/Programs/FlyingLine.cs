using UnityEngine;
using Verse;

namespace Disco.Programs
{
    public class FlyingLine : DiscoProgram
    {
        public Color LineColor, DefaultColor;
        public int MoveInterval = 10;
        public bool Forwards = true;

        private Direction direction;
        private IntVec3 cacheMoveDir;
        private IntVec3 lineCell;
        private int movesDone;

        public FlyingLine(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            LineColor = Def.Get("lineColor", Color.white);
            DefaultColor = Def.Get("defaultColor", new Color(0, 0, 0, 0));
            MoveInterval = Def.Get("moveInterval", 4);
            direction = (Direction)Def.Get("direction", 0);
            Forwards = Def.Get("forwards", true);

            var rect = DJStand.FloorBounds;
            var bl = new IntVec3(rect.minX, 0, rect.minZ);
            var br = new IntVec3(rect.maxX, 0, rect.minZ);
            var tl = new IntVec3(rect.minX, 0, rect.maxZ);
            var tr = new IntVec3(rect.maxX, 0, rect.maxZ);

            switch (direction)
            {
                case Direction.Horizontal:
                    cacheMoveDir = new IntVec3(1, 0, 0) * (Forwards ? 1 : -1);
                    lineCell = Forwards ? bl : br;
                    break;
                case Direction.Vertical:
                    cacheMoveDir = new IntVec3(0, 0, 1) * (Forwards ? 1 : -1);
                    lineCell = Forwards ? bl : tl;
                    break;
                case Direction.Diagonal:
                    cacheMoveDir = new IntVec3(1, 0, -1) * (Forwards ? 1 : -1);
                    lineCell = Forwards ? tl : br;
                    break;
                case Direction.DiagonalInverted:
                    cacheMoveDir = new IntVec3(1, 0, 1) * (Forwards ? 1 : -1);
                    lineCell = Forwards ? bl : tr;
                    break;
            }
        }

        public virtual bool IsOnLine(IntVec3 cell)
        {
            switch (direction)
            {
                case Direction.Horizontal:
                    return cell.x == lineCell.x;
                case Direction.Vertical:
                    return cell.z == lineCell.z;
                case Direction.Diagonal: // Rise is same as run.
                    return cell.x - lineCell.x == cell.z - lineCell.z;
                case Direction.DiagonalInverted: // Rise is negative run
                    return lineCell.x - cell.x ==  cell.z - lineCell.z;
            }
            return false;
        }

        public override void Tick()
        {
            base.Tick();

            if (TickCounter % MoveInterval == 0)
            {
                lineCell += cacheMoveDir;
                movesDone++;
            }

            if (movesDone >= 2 && !DJStand.FloorBounds.Contains(lineCell))
                Remove();
        }

        public override Color ColorFor(IntVec3 cell)
        {
            return IsOnLine(cell) ? LineColor : DefaultColor;
        }

        private enum Direction
        {
            Horizontal,
            Vertical,
            Diagonal,
            DiagonalInverted
        }
    }
}
