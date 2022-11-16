using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils.Collections;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.User;
using Object = UnityEngine.Object;
using System.IO;
using System.Globalization;
using HarmonyLib;
using RG.Scene.Action.UI;
using System.Runtime.CompilerServices;
using Il2CppSystem.Globalization;
using UnityEngine.Playables;
using CultureInfo = System.Globalization.CultureInfo;

namespace RG_Cheats
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RG_Cheats : BasePlugin
    {
        public const string PluginName = "RG_Cheats";
        public const string GUID = "SpockBauru.RG.Cheats";
        public const string Version = "0.1";

        static internal ConfigEntry<bool> Enable;

        public static Actor charaStatus;

        private static AssetBundle bundle;
        private static Canvas cheatCanvas;
        public static Text title;

        public static InputField stamina;
        public static InputField money;

        public static Button apply;

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
        }

        private static class Hooks
        {
            static string activeCharacter = null;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.Start))]
            private static void StartUI()
            {
                Debug.Log("===================START=====================");
                if (bundle == null) bundle = AssetBundle.LoadFromMemory(CheatsResources.cheatcanvas);
                cheatCanvas = RG_Cheats.InstantiateFromBundle(bundle, "CheatCanvas").GetComponent<Canvas>();
                cheatCanvas.gameObject.SetActive(false);

                title = cheatCanvas.transform.FindChild("Title").GetComponent<Text>();
                title.text = "Room Girl Cheats v" + Version.ToString();
                CircleText(title, 3, new Color(0, 0.5412f, 0.6549f, 0.5f), new Vector2(3.1f, - 3.2f));

                stamina = cheatCanvas.transform.FindChild("Stamina").GetComponent<InputField>();
                stamina.contentType = InputField.ContentType.IntegerNumber;
                stamina.characterLimit = 3;

                money = cheatCanvas.transform.FindChild("Money").GetComponent<InputField>();
                money.contentType = InputField.ContentType.IntegerNumber;
                money.characterLimit = 4;

                apply = cheatCanvas.transform.FindChild("Apply").GetComponent<Button>();
                apply.onClick.AddListener((UnityAction)UpdateCharaStatus);
            }

            // Select Character in the Menu
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CharaSelectOption), nameof(CharaSelectOption.ChangeButtonState))]
            private static void ButtonStateUI(CharaSelectOption __instance, CharaSelectOption.ButtonState btnState)
            {
                string thisCharacter = __instance.Owner.name;

                if (btnState == CharaSelectOption.ButtonState.Select)
                {
                    activeCharacter = thisCharacter;
                    cheatCanvas.gameObject.SetActive(true);

                    charaStatus = __instance.Owner;
                    UpdateCanvasValues(charaStatus);
                }

                if (btnState == CharaSelectOption.ButtonState.Deselect &&
                    thisCharacter.Equals(activeCharacter))
                {
                    activeCharacter = null;
                    cheatCanvas.gameObject.SetActive(false);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StatusUI), nameof(StatusUI.UpdateUI))]
            private static void UpdateStatus(Actor actor)
            {
                if (activeCharacter.Equals(actor.name))
                {
                    UpdateCanvasValues(actor);
                }
            }
        }

        public static void UpdateCharaStatus()
        {
            float staminaFloat = float.Parse(stamina.text, CultureInfo.InvariantCulture.NumberFormat);
            //Stamina is Parameter 0
            charaStatus._status.Parameters[0] = Mathf.Clamp(staminaFloat, 0, 100);


            // Money is Parameter 1
            float moneyFloat = float.Parse(money.text, CultureInfo.InvariantCulture.NumberFormat);
            charaStatus._status.Parameters[1] = Mathf.Clamp(moneyFloat, 0, 1000000);

            //Update UI with current status
            StatusUI statusUI = StatusUI.FindObjectOfType<StatusUI>();
            statusUI.UpdateUI(charaStatus);
        }

        public static void UpdateCanvasValues(Actor status)
        {
            stamina.text = status._status.Parameters[0].ToString("0");
            money.text = status._status.Parameters[1].ToString();
        }

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

        public static void CircleText(Text text, int circlecount, Color color, Vector2 distance)
        {
            _ = text.gameObject.AddComponent<CircleOutline>();
            CircleOutline outline = (CircleOutline)text.GetComponent<CircleOutline>();
            outline.CircleCount = circlecount;
            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }

    //public class MonoBehaviourCheats : MonoBehaviour
    //{
    //    public MonoBehaviourCheats(IntPtr handle) : base(handle) { }

    //    private WaitForSeconds oneSecond = new WaitForSeconds(1f);

    //    GameObject actionSceneObject;
    //    ActionScene actionSceneScript;

    //    List<Actor> womenList;
    //    List<Actor> menList;

    //    Actor character;
    //    int charaCount;
    //    Status charaStatus;

    //    //public static Canvas cheatCanvas;
    //    //AssetBundle bundle;

    //    private void Start()
    //    {
    //        SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>(OnSceneLoaded));
    //    }

    //    private void OnSceneLoaded(Scene scene, LoadSceneMode lsm)
    //    {
    //        if (!RG_Cheats.Enable.Value) return;
    //        if (scene.name != "Action") return;

    //        actionSceneObject = GameObject.Find("ActionScene");
    //        actionSceneScript = actionSceneObject.GetComponent<ActionScene>();
    //        StartCoroutine(ActionScene(actionSceneScript).WrapToIl2Cpp());

    //        //if (bundle == null) bundle = AssetBundle.LoadFromMemory(CheatsResources.cheatcanvas);
    //        //cheatCanvas = RG_Cheats.InstantiateFromBundle(bundle, "CheatCanvas").GetComponent<Canvas>();
    //        //cheatCanvas.gameObject.SetActive(false);
    //    }

    //    private IEnumerator ActionScene(ActionScene actionScene)
    //    {
    //        while (RG_Cheats.Enable.Value)
    //        {
    //            yield return oneSecond;
    //            if (actionScene == null) yield break;

    //            womenList = actionScene._femaleActors;
    //            if (womenList.Count > 0) ApplyCheats(womenList) ;

    //            menList = actionScene._maleActors;
    //            if (menList.Count > 0) ApplyCheats(menList);
    //        }
    //    }

    //    private void ApplyCheats(List<Actor> charaList)
    //    {
    //        charaCount = charaList.Count;
    //        for (int i = 0; i < charaCount; i++)
    //        {
    //            character = charaList[i];
    //            charaStatus = character._status;

    //            // Parameter 0 is Stamina
    //            if (RG_Cheats.InfiniteStamina.Value) charaStatus.Parameters[0] = 100;

    //            // Parameter 1 is Money
    //            if (RG_Cheats.InfiniteMoney.Value) charaStatus.Parameters[1] = 9999;
    //        }
    //    }

    //}
}
