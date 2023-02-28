// System
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// BepInEx
using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnhollowerBaseLib;
using Il2CppSystem.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;

// Game specific
using static HVoiceCtrl;

using ADV;
using Illusion.Unity;

namespace IllusionPlugins
{

    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RG_TextDump : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.RG_TextDump";
        public const string PluginName = "RG_TextDump";
        public const string Version = "0.1";

        private static Il2CppReferenceArray<Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, VoiceListInfo>>>>> weirdDictionary;
        private static HScene hentaiScene;

        static System.Collections.Generic.HashSet<string> SubtitleHash;
        static bool isAdvDumped = false;

        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            Debug.Log("== 0 ==");
            SceneManager.add_sceneUnloaded(new Action<Scene>((s) => WriteSubtitles()));
        }

        static void ReadSubtitles(string path)
        {
            string[] allLines = File.ReadAllLines(path);
            SubtitleHash = new System.Collections.Generic.HashSet<string>(allLines);
        }

        static void WriteSubtitles()
        {
            WriteFile("Subtitles.txt", SubtitleHash);
        }

        static void WriteFile(string path, List<string> text)
        {
            string[] stringArray = text.ToArray();
            File.WriteAllLines(path, stringArray, Encoding.UTF8);
        }

        static void WriteFile(string path, System.Collections.Generic.HashSet<string> hashSet)
        {
            string[] stringArray = new string[hashSet.Count];
            hashSet.CopyTo(stringArray);
            File.WriteAllLines(path, stringArray, Encoding.UTF8);
        }

        internal static class Hooks
        {
            // H-Scene initialization
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HScene), nameof(HScene.Start))]
            private static void HSceneStart(HScene __instance)
            {
                weirdDictionary = __instance.CtrlVoice._voiceList.DicDicDicDicVoice;
                hentaiScene = __instance;
            }

            // ============================= Dump Subtitles =============================
            // Get all subtitles when girl is loaded on H-scenes. Must enter with each girls to get all subtitles
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Manager.Voice), nameof(Manager.Voice.PlayStandby))]
            private static void GetTextFromVoice()
            {
                if (hentaiScene == null) return;
                ReadSubtitles("Subtitles.txt");

                string currentText;

                foreach (var female in weirdDictionary)
                {
                    if (female == null) continue;
                    foreach (var mode in female.Values)
                        foreach (var sheet in mode.Values)
                            foreach (var kind in sheet.Values)
                                foreach (var voiceID in kind.Values)
                                {
                                    currentText = voiceID.Word;
                                    currentText = currentText.Replace("\n", "\\n");
                                    currentText = "//" + currentText + "=";
                                    AddSubtitle(currentText);
                                }
                }
            }

            public static void AddSubtitle(string text)
            {
                if (SubtitleHash.Contains(text)) return;
                SubtitleHash.Add(text);
            }

            // ============================= Dump ADV =============================
            // =================== INCOMPLETE!!!! ===================
            [HarmonyPostfix]
            [HarmonyPatch(typeof(TextScenario), nameof(TextScenario.LoadFile))]
            private static void LoadFile1(string bundle, string asset, TextScenario __instance)
            {
                Debug.Log("== Bundle: " + bundle + " Asset: " + asset + " ==");

                if (isAdvDumped) return;

                List<string> ADVDialogues = new List<string>();
                var fileList = CommonLib.GetAssetBundleNameListFromPath("adv\\scenario", subdirCheck: true);
                string currentText;

                foreach (string item in fileList)
                {
                    ScenarioData[] allAssets = AssetBundleManager.LoadAllAsset(item, UnhollowerRuntimeLib.Il2CppType.Of<ScenarioData>()).GetAllAssets<ScenarioData>();
                    foreach (ScenarioData scenarioData in allAssets)
                    {
                        var listData = scenarioData.list;
                        foreach (var data in listData)
                        {
                            var args = data.Args;
                            for (int j = 0; j < args.Length; j++)
                            {
                                // If this line is [H] and the following line contains japanese characters
                                if (args[j].StartsWith("[H]"))
                                {
                                    currentText = args[j + 1];
                                    if (Regex.IsMatch(currentText, "[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]+|[々〆〤ヶ]+"))
                                    {
                                        currentText = currentText.Replace("\n", "\\n");
                                        currentText = "//" + currentText + "=";
                                        ADVDialogues.Add(args[j + 1]);
                                    }
                                }

                            }
                        }
                    }
                }
                WriteFile("ADVDialogues.txt", ADVDialogues);
                isAdvDumped = true;
            }
        }
    }
}
