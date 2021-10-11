using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Disco
{
    public class EdgeDistanceCalculator
    {
        private static object key = new object();

        public float[] Run(IReadOnlyCollection<IntVec3> cellsRaw)
        {
            float[] distances = new float[cellsRaw.Count];

            HashSet<IntVec3> exteriorCells = new HashSet<IntVec3>(128);
            HashSet<IntVec3> cells = new HashSet<IntVec3>(cellsRaw.Count);
            cells.AddRange(cellsRaw);

            // Step 1:
            // Make a list of all edge cells.
            lock (key)
            {
                foreach (var cell in cells)
                {
                    var adjList = GenAdjFast.AdjacentCells8Way(cell);
                    foreach (var adj in adjList.Where(adj => !cells.Contains(adj)))
                        exteriorCells.Add(adj);
                }
            }

            // Step 2: For each floor cell, find closest exterior cell and save distance.
            int index = 0;
            foreach (var cell in cells)
            {
                Vector2 pos = new Vector2(cell.x + 0.5f, cell.z + 0.5f);
                float closest = float.MaxValue;

                foreach (var other in exteriorCells)
                {
                    Vector2 otherPos = new Vector2(other.x + 0.5f, other.z + 0.5f);
                    float sqrDst = (otherPos - pos).sqrMagnitude;
                    if (sqrDst < closest)
                        closest = sqrDst;
                }

                float dst = Mathf.Sqrt(closest);
                distances[index++] = dst;
            }

            return distances;
        }
    }
}