using System;
using System.Collections.Generic;
using Verse;

namespace Disco
{
    public class DiscoTracker : MapComponent
    {
        private List<Building_DJStand> allStands = new List<Building_DJStand>();

        public DiscoTracker(Map map) : base(map)
        {
        }

        public IEnumerable<Building_DJStand> GetAllValidDJStands()
        {
            if (allStands == null)
                yield break;
            foreach (var item in allStands)
            {
                if (item.DestroyedOrNull())
                    continue;

                if (item.IsReadyForDiscoSimple())
                    yield return item;
            }
        }

        public bool AnyActiveStand()
        {
            if (allStands == null)
                return false;

            foreach (var item in allStands)
            {
                if (item.DestroyedOrNull())
                    continue;

                if (item.PickSequenceIfNull)
                    return true;
            }
            return false;
        }

        public void Register(Building_DJStand stand)
        {
            if (!stand.DestroyedOrNull() && !allStands.Contains(stand))
                allStands.Add(stand);
        }

        public void UnRegister(Building_DJStand stand)
        {
            if (stand != null && allStands.Contains(stand))
                allStands.Remove(stand);
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            Core.Log("Map removed");
        }

        public Building_DJStand GetDJStandForCell(IntVec3 pos)
        {
            foreach (var stand in allStands)
            {
                if (stand.DestroyedOrNull())
                    continue;

                var hashSet = stand.GetFloorCells();
                if (hashSet == null)
                    continue;

                if (hashSet.Contains(pos))
                    return stand;
            }
            return null;
        }
    }
}
