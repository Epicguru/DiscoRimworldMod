using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Disco.Programs;
using RimWorld;
using UnityEngine;
using Verse;

namespace Disco
{
    public class Settings : ModSettings
    {
        public static float FinalMusicVolume
        {
            get
            {
                if (UseCustomVolume)
                    return CustomMusicVolume;
                return Prefs.VolumeMusic;
            }
        }

        public static (string, AudioType) MakeFrom(string filePath, bool onlyValidFormats = false)
        {
            var info = new FileInfo(filePath);
            AudioType type = AudioType.UNKNOWN;
            string ext = info.Extension.ToLowerInvariant();

            if (ext == ".wav")
                type = AudioType.WAV;
            else if (ext == ".ogg")
                type = AudioType.OGGVORBIS;
            else if (ext == ".mp3" || ext == ".mp2")
                type = AudioType.MPEG;

            if (onlyValidFormats && type == AudioType.UNKNOWN)
                return (null, AudioType.UNKNOWN);
            return (filePath, type);
        }

        [TweakValue("_Disco!")]
        [Setting(true)]
        public static bool UseCustomVolume = true;

        [TweakValue("_Disco!", 0, 1)]
        [Setting(0.75f, isPct = true)]
        public static float CustomMusicVolume = 0.75f;

        [TweakValue("_Disco!", 100, 32768)]
        public static int DiscoMaxFloorSize = 5000;

        [TweakValue("_Disco!", 0, 1)]
        [Setting(0.8f, isPct = true)]
        public static float DiscoFloorColorIntensity = 0.8f;

        [TweakValue("_Disco!", 0, 60)]
        [Setting(10f, min = 0, max = 60)]
        public static float ManualTriggerCooldown = 10f;

        [TweakValue("_Disco!")]
        [Setting(true)]
        public static bool DoLowPass = true;

        [TweakValue("_Disco!")]
        [Setting(true)]
        public static bool DoReverb = true;

        private static List<CustomFileItem> customFiles = new List<CustomFileItem>();
        private static List<SettingField> fields;
        private static Vector2 scroll;
        private static float height;
        private static List<ProgramDef> addedDefs = new List<ProgramDef>();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref UseCustomVolume, "useCustomVolume", true);
            Scribe_Values.Look(ref CustomMusicVolume, "customVolume", 0.75f);
            Scribe_Values.Look(ref DiscoFloorColorIntensity, "floorIntensity", 0.8f);
            Scribe_Values.Look(ref ManualTriggerCooldown, "manualTriggerCooldown", 10f);
            Scribe_Values.Look(ref DoLowPass, "doLowPass", true);
            Scribe_Values.Look(ref DoReverb, "doReverb", true);

