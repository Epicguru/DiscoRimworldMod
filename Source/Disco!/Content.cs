﻿using UnityEngine;
using Verse;

namespace Disco
{
    [StaticConstructorOnStartup]
    public static class Content
    {
        public static Graphic DiscoFloorGlowGraphic;

        internal static void LoadDiscoFloorGraphics(Building b)
        {
            var gd = b.DefaultGraphic.data;
            Graphic MakeUnlit(string path, Vector2 size)
            {
                return GraphicDatabase.Get(gd.graphicClass, path, DiscoDefOf.Transparent.Shader, size, Color.white, Color.white, gd, gd.shaderParameters);
            }
            DiscoFloorGlowGraphic = MakeUnlit("DSC/Effects/FloorGlow", Vector2.one);
        }
    }
}
