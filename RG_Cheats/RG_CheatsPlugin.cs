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

using RG;
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
        private static InputField sexperienceInput;

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
        private static InputField homeInput;
        private static InputField casinoInput;
        private static InputField cafeInput;
        private static InputField parkInput;
        private static InputField restaurantInput;
        private static InputField hotelInput;
        private static InputField eventInput;

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

                apply2Button = canvas02.transform.FindChild("Apply2").GetComponent<Button>();
                apply2Button.onClick.AddListener((UnityAction)UpdateCharaStatus);

                //=========================== Cheat Canvas 2 - Advanced Mode ================================
                canvas02Background = canvas02.transform.FindChild("BackGround").GetComponent<Image>();
                canvas02Tiled = canvas02.transform.FindChild("BkgTextTiled").GetComponent<Image>();

                advancedToggle = canvas02.transform.FindChild("AdvancedToggle").GetComponent<Toggle>();
                advancedToggle.onValueChanged.AddListener((UnityAction<bool>)ToggleAdvancedChanged);

                advancedMenu = canvas02.transform.FindChild("Advanced").gameObject;

                sexperienceInput = advancedMenu.transform.FindChild("HExperience").GetComponent<InputField>();
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
                homeInput = advancedMenu.transform.FindChild("Home").GetComponent<InputField>();
                casinoInput = advancedMenu.transform.FindChild("Cassino").GetComponent<InputField>();
                cafeInput = advancedMenu.transform.FindChild("Cafe").GetComponent<InputField>();
                parkInput = advancedMenu.transform.FindChild("Park").GetComponent<InputField>();
                restaurantInput = advancedMenu.transform.FindChild("Restaurant").GetComponent<InputField>();
                hotelInput = advancedMenu.transform.FindChild("Hotel").GetComponent<InputField>();
                eventInput = advancedMenu.transform.FindChild("Event").GetComponent<InputField>();

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
            int parameter;
            float value;

            if (canvas01.gameObject.active)
            {
                Debug.Log("Update canvas01 status");
                // Stamina
                parameter = (int)Define.Action.StatusCategory.Health;
                float.TryParse(staminaInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Money
                parameter = (int)Define.Action.StatusCategory.Money;
                float.TryParse(moneyInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 999999);

                // Room Points are inside userFile
                int oldRoomPoints = userFile.RoomPoint;
                int newRoomPoints;
                int.TryParse(roomPointsInput.text, out newRoomPoints);
                userFile.RoomPoint = Mathf.Clamp(newRoomPoints, 0, 999999);

                // Update Room Point UI
                if (oldRoomPoints != newRoomPoints)
                {
                    bool isPositive = oldRoomPoints <= newRoomPoints;
                    generalUI.ApplyRoomPointUI(isPositive);
                }
            }

            // Expertise
            parameter = (int)Define.Action.StatusCategory.Performance;
            float.TryParse(expertiseInput.text, out value);
            character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 900);

            // Hobby
            parameter = (int)Define.Action.StatusCategory.Hobby;
            float.TryParse(hobbyInput.text, out value);
            character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 900);

            // Social
            parameter = (int)Define.Action.StatusCategory.Sociable;
            float.TryParse(socialInput.text, out value);
            character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 900);

            // Romance (Love status)
            parameter = (int)Define.Action.StatusCategory.Love;
            float.TryParse(romanceInput.text, out value);
            character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 900);

            // Appeal
            parameter = (int)Define.Action.StatusCategory.Sexy;
            float.TryParse(appealInput.text, out value);
            character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 900);

            if (advancedToggle.isOn)
            {
                Debug.Log("Update advanced status");
                // H-Experience
                parameter = (int)Define.Action.StatusCategory.Sexperience;
                float.TryParse(sexperienceInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Satisfaction
                parameter = (int)Define.Action.StatusCategory.Satisfaction;
                float.TryParse(satisfactionInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Dissatisfaction
                parameter = (int)Define.Action.StatusCategory.Dissatisfaction;
                float.TryParse(dissatisfactionInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Serious
                parameter = (int)Define.Action.StatusCategory.Honesty;
                float.TryParse(seriousInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Playful
                parameter = (int)Define.Action.StatusCategory.Naughty;
                float.TryParse(playfulInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Eccentric
                parameter = (int)Define.Action.StatusCategory.Unique;
                float.TryParse(eccentricInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Sleepiness
                parameter = (int)Define.Action.StatusCategory.Sleepiness;
                float.TryParse(sleepinessInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Fatigue
                parameter = (int)Define.Action.StatusCategory.Fatigue;
                float.TryParse(fatigueInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Bladder
                float.TryParse(bladderInput.text, out value);
                parameter = (int)Define.Action.StatusCategory.Bladder;
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Talk
                parameter = (int)Define.Action.StatusCategory.Talk;
                float.TryParse(talkInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Romance2 (Romance urge)
                parameter = (int)Define.Action.StatusCategory.Romance;
                float.TryParse(romanceInput2.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Broken
                parameter = (int)Define.Action.StatusCategory.Broken;
                float.TryParse(brokenInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Libido
                parameter = (int)Define.Action.StatusCategory.Libido;
                float.TryParse(libidoInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Home
                parameter = (int)Define.Action.StatusCategory.Home;
                float.TryParse(homeInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Casino
                parameter = (int)Define.Action.StatusCategory.Casino;
                float.TryParse(casinoInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Cafe
                parameter = (int)Define.Action.StatusCategory.Cafe;
                float.TryParse(cafeInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Park
                parameter = (int)Define.Action.StatusCategory.Park;
                float.TryParse(parkInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Restaurant
                parameter = (int)Define.Action.StatusCategory.Restaurant;
                float.TryParse(restaurantInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Hotel
                parameter = (int)Define.Action.StatusCategory.LoveHotel;
                float.TryParse(hotelInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);

                // Event
                parameter = (int)Define.Action.StatusCategory.Event;
                float.TryParse(eventInput.text, out value);
                character._status.Parameters[parameter] = Mathf.Clamp(value, 0, 100);
            }

            // Update UI with current status
            statusUI.UpdateUI(character);
        }

        // Updating Cheats Menu
        public static void UpdateCheatCanvas(Actor character)
        {
            if (character._status == null) return;

            int parameter;

            if (canvas01.gameObject.active)
            {
                Debug.Log("Update canvas01 canvas");
                parameter = (int)Define.Action.StatusCategory.Health;
                staminaInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Money;
                moneyInput.text = character._status.Parameters[parameter].ToString();

                roomPointsInput.text = userFile.RoomPoint.ToString();
            }

            parameter = (int)Define.Action.StatusCategory.Performance;
            expertiseInput.text = character._status.Parameters[parameter].ToString("0");

            parameter = (int)Define.Action.StatusCategory.Hobby;
            hobbyInput.text = character._status.Parameters[parameter].ToString("0");

            parameter = (int)Define.Action.StatusCategory.Sociable;
            socialInput.text = character._status.Parameters[parameter].ToString("0");

            parameter = (int)Define.Action.StatusCategory.Love;
            romanceInput.text = character._status.Parameters[parameter].ToString("0");

            parameter = (int)Define.Action.StatusCategory.Sexy;
            appealInput.text = character._status.Parameters[parameter].ToString("0");

            if (advancedToggle.isOn)
            {
                Debug.Log("Update advanced canvas");
                parameter = (int)Define.Action.StatusCategory.Satisfaction;
                satisfactionInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Dissatisfaction;
                dissatisfactionInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Sexperience;
                sexperienceInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Honesty;
                seriousInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Naughty;
                playfulInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Unique;
                eccentricInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Sleepiness;
                sleepinessInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Fatigue;
                fatigueInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Bladder;
                bladderInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Talk;
                talkInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Romance;
                romanceInput2.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Broken;
                brokenInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Libido;
                libidoInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Home;
                homeInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Casino;
                casinoInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Cafe;
                cafeInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Park;
                parkInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Restaurant;
                restaurantInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.LoveHotel;
                hotelInput.text = character._status.Parameters[parameter].ToString("0");

                parameter = (int)Define.Action.StatusCategory.Event;
                eventInput.text = character._status.Parameters[parameter].ToString("0");
            }
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
                statusUI.UpdateUI(character);
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
                canvas02Background.rectTransform.sizeDelta = new Vector2(1030f, 775f);
                canvas02Tiled.rectTransform.sizeDelta = new Vector2(1000f, 553f);
                statusUI.UpdateUI(character);
            }
            else
            {
                canvas02Background.rectTransform.sizeDelta = new Vector2(250f, 775f);
                canvas02Tiled.rectTransform.sizeDelta = new Vector2(225f, 553f);
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
