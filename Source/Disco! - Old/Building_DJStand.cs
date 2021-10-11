using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Disco.Programs;
using RimWorld;
using UnityEngine;
using Verse;

namespace Disco
{
    public class Building_DJStand : Building
    {
        [TweakValue("_Disco!")]
        private static bool DebuggingMode = false;

        private const int NO_POWER_WARNING = 60 * 4;
        private const int NO_POWER_SHUTDOWN = 60 * 24;

        private static readonly Queue<IntVec3> openNodes = new Queue<IntVec3>(64);
        private static readonly HashSet<IntVec3> closedNodes = new HashSet<IntVec3>(64);
        private static readonly List<IntVec3> tempDisplayCells = new List<IntVec3>(128);

        public static void FloodFillDiscoFloorCells(Map map, IntVec3 startCell, int maxSize, ref HashSet<IntVec3> cells)
        {
            if (map == null)
                return;

            cells ??= new HashSet<IntVec3>(64);
            cells.Clear();
            openNodes.Clear();
            closedNodes.Clear();

            var floorDef = DiscoDefOf.DSC_DiscoFloor;
            var terrainGrid = map.terrainGrid;

            bool IsDiscoFloorAndPassable(IntVec3 cell)
            {
                if (!cell.InBounds(map))
                    return false;

                if (terrainGrid.TerrainAt(cell) != floorDef)
                    return false;

                return cell.Walkable(map);
            }

            if (IsDiscoFloorAndPassable(startCell))
                openNodes.Enqueue(startCell);

            while (openNodes.Count > 0)
            {
                IntVec3 current = openNodes.Dequeue();
                cells.Add(current);
                closedNodes.Add(current);
                if (cells.Count >= maxSize)
                    break;

                // Try add neighbors.
                for (int x = current.x - 1; x <= current.x + 1; x++)
                {
                    IntVec3 cell = new IntVec3(x, current.y, current.z);
                    if (closedNodes.Contains(cell) || openNodes.Contains(cell))
                        continue;

                    if (IsDiscoFloorAndPassable(cell))
                        openNodes.Enqueue(cell);
                }
                for (int z = current.z - 1; z <= current.z + 1; z++)
                {
                    IntVec3 cell = new IntVec3(current.x, current.y, z);
                    if (closedNodes.Contains(cell) || openNodes.Contains(cell))
                        continue;

                    if (IsDiscoFloorAndPassable(cell))
                        openNodes.Enqueue(cell);
                }
            }

            openNodes.Clear();
            closedNodes.Clear();
        }

        public CompPowerTrader Power => _power ??= GetComp<CompPowerTrader>();
        private CompPowerTrader _power;

        public SequenceHandler CurrentSequence
        {
            get
            {
                return _sequence;
            }
            set
            {
                if (_sequence == value)
                    return;

                SetProgramStack(null);
                _sequence = value;
            }
        }
        public List<(DiscoProgram program, BlendMode mode)> ActivePrograms = new List<(DiscoProgram, BlendMode)>();
        public CellRect FloorBounds => glowGrid?.Rect ?? default;
        public IReadOnlyCollection<IntVec3> DancingCells => floorCells;
        public bool PickSequenceIfNull;
        public float CurrentSongAmplitude;
        public bool NoPowerShutdown = false;

        private int ticksSinceForceStarted = Mathf.RoundToInt(Settings.ManualTriggerCooldown * 60000) + 100;
        private readonly List<FloatMenuOption> options = new List<FloatMenuOption>();
        private readonly List<FloatMenuOption> options2 = new List<FloatMenuOption>();
        private readonly List<FloatMenuOption> options3 = new List<FloatMenuOption>();
        private readonly List<FloatMenuOption> options4 = new List<FloatMenuOption>();
        private HashSet<IntVec3> floorCells = new HashSet<IntVec3>(Settings.DiscoMaxFloorSize);
        private int tickCounter;
        private DiscoFloorGlowGrid glowGrid;
        private MaterialPropertyBlock block;
        private ProgramDef tempGizmoProgram;
        private float[] edgeDistances;
        private float highestEdgeDistance;
        private bool runningTask;
        private SequenceHandler _sequence;
        private int scanIndex = -120;
        private int ticksWithoutPower = 0;

