using System;
using Disco.Audio;
using UnityEngine;
using Verse;
#if V15
using LudeonTK;
#endif

namespace Disco
{
    public class UnityHook : MonoBehaviour
    {
        [TweakValue("Disco!", 0, 1)]
        public static bool DrawDebugGUI = false;

        public static event Action<bool> OnPauseChange;

        public static event Action UponApplicationQuit;
        private bool lastPaused;

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void Update()
        {
            if (Current.Game != null)
            {
                bool currentPaused = Find.TickManager?.Paused ?? false;
                if (lastPaused != currentPaused)
                {
                    OnPauseChange?.Invoke(currentPaused);
                    lastPaused = currentPaused;
                }
            }

            bool removeAll = Current.ProgramState != ProgramState.Playing;
            AudioSourceManager.Tick(removeAll);
        }

        private void OnApplicationQuit()
        {
            Core.Log("Detected application quit...");
            UponApplicationQuit?.Invoke();
        }

        private void OnGUI()
        {
            if (!DrawDebugGUI)
                return;

            DrawDiscoDebug();
        }

        private void DrawDiscoDebug()
        {
            if (Current.ProgramState != ProgramState.Playing)
                return;

            var dt = Find.CurrentMap?.GetComponent<DiscoTracker>();
            if (dt == null)
                return;

            foreach (var stand in dt.GetAllValidDJStands())
            {
                stand.DebugOnGUI();
            }
        }
    }
}
