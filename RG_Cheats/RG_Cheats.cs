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
using RG.Scene;
using RG.Scene.Action.Core;
using RG.User;


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
        static internal ConfigEntry<bool> InfiniteStamina;
        static internal ConfigEntry<bool> InfiniteMoney;
        public GameObject SpockBauru;

        public override void Load()
        {
            Enable = Config.Bind("General",
                                 "Enable Cheats",
                                 false,
                                 "Reload the game to activate");
            InfiniteStamina = Config.Bind("General",
                                          "Infinite Stamina",
                                          false,
                                          "Refill your Stamina bar after while");
            InfiniteMoney = Config.Bind("General",
                                        "Infinite Money",
                                        false,
                                        "Refill your Money up to 9999");

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<MonoBehaviourCheats>();

            // Add the monobehavior component to your personal GameObject. Try to not duplicate.
            SpockBauru = GameObject.Find("SpockBauru");
            if (SpockBauru == null)
            {
                SpockBauru = new GameObject("SpockBauru");
                GameObject.DontDestroyOnLoad(SpockBauru);
                SpockBauru.hideFlags = HideFlags.HideAndDontSave;
                SpockBauru.AddComponent<MonoBehaviourCheats>();
            }
            else SpockBauru.AddComponent<MonoBehaviourCheats>();
        }
    }

    public class MonoBehaviourCheats : MonoBehaviour
    {
        public MonoBehaviourCheats(IntPtr handle) : base(handle) { }

        private WaitForSeconds oneSecond = new WaitForSeconds(1f);

        GameObject actionSceneObject;
        ActionScene actionSceneScript;

        List<Actor> womenList;
        List<Actor> menList;

        Actor character;
        int charaCount;
        Status charaStatus;

        private void Start()
        {
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>(OnSceneLoaded));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode lsm)
        {
            if (!RG_Cheats.Enable.Value) return;
            if (scene.name != "Action") return;

            actionSceneObject = GameObject.Find("ActionScene");
            actionSceneScript = actionSceneObject.GetComponent<ActionScene>();
            StartCoroutine(ActionScene(actionSceneScript).WrapToIl2Cpp());
        }

        private IEnumerator ActionScene(ActionScene actionScene)
        {
            while (RG_Cheats.Enable.Value)
            {
                yield return oneSecond;
                if (actionScene == null) yield break;

                womenList = actionScene._femaleActors;
                if (womenList.Count > 0) ApplyCheats(womenList) ;

                menList = actionScene._maleActors;
                if (menList.Count > 0) ApplyCheats(menList);
            }
        }

        private void ApplyCheats(List<Actor> charaList)
        {
            charaCount = charaList.Count;
            for (int i = 0; i < charaCount; i++)
            {
                character = charaList[i];
                charaStatus = character._status;

                // Parameter 0 is Stamina
                if (RG_Cheats.InfiniteStamina.Value) charaStatus.Parameters[0] = 100;

                // Parameter 1 is Money
                if (RG_Cheats.InfiniteMoney.Value) charaStatus.Parameters[1] = 9999;
            }
        }
    }
}