        public override void PostMapInit()
        {
            base.PostMapInit();
            Register();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Register(map);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Remove();
            base.Destroy(mode);
            SetProgramStack(null);
        }

        public override void Notify_MyMapRemoved()
        {
            Remove();
            base.Notify_MyMapRemoved();
            SetProgramStack(null);
        }

        public override void Tick()
        {
            base.Tick();

            Power.PowerOutput = -GetTargetPowerDraw();
            if (Power.PowerOn)
                ticksWithoutPower = 0;
            else
                ticksWithoutPower++;

            ticksSinceForceStarted++;

            if (!DebuggingMode && PickSequenceIfNull)
            {
                if (ticksWithoutPower == NO_POWER_WARNING)
                {
                    Messages.Message("DSC.NoPowerWarning".Translate(), MessageTypeDefOf.CautionInput);
                }

                if (ticksWithoutPower >= NO_POWER_SHUTDOWN)
                {
                    Messages.Message("DSC.NoPowerShutdown".Translate(), MessageTypeDefOf.CautionInput);
                    ticksWithoutPower = -100;
                    NoPowerShutdown = true;
                }
            }

            if (!PickSequenceIfNull)
                NoPowerShutdown = false;

            if (floorCells.Count >= 2 && glowGrid != null)
            {
                if (CurrentSequence != null && !DebuggingMode)
                {
                    var dj = InteractionCell.GetFirstPawn(Map);
                    if (dj == null)
                    {
                        Core.Warn("Cancelling current sequence because there is no DJ pawn. Enable Debugging Mode to disable this behaviour.");
                        CurrentSequence = null;
                        PickSequenceIfNull = false;
                    }
                    if (!PickSequenceIfNull)
                    {
                        CurrentSequence = null;
                    }
                }

                if (CurrentSequence == null && PickSequenceIfNull)
                {
                    int w = FloorBounds.Width;
                    int h = FloorBounds.Height;
                    CurrentSequence = DefDatabase<SequenceDef>.AllDefsListForReading.RandomElementByWeight(def => def.CanRunOn(w, h) ? def.Weight : 0).CreateAndInitHandler(this);
                    Core.Log($"Picked new random sequence: {CurrentSequence.Def.defName} for a {w}x{h} dance floor.");
                }
                try
                {
                    bool tickNow = true;
                    if (!Settings.GameSpeedAffectsMusic)
                    {
                        if (tickCounter % (int)Find.TickManager.TickRateMultiplier != 0)
                            tickNow = false;
                    }

                    if(tickNow)
                        TickFloor();
                }
                catch (Exception e)
                {
                    Core.Error("Exception ticking disco floor. That's wiggity wack, yo.", e);
                }
            }

            // Scan for expanded floor area.
            if (floorCells != null)
            {
                scanIndex++;
                if (scanIndex >= floorCells.Count)
                    scanIndex = 0;
                if (scanIndex >= 0)
                {
                    bool needsToRefresh = ScanForNewFloor(GetFloorCell(scanIndex));
                    if (needsToRefresh)
                    {
                        RecalculateFloor();
                        scanIndex = -120;
                    }
                }
            }

            tickCounter++;
            if (tickCounter % 120 != 0 || floorCells.Count >= 2)
                return;

            RecalculateFloor();
        }

        public virtual float GetTargetPowerDraw()
        {
            if (!PickSequenceIfNull)
            {
                return 50f;
            }

            return Mathf.Max(50f, (floorCells?.Count ?? 0) * Settings.WattsPerFloorTile);
        }

        public HashSet<IntVec3> GetFloorCells()
        {
            return floorCells;
        }

        private IntVec3 GetFloorCell(int index)
        {
            if (floorCells == null)
                return default;
            if (index < 0 || index >= floorCells.Count)
                return default;

            int i = 0;
            foreach (var item in floorCells)
            {
                if (index == i)
                    return item;
                i++;
            }
            return default;
        }

