using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Disco.Audio
{
    public static class AudioSourceManager
    {


        private static int lowPassLow = 285;
        private static int lowPassHigh = 22000;
        private static float volumeFar = 0.3f;

        [TweakValue("_Disco!", 0f, 100f)]
        private static float cameraClosest = 15;
        [TweakValue("_Disco!", 0f, 100f)]
        private static float cameraFarthest = 45;

        internal static List<ManagedAudioSource> activeSources = new List<ManagedAudioSource>(32);
        internal static float cameraRootSize;

        private static int id;
        private static FieldInfo rootSizeField = typeof(CameraDriver).GetField("rootSize", BindingFlags.NonPublic | BindingFlags.Instance);

        [DebugAction("Disco!", actionType = DebugActionType.Action)]
        private static void ShowAudioDebugWindow()
        {
            Find.WindowStack.Add(new ASMDebugWindow());
        }

        private static Transform GetParent()
        {
            return Find.Camera.transform;
        }

        public static ManagedAudioSource CreateSource(AudioClip clip, Map map)
        {
            GameObject go = new GameObject($"Disco! Audio Source #{id++}");
            go.transform.parent = GetParent();

            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.playOnAwake = false;
            src.volume = 1;
            src.pitch = 1;
            src.dopplerLevel = 0;
            src.panStereo = 0;
            src.spatialBlend = 0;
            src.bypassListenerEffects = true;
            src.bypassReverbZones = true;

            AudioLowPassFilter lowPass = null;
            AudioReverbFilter reverb = null;

            if (Settings.DoLowPass)
            {
                lowPass = go.AddComponent<AudioLowPassFilter>();
                lowPass.cutoffFrequency = lowPassHigh;
            }
            if (Settings.DoReverb)
            {
                reverb = go.AddComponent<AudioReverbFilter>();
                reverb.reverbPreset = AudioReverbPreset.Quarry;
            }

            var managed = new ManagedAudioSource()
            {
                Source = src,
                LowPass = lowPass,
                Reverb = reverb,
                MapId = map?.uniqueID ?? -1
            };

            activeSources.Add(managed);
            return managed;
        }

        public static void Tick(bool removeAll)
        {
            if (removeAll)
            {
                activeSources.ForEach(src =>
                {
                    if(src.Source != null)
                        Object.Destroy(src.Source.gameObject);
                });
                activeSources.Clear();
                return;
            }

            if (Find.CameraDriver != null)
                cameraRootSize = (float)rootSizeField.GetValue(Find.CameraDriver);
            
            for (int i = 0; i < activeSources.Count; i++)
            {
                bool remove = Tick(i);
                if (remove)
                {
                    var src = activeSources[i];
                    if(src.Source != null)
                        Object.Destroy(activeSources[i].Source.gameObject);
                    activeSources.RemoveAt(i);
                    i--;
                }
            }
        }

        private static bool Tick(int index)
        {
            var item = activeSources[index];
            if (item.Source == null)
                return true;
            if ((item.IsPlaying ?? item.DefaultIsPlaying).Invoke() == false)
                return true;
            if (item.Remove)
                return true;

            int currMapIndex = Find.CurrentMap?.uniqueID ?? -1;
            bool isOnCurrentMap = currMapIndex == item.MapId && !WorldRendererUtility.WorldRenderedNow;

            if (item.LowPass != null)
            {
                float cutoff = GetLowPassCutoff(item.Area);
                item.LowPass.cutoffFrequency = cutoff;
            }

            float volumeMulti = isOnCurrentMap ? GetVolumeMulti(item.Area) : 0f;
            float vol = volumeMulti * item.TargetVolume * Settings.FinalMusicVolume;
            item.Source.volume = vol;

            return false;
        }

        internal static float CalcLerp(CellRect? bounds)
        {
            float zoom = Mathf.InverseLerp(cameraClosest, cameraFarthest, cameraRootSize);
            float area;
            if (bounds != null)
            {
                const float MIN_DST = 10;
                const float FADE_DST = 40;

                float sqrDst = bounds.Value.ClosestDistSquaredTo(Find.CameraDriver.MapPosition);
                if(sqrDst > MIN_DST * MIN_DST)
                {
                    if (sqrDst > (MIN_DST + FADE_DST) * (MIN_DST + FADE_DST))
                    {
                        area = 1f;
                    }
                    else
                    {
                        float dst = Mathf.Sqrt(sqrDst);
                        area = Mathf.Clamp01((dst - MIN_DST) / FADE_DST);
                    }
                }
                else
                {
                    area = 0f;
                }
                if (area > zoom)
                    return area;
            }
            return zoom;
        }

        internal static float GetLowPassCutoff(CellRect? bounds)
        {
            float lerp = 1f - CalcLerp(bounds);
            float cutoff = Mathf.Lerp(lowPassLow, lowPassHigh, lerp); // Lerp is 1 when zoomed close in.
            return cutoff;
        }

        internal static float GetVolumeMulti(CellRect? bounds)
        {
            float lerp = CalcLerp(bounds);
            float vol = Mathf.Lerp(1f, volumeFar, lerp); // Lerp is 0 when zoomed close in.
            return vol;
        }
    }

    internal class ASMDebugWindow : Window
    {
        private StringBuilder str = new StringBuilder();

        public ASMDebugWindow()
        {
            forcePause = false;
            preventCameraMotion = false;
            doCloseX = true;
            closeOnClickedOutside = false;
            draggable = true;
            resizeable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            str.Clear();
            str.Append("Camera zoom: ").AppendLine(AudioSourceManager.cameraRootSize.ToString());
            str.AppendLine();

            foreach (var item in AudioSourceManager.activeSources)
            {
                str.Append("GO Name: ").AppendLine(item.Source == null ? "<null>" : item.Source.gameObject.name);
                str.Append("Map: ").AppendLine(item.MapId.ToString());
                str.Append("Volume final: ").AppendLine((item.Source == null ? -1 : item.Source.volume).ToString(CultureInfo.InvariantCulture));
                str.Append("Remove: ").AppendLine(item.Remove.ToString());
                str.Append("Lerp: ").AppendLine(AudioSourceManager.CalcLerp(item.Area).ToString());
                str.Append("Low pass: ").AppendLine(AudioSourceManager.GetLowPassCutoff(item.Area).ToString());
                str.Append("Volume multi: ").AppendLine(AudioSourceManager.GetVolumeMulti(item.Area).ToString());
                str.Append("Is playing: ").AppendLine((item.IsPlaying ?? item.DefaultIsPlaying).Invoke().ToString());
                str.AppendLine();
            }

            Widgets.Label(inRect, str.ToString());
        }
    }
}
