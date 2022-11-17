using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using RG.Scene.Action.Core;
using RG.User;
using System.IO;
using HarmonyLib;
using RG.Scene.Action.UI;
using RG.Scene.Home.UI;

using Object = UnityEngine.Object;
using CultureInfo = System.Globalization.CultureInfo;

namespace RG_Cheats
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RG_Cheats : BasePlugin
    {
        public const string PluginName = "RG_Cheats";
        public const string GUID = "SpockBauru.RG.Cheats";
        public const string Version = "0.2.1";

        static internal ConfigEntry<bool> Enable;

        public static Actor charaStatus;
        public static UserFile userFile;
        public static StatusUI statusUI;

        private static AssetBundle bundle;
        private static GameObject cheatUI;
        private static Canvas canvas01;
        public static Text title;

        public static InputField stamina;
        public static InputField money;
        public static InputField roomPoints;

        public static Button apply;
        public static Button close;

        public override void Load()
        {
            Enable = Config.Bind("General",
                                 "Enable Cheats",
                                 true,
                                 "Reload the game to Enable/Disable");
            if (Enable.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<CheatComponent>();

        }

        public static class Hooks
        {
            public static string currentCharacter = null;
            public static string oldCharacter = null;

            // Loading Cheats Menu
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.Start))]
            private static void StartUI(StatusUI __instance)
            {
                statusUI = __instance;
                Canvas canvasStatusUI = statusUI.transform.FindChild("MoveArea").GetComponent<Canvas>();

                if (bundle == null) bundle = AssetBundle.LoadFromMemory(CheatsResources.cheatcanvas);
                cheatUI = RG_Cheats.InstantiateFromBundle(bundle, "CheatCanvas");

                canvas01 = cheatUI.transform.FindChild("SubCanvas1").GetComponent<Canvas>();
                canvas01.transform.SetParent(canvasStatusUI.transform, false);
                canvas01.gameObject.AddComponent<CheatComponent>();

                title = canvas01.transform.FindChild("Title").GetComponent<Text>();
                title.text = "Room Girl Cheats v" + Version.ToString();
                CircleText(title, 3, new Color(0, 0.5412f, 0.6549f, 0.5f), new Vector2(3.1f, -3.2f));

                stamina = canvas01.transform.FindChild("Stamina").GetComponent<InputField>();
                stamina.contentType = InputField.ContentType.IntegerNumber;
                stamina.characterLimit = 3;

                money = canvas01.transform.FindChild("Money").GetComponent<InputField>();
                money.contentType = InputField.ContentType.IntegerNumber;
                money.characterLimit = 6;

                roomPoints = canvas01.transform.FindChild("RoomPoints").GetComponent<InputField>();
                roomPoints.contentType = InputField.ContentType.IntegerNumber;
                roomPoints.characterLimit = 6;

                apply = canvas01.transform.FindChild("Apply").GetComponent<Button>();
                apply.onClick.AddListener((UnityAction)UpdateCharaStatus);

                close = canvas01.transform.FindChild("Close").GetComponent<Button>();
                close.onClick.AddListener((UnityAction)delegate { canvas01.gameObject.SetActive(false); });

                Debug.Log("==================Start==================");
            }

            // Get the selected character
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CharaSelectOption), nameof(CharaSelectOption.ChangeButtonState))]
            private static void ButtonStateUI(CharaSelectOption __instance,CharaSelectOption.ButtonState btnState)
            {
                if (btnState == CharaSelectOption.ButtonState.Select)
                {
                    //charaStatus = statusUI.Target;
                    charaStatus = __instance.Owner;
                    currentCharacter = charaStatus.name;

                    if (currentCharacter != oldCharacter)
                    {
                        oldCharacter = currentCharacter;
                        UpdateCheatCanvas(charaStatus);
                    }
                }
            }

            // Update Cheats Menu when Status canvas is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.UpdateUI))]
            private static void UpdateStatus(Actor actor)
            {
                if (currentCharacter.Equals(actor.name))
                {
                    UpdateCheatCanvas(actor);
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
        public static void UpdateCharaStatus()
        {
            //Stamina is Parameter 0
            float staminaFloat = float.Parse(stamina.text, CultureInfo.InvariantCulture.NumberFormat);
            charaStatus._status.Parameters[0] = Mathf.Clamp(staminaFloat, 0, 100);

            // Money is Parameter 1
            float moneyFloat = float.Parse(money.text, CultureInfo.InvariantCulture.NumberFormat);
            charaStatus._status.Parameters[1] = Mathf.Clamp(moneyFloat, 0, 999999);

            // Room Points are inside userFile
            int oldRoomPoints = userFile.RoomPoint;
            int newRoomPoints = int.Parse(roomPoints.text, CultureInfo.InvariantCulture.NumberFormat);
            userFile.RoomPoint = Mathf.Clamp(newRoomPoints, 0, 999999);

            //Update UI with current status
            StatusUI statusUI = StatusUI.FindObjectOfType<StatusUI>();
            statusUI.UpdateUI(charaStatus);

            // Update Room Point UI
            if (oldRoomPoints != newRoomPoints)
            {
                bool isPositive = oldRoomPoints <= newRoomPoints;
                GeneralUI generalUI = GeneralUI.FindObjectOfType<GeneralUI>();
                generalUI.ApplyRoomPointUI(isPositive);
            }
        }

        // Updating Cheats Menu
        public static void UpdateCheatCanvas(Actor status)
        {
            stamina.text = status._status.Parameters[0].ToString("0");
            money.text = status._status.Parameters[1].ToString();
            roomPoints.text = userFile.RoomPoint.ToString();
        }

        // Because everything is harder with IL2CPP :(
        public static GameObject InstantiateFromBundle(AssetBundle bundle, string assetName)
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
        public static void CircleText(Text text, int circlecount, Color color, Vector2 distance)
        {
            _ = text.gameObject.AddComponent<CircleOutline>();
            CircleOutline outline = (CircleOutline)text.GetComponent<CircleOutline>();
            outline.CircleCount = circlecount;
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