        private bool ScanForNewFloor(IntVec3 cell)
        {
            Map map = base.Map;
            var comp = map.GetComponent<DiscoTracker>();

            bool IsDiscoFloorAndPassable(IntVec3 pos)
            {
                if (!pos.InBounds(map))
                    return false;

                if (map.terrainGrid.TerrainAt(pos) != DiscoDefOf.DSC_DiscoFloor)
                    return false;

                return pos.Walkable(map);
            }

            foreach (var offset in GenAdj.AdjacentCellsAndInside)
            {
                IntVec3 pos = cell + offset;
                bool isFloor = IsDiscoFloorAndPassable(pos);

                if (!isFloor)
                    continue;

                var owner = comp.GetDJStandForCell(pos);
                if (owner == null)
                    return true;
            }
            return false;
        }

        public IntVec3 GetGatherSpot()
        {
            if (floorCells == null || floorCells.Count < 10)
                return Position;

            if(FloorBounds.CenterCell.Walkable(Map))
                return FloorBounds.CenterCell;
            return Position;
        }

        public void RecalculateFloor()
        {
            FloodFillDiscoFloorCells(Map, TryGetFloorStart(), Settings.DiscoMaxFloorSize, ref floorCells);
            glowGrid = floorCells.Count >= 2 ? new DiscoFloorGlowGrid(floorCells) : null;
            block ??= new MaterialPropertyBlock();

            if (floorCells.Count >= 2)
            {
                if (runningTask)
                {
                    Core.Error("Background thread is already calculating edge distances. Wait a bit before recalculating floor, please!");
                    return;
                }

                runningTask = true;
                Task.Run(() =>
                {
                    float[] temp = new EdgeDistanceCalculator().Run(floorCells);
                    edgeDistances = RemapEdgeDistances(temp, out highestEdgeDistance);
                    runningTask = false;
                });
            }
        }

        public float GetCellDistanceFromEdge(IntVec3 cell, bool inverted = false)
        {
            if (!FloorBounds.Contains(cell))
                return -1;

            if (edgeDistances == null)
                return -1;

            cell -= FloorBounds.BottomLeft;
            int index = cell.x + cell.z * FloorBounds.Width;
            if (index < 0 || index >= edgeDistances.Length)
                return -1;

            if (inverted)
                return highestEdgeDistance - edgeDistances[index];
            return edgeDistances[index];
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref PickSequenceIfNull, "rf_pickSeqIfNull");
            Scribe_Values.Look(ref NoPowerShutdown, "rf_noPowerShutdown");
            Scribe_Values.Look(ref ticksWithoutPower, "rf_noPowerTicks");
            Scribe_Values.Look(ref ticksSinceForceStarted, "rf_ticksSinceForceStarted", Mathf.RoundToInt(Settings.ManualTriggerCooldown * 60000) + 100);
        }

        private float[] RemapEdgeDistances(float[] rawDistances, out float highest)
        {
            var cells = floorCells;
            var bounds = glowGrid.Rect;

            if (cells.Count != rawDistances.Length)
            {
                Core.Error("cells.Count != rawDistances.Length");
                highest = -1;
                return null;
            }

            IntVec3 boundsMin = bounds.BottomLeft;

            float[] forBounds = new float[bounds.Area];
            for (int i = 0; i < forBounds.Length; i++)
                forBounds[i] = -1;

            highest = -1;
            int index = 0;
            foreach(var cell in floorCells)
            {
                float rawDst = rawDistances[index++];

                IntVec3 local = cell - boundsMin;
                int localIndex = local.x + local.z * bounds.Width;
                forBounds[localIndex] = rawDst;
                if (rawDst > highest)
                    highest = rawDst;
            }

            return forBounds;
        }

