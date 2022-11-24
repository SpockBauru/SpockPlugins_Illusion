using System;
using System.IO;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnhollowerRuntimeLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using RG.Scene.Action.Core;
using RG.Scene.Action.UI;
using RG.Scene.Home.UI;
using RG.User;

using Object = UnityEngine.Object;

namespace RG_Cheats
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RG_CheatsPlugin : BasePlugin
    {
        public const string PluginName = "RG Cheats";
        public const string GUID = "SpockBauru.RG.Cheats";
        public const string Version = "0.3";

        internal static ConfigEntry<bool> EnableConfig;
        internal static ConfigEntry<bool> RefillStanimaConfig;

        private static Actor character;
        private static UserFile userFile;
        private static StatusUI statusUI;
        private static GeneralUI generalUI;
        private static string currentCharacter;
        private static string oldCharacter;

        private static AssetBundle bundle;
        private static GameObject cheatUI;
        private static Canvas canvas01;
        private static Canvas canvas02;
        private static GameObject advancedMenu;
        private static Text title;

        private static InputField staminaInput;
        private static InputField moneyInput;
        private static InputField roomPointsInput;
        private static InputField expertiseInput;
        private static InputField hobbyInput;
        private static InputField socialInput;
        private static InputField romanceInput;
        private static InputField appealInput;
        private static InputField hExpInput;

        private static InputField satisfactionInput;
        private static InputField dissatisfactionInput;
        private static InputField seriousInput;
        private static InputField playfulInput;
        private static InputField eccentricInput;
        private static InputField sleepinessInput;
        private static InputField fatigueInput;
        private static InputField bladderInput;
        private static InputField talkInput;
        private static InputField romanceInput2;
        private static InputField brokenInput;
        private static InputField libidoInput;

        private static Button openButton;
        private static Button closeButton;
        private static Button applyButton;
        private static Toggle refillStaminaToggle;
        private static Toggle advancedToggle;
        private static Button apply2Button;
        private static Image canvas02Background;
        private static Image canvas02Tiled;

        public override void Load()
        {
            EnableConfig = Config.Bind("General",
                                 "Enable Cheats",
                                 true,
                                 "Reload the game to Enable/Disable");

            RefillStanimaConfig = Config.Bind("General",
                                        "Refill Stamina",
                                        false,
                                        "Auto refill the stamina level of the select character up to 100%");

            if (EnableConfig.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }
        }

        private static class Hooks
        {
            // Loading Cheats Menu
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.Start))]
            private static void StartUI(StatusUI __instance)
            {
                oldCharacter = null;
                currentCharacter = null;

                generalUI = GeneralUI.FindObjectOfType<GeneralUI>();
                statusUI = __instance;
                Canvas canvasStatusUI = statusUI.transform.FindChild("MoveArea").GetComponent<Canvas>();

                if (bundle == null) bundle = AssetBundle.LoadFromMemory(CheatsResources.cheatcanvas);
                cheatUI = RG_CheatsPlugin.InstantiateFromBundle(bundle, "CheatCanvas");


                //=========================== Cheat Canvas 1 (top) =================================
                canvas01 = cheatUI.transform.FindChild("CheatCanvas1").GetComponent<Canvas>();
                canvas01.transform.SetParent(canvasStatusUI.transform, false);

                title = canvas01.transform.FindChild("Title").GetComponent<Text>();
                title.text = "Room Girl Cheats v" + Version.ToString();
                CircleText(title, 3, new Color(0, 0.5412f, 0.6549f, 0.5f), new Vector2(3.1f, -3.2f));

                staminaInput = canvas01.transform.FindChild("Stamina").GetComponent<InputField>();
                moneyInput = canvas01.transform.FindChild("Money").GetComponent<InputField>();
                roomPointsInput = canvas01.transform.FindChild("RoomPoints").GetComponent<InputField>();

                openButton = cheatUI.transform.FindChild("Open").GetComponent<Button>();
                openButton.transform.SetParent(canvasStatusUI.transform.parent, false);
                openButton.onClick.AddListener((UnityAction)OpenClose);

                closeButton = canvas01.transform.FindChild("Close").GetComponent<Button>();
                closeButton.onClick.AddListener((UnityAction)OpenClose);

                refillStaminaToggle = canvas01.transform.FindChild("RefillStamina").GetComponent<Toggle>();
                if (RefillStanimaConfig.Value) refillStaminaToggle.isOn = true;
                refillStaminaToggle.onValueChanged.AddListener((UnityAction<bool>)ToggleRefillStaminaChanged);

                applyButton = canvas01.transform.FindChild("Apply").GetComponent<Button>();
                applyButton.onClick.AddListener((UnityAction)UpdateCharaStatus);


                //=========================== Cheat Canvas 2 (Satus Menu) ================================
                Canvas otherInfo = canvasStatusUI.transform.FindChild("OtherInfo/MoveContent").GetComponent<Canvas>();

                canvas02 = cheatUI.transform.FindChild("CheatCanvas2").GetComponent<Canvas>();
                canvas02.transform.SetParent(otherInfo.transform, false);

                title = canvas02.transform.FindChild("Title").GetComponent<Text>();
                CircleText(title, 3, new Color(0, 0.5412f, 0.6549f, 0.5f), new Vector2(3.1f, -3.2f));

                expertiseInput = canvas02.transform.FindChild("Expertise").GetComponent<InputField>();
                hobbyInput = canvas02.transform.FindChild("Hobby").GetComponent<InputField>();
                socialInput = canvas02.transform.FindChild("Social").GetComponent<InputField>();
                romanceInput = canvas02.transform.FindChild("Romance").GetComponent<InputField>();
                appealInput = canvas02.transform.FindChild("Appeal").GetComponent<InputField>();
                hExpInput = canvas02.transform.FindChild("HExperience").GetComponent<InputField>();

                apply2Button = canvas02.transform.FindChild("Apply2").GetComponent<Button>();
                apply2Button.onClick.AddListener((UnityAction)UpdateCharaStatus);

                //=========================== Cheat Canvas 2 - Advanced Mode ================================
                canvas02Background = canvas02.transform.FindChild("BackGround").GetComponent<Image>();
                canvas02Tiled = canvas02.transform.FindChild("BkgTextTiled").GetComponent<Image>();

                advancedToggle = canvas02.transform.FindChild("AdvancedToggle").GetComponent<Toggle>();
                advancedToggle.onValueChanged.AddListener((UnityAction<bool>)ToggleAdvancedChanged);

                advancedMenu = canvas02.transform.FindChild("Advanced").gameObject;

                satisfactionInput = advancedMenu.transform.FindChild("Satisfaction").GetComponent<InputField>();
                dissatisfactionInput = advancedMenu.transform.FindChild("Dissatisfaction").GetComponent<InputField>();
                seriousInput = advancedMenu.transform.FindChild("Serious").GetComponent<InputField>();
                playfulInput = advancedMenu.transform.FindChild("Playful").GetComponent<InputField>();
                eccentricInput = advancedMenu.transform.FindChild("Eccentric").GetComponent<InputField>();
                sleepinessInput = advancedMenu.transform.FindChild("Sleepiness").GetComponent<InputField>();
                fatigueInput = advancedMenu.transform.FindChild("Fatigue").GetComponent<InputField>();
                bladderInput = advancedMenu.transform.FindChild("Bladder").GetComponent<InputField>();
                talkInput = advancedMenu.transform.FindChild("Talk").GetComponent<InputField>();
                romanceInput2 = advancedMenu.transform.FindChild("Romance").GetComponent<InputField>();
                brokenInput = advancedMenu.transform.FindChild("Broken").GetComponent<InputField>();
                libidoInput = advancedMenu.transform.FindChild("Libido").GetComponent<InputField>();

            }

            // Get the selected character
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CharaSelectOption), nameof(CharaSelectOption.ChangeButtonState))]
            private static void ButtonStateChanged(CharaSelectOption.ButtonState btnState, CharaSelectOption __instance)
            {
                if (__instance.Owner == null) return;
                if (btnState != CharaSelectOption.ButtonState.Select) return;

                character = __instance.Owner;
                currentCharacter = character.name;

                if (currentCharacter != oldCharacter)
                {
                    oldCharacter = currentCharacter;
                    statusUI.UpdateUI(character);
                }
            }

            // Update Cheats Menu when Status canvas is updated 
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.UpdateUI))]
            private static void UpdateStatusUI()
            {
                if (character._status == null) return;

                if (currentCharacter.Equals(character.name, StringComparison.Ordinal))
                {
                    if (refillStaminaToggle.isOn) character._status.Parameters[0] = 100;
                    UpdateCheatCanvas(character);
                }
            }

            // Getting Current User File, RoomPoints are inside
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Manager.Game), nameof(Manager.Game.Initialize))]
            private static void GetUserFile()
            {
                userFile = Manager.Game.UserFile;
            }
        }


        // The character status are inside a huge enum of Parameters
        private static void UpdateCharaStatus()
        {
            float tempFloat;

            // Stamina is Parameter 0
            float.TryParse(staminaInput.text, out tempFloat);
            character._status.Parameters[0] = Mathf.Clamp(tempFloat, 0, 100);

            // Money is Parameter 1
            float.TryParse(moneyInput.text, out tempFloat);
            character._status.Parameters[1] = Mathf.Clamp(tempFloat, 0, 999999);

            // Room Points are inside userFile
            int oldRoomPoints = userFile.RoomPoint;
            int newRoomPoints;
            int.TryParse(roomPointsInput.text, out newRoomPoints);
            userFile.RoomPoint = Mathf.Clamp(newRoomPoints, 0, 999999);

            // Expertise is Parameter 22
            float.TryParse(expertiseInput.text, out tempFloat);
            character._status.Parameters[22] = Mathf.Clamp(tempFloat, 0, 900);

            // Hobby is Parameter 23
            float.TryParse(hobbyInput.text, out tempFloat);
            character._status.Parameters[23] = Mathf.Clamp(tempFloat, 0, 900);

            // Social is Parameter 24
            float.TryParse(socialInput.text, out tempFloat);
            character._status.Parameters[24] = Mathf.Clamp(tempFloat, 0, 900);

            // Romance is Parameter 25
            float.TryParse(romanceInput.text, out tempFloat);
            character._status.Parameters[25] = Mathf.Clamp(tempFloat, 0, 900);

            // Appeal is Parameter 26
            float.TryParse(appealInput.text, out tempFloat);
            character._status.Parameters[26] = Mathf.Clamp(tempFloat, 0, 900);

            // H-Experience is Parameter 4
            float.TryParse(hExpInput.text, out tempFloat);
            character._status.Parameters[4] = Mathf.Clamp(tempFloat, 0, 100);

            // Satisfaction is Parameter 2
            float.TryParse(satisfactionInput.text, out tempFloat);
            character._status.Parameters[2] = Mathf.Clamp(tempFloat, 0, 100);

            // Dissatisfaction is Parameter 3
            float.TryParse(dissatisfactionInput.text, out tempFloat);
            character._status.Parameters[3] = Mathf.Clamp(tempFloat, 0, 100);

            // Serious is Parameter 5
            float.TryParse(seriousInput.text, out tempFloat);
            character._status.Parameters[5] = Mathf.Clamp(tempFloat, 0, 100);

            // Playful is Parameter 6
            float.TryParse(playfulInput.text, out tempFloat);
            character._status.Parameters[6] = Mathf.Clamp(tempFloat, 0, 100);

            // Eccentric is Parameter 7
            float.TryParse(eccentricInput.text, out tempFloat);
            character._status.Parameters[7] = Mathf.Clamp(tempFloat, 0, 100);

            // Sleepiness is Parameter 8
            float.TryParse(sleepinessInput.text, out tempFloat);
            character._status.Parameters[8] = Mathf.Clamp(tempFloat, 0, 100);

            // Fatigue is Parameter 9
            float.TryParse(fatigueInput.text, out tempFloat);
            character._status.Parameters[9] = Mathf.Clamp(tempFloat, 0, 100);

            // Bladder is Parameter 10
            float.TryParse(bladderInput.text, out tempFloat);
            character._status.Parameters[10] = Mathf.Clamp(tempFloat, 0, 99);

            // Talk is Parameter 11
            float.TryParse(talkInput.text, out tempFloat);
            character._status.Parameters[11] = Mathf.Clamp(tempFloat, 0, 100);

            // Romance2 is Parameter 12
            float.TryParse(romanceInput2.text, out tempFloat);
            character._status.Parameters[12] = Mathf.Clamp(tempFloat, 0, 100);

            // Broken is Parameter 13
            float.TryParse(brokenInput.text, out tempFloat);
            character._status.Parameters[13] = Mathf.Clamp(tempFloat, 0, 100);

            // Libido is Parameter 14
            float.TryParse(libidoInput.text, out tempFloat);
            character._status.Parameters[14] = Mathf.Clamp(tempFloat, 0, 100);



            // Update UI with current status
            statusUI.UpdateUI(character);

            // Update Room Point UI
            if (oldRoomPoints != newRoomPoints)
            {
                bool isPositive = oldRoomPoints <= newRoomPoints;
                generalUI.ApplyRoomPointUI(isPositive);
            }
        }

        // Updating Cheats Menu
        public static void UpdateCheatCanvas(Actor character)
        {
            if (character._status == null) return;

            staminaInput.text = character._status.Parameters[0].ToString("0");
            moneyInput.text = character._status.Parameters[1].ToString();
            roomPointsInput.text = userFile.RoomPoint.ToString();
            expertiseInput.text = character._status.Parameters[22].ToString("0");
            hobbyInput.text = character._status.Parameters[23].ToString("0");
            socialInput.text = character._status.Parameters[24].ToString("0");
            romanceInput.text = character._status.Parameters[25].ToString("0");
            appealInput.text = character._status.Parameters[26].ToString("0");
            satisfactionInput.text = character._status.Parameters[2].ToString("0");
            dissatisfactionInput.text = character._status.Parameters[3].ToString("0");
            hExpInput.text = character._status.Parameters[4].ToString("0");
            seriousInput.text = character._status.Parameters[5].ToString("0");
            playfulInput.text = character._status.Parameters[6].ToString("0");
            eccentricInput.text = character._status.Parameters[7].ToString("0");
            sleepinessInput.text = character._status.Parameters[8].ToString("0");
            fatigueInput.text = character._status.Parameters[9].ToString("0");
            bladderInput.text = character._status.Parameters[10].ToString("0");
            talkInput.text = character._status.Parameters[11].ToString("0");
            romanceInput2.text = character._status.Parameters[12].ToString("0");
            brokenInput.text = character._status.Parameters[13].ToString("0");
            libidoInput.text = character._status.Parameters[14].ToString("0");
        }

        private static void OpenClose()
        {
            if (canvas01.gameObject.active)
            {
                canvas01.gameObject.SetActive(false);
                openButton.gameObject.SetActive(true);
            }
            else
            {
                canvas01.gameObject.SetActive(true);
                openButton.gameObject.SetActive(false);
            }
        }

        private static void ToggleRefillStaminaChanged(bool state)
        {
            RefillStanimaConfig.Value = state;
            if (state) statusUI.UpdateUI(character);
        }

        private static void ToggleAdvancedChanged(bool state)
        {
            if (state)
            {
                canvas02Background.rectTransform.sizeDelta = new Vector2(640f, 890f);
                canvas02Tiled.rectTransform.sizeDelta = new Vector2(610f, 659f);
            }
            else
            {
                canvas02Background.rectTransform.sizeDelta = new Vector2(250f, 890f);
                canvas02Tiled.rectTransform.sizeDelta = new Vector2(225f, 659f);
            }
            advancedMenu.SetActive(state);
        }

        // Because everything is harder with IL2CPP :(
        private static GameObject InstantiateFromBundle(AssetBundle bundle, string assetName)
        {
            var asset = bundle.LoadAsset(assetName, Il2CppType.From(typeof(GameObject)));
            var obj = Object.Instantiate(asset);

            foreach (var rootGameObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGameObject.GetInstanceID() == obj.GetInstanceID())
                {
                    rootGameObject.name = assetName;
                    return rootGameObject;
                }
            }

            throw new FileLoadException("Could not instantiate asset " + assetName);
        }

        // Fancy text contour from Illusion code
        private static void CircleText(Text text, int circlecount, Color color, Vector2 distance)
        {
            text.gameObject.AddComponent<CircleOutline>();
            CircleOutline outline = (CircleOutline)text.GetComponent<CircleOutline>();
            outline.CircleCount = circlecount;
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
