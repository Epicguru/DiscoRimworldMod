using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Disco
{
    public class JobDriver_DanceShuffle : JobDriver_Dance
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to the dance floor or move across it slightly.
            var goToFloor = GoToDanceFloorOrMoveSlightly();
            if (goToFloor != null)
                yield return goToFloor;

            // Stand still.
            yield return Toils_General.StopDead();

            // This stuff here is just to make sure the pawn doesn't try to literally
            // shuffle out of the disco room.
            var map = pawn.Map;
            var startRoom = pawn.Position.GetRoom(map);
            var startPos = pawn.Position;
            bool IsValidSpot(IntVec3 pos)
            {
                return pos.GetRoom(map) == startRoom;
            }

            // Local function that returns the pawn to the starting (center) cell,
            // then faces forwards (south) and waits for 15 ticks.
            IEnumerable<Toil> Reset()
            {
                yield return Toils_Goto.GotoCell(startPos, PathEndMode.OnCell);
                yield return FaceDir(0, -1);
                yield return Toils_General.Wait(15);
            }

            // Repeat twice...
            for (int i = 0; i < 2; i++)
            {
                // Right
                if (IsValidSpot(startPos + new IntVec3(1, 0, 0)))
                {
                    yield return Toils_Goto.GotoCell(startPos + new IntVec3(1, 0, 0), PathEndMode.OnCell);
                    yield return FaceDir(1, 0);
                    yield return Toils_General.Wait(25);
                }

                // Reset
                foreach (var e in Reset())
                    yield return e;

                // Down
                if (IsValidSpot(startPos + new IntVec3(0, 0, -1)))
                {
                    yield return Toils_Goto.GotoCell(startPos + new IntVec3(0, 0, -1), PathEndMode.OnCell);
                    yield return FaceDir(0, -1);
                    yield return Toils_General.Wait(25);
                }

                // Reset
                foreach (var e in Reset())
                    yield return e;

                // Left
                if (IsValidSpot(startPos + new IntVec3(-1, 0, 0)))
                {
                    yield return Toils_Goto.GotoCell(startPos + new IntVec3(-1, 0, 0), PathEndMode.OnCell);
                    yield return FaceDir(-1, 0);
                    yield return Toils_General.Wait(25);
                }

                // Reset
                foreach (var e in Reset())
                    yield return e;

                // Up
                if (IsValidSpot(startPos + new IntVec3(0, 0, 1)))
                {
                    yield return Toils_Goto.GotoCell(startPos + new IntVec3(0, 0, 1), PathEndMode.OnCell);
                    yield return FaceDir(0, 1);
                    yield return Toils_General.Wait(25);
                }

                // Reset
                foreach (var e in Reset())
                    yield return e;
            }
        }
    }
}