        internal void DebugOnGUI()
        {
            if (edgeDistances != null && floorCells != null)
            {
                foreach (var cell in floorCells)
                {
                    float dst = GetCellDistanceFromEdge(cell);
                    GenMapUI.DrawThingLabel((Vector3)GenMapUI.LabelDrawPosFor(cell), dst.ToString("F1"), Color.cyan);
                }
            }
            else
            {
                Core.Warn($"{edgeDistances?.Length ?? -1} vs {floorCells?.Count ?? -1}");
            }
        }

        private IntVec3 TryGetFloorStart()
        {
            var map = Map;
            bool IsValid(IntVec3 cell)
            {
                return cell.GetTerrain(map) == DiscoDefOf.DSC_DiscoFloor && cell.Walkable(map);
            }

            if (IsValid(Position))
                return Position;
            if (IsValid(Position + new IntVec3(-1, 0, 0)))
                return Position + new IntVec3(-1, 0, 0);
            if (IsValid(Position + new IntVec3(1, 0, 0)))
                return Position + new IntVec3(1, 0, 0);
            if (IsValid(Position + new IntVec3(0, 0, -1)))
                return Position + new IntVec3(0, 0, -1);
            if (IsValid(Position + new IntVec3(0, 0, 1)))
                return Position + new IntVec3(0, 0, 1);
            return Position;
        }

