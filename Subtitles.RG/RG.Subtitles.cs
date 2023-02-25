// BepInEx
using HarmonyLib;
using UnhollowerBaseLib;
using Il2CppSystem.Collections.Generic;

// Unity things
using UnityEngine;
using UnityEngine.SceneManagement;

// Game specific
using static HVoiceCtrl;

// This file is game specific. It must provide the scene, voice file and text to Core.Subtitles
namespace IllusionPlugins
{
    public partial class Subtitles
    {
        private static Il2CppReferenceArray<Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, VoiceListInfo>>>>> weirdDictionary;
        //private static List<PlayVoiceinfo> weirdIndexes = new List<PlayVoiceinfo>();
        private static HScene hentaiScene;
        private static string currentFile;
        private static string currentText;

        internal static class Hooks
        {
            // H-Scene initialization
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HScene), nameof(HScene.Start))]
            private static void HSceneStart(HScene __instance)
            {
                weirdDictionary = __instance.CtrlVoice._voiceList.DicDicDicDicVoice;
                hentaiScene = __instance;
                // Making the Subtitle Canvas
                MakeCanvas(SceneManager.GetActiveScene());
            }

            // Play all voices
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Manager.Voice), nameof(Manager.Voice.PlayStandby))]
            private static void GetTextFromVoice(Manager.Voice.Loader loader, AudioSource audioSource)
            {
                if (hentaiScene == null) return;

                string audioFile = loader.Asset;

                //Debug.Log(" audioFile: " + audioFile);

                //// THIS WAY WOULD BE MORE OPTIMIZED, BUT IL2CPP CANNOT GET THE INDEXES...
                //for (int femaleIndex = 0; femaleIndex < weirdDictionary.Count; femaleIndex++)
                //{
                //    var female = weirdDictionary[femaleIndex];
                //    if (female == null) continue;

                //    // Each file is picked at random. Usually there are not more than 8 files
                //    for (int i = 0; i < weirdIndexes.Count; i++)
                //    {
                //        currentFile = female[weirdIndexes[i].Mode][weirdIndexes[i].Sheet][weirdIndexes[i].Kind][weirdIndexes[i].VoiceID].NameFile;
                //        Debug.Log("NameFile: " + currentFile);
                //        if (currentFile == audioFile)
                //        {
                //            currentText = female[weirdIndexes[i].Mode][weirdIndexes[i].Sheet][weirdIndexes[i].Kind][weirdIndexes[i].VoiceID].Word;
                //            //Debug.Log("File: " + audioSource.gameObject.name + " Text: " + currentText);
                //            Debug.Log(currentText);
                //            SubtitlesCanvas.DisplaySubtitle(audioSource.gameObject, currentText);
                //        }
                //    }
                //}

                // Searching all dictionary... Its dumb but IL2CPP is not cooperating
                foreach (var female in weirdDictionary)
                {
                    if (female == null) continue;
                    foreach(var mode in female.Values)
                        foreach (var sheet in mode.Values)
                            foreach (var kind in sheet.Values)
                                foreach (var voiceID in kind.Values)
                                {
                                    currentFile = voiceID.NameFile;
                                    if (currentFile == audioFile)
                                    {
                                        currentText = voiceID.Word;
                                        // Send the text to be displayed
                                        SubtitlesCanvas.DisplaySubtitle(audioSource, currentText);
                                        break;
                                    }
                                }
                }
            }


            //// Voice in the beginning - ILCCPP CANT HANDLE THIS METHOD! MANY SUBTITLES MISSING BECAUSE OF IT!!!!!!
            //[HarmonyPostfix]
            //[HarmonyPatch(typeof(HVoiceCtrl), nameof(HVoiceCtrl.GetPlayListNum))]
            //private static void GetDicIncexesBegining(ref List<PlayVoiceinfo> __result)
            //{
            //    weirdIndexes = __result;
            //}

            //// General Voices
            //[HarmonyPostfix]
            //[HarmonyPatch(typeof(HVoiceCtrl), nameof(HVoiceCtrl.GetPlayNum))]
            //private static void GetDicIncexes(ref List<PlayVoiceinfo> __result)
            //{
            //    weirdIndexes = __result;
            //}

            //// Voice when the position change
            //[HarmonyPostfix]
            //[HarmonyPatch(typeof(HVoiceCtrl), nameof(HVoiceCtrl.GetStartPlayNum))]
            //private static void GetDicIncexesStart(ref List<PlayVoiceinfo> __result)
            //{
            //    weirdIndexes = __result;
            //}

            //// Voice in the first position
            //[HarmonyPostfix]
            //[HarmonyPatch(typeof(HVoiceCtrl), nameof(HVoiceCtrl.GetStartPlayListNum))]
            //private static void GetDicIncexesBegining(ref List<PlayVoiceinfo> __result)
            //{
            //    weirdIndexes = __result;
            //}
        }
    }
}
