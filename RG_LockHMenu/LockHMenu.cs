using System;

// BepInEx gang
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity things
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

// Assembly-CSharp
using SceneAssist;

using Input = UnityEngine.Input;

namespace RG_LockHMenu
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class LockHMenu : BasePlugin
    {
        public const string PluginName = "RG Lock H-Menu";
        public const string GUID = "SpockBauru.RG.LockHMenu";
        public const string Version = "0.2";

        internal static ConfigEntry<bool> EnableConfig;
        internal static ConfigEntry<bool> LockMenuConfig;

        public static GameObject SpockBauru;
        private static GameObject lockIcon;
        private static GameObject femaleLockToggle;
        private static GameObject iconHelpOriginal;

        private static bool slideScriptNeedsInitialize;
        private static UISlideVisible sliderScript;
        private static PointerEnterExitAction sliderPointer;
        private static CanvasGroup sliderGroup;

        public override void Load()
        {
            EnableConfig = Config.Bind("General",
                                       "Enable Mod",
                                       true,
                                       "Reload the game to Enable/Disable");

            LockMenuConfig = Config.Bind("General",
                                     "Lock H-Menu",
                                     true,
                                     "Don't hide H-Henu in sex scenes");

            if (EnableConfig.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<DontCloseMod>();

            // Add the monobehavior component to your personal GameObject. Try to not duplicate.
            SpockBauru = GameObject.Find("SpockBauru");
            if (SpockBauru == null)
            {
                SpockBauru = new GameObject("SpockBauru");
                GameObject.DontDestroyOnLoad(SpockBauru);
                SpockBauru.hideFlags = HideFlags.DontSave;
                SpockBauru.AddComponent<DontCloseMod>();
            }
            else SpockBauru.AddComponent<DontCloseMod>();
        }

        private static class Hooks
        {
            // Slide menu animation
            [HarmonyPostfix]
            [HarmonyPatch(typeof(UISlideVisible), nameof(UISlideVisible.Update))]
            private static void StartSliderUI(UISlideVisible __instance)
            {
                if (!slideScriptNeedsInitialize) return;
                slideScriptNeedsInitialize = false;

                GameObject sliderScriptGameobject = __instance.gameObject;
                sliderScript = sliderScriptGameobject.GetComponent<UISlideVisible>();
                sliderPointer = sliderScriptGameobject.GetComponent<PointerEnterExitAction>();
                sliderGroup = sliderScriptGameobject.GetComponentInChildren<CanvasGroup>();
            }

            // H-Menu buttons
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HUIBtnAction), nameof(HUIBtnAction.Start))]
            private static void StartH_UI(HUIBtnAction __instance)
            {
                if (__instance.gameObject.name.Equals("female", StringComparison.Ordinal))
                {
                    femaleLockToggle = __instance.gameObject;

                    // Initializing gameobject
                    lockIcon = new GameObject("LockHMenu");
                    lockIcon.transform.SetParent(femaleLockToggle.transform.parent.transform.parent);
                    lockIcon.transform.localPosition = new Vector3(-180, - 300, 0);
                    lockIcon.transform.localScale = 0.6f * Vector3.one;
                    DontCloseMod.Initialize();
                }
            }

            // Getting Icon Help object
            [HarmonyPostfix]
            [HarmonyPatch(typeof(IconHelp), nameof(IconHelp.Start))]
            private static void IconHelpStart(IconHelp __instance)
            {
                iconHelpOriginal = __instance.transform.gameObject;
            }
        }

        public class DontCloseMod : MonoBehaviour
        {
            // Constructor needed to use Start, Update, etc...
            public DontCloseMod(IntPtr handle) : base(handle) { }

            private static Image lockBackground;
            private static Toggle lockToggle;
            private static GameObject iconHelp;

            PointerEventData pointerEvent = new PointerEventData(EventSystem.current);
            Il2CppSystem.Collections.Generic.List<RaycastResult> raycastResults = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            bool isHit;
            float checkTime = 0;
            static Color backgroundColor = new Color(0.576f, 0.901f, 0.811f, 1f);

            public static void Initialize()
            {
                // Add Background and checkmark
                Instantiate(femaleLockToggle.transform.GetChild(0), lockIcon.transform);
                lockBackground = lockIcon.GetComponentInChildren<Image>();
                lockBackground.color = backgroundColor;
                lockBackground.gameObject.name = "lockBackground";

                Image lockCheckmark = lockBackground.transform.GetChild(0).gameObject.GetComponentInChildren<Image>();
                lockCheckmark.color = new Color(0.7f, 0.9f, 1f, 1f);
                lockCheckmark.gameObject.name = "lockCheckmark";

                // Initializing Toggle
                lockIcon.AddComponent<Toggle>();
                lockToggle = lockIcon.GetComponent<Toggle>();
                lockToggle.targetGraphic = lockBackground;
                lockToggle.graphic = lockCheckmark;
                lockToggle.toggleTransition = Toggle.ToggleTransition.Fade;
                lockToggle.transition = Selectable.Transition.None;
                lockToggle.isOn = LockMenuConfig.Value;
                lockToggle.onValueChanged.AddListener((UnityAction<bool>)ToggleSliderMenu);

                // Add animation
                lockIcon.AddComponent<HUIBtnAction>();
                HUIBtnAction lockMenuAnimation = lockIcon.GetComponent<HUIBtnAction>();
                lockMenuAnimation._ownTgl = lockToggle;
                lockMenuAnimation._imgTransform = lockBackground.rectTransform;
                lockMenuAnimation._defScale = new Vector3(1, 1, 1);
                lockMenuAnimation._bigImgScale = new Vector3(1.625f, 1.625f, 1.625f);
                lockMenuAnimation._smallImgScale = new Vector3(1.375f, 1.375f, 1.375f);
                lockMenuAnimation._imgScaleEasingTime = 0.1f;
                
                // Add Icon Help
                iconHelp = Instantiate(iconHelpOriginal.transform.GetChild(0), lockIcon.transform).gameObject;
                iconHelp.SetActive(false);
                iconHelp.transform.localPosition = new Vector3(50, 0, 0);
                iconHelp.transform.localScale = new Vector3(0.85f, 0.85f, 1);

                CanvasGroup iconHelpCanvasGroup = iconHelp.GetComponent<CanvasGroup>();
                iconHelpCanvasGroup.alpha = 1;

                Image iconHelpBackground = iconHelp.GetComponent<Image>();
                iconHelpBackground.rectTransform.sizeDelta = new Vector2(500f, 144f);

                Text iconHelpText = iconHelp.GetComponentInChildren<Text>();
                iconHelpText.text = "Lock Menu Mod v" + Version;

                // Initializing stats
                ToggleSliderMenu(lockToggle.isOn);
            }

            private static void ToggleSliderMenu(bool activated)
            {
                if (activated)
                {
                    backgroundColor.a = 0;

                    sliderScript.enabled = false;
                    sliderPointer.enabled = false;
                    sliderGroup.gameObject.SetActive(true);
                    sliderGroup.interactable = true;
                    sliderGroup.alpha = 1;
                    sliderGroup.transform.localPosition = Vector3.zero;
                    sliderGroup.blocksRaycasts = true;
                }
                else
                {
                    backgroundColor.a = 1;

                    sliderScript.enabled = true;
                    sliderPointer.enabled = true;
                }

                lockBackground.color = backgroundColor;
                LockMenuConfig.Value = activated;
            }

            // Using Update to get mouse over button, because IL2CPP don't let-me do the right way...
            private void Update()
            {
                // check 3 times a second
                checkTime += Time.deltaTime;
                if (checkTime < 0.333f) return;
                checkTime = 0;

                if (femaleLockToggle == null)
                {
                    slideScriptNeedsInitialize = true;
                    return;
                }

                // Show tip over icon
                iconHelp.SetActive(IsMouseOverButton());

                // Check if Slider UI is disabled
                if (!lockToggle.isOn) return;
                if (!sliderGroup.gameObject.active) ToggleSliderMenu(true);
            }

            private bool IsMouseOverButton()
            {
                pointerEvent.position = Input.mousePosition;
                EventSystem.current.RaycastAll(pointerEvent, raycastResults);
                isHit = false;

                for (int i = 0; i < raycastResults.Count; i++)
                {
                    if (raycastResults[i].gameObject.name.Equals("lockBackground", StringComparison.Ordinal)) isHit = true;
                }

                return isHit;
            }
        }
    }
}
