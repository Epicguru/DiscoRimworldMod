using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Disco
{
    public class DiscoFloorGlowGrid
    {
        public static readonly Color DefaultColor = new Color(0, 0, 0, 0);

        public CellRect Rect { get; private set; }

        private Color[] colors;
        private Matrix4x4[] matrices;

        public DiscoFloorGlowGrid(IReadOnlyCollection<IntVec3> cells)
        {
            ConstructFrom(cells);
        }

        public void ConstructFrom(IReadOnlyCollection<IntVec3> cells)
        {
            if (cells == null || cells.Count == 0)
                throw new ArgumentNullException(nameof(cells));

            int minX = int.MaxValue, maxX = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;

            foreach (var cell in cells)
            {
                if (cell.x < minX)
                    minX = cell.x;
                if (cell.z < minZ)
                    minZ = cell.z;
                if (cell.x > maxX)
                    maxX = cell.x;
                if (cell.z > maxZ)
                    maxZ = cell.z;
            }

            int w = maxX - minX + 1;
            int h = maxZ - minZ + 1;
            Rect = new CellRect(minX, minZ, w, h);
            colors = new Color[Rect.Area];

            float y = AltitudeLayer.DoorMoveable.AltitudeFor();
            matrices = new Matrix4x4[Rect.Area];
            foreach (var cell in Rect)
            {
                matrices[GetCellLocalIndex(cell)] = Matrix4x4.TRS(cell.ToVector3ShiftedWithAltitude(y), Quaternion.identity, Vector3.one);
            }
        }

        public void ClearAll()
        {
            SetAllColors(DefaultColor);
        }

        public void SetAllColors(Color color)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
        }

        public void SetAllColors(Func<IntVec3, Color> cellToColor, Building_DJStand.BlendMode mode)
        {
            if (cellToColor == null)
                return;

            foreach (var cell in Rect)
            {
                Color color = cellToColor(cell);
                int index = GetCellLocalIndex(cell);
                Color current = colors[index];
                Color final = DefaultColor;

                switch (mode)
                {
                    case Building_DJStand.BlendMode.Override:
                        final = color;
                        break;
                    case Building_DJStand.BlendMode.Add:
                        final = current + color * color.a;
                        break;
                    case Building_DJStand.BlendMode.Multiply:
                        final = current * color;
                        break;
                    case Building_DJStand.BlendMode.Normal:
                        //float ao = color.a + current.a * (1f - color.a);
                        //final = (color * color.a + current * current.a * (1f - color.a)) / ao;

                        //final = color.a * color + current.a * current - color.a * current.a * current;

                        final = Color.Lerp(current, color, color.a);

                        break;
                    default:
                        Core.Error($"Unhandled blend mode: {mode}");
                        break;
                }

                colors[index] = final;
            }
        }

        public Color GetColor(IntVec3 cell)
        {
            if (colors == null)
                return DefaultColor;

            int index = GetCellLocalIndex(cell);
            if (index == -1)
                return DefaultColor;

            return colors[index];
        }

        public void GetColorAndMatrix(IntVec3 cell, out Color color, out Matrix4x4 matrix)
        {
            color = default;
            matrix = default;
            if (colors == null)
                return;

            int index = GetCellLocalIndex(cell);
            if (index == -1)
                return;

            color = colors[index];
            matrix = matrices[index];
        }

        public void SetColor(IntVec3 cell, Color color)
        {
            if (colors == null)
                return;

            int index = GetCellLocalIndex(cell);
            if (index == -1)
                return;

            colors[index] = color;
        }

        private int GetCellLocalIndex(IntVec3 cell)
        {
            if (!Rect.Contains(cell))
                return -1;

            cell.x -= Rect.minX;
            cell.z -= Rect.minZ;
            return cell.x + cell.z * Rect.Width;
        }
    }
}
