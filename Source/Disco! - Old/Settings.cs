using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

        [TweakValue("_Disco!")]
        [Setting(true)]
        public static bool GameSpeedAffectsMusic = false;

        [TweakValue("_Disco!")]
        [Setting(true)]
        public static bool DoLowPass = true;

        [TweakValue("_Disco!")]
        [Setting(true)]
        public static bool DoReverb = true;

        [TweakValue("_Disco!", 100, 32768)]
        public static int DiscoMaxFloorSize = 5000;

        [TweakValue("_Disco!", 0, 1)]
        [Setting(0.8f, isPct = true)]
        public static float DiscoFloorColorIntensity = 0.8f;

        [TweakValue("_Disco", 0, 10)]
        [Setting(1f, min = 0, max = 10)]
        public static float WattsPerFloorTile = 1f;

        [TweakValue("_Disco!", 0, 60)]
        [Setting(10f, min = 0, max = 60)]
        public static float ManualTriggerCooldown = 10f;

        public static Dictionary<SequenceDef, float> sequenceWeights = new Dictionary<SequenceDef, float>();
        public static Dictionary<ProgramDef, float> builtInSongWeights = new Dictionary<ProgramDef, float>();

        internal static bool dontShowAgain;

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
            Scribe_Values.Look(ref dontShowAgain, "dontShowAgain", false);

            Scribe_Collections.Look(ref customFiles, "filePaths", LookMode.Deep);
            Scribe_Collections.Look(ref sequenceWeights, "sequenceWeights", LookMode.Undefined, LookMode.Value);
            customFiles ??= new List<CustomFileItem>();
            sequenceWeights ??= new Dictionary<SequenceDef, float>();
            builtInSongWeights ??= new Dictionary<ProgramDef, float>();
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

            Widgets.BeginScrollView(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.5f, originalH - 200), ref scroll, new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.45f, height));

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
            if (Widgets.ButtonText(new Rect(rect.x + rect.width * 0.5f, originalY + originalH - 145, rect.width * 0.5f, 36), "Apply"))
            {
                ApplyCustomSongs();
            }
            GUI.color = Color.white;

            if (Widgets.ButtonText(new Rect(rect.x + rect.width * 0.5f, originalY + originalH - 60, rect.width * 0.5f, 36), "DSC.ChangeSequenceWeightsButton".Translate()))
            {
                sequenceWeights ??= new Dictionary<SequenceDef, float>();
                foreach (var sequence in DefDatabase<SequenceDef>.AllDefsListForReading)
                {
                    if (!sequenceWeights.ContainsKey(sequence))
                    {
                        sequenceWeights.Add(sequence, 1f);
                    }
                }
                Find.WindowStack.Add(new AdjustSequenceWeightsDialog(){SequenceWeights = sequenceWeights});
            }
            if (Widgets.ButtonText(new Rect(rect.x + rect.width * 0.5f, originalY + originalH - 100, rect.width * 0.5f, 36), "DSC.ChangeSongWeightsButton".Translate()))
            {
                builtInSongWeights ??= new Dictionary<ProgramDef, float>();
                foreach (var prog in DefDatabase<ProgramDef>.AllDefsListForReading)
                {
                    if (!typeof(SongPlayer).IsAssignableFrom(prog.programClass))
                        continue;

                    if (!builtInSongWeights.ContainsKey(prog))
                    {
                        builtInSongWeights.Add(prog, 1f);
                    }
                }
                Find.WindowStack.Add(new AdjustSongWeightsDialog() { SongWeights = builtInSongWeights });
            }
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
                def.GroupWeight = file.Weight;
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
            if(addedDefs.Count > 0)
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
        public override Vector2 InitialSize => new Vector2(550, 155);

        public Action<string> OnAccept;

        private string filePath;

        public AddSongDialog()
        {
            closeOnClickedOutside = false;
            doCloseX = true;
            doCloseButton = false;
            draggable = true;
            closeOnAccept = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32), "DSC.PasteFile".Translate());
            filePath = Widgets.TextArea(new Rect(inRect.x, inRect.y + 36, inRect.width, 44), filePath);
            bool canAdd = File.Exists(filePath) || Directory.Exists(filePath);
            if (!canAdd)
                GUI.color = Color.grey;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + 84, inRect.width, 32), "DSC.Add".Translate(), active: canAdd))
            {
                OnAccept?.Invoke(filePath);
                Close();
            }
            GUI.color = Color.white;
        }
    }

    internal class AdjustSequenceWeightsDialog : Window
    {
        public Dictionary<SequenceDef, float> SequenceWeights;

        private string[] buffer;
        private float[] weights;
        private float height;
        private Vector2 scroll;

        public AdjustSequenceWeightsDialog()
        {
            optionalTitle = "DSC.AdjustSequenceWeights".Translate();
            closeOnClickedOutside = true;
            doCloseX = true;
            doCloseButton = false;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (SequenceWeights == null)
            {
                Core.Error("Null sequence weights, closing dialog.");
                Close();
                return;
            }

            buffer ??= new string[SequenceWeights.Count];
            weights ??= new float[SequenceWeights.Count];

            Rect rect = inRect;
            Widgets.BeginScrollView(rect, ref scroll, new Rect(rect.x, rect.y, rect.width - 25, height));
            height = 0;

            rect.width -= 25;
            rect.height = 70;

            Text.Font = GameFont.Medium;

            int i = 0;

            foreach (var pair in SequenceWeights)
            {
                string label = pair.Key.label;
                float weight = pair.Value;
                weights[i] = weight;

                Widgets.DrawBox(rect, 1);
                Rect inner = rect.ContractedBy(3);
                rect.y += 80;
                height += 80;
                GUI.color = weight <= 0f ? Color.grey : Color.green;
                Widgets.Label(inner, label);
                GUI.color = Color.white;
                inner.y += 30;
                inner.height = 28;

                string buff = buffer[i];
                buff ??= weight.ToString(CultureInfo.InvariantCulture);
                Widgets.TextFieldNumericLabeled(inner, "DSC.WeightReadout".Translate(), ref weight, ref buff);
                if (weight < 0 && !float.IsNaN(weight) && !float.IsInfinity(weight))
                {
                    weight = 0;
                    buff = "0";
                }
                weights[i] = weight;
                buffer[i++] = buff;
            }

            i = 0;
            foreach (var key in SequenceWeights.Keys.ToArray())
            {
                SequenceWeights[key] = weights[i++];
            }

            Widgets.EndScrollView();
        }
    }

    internal class AdjustSongWeightsDialog : Window
    {
        public Dictionary<ProgramDef, float> SongWeights;

        private string[] buffer;
        private float[] weights;
        private float height;
        private Vector2 scroll;

        public AdjustSongWeightsDialog()
        {
            optionalTitle = "DSC.AdjustSongWeights".Translate();
            closeOnClickedOutside = true;
            doCloseX = true;
            doCloseButton = false;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (SongWeights == null)
            {
                Core.Error("Null song weights, closing dialog.");
                Close();
                return;
            }

            buffer ??= new string[SongWeights.Count];
            weights ??= new float[SongWeights.Count];

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 56), "DSC.SongWeightsNote".Translate());
            inRect.y += 56;
            inRect.height -= 56;
            Text.Font = GameFont.Medium;

            Rect rect = inRect;
            Widgets.BeginScrollView(rect, ref scroll, new Rect(rect.x, rect.y, rect.width - 25, height));
            height = 0;

            rect.width -= 25;
            rect.height = 70;

            Text.Font = GameFont.Medium;

            int i = 0;

            foreach (var pair in SongWeights)
            {
                string label = pair.Key.inputs.TryGetValue("credits", "???");
                float weight = pair.Value;
                weights[i] = weight;

                Widgets.DrawBox(rect, 1);
                Rect inner = rect.ContractedBy(3);
                rect.y += 80;
                height += 80;
                GUI.color = weight <= 0f ? Color.grey : Color.green;
                Widgets.Label(inner, label);
                GUI.color = Color.white;
                inner.y += 30;
                inner.height = 28;

                string buff = buffer[i];
                buff ??= weight.ToString(CultureInfo.InvariantCulture);
                Widgets.TextFieldNumericLabeled(inner, "DSC.WeightReadout".Translate(), ref weight, ref buff);
                if (weight < 0 && !float.IsNaN(weight) && !float.IsInfinity(weight))
                {
                    weight = 0;
                    buff = "0";
                }
                weights[i] = weight;
                buffer[i++] = buff;
            }

            i = 0;
            foreach (var key in SongWeights.Keys.ToArray())
            {
                SongWeights[key] = weights[i++];
            }
            Widgets.EndScrollView();
        }
    }
}
