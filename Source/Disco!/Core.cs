using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Disco
{
    public class Core : Mod
    {
        public static ModContentPack ContentPack { get; private set; }
        public static Core Instance { get; private set; }

        internal static void Log(string msg)
        {
            Verse.Log.Message($"<color=magenta>[Disco]</color> {msg ?? "<null>"}");
        }

        internal static void Warn(string msg)
        {
            Verse.Log.Warning($"[Disco] {msg ?? "<null>"}");
        }

        internal static void Error(string msg, Exception exception = null)
        {
            Verse.Log.Error($"[Disco] {msg ?? "<null>"}");
            if(exception != null)
                Verse.Log.Error(exception.ToString());
        }

        public Core(ModContentPack content) : base(content)
        {
            Log("Hello, world!");
            Instance = this;
            ContentPack = content;

            // Apply harmony patches.
            var harmony = new Harmony("co.uk.epicguru.disco");
            try
            {
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                Error("Failed to apply 1 or more harmony patches! Mod will not work as intended. Contact author.", e);
            }
            finally
            {
                Log($"Patched {harmony.GetPatchedMethods().EnumerableCount()} methods:\n{string.Join(",\n", harmony.GetPatchedMethods())}");
            }

            // Create MonoBehaviour hook.
            var go = new GameObject("Disco! hook");
            go.AddComponent<UnityHook>();
            Log("Created Unity hook game object.");
        }
    }
}
