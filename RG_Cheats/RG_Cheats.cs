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

namespace RG_Cheats
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RG_Cheats : BasePlugin
    {
        public const string PluginName = "RG_Cheats";
        public const string GUID = "SpockBauru.RG.Cheats";
        public const string Version = "0.3";

        internal static ConfigEntry<bool> Enable;
        internal static ConfigEntry<bool> RefillStanima;

        public static Actor character;
        private static UserFile userFile;
        public static StatusUI statusUI;
        private static GeneralUI generalUI;
        public static string currentCharacter;
        private static string oldCharacter;

        private static AssetBundle bundle;
        private static GameObject cheatUI;
        private static Canvas canvas01;
        private static Canvas canvas02;
        private static Text title;

        private static InputField stamina;
        private static InputField money;
        private static InputField roomPoints;
        private static InputField expertise;
        private static InputField hobby;
        private static InputField social;
        private static InputField romance;
        private static InputField appeal;

        private static Button open;
        private static Button close;
        private static Button apply;
        private static Toggle toggleRefillStamina;
        private static Button apply2;

        public override void Load()
        {
            Enable = Config.Bind("General",
                                 "Enable Cheats",
                                 true,
                                 "Reload the game to Enable/Disable");

            RefillStanima = Config.Bind("General",
                                        "Refill Stamina",
                                        false,
                                        "Auto refill the stamina level of the select character up to 100%");

            if (Enable.Value)
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
                cheatUI = RG_Cheats.InstantiateFromBundle(bundle, "CheatCanvas");


                //=========================== Cheat Canvas 1 (top) =================================
                canvas01 = cheatUI.transform.FindChild("CheatCanvas1").GetComponent<Canvas>();
                canvas01.transform.SetParent(canvasStatusUI.transform, false);

                title = canvas01.transform.FindChild("Title").GetComponent<Text>();
                title.text = "Room Girl Cheats v" + Version.ToString();
                CircleText(title, 3, new Color(0, 0.5412f, 0.6549f, 0.5f), new Vector2(3.1f, -3.2f));

                stamina = canvas01.transform.FindChild("Stamina").GetComponent<InputField>();
                money = canvas01.transform.FindChild("Money").GetComponent<InputField>();
                roomPoints = canvas01.transform.FindChild("RoomPoints").GetComponent<InputField>();

                open = cheatUI.transform.FindChild("Open").GetComponent<Button>();
                open.transform.SetParent(canvasStatusUI.transform.parent, false);
                open.onClick.AddListener((UnityAction)OpenClose);

                close = canvas01.transform.FindChild("Close").GetComponent<Button>();
                close.onClick.AddListener((UnityAction)OpenClose);

                toggleRefillStamina = canvas01.transform.FindChild("RefillStamina").GetComponent<Toggle>();
                if (RefillStanima.Value) toggleRefillStamina.isOn = true;
                toggleRefillStamina.onValueChanged.AddListener((UnityAction<bool>)ToggleRefillStaminaChanged);

                apply = canvas01.transform.FindChild("Apply").GetComponent<Button>();
                apply.onClick.AddListener((UnityAction)UpdateCharaStatus);


                //=========================== Cheat Canvas 2 (Satus Menu) ================================
                Canvas otherInfo = canvasStatusUI.transform.FindChild("OtherInfo/MoveContent").GetComponent<Canvas>();

                canvas02 = cheatUI.transform.FindChild("CheatCanvas2").GetComponent<Canvas>();
                canvas02.transform.SetParent(otherInfo.transform, false);

                title = canvas02.transform.FindChild("Title").GetComponent<Text>();
                CircleText(title, 3, new Color(0, 0.5412f, 0.6549f, 0.5f), new Vector2(3.1f, -3.2f));

                expertise = canvas02.transform.FindChild("Expertise").GetComponent<InputField>();
                hobby = canvas02.transform.FindChild("Hobby").GetComponent<InputField>();
                social = canvas02.transform.FindChild("Social").GetComponent<InputField>();
                romance = canvas02.transform.FindChild("Romance").GetComponent<InputField>();
                appeal = canvas02.transform.FindChild("Appeal").GetComponent<InputField>();

                apply2 = canvas02.transform.FindChild("Apply2").GetComponent<Button>();
                apply2.onClick.AddListener((UnityAction)UpdateCharaStatus);
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

                if (currentCharacter.Equals(character.name))
                {
                    if (toggleRefillStamina.isOn) character._status.Parameters[0] = 100;
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
            float.TryParse(stamina.text, out tempFloat);
            character._status.Parameters[0] = Mathf.Clamp(tempFloat, 0, 100);

            // Money is Parameter 1
            float.TryParse(money.text, out tempFloat);
            character._status.Parameters[1] = Mathf.Clamp(tempFloat, 0, 999999);

            // Room Points are inside userFile
            int oldRoomPoints = userFile.RoomPoint;
            int newRoomPoints;
            int.TryParse(roomPoints.text, out newRoomPoints);
            userFile.RoomPoint = Mathf.Clamp(newRoomPoints, 0, 999999);

            // Expertise is Parameter 22
            float.TryParse(expertise.text, out tempFloat);
            character._status.Parameters[22] = Mathf.Clamp(tempFloat, 0, 900);

            // Hobby is Parameter 23
            float.TryParse(hobby.text, out tempFloat);
            character._status.Parameters[23] = Mathf.Clamp(tempFloat, 0, 900);

            // Social is Parameter 24
            float.TryParse(social.text, out tempFloat);
            character._status.Parameters[24] = Mathf.Clamp(tempFloat, 0, 900);

            // Romance is Parameter 25
            float.TryParse(romance.text, out tempFloat);
            character._status.Parameters[25] = Mathf.Clamp(tempFloat, 0, 900);

            // Appeal is Parameter 26
            float.TryParse(appeal.text, out tempFloat);
            character._status.Parameters[26] = Mathf.Clamp(tempFloat, 0, 900);

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

            stamina.text = character._status.Parameters[0].ToString("0");
            money.text = character._status.Parameters[1].ToString();
            roomPoints.text = userFile.RoomPoint.ToString();
            expertise.text = character._status.Parameters[22].ToString("0");
            hobby.text = character._status.Parameters[23].ToString("0");
            social.text = character._status.Parameters[24].ToString("0");
            romance.text = character._status.Parameters[25].ToString("0");
            appeal.text = character._status.Parameters[26].ToString("0");
        }

        private static void OpenClose()
        {
            if (canvas01.gameObject.active)
            {
                canvas01.gameObject.SetActive(false);
                open.gameObject.SetActive(true);
            }
            else
            {
                canvas01.gameObject.SetActive(true);
                open.gameObject.SetActive(false);
            }
        }

        private static void ToggleRefillStaminaChanged(bool state)
        {
            RefillStanima.Value = state;
            if (state) statusUI.UpdateUI(character);
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
            _ = text.gameObject.AddComponent<CircleOutline>();
            CircleOutline outline = (CircleOutline)text.GetComponent<CircleOutline>();
            outline.CircleCount = circlecount;
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
