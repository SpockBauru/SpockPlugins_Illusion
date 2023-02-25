// System
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// This file is not game specific. It takes a string and display the subtitle using only Unity libs.
// Conditional compile may apply to Unity versions.
namespace IllusionPlugins
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BasePlugin
    {
        // Plugin consts
        public const string GUID = "com.SpockBauru.IllusionPlugins.Subtitles";
        public const string PluginName = "Subtitles";
        public const string Version = "0.1";
        public const string PluginNameInternal = Constants.Prefix + "_Subtitles";

        // BepInEx Config
        //internal static Subtitles Instance;
        internal static ManualLogSource Logger;
        internal static ConfigEntry<bool> EnableConfig;

        // Plugin variables
        public static GameObject SpockBauru;

        public override void Load()
        {
            EnableConfig = Config.Bind("General",
                                 "Enable Subtitles",
                                 true,
                                 "Reload the game to Enable/Disable");

            if (EnableConfig.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<SubtitlesCanvas>();

            // Add the monobehavior component to your personal GameObject. Try to not duplicate.
            SpockBauru = GameObject.Find("SpockBauru");
            if (SpockBauru == null)
            {
                SpockBauru = new GameObject("SpockBauru");
                GameObject.DontDestroyOnLoad(SpockBauru);
                SpockBauru.hideFlags = HideFlags.DontSave;
                SpockBauru.AddComponent<SubtitlesCanvas>();
            }
            else SpockBauru.AddComponent<SubtitlesCanvas>();
        }

        public class SubtitlesCanvas : MonoBehaviour
        {
            // Constructor needed to use Start, Update, etc...
            public SubtitlesCanvas(IntPtr handle) : base(handle) { }

            public static void DisplaySubtitle(GameObject voiceFile, string text)
            {
                Debug.Log("File: " + voiceFile.name + " Text: " + text);
            }
        }

    }

}
