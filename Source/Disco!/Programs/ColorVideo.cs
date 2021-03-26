using System.IO;
using Disco.Audio;
using RimWorld;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Video;
using Verse;
using Object = UnityEngine.Object;

namespace Disco.Programs
{
    public class ColorVideo : DiscoProgram, IMusicVolumeReporter
    {
        private static GameObject _tempGo;
        private static GameObject GetParentObject()
        {
            if (_tempGo == null)
            {
                _tempGo = new GameObject("Disco! video player");
            }
            return _tempGo;
        }

        [DebugAction("Disco!")]
        private static void DebugVideoPlayers()
        {
            if (_tempGo == null)
            {
                Core.Log("No video players (no game object)");
                return;
            }

            int count = _tempGo.GetComponents<VideoPlayer>().Length;
            Core.Log($"{count} video players");
        }

        private VideoPlayer player;
        private ManagedAudioSource audioContainer;
        private Texture2D tempTex;
        private Color[] colorGrid;
        private CellRect vidBounds;
        private long currentFrameIndex = -1;
        private long lastRenderedFrame = -1;
        private bool isSubscribed = false;
        private bool removed = false;
        private float currentAmp;
        private static float[] samples = new float[4096];

        public ColorVideo(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            string path = Def.Get<string>("filePath");
            float volume = Def.Get("volume", 1f);
            bool mute = Def.Get("mute", false);

            if (volume <= 0f)
                mute = true;

            // Resolve the file path.
            ModContentPack mcp = Def.modContentPack;
            if (mcp == null)
            {
                string[] split = path.Split('/');
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
            path = Path.Combine(mcp.RootDir, path);

            player = GetParentObject().AddComponent<VideoPlayer>();
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = mute ? VideoAudioOutputMode.None : VideoAudioOutputMode.AudioSource;

            if (!mute)
            {
                audioContainer = AudioSourceManager.CreateSource(null, DJStand.Map);
                audioContainer.TargetVolume = volume;
                audioContainer.Area = DJStand.FloorBounds;
                audioContainer.IsPlaying = () => player != null && !removed;
                player.SetTargetAudioSource(0, audioContainer.Source);

                string msg = "DSC.NowPlaying".Translate(Def.Get("credits", "Unknown"));
                Messages.Message(msg, MessageTypeDefOf.PositiveEvent);
            }

            player.isLooping = false;
            player.sendFrameReadyEvents = true;
            player.playbackSpeed = Find.TickManager.TickRateMultiplier;
            player.loopPointReached += OnVideoReachEnd;
            player.frameReady += Player_frameReady;
            player.errorReceived += Player_errorReceived;

            OnPauseChange(Find.TickManager.Paused);
            player.url = path;

            if(!isSubscribed)
                UnityHook.OnPauseChange += OnPauseChange;
        }

        private float GetSamplesAverage(int channel)
        {
            audioContainer.Source.GetOutputData(samples, channel);
            double sum = 0;
            foreach (var sample in samples)
            {
                sum += sample * sample;
            }
            return Mathf.Sqrt((float)(sum / samples.Length));
        }

        private void Player_errorReceived(VideoPlayer source, string message)
        {
            Core.Error($"Color video player error: {message}");
        }

        private void Player_frameReady(VideoPlayer source, long frameIdx)
        {
            currentFrameIndex = frameIdx;
        }

        protected virtual void OnVideoReachEnd(VideoPlayer _)
        {
            Object.Destroy(player);
            player = null;
        }

        public override void Tick()
        {
            base.Tick();

            if (player == null)
            {
                tempTex = null;
                Remove();
                return;
            }
            player.playbackSpeed = Find.TickManager.TickRateMultiplier;
            if (!player.isPrepared)
                return;

            if (lastRenderedFrame == currentFrameIndex)
                return;

            var tex = player.texture as RenderTexture;
            if (tex == null)
                return;

            int every = (int) Find.TickManager.TickRateMultiplier;
            if (TickCounter % every != 0)
                return;

            if (audioContainer != null && audioContainer.Source != null)
            {
                currentAmp = 0;
                currentAmp += GetSamplesAverage(0);
                currentAmp += GetSamplesAverage(1);
                currentAmp /= audioContainer.Source.volume;
            }

            tempTex ??= new Texture2D(tex.width, tex.height, tex.graphicsFormat, TextureCreationFlags.None);
            //Core.Log($"{player.audioTrackCount}, {player.controlledAudioTrackCount}"); //, {player.GetTargetAudioSource(0)?.spatialBlend}, {player.GetTargetAudioSource(0).transform.position}");

            int gw = DJStand.FloorBounds.Width;
            int gh = DJStand.FloorBounds.Height;
            int offX = Mathf.RoundToInt((gw - tex.width) / 2f);
            int offZ = Mathf.RoundToInt((gh - tex.height) / 2f);
            int minX = DJStand.FloorBounds.minX + offX;
            int minZ = DJStand.FloorBounds.minZ + offZ;
            vidBounds = new CellRect(minX, minZ, tex.width, tex.height);

            var old = RenderTexture.active;
            RenderTexture.active = tex;
            tempTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tempTex.Apply();
            RenderTexture.active = old;
            colorGrid = tempTex.GetPixels(0, 0, tex.width, tex.height, 0);
        }

        protected virtual int CellToVidIndex(IntVec3 cell)
        {
            if (!vidBounds.Contains(cell))
                return -1;

            int localX = cell.x - vidBounds.minX;
            int localZ = cell.z - vidBounds.minZ;

            return localX + localZ * (int)player.width;
        }

        public override Color ColorFor(IntVec3 cell)
        {
            if (player == null || colorGrid == null)
                return default;

            int index = CellToVidIndex(cell);
            if (index == -1)
                return default;

            return colorGrid[index];
        }

        protected virtual void OnPauseChange(bool paused)
        {
            if (player != null)
                player.playbackSpeed = paused ? 0 : Find.TickManager.TickRateMultiplier;
        }

        public override void Dispose()
        {
            base.Dispose();

            removed = true;

            if (tempTex != null)
                Object.Destroy(tempTex);

            if (player != null)
            {
                Object.Destroy(player);
                player = null;
            }

            if(isSubscribed)
                UnityHook.OnPauseChange -= OnPauseChange;
        }

        public float GetMusicAmplitude()
        {
            return currentAmp;
        }
    }
}
