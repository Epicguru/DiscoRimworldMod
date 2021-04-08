using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Disco
{
    [StaticConstructorOnStartup]
    internal static class Temp
    {
        [HarmonyPatch(typeof(UIRoot_Entry), "Init")]
        static class ImportantMessage
        {
            static void Prefix()
            {

                try
                {
                    //if (!Settings.dontShowAgain)
                    //    ShowMsg();
                }
                catch (Exception e)
                {
                    Log.Warning("Oops: " + e);
                }
            }
        }

        static void ShowMsg()
        {
            Find.WindowStack.Add(new CustomWindow());
        }

        class CustomWindow : Window
        {
            private bool dontShowAgain = true;

            public CustomWindow()
            {
                base.doCloseButton = true;
            }

            public override void DoWindowContents(Rect inRect)
            {
                Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 150);
                Widgets.Label(titleRect, $"<size=34><color=cyan><b>{"DSC.ThanksTitle".Translate()}</b></color></size>");

                Rect textRect = new Rect(inRect.x, inRect.y + 150, inRect.width, inRect.height - 150);
                Widgets.Label(textRect, "DSC.ThanksText".Translate());

                Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.yMax - 72, inRect.width * 0.35f, 32), "DSC.DontShowAgain".Translate(), ref dontShowAgain);
            }

            public override void PreClose()
            {
                base.PreClose();

                if (dontShowAgain)
                {
                    Settings.dontShowAgain = true;
                    Core.Instance.WriteSettings();
                }
            }
        }
    }
}
