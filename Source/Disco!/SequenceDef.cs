using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Disco
{
    public class SequenceDef : Def
    {
        public float weight = 1f;
        public IntVec2? minFloorSize = null;

        public List<DiscoSequenceAction> actions = new List<DiscoSequenceAction>();
        public Type handlerType = typeof(SequenceHandler);

        public bool CanRunOn(int floorWidth, int floorHeight)
        {
            if (minFloorSize == null)
                return true;

            return floorWidth >= minFloorSize.Value.x && floorHeight >= minFloorSize.Value.z;
        }

        public SequenceHandler CreateAndInitHandler(Building_DJStand stand)
        {
            if (handlerType == null)
                return null;

            var instance = Activator.CreateInstance(handlerType, this, stand) as SequenceHandler;
            if (instance == null)
                return null;

            instance.Init();
            return instance;
        }
    }

    public enum DiscoSequenceActionType
    {
        None,
        Wait,
        WaitMem,
        Start,
        Add,
        Repeat,
        Clear,
        WaitLast,
        PickRandom,
        MemAdd,
        MemRemove,
        TintMem,
        DestroyMem
    }

    [Serializable]
    public class DiscoSequenceAction
    {
        private static readonly Color[] randomColors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.yellow
        };

        public Color? Tint => randomTint ? randomColors.RandomElement() : tint;

        public DiscoSequenceActionType type = DiscoSequenceActionType.None;
        public Building_DJStand.BlendMode blend = Building_DJStand.BlendMode.Multiply;
        public int ticks = 30;
        public DiscoDict overrides = null;
        public int times = 0;
        public List<DiscoSequenceAction> actions;
        public bool oneMinus = false;
        public bool oneMinusAlpha = false;
        public bool atBottom = false;
        public bool addToMemory = false;
        public bool usePreferred = true;
        public bool onlyIfMeetsSize = false;
        public float chance = 1f;
        public float weight = 1f;

        private ProgramDef program;
        private List<ProgramDef> randomProgramFrom;
        private Color? tint = null;
        private bool randomTint = false;

        public ProgramDef GetProgram(int floorWidth, int floorHeight)
        {
            if ((randomProgramFrom?.Count ?? 0) > 0)
            {
                return randomProgramFrom.RandomElement();
            }
            if (program == null)
                return null;

            if (!usePreferred || program.prefer == null)
                return program;

            foreach (var preferred in program.prefer)
            {
                if (preferred.CanRunOn(floorWidth, floorHeight))
                    return preferred;
            }
            return program;
        }
    }
}
