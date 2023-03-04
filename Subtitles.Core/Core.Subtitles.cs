// System
using System;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// This file is not game specific. Its responsible to manage the Subtitle Canvas and text.
namespace IllusionPlugins
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.Subtitles";
        public const string PluginName = "Subtitles";
        public const string Version = "0.1";
        public const string PluginNameInternal = Constants.Prefix + "_Subtitles";

        // BepInEx Config
        internal static ConfigEntry<bool> EnableConfig;

        // Plugin variables
        static GameObject canvasObject;

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
        }

        /// <summary>
        /// Create the subtitle canvas in the desired scene
        /// </summary>
        /// <param name="scene"></param>
        public static void MakeCanvas(Scene scene)
        {
            if (canvasObject != null) return;

            // Creating Canvas object
            canvasObject = new GameObject("SubtitleCanvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.AddComponent<SubtitlesCanvas>();
        }

        public class SubtitlesCanvas : MonoBehaviour
        {
            // Constructor needed to use Start, Update, etc...
            public SubtitlesCanvas(IntPtr handle) : base(handle) { }

            static GameObject subtitleObject;
            static Text subtitle;

            static float time = 0;
            static float clipLenght = 0;

            void Start()
            {
                // Setting canvas attributes
                var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                canvasObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

                // Setting subtitle object
                subtitleObject = new GameObject("SubtitleText");
                subtitleObject.transform.SetParent(canvasObject.transform);

                int fontSsize = (int)(Screen.height / 25.0f);

                RectTransform subtitleRect = subtitleObject.AddComponent<RectTransform>();
                subtitleRect.pivot = new Vector2(0, -1);
                subtitleRect.sizeDelta = new Vector2(Screen.width * 0.990f, fontSsize + (fontSsize * 0.05f));

                subtitle = subtitleObject.AddComponent<Text>();
                subtitle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                subtitle.fontSize = fontSsize;
                subtitle.fontStyle = FontStyle.Bold;
                subtitle.alignment = TextAnchor.LowerCenter;
                subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
                subtitle.verticalOverflow = VerticalWrapMode.Overflow;
                subtitle.color = Color.white;
                subtitle.text = "";

                var outline = subtitleObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(2.0f, -2.0f);
            }

            /// <summary>
            /// Display the subtitle text while the voiceFile is active
            /// </summary>
            /// <param name="voiceFile"></param>
            /// <param name="text"></param>
            public static void DisplaySubtitle(AudioSource voiceFile, string text)
            {
                subtitle.text = text;
                clipLenght = voiceFile.clip.length;
                time = 0;
            }

            // Using Update because coroutines, onDestroy and onDisable are not working as intended
            void Update()
            {
                if (time < clipLenght) subtitleObject.active = true;
                else subtitleObject.active = false;
                time += Time.deltaTime;
            }
        }
    }
}