        public void TickFloor()
        {
            CurrentSequence?.Tick();
            if (CurrentSequence != null && CurrentSequence.IsDone)
            {
                CurrentSequence = null;
                SetProgramStack(null);
                CurrentSongAmplitude = 0;
            }

            if (CurrentSequence == null && ActivePrograms.Count > 0 && !DebuggingMode)
            {
                SetProgramStack(null);
                CurrentSongAmplitude = 0;
                Core.Warn("Cleared program stack since there is no active sequence. Enable Dev Mod to disable this functionality.");
            }

            if (ActivePrograms.Count == 0)
            {
                glowGrid.ClearAll();
                CurrentSongAmplitude = 0;
                return;
            }

            bool firstVolumeMod = true;
            bool first = true;
            for (int i = 0; i < ActivePrograms.Count; i++)
            {
                var pair = ActivePrograms[i];
                var layer = pair.program;
                var blendMode = pair.mode;

                if (layer.ShouldRemove)
                {
                    ActivePrograms.RemoveAt(i);
                    i--;
                    layer.Dispose();
                    continue;
                }

                try
                {
                    layer.Tick();
                    if (layer is IMusicVolumeReporter reporter)
                    {
                        float vol = reporter.GetMusicAmplitude();
                        if (vol > CurrentSongAmplitude || firstVolumeMod)
                        {
                            CurrentSongAmplitude = vol;
                            firstVolumeMod = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    Core.Error($"Exception ticking disco floor program '{layer.GetType().Name}'", e);
                }

                try
                {
                    if (blendMode != BlendMode.Ignore)
                    {
                        glowGrid.SetAllColors(cell =>
                        {
                            Color c = layer.ColorFor(cell);
                            if (layer.OneMinus)
                                c = Color.white - c;
                            if (layer.OneMinusAlpha)
                                c.a = 1f - c.a;
                            if (layer.Tint != null)
                                c *= layer.Tint.Value;
                            return c;
                        },
                        first ? BlendMode.Override : blendMode);
                    }
                }
                catch (Exception e)
                {
                    Core.Error($"Exception getting colors from disco floor program '{layer.GetType().Name}'", e);
                }

                layer.TickCounter++;
                if(blendMode != BlendMode.Ignore)
                    first = false;
            }

        }

        public void SetProgramStack(DiscoProgram program)
        {
            foreach (var item in ActivePrograms)
            {
                item.program.Dispose();
            }
            ActivePrograms.Clear();
            if (program != null)
            {
                ActivePrograms.Add((program, BlendMode.Override));
            }
        }

        public void AddProgramStack(DiscoProgram program, BlendMode mode, int? index = null)
        {
            if (program != null)
            {
                if (index != null)
                    ActivePrograms.Insert(index.Value, (program, mode));
                else
                    ActivePrograms.Add((program, mode));
            }
        }

        public override void Draw()
        {
            base.Draw();

            if (glowGrid != null && floorCells.Count >= 2)
                DrawFloor();
        }

        public void DrawFloor()
        {
            if (Content.DiscoFloorGlowGraphic == null)
                Content.LoadDiscoFloorGraphics(this);

            float aMulti = Settings.DiscoFloorColorIntensity;

            void Clamp(ref Color c)
            {
                c.r = Mathf.Clamp01(c.r);
                c.g = Mathf.Clamp01(c.g);
                c.b = Mathf.Clamp01(c.b);
                c.a = Mathf.Clamp01(c.a);
            }

            foreach (var cell in floorCells)
            {
                glowGrid.GetColorAndMatrix(cell, out var color, out var matrix);
                color.a *= aMulti;
                Clamp(ref color);

                //Map.glowGrid.glowGrid[Map.cellIndices.CellToIndex(cell)] = color;
                if (color.a != 0f)
                {
                    block.SetColor("_Color", color);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, Content.DiscoFloorGlowGraphic.MatNorth, 0, Find.Camera, 0, block);
                    //Map.glowGrid.MarkGlowGridDirty(cell);
                }
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            tempDisplayCells.Clear();
            tempDisplayCells.AddRange(floorCells);

            GenDraw.DrawFieldEdges(tempDisplayCells, Color.cyan);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            var worker = DiscoDefOf.DSC_DiscoGathering.Worker;
            int cooldownTicks = Mathf.RoundToInt(Settings.ManualTriggerCooldown * 60000f);
            bool onCooldown = ticksSinceForceStarted < cooldownTicks;
            bool canStartNow = worker.CanExecute(Map);
            bool noPower = !Power.PowerOn;
            yield return new Command_Action()
            {
                action = () =>
                {
                    bool worked = worker.TryExecute(Map);
                    if (!worked)
                    {
                        Messages.Message("DSC.FailedToStart".Translate(), MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        ticksSinceForceStarted = 0;
                    }
                },
                defaultLabel = "DSC.TriggerNowLabel".Translate(),
                defaultDesc = "DSC.TriggerNowDesc".Translate(),
                disabled = onCooldown || !canStartNow || noPower,
                disabledReason = noPower ? "DSC.TriggerNowDisabledPower".Translate() : onCooldown ? "DSC.TriggerNowDisabledCooldown".Translate() : "DSC.TriggerNowDisabledOther".Translate(),
                icon = Content.StartIcon,
                defaultIconColor = Color.yellow
            };

            if (PickSequenceIfNull)
            {
                yield return new Command_Action()
                {
                    action = () =>
                    {
                        CurrentSequence = null;
                        Core.Log("Shuffling...");
                    },
                    icon = Content.ShuffleIcon,
                    defaultIconColor = new Color(0.6f, 1f, 0.6f, 1f),
                    alsoClickIfOtherInGroupClicked = false,
                    defaultLabel = "DSC.ShuffleLabel".Translate(),
                    defaultDesc = "DSC.ShuffleDesc".Translate()
                };
            }

            if (!Prefs.DevMode)
                yield break;

            yield return new Command_Toggle()
            {
                defaultLabel = "Toggle debugging mode",
                defaultDesc = "In debugging mode the DJ stand will behave differently, making it easier to inspect programs and sequence.",
                toggleAction = () =>
                {
                    DebuggingMode = !DebuggingMode;
                },
                isActive = () => DebuggingMode
            };

            //yield return new Command_Action()
            //{
            //    defaultLabel = "Recalculate floor",
            //    action = RecalculateFloor
            //};

            options.Clear();
            options2.Clear();
            options3.Clear();
            options4.Clear();

            options.Add(new FloatMenuOption("None", () =>
            {
                SetProgramStack(null);
            }));
            foreach (var d in DefDatabase<ProgramDef>.AllDefsListForReading)
            {
                options.Add(new FloatMenuOption(d.defName.Replace("DSC_DP_", ""), () =>
                {
                    SetProgramStack(d.MakeProgram(this));
                }));
            }
        
        
            foreach (var mode in Enum.GetValues(typeof(BlendMode)))
            {
                var realMode = (BlendMode)mode;
                options3.Add(new FloatMenuOption(realMode.ToString(), () =>
                {
                    AddProgramStack(tempGizmoProgram.MakeProgram(this), realMode);
                    tempGizmoProgram = null;
                }));
            }
        
        
            foreach (var d in DefDatabase<ProgramDef>.AllDefsListForReading)
            {
                options2.Add(new FloatMenuOption(d.defName.Replace("DSC_DP_", ""), () =>
                {
                    tempGizmoProgram = d;
                    Find.WindowStack.Add(new FloatMenu(options3, "Select a blend mode"));
                }));
            }
        
        
            options4.Add(new FloatMenuOption("None", () =>
            {
                CurrentSequence = null;
            }));
            foreach (var d in DefDatabase<SequenceDef>.AllDefsListForReading)
            {
                options4.Add(new FloatMenuOption(d.defName.Replace("DSC_S_", ""), () =>
                {
                    CurrentSequence = d.CreateAndInitHandler(this);
                }));
            }
            

            yield return new Command_Action()
            {
                defaultLabel = "Set program",
                action = () =>
                {
                    Find.WindowStack.Add(new FloatMenu(options, "Select a program"));
                }
            };

            yield return new Command_Action()
            {
                defaultLabel = "Add program",
                action = () =>
                {
                    Find.WindowStack.Add(new FloatMenu(options2, "Select a program"));
                }
            };

            yield return new Command_Action()
            {
                defaultLabel = "Start sequence",
                action = () =>
                {
                    Find.WindowStack.Add(new FloatMenu(options4, "Select a sequence"));
                }
            };

            yield return new Command_Toggle()
            {
                defaultLabel = "Toggle auto pick",
                isActive = () => PickSequenceIfNull,
                toggleAction = () => { PickSequenceIfNull = !PickSequenceIfNull; }
            };
        }

        private StringBuilder str = new StringBuilder();
        public override string GetInspectString()
        {
            if (!Prefs.DevMode)
            {
                if (floorCells == null || floorCells.Count == 0)
                    return "DSC.NoFloor".Translate();

                if (floorCells.Count < 10)
                    return "DSC.FloorTooSmall".Translate();

                if (!Power.PowerOn)
                    return "DSC.NoPower".Translate((Settings.WattsPerFloorTile * (floorCells?.Count ?? 0)).ToString("F1"));

                return $"{floorCells.Count} disco floor tiles. Let's groove!";
            }

            str.Clear();

            str.Append("Power: ").Append("requesting ").Append(Power.PowerOutput).Append("W, has power: ").AppendLine(Power.PowerOn ? "Yes" : "No");
            str.Append("Sequence: ").AppendLine(CurrentSequence?.Def?.defName ?? "<null>");
            str.Append("Grid bounds: ").AppendLine(FloorBounds.ToString());
            str.Append("Current volume: ").AppendLine(CurrentSongAmplitude.ToString());

            foreach (var thing in ActivePrograms)
            {
                str.Append(thing.program.GetType().Name).Append(" - blend: ").Append(thing.mode).AppendLine();
            }

            return str.ToString().Trim();
        }

        public bool IsReadyForDiscoSimple()
        {
            return floorCells != null && floorCells.Count >= 10 && Power.PowerOn;
        }

        private void Register(Map overrideMap = null)
        {
            var map = overrideMap ?? Map;
            var tracker = map?.GetComponent<DiscoTracker>();
            if (tracker == null)
            {
                Core.Error("Failed to register, null map or tracker component.");
                return;
            }

            tracker.Register(this);
        }

        private void Remove()
        {
            var map = Map;
            var tracker = map?.GetComponent<DiscoTracker>();
            if (tracker == null)
            {
                Core.Error("Failed to un-register, null map or tracker component.");
                return;
            }

            tracker.UnRegister(this);
        }

        public enum BlendMode
        {
            Override,
            Add,
            Multiply,
            Normal,
            Ignore
        }
    }
}
