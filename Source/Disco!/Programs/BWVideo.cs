using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using VideoTool;

namespace Disco.Programs
{
    public class BWVideo : DiscoProgram
    {
        public Color WhiteColor, BlackColor;

        public string FilePath { get; private set; }
        public int VideoWidth => video?.Width ?? -1;
        public int VideoHeight => video?.Height ?? -1;
        public int VideoFrameRate => video?.FrameRate ?? -1;

        private VideoLoader video;
        private int frameSwapInterval;
        private int tickCounterCustom;
        private CellRect vidBounds;
        private int toRepeat;

        public BWVideo(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            WhiteColor = Def.Get("whiteColor", Color.white);
            BlackColor = Def.Get("blackColor", new Color(0, 0, 0, 0));
            toRepeat = Def.Get("times", 1) - 1;
            FilePath = Def.Get<string>("filePath");

            // Resolve the file path.
            ModContentPack mcp = Def.modContentPack;
            if (mcp == null)
            {
                string[] split = FilePath.Split('/');
                split[0] = split[0].ToLowerInvariant();
                Core.Warn($"Video program def '{Def.defName}' has been added by a patch operation. Attempting to resolve filepath...");
                var found = LoadedModManager.RunningModsListForReading.FirstOrFallback(mod => mod.PackageId.ToLowerInvariant() == split[0]);
                if (found == null)
                {
                    Core.Error($"Failed to resolve mod folder path from id '{split[0]}'. See below for how to solve this issue.");
                    Core.Error("If you mod's package ID is 'my.mod.name' and your video file is in 'MyModFolder/Videos/Video.bwcv' then the correct filePath would be 'my.mod.name/Videos/Video.bwcv'");
                    Remove();
                    return;
                }
                Core.Warn("Successfully resolved file path.");
                mcp = found;
            }
            FilePath = Path.Combine(mcp.RootDir, FilePath);
            if (string.IsNullOrEmpty(new FileInfo(FilePath).Extension))
                FilePath += ".bwcv";

            // Load video on another thread.
            Task.Run(() =>
            {
                try
                {
                    VideoLoader vid = new VideoLoader();
                    vid.Load(FilePath);
                    Core.Log($"Loaded {vid.BytesLoaded} bytes of video data from {FilePath}");
                    if (!vid.LoadNextFrame())
                        throw new Exception("Failed to load first frame");
                    this.video = vid;
                }
                catch(Exception e)
                {
                    Core.Error($"Exception loading video program from '{FilePath}'", e);
                    this.Remove();
                }
            });
        }

        public override void Tick()
        {
            base.Tick();

            if (video == null)
                return;

            if (frameSwapInterval <= 0)
            {
                frameSwapInterval = 60 / VideoFrameRate;

                int gw = DJStand.FloorBounds.Width;
                int gh = DJStand.FloorBounds.Height;
                int offX = Mathf.RoundToInt((gw - VideoWidth) / 2f);
                int offZ = Mathf.RoundToInt((gh - VideoHeight) / 2f);
                int minX = DJStand.FloorBounds.minX + offX;
                int minZ = DJStand.FloorBounds.minZ + offZ;
                vidBounds = new CellRect(minX, minZ, VideoWidth, VideoHeight);
            }

            tickCounterCustom++;
            if (tickCounterCustom % frameSwapInterval == 0)
            {
                bool hasNextFrame = video.LoadNextFrame();
                if (!hasNextFrame)
                {
                    if (toRepeat > 0)
                    {
                        video.Restart();
                        tickCounterCustom = 1;
                        toRepeat--;
                    }
                    else
                    {
                        Remove();
                        video.Dispose();
                        video = null;
                    }
                }
            }
        }

        protected virtual int CellToVidIndex(IntVec3 cell)
        {
            if (!vidBounds.Contains(cell))
                return -1;

            int localX = cell.x - vidBounds.minX;
            int localZ = cell.z - vidBounds.minZ;

            return localX + localZ * VideoWidth;
        }

        public override Color ColorFor(IntVec3 cell)
        {
            if (video == null)
                return default;

            int index = CellToVidIndex(cell);
            if (index == -1)
                return default;

            return video.CurrentFrame[index] ? WhiteColor : BlackColor;
        }
    }
}
