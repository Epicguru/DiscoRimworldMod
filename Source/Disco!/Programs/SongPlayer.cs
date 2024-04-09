using Disco.Audio;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
#if V15
using LudeonTK;
#endif

namespace Disco.Programs
{
    public class SongPlayer : DiscoProgram, IMusicVolumeReporter
    {
        private static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        private static float[] samples = new float[4096];

        [DebugAction("Disco!", actionType = DebugActionType.Action)]
        private static void LogLoadedClips()
        {
            Core.Log($"There are {clipCache.Count} loaded audio clips:");
            foreach (var pair in clipCache)
            {
                Core.Log($"{new FileInfo(pair.Key).Name}: {pair.Value.length} seconds of {pair.Value.frequency / 1000}Hz, {pair.Value.channels} channels.");
            }
        }

        private ManagedAudioSource source;
        private bool removed = false;
        private float volume;
        private AudioClip clipToAdd;
        private string filePath;
        private bool playingLastFrame = true;
        private bool registered = false;
        private float currentAmp;

        public SongPlayer(ProgramDef def) : base(def)
        {
        }

        public override void Init()
        {
            filePath = Def.Get<string>("filePath");
            volume = Def.Get("volume", 1f);
            string format = Def.Get("format", "OGGVORBIS");
            if (!Enum.TryParse<AudioType>(format.Trim(), true, out var formatEnum))
            {
                Core.Error($"Failed to parse audio format '{format}'.");
                Array arr = Enum.GetValues(typeof(AudioType));
                object[] args = arr.Cast<object>().ToArray();
                Core.Error($"Valid values are: {string.Join(", ", args)}");
                Core.Error("Note: .mp3 is NOT SUPPORTED. I recommend using .ogg (OGGVORBIS) or .wav (WAV)");
                Remove();
                return;
            }

            // Resolve the file path.
            ModContentPack mcp = Def.modContentPack;
            if (mcp == null)
            {
                string[] split = filePath.Split('/');
                split[0] = split[0].ToLowerInvariant();
                Core.Warn($"Song player program def '{Def.defName}' has been added by a patch operation. Attempting to resolve filepath...");
                var found = LoadedModManager.RunningModsListForReading.FirstOrFallback(mod => mod.PackageId.ToLowerInvariant() == split[0]);
                if (found == null)
                {
                    Core.Error($"Failed to resolve mod folder path from id '{split[0]}'. See below for how to solve this issue.");
                    Core.Error("If you mod's package ID is 'my.mod.name' and your song file is in 'MyModFolder/Songs/Song.mp3' then the correct filePath would be 'my.mod.name/Songs/Song.mp3'");
                    Remove();
                    return;
                }
                Core.Warn("Successfully resolved file path.");
                mcp = found;
            }
            filePath = Path.Combine(mcp.RootDir, filePath);

            if (clipCache.TryGetValue(filePath, out var clip))
            {
                FlagLoadedForClip(clip);
            }
            else
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Core.Log($"Loading '{filePath}' as {formatEnum} ...");
                        var c = await AudioLoader.TryLoadAsync(filePath, formatEnum);
                        clipToAdd = c; // Push to main thread, see Tick()
                        Core.Log("Done");
                    }
                    catch (Exception e)
                    {
                        Core.Error($"Failed loading song clip from '{filePath}' in format {formatEnum}", e);
                        Remove();
                    }
                });
            }

            string msg = "DSC.NowPlaying".Translate(Def.Get("credits", "Unknown"));
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent);
        }

        private void FlagLoadedForClip(AudioClip clip)
        {
            source = AudioSourceManager.CreateSource(clip, DJStand.Map);
            source.TargetVolume = volume;
            source.Area = DJStand.FloorBounds;
            source.Source.pitch = Settings.GameSpeedAffectsMusic ? Find.TickManager.TickRateMultiplier : Find.TickManager.Paused ? 0f : 1f;
            source.Source.Play();
            source.IsPlaying = () => source?.Source != null && !removed;

            if (!registered)
            {
                UnityHook.OnPauseChange += UnityHook_OnPauseChange;
                registered = true;
            }
        }

        private void UnityHook_OnPauseChange(bool paused)
        {
            bool active = source?.Source != null && !removed;
            if (!active)
            {
                if (registered)
                {
                    UnityHook.OnPauseChange -= UnityHook_OnPauseChange;
                    registered = false;
                }
                return;
            }

            source.Source.pitch = paused ? 0 : Settings.GameSpeedAffectsMusic ? Find.TickManager.TickRateMultiplier : 1f;
        }

        private float GetSamplesAverage(int channel)
        {
            source.Source.GetOutputData(samples, channel);
            double sum = 0;
            foreach (var sample in samples)
            {
                sum += sample * sample;
            }
            return Mathf.Sqrt((float)(sum / samples.Length));
        }

        public override void Tick()
        {
            base.Tick();

            if (clipToAdd != null)
            {
                clipCache.Add(filePath, clipToAdd);
                FlagLoadedForClip(clipToAdd);
                clipToAdd = null;
            }

            if (source != null && source.Source != null)
            {
                source.Source.pitch = Settings.GameSpeedAffectsMusic ? Find.TickManager.TickRateMultiplier : Find.TickManager.Paused ? 0f : 1f;
                bool playing = source.Source.isPlaying;
                if (!playing && !playingLastFrame)
                    Remove();
                playingLastFrame = playing;

                if (TickCounter % (int) Find.TickManager.TickRateMultiplier == 0)
                {
                    currentAmp = 0;
                    currentAmp += GetSamplesAverage(0);
                    currentAmp += GetSamplesAverage(1);
                    currentAmp /= source.Source.volume;
                    currentAmp /= Prefs.VolumeGame;
                }
            }
        }

        public override Color ColorFor(IntVec3 cell)
        {
            return default;
        }

        public override void Dispose()
        {
            base.Dispose();

            removed = true;
            if (registered)
            {
                UnityHook.OnPauseChange -= UnityHook_OnPauseChange;
                registered = false;
            }
        }

        public float GetMusicAmplitude()
        {
            return currentAmp;
        }
    }
}
