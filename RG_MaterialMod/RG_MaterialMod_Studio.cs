using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils;
using BepInEx.IL2CPP.Utils.Collections;
using BepInEx.Logging;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// Extended Save
using ExtensibleSaveFormat;

// Game Specific
using RG;
using Chara;
using CharaCustom;

//Plugin Specific


namespace IllusionPlugins
{
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal class RG_MaterialMod_Studio : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.MaterialMod_Studio";
        public const string PluginName = "MaterialMod_Studio";
        public const string Version = "0.2.0";
        public const string PluginNameInternal = Constants.Prefix + "_MaterialMod_Studio";

        internal static new ManualLogSource Log;
        public static GameObject SpockBauru;

        public override void Load()
        {
            Log = base.Log;
            Harmony.CreateAndPatchAll(typeof(RG_MaterialMod_Studio_Hooks), GUID);

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<MaterialModMonoBehaviour> ();

            // Add the monobehavior component to your personal GameObject. Try to not duplicate.
            SpockBauru = GameObject.Find("SpockBauru");
            if (SpockBauru == null)
            {
                SpockBauru = new GameObject("SpockBauru");
                GameObject.DontDestroyOnLoad(SpockBauru);
                SpockBauru.hideFlags = HideFlags.DontSave;
                SpockBauru.AddComponent<MaterialModMonoBehaviour>();
            }
            else SpockBauru.AddComponent<MaterialModMonoBehaviour>();

            //ExtendedSave.CardBeingLoaded += MaterialModMonoBehaviour.LoadCard;
        }
    }
}
