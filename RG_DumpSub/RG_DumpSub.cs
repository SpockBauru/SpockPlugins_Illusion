// System
using System;
using System.IO;
using System.Text;

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
using Scene = UnityEngine.SceneManagement.Scene;
using RG.Utils;

// This file is game specific. It must provide the scene, voice file and text to Core.Subtitles
namespace IllusionPlugins
{

    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RG_DumpSub : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.RG_DumpSub";
        public const string PluginName = "RG_DumpSub";
        public const string Version = "0.1";


        private static Il2CppReferenceArray<Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, VoiceListInfo>>>>> weirdDictionary;
        //private static List<PlayVoiceinfo> weirdIndexes = new List<PlayVoiceinfo>();
        private static HScene hentaiScene;
        private static string currentFile = "Subtitles.txt";
        private static string currentText;

        static System.Collections.Generic.HashSet<string> textHash;

        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            Debug.Log("== 0 ==");
            readFile();

            SceneManager.add_sceneUnloaded(new Action<Scene>((s) => writeFile()));
        }

        void readFile()
        {
            string[] allLines = File.ReadAllLines(currentFile);
            textHash = new System.Collections.Generic.HashSet<string>(allLines);
        }

        static void writeFile()
        {
            string[] stringArray = new string[textHash.Count];
            textHash.CopyTo(stringArray);
            File.WriteAllLines(currentFile, stringArray, Encoding.UTF8);
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

            // Get all text
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Manager.Voice), nameof(Manager.Voice.PlayStandby))]
            private static void GetTextFromVoice()
            {
                if (hentaiScene == null) return;

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
                                    AddToSet(currentText);
                                }
                }
            }
        }
        public static void AddToSet(string text)
        {
            if (textHash.Contains(text)) return;
            textHash.Add(text);
        }
    }
}