            Scribe_Collections.Look(ref customFiles, "filePaths", LookMode.Deep);
            customFiles ??= new List<CustomFileItem>();
        }

        private static void GenerateSettingsFields()
        {
            fields = new List<SettingField>();
            foreach(var field in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var attr = field.GetCustomAttribute<SettingAttribute>();
                if (attr == null)
                    continue;

                var created = new SettingField {Attribute = attr, FInfo = field};
                fields.Add(created);
            }
        }

        public static void Draw(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Color redish = new Color(1, 0.4f, 0.4f, 1f);

            Rect NextRect(float height, float? widthPercentage = null)
            {
                float w = rect.width * (widthPercentage ?? 0.45f);
                var toReturn = new Rect(rect.x, rect.y, w, height);
                rect.y += height + 10;
                rect.height -= height + 10;
                return toReturn;
            }
            Rect NextRect2(float height, float? widthPercentage = null)
            {
                float w = rect.width * (widthPercentage ?? 0.45f);
                var toReturn = new Rect(rect.x + rect.width * 0.5f, rect.y, w, height);
                rect.y += height + 10;
                rect.height -= height + 10;
                return toReturn;
            }

            const float H = 32;
            float originalY = rect.y;
            float originalH = rect.height;

            if (fields == null)
                GenerateSettingsFields();

            NextRect(10);

            foreach (var f in fields)
            {
                Rect draw = NextRect(H);
                TooltipHandler.TipRegion(draw, f.GetDescription());
                if (f.FInfo.FieldType == typeof(bool))
                {
                    bool curr = (bool)f.GetValue();
                    Widgets.CheckboxLabeled(draw, f.GetLabel(), ref curr);
                    f.SetValue(curr);
                    continue;
                }
                if (f.FInfo.FieldType == typeof(float))
                {
                    float curr = (float)f.GetValue();
                    string extra = f.Attribute.isPct ? $" ({100f * curr:F0}%)" : $" ({curr:F1})";
                    float min = f.Attribute.min, max = f.Attribute.max;
                    if (f.Attribute.isPct)
                    {
                        min = 0;
                        max = 1;
                    }
                    curr = Widgets.HorizontalSlider(draw, curr, min, max, label: f.GetLabel() + extra);
                    f.SetValue(curr);
                    continue;
                }
                if (f.FInfo.FieldType == typeof(int))
                {
                    int curr = (int)f.GetValue();
                    string extra = $" ({curr})";
                    curr = Mathf.RoundToInt(Widgets.HorizontalSlider(draw, curr, f.Attribute.min, f.Attribute.max, label: f.GetLabel() + extra));
                    f.SetValue(curr);
                    continue;
                }
            }

            GUI.color = redish;
            if (Widgets.ButtonText(NextRect(32, 0.4f), "DSC.SET.Reset".Translate()))
                foreach (var f in fields)
                    f.SetValue(f.Attribute.GetDefaultValue());
            GUI.color = Color.white;

            rect.y = originalY;

            if (Widgets.ButtonText(NextRect2(32), "DSC.AddSongs".Translate()))
            {
                Find.WindowStack.Add(new AddSongDialog()
                {
                    OnAccept = path =>
                    {
                        foreach (var item in customFiles)
                            if (item.FilePath == path)
                                return;

                        if (Directory.Exists(path))
                        {
                            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                            {
                                var pair = MakeFrom(file, true);
                                if (pair.Item1 == null)
                                    continue;

                                bool stop = false;
                                foreach (var item in customFiles)
                                {
                                    if (item.FilePath == file)
                                    {
                                        stop = true;
                                        break;
                                    }    
                                }
                                if (stop)
                                    continue;

                                customFiles.Add(new CustomFileItem()
                                {
                                    FilePath = file,
                                    Format = pair.Item2
                                });
                            }
                        }
                        else
                        {
                            var pair = MakeFrom(path, false);

                            customFiles.Add(new CustomFileItem()
                            {
                                FilePath = path,
                                Format = pair.Item2
                            });
                        }
                    }
                });
            }

            Widgets.BeginScrollView(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, originalH - 100), ref scroll, new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.45f, height));

            height = 0;
            Text.Font = GameFont.Small;
            for (int i = 0; i < customFiles.Count; i++)
            {
                var item = customFiles[i];
                var info = new FileInfo(item.FilePath);
                var r = NextRect2(80);
                Widgets.DrawBox(r, 1);
                r = r.ContractedBy(4);
                string label = info.Name;
                if (!item.Exists)
                    GUI.color = redish;
                Widgets.Label(r, label);
                TooltipHandler.TipRegion(new Rect(r.x, r.y, r.width, 26), info.FullName);
                GUI.color = Color.white;
                string formatStr = "DSC.Format".Translate(item.Format.ToString());
                if (!item.IsValidFormat)
                    GUI.color = redish;
                Widgets.Label(new Rect(r.x, r.y + 20, r.width, r.height), formatStr);
                GUI.color = Color.white;

                GUI.color = redish;
                bool remove = Widgets.ButtonText(new Rect(r.x, r.y + 46, 100, 26), "DSC.Remove".Translate());
                GUI.color = Color.white;
                Widgets.TextFieldNumericLabeled(new Rect(r.x + 105, r.y + 46, r.width - 105, 26), "DSC.RandomChance".Translate(), ref item.Weight, ref item.WeightRaw);
                if (remove)
                {
                    customFiles.RemoveAt(i);
                    i--;
                }

                height += 90;
            }
            Widgets.EndScrollView();

            GUI.color = new Color(0.5f, 1f, 0.5f, 1f);
            Text.Font = GameFont.Medium;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width * 0.5f, originalY + originalH - 45, rect.width * 0.5f, 36), "Apply"))
            {
                ApplyCustomSongs();
            }
            GUI.color = Color.white;
        }

        private static IEnumerable<ProgramDef> GenerateValidDefs()
        {
            if (customFiles == null)
                yield break;

            foreach (var file in customFiles)
            {
                if (file == null)
                    continue;
                if (!file.Exists)
                {
                    Core.Warn($"Ignoring {file.FilePath}, file does not exist!");
                    continue;
                }

                if (!file.IsValidFormat)
                {
                    Core.Warn($"Ignoring {file.FilePath}, invalid format!");
                    continue;
                }

                var info = new FileInfo(file.FilePath);
                string defName = info.Name;
                defName = defName.Replace(info.Extension, "");
                defName = defName.Replace(" ", "_");
                defName = "CUSTOM_" + defName;

                var def = new ProgramDef();
                def.modContentPack = Core.ContentPack;
                def.defName = defName;
                def.label = $"Autogenerated song def for {info.Name}";
                def.description = "Nothing to see here";
                def.groupWeight = file.Weight;
                def.programClass = typeof(SongPlayer);
                def.groups = new List<string>() {"Songs"};
                def.inputs = new DiscoDict();
                def.inputs.Add("volume", "1");
                def.inputs.Add("format", file.Format.ToString());
                def.inputs.Add("filePath", file.FilePath);
                def.inputs.Add("credits", info.Name.Replace(info.Extension, ""));

                yield return def;
            }
        }

        public static void ApplyCustomSongs()
        {
            var removeMethod = typeof(DefDatabase<ProgramDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic);
            object[] args = new object[1];
            foreach (var item in addedDefs)
            {
                args[0] = item;
                removeMethod.Invoke(null, args);
            }
            addedDefs.Clear();

            foreach (var item in GenerateValidDefs())
            {
                DefDatabase<ProgramDef>.Add(item);
                addedDefs.Add(item);
            }
            Core.Log($"Registered {addedDefs.Count} custom songs");
            Messages.Message("DSC.RegisteredSongs".Translate(addedDefs.Count), MessageTypeDefOf.SilentInput);
        }
    }

    public class SettingAttribute : Attribute
    {
        public float min, max;
        public bool isPct;
        private readonly object defaultValue;

        public SettingAttribute(object defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public object GetDefaultValue() => defaultValue;
    }

    public class SettingField
    {
        private const string HEADER = "DSC";

        public SettingAttribute Attribute;
        public FieldInfo FInfo;

        public string GetLabel()
        {
            string fieldName = FInfo.Name;
            return $"{HEADER}.SET.{fieldName}Label".Translate();
        }

        public string GetDescription()
        {
            string fieldName = FInfo.Name;
            return $"{HEADER}.SET.{fieldName}Desc".Translate().Trim() + '\n' + $"{HEADER}.SET.DefaultValue".Translate(Attribute.GetDefaultValue()?.ToString() ?? "???");
        }

        public object GetValue()
        {
            return FInfo.GetValue(null);
        }

        public void SetValue(object value)
        {
            FInfo.SetValue(null, value);
        }
    }

    public class CustomFileItem : IExposable
    {
        public string FilePath;
        public AudioType Format = AudioType.UNKNOWN;
        public float Weight = 1f;
        public string WeightRaw = "1";

        public bool Exists => File.Exists(FilePath);
        public bool IsValidFormat => Format != AudioType.UNKNOWN && Format != AudioType.MPEG && Format != AudioType.AUDIOQUEUE && Format != AudioType.MPEG;

        public void ExposeData()
        {
            Scribe_Values.Look(ref FilePath, "filePath");
            Scribe_Values.Look(ref Format, "format");
            Scribe_Values.Look(ref Weight, "weight");

            WeightRaw = Weight.ToString();
        }
    }

    internal class AddSongDialog : Window
    {
        public override Vector2 InitialSize => new Vector2(550, 160);

        public Action<string> OnAccept;

        private string filePath;

        public AddSongDialog()
        {
            closeOnClickedOutside = false;
            doCloseX = true;
            doCloseButton = false;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32), "DSC.PasteFile".Translate());
            filePath = Widgets.TextArea(new Rect(inRect.x, inRect.y + 36, inRect.width, 64), filePath);
            bool canAdd = File.Exists(filePath) || Directory.Exists(filePath);
            if (!canAdd)
                GUI.color = Color.grey;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + 104, inRect.width, 32), "DSC.Add".Translate(), active: canAdd))
            {
                OnAccept?.Invoke(filePath);
                Close();
            }
            GUI.color = Color.white;
        }
    }
}
