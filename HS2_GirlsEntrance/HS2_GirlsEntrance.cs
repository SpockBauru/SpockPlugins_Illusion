using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using UnityEngine.UI;
using HS2;
using Manager;
using SceneAssist;
using Illusion.Game;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Actor;
using ADV;
using AIChara;
using CameraEffector;
using CharaCustom;
using Illusion.Anime;
using Illusion.Extensions;
using GameLoadCharaFileSystem;
using UIAnimatorCore;
using UnityEngine.EventSystems;
using System.ComponentModel;
using UniRx;

namespace HS2_GirlsEntrance
{
    [BepInProcess("HoneySelect2")]
    [BepInPlugin("spockbauru.hs2.girlsentrance", "HS2_GirlsEntrance", Version)]
    public class HS2_GirlsEntrance : BaseUnityPlugin
    {
        //Set versuon in BepInEx and in AssemblyInfo
        public const string Version = "0.2";

        //User Configurations
        private static ConfigEntry<bool> Enabled;
        private static ConfigEntry<EntranceOption> MakeEntrance;
        private static ConfigEntry<WherePlayAnimation> WherePlay;

        //User can define girl's state to play animation
        private enum EntranceOption
        {
            [Description("On Girl's First Time")]
            FirstTime,
            [Description("Every Time")]
            EveryTime
        }

        //User can define in which place he wants to play the animation
        private enum WherePlayAnimation
        {
            [Description("Before Map Select")]
            MapSelect,
            [Description("When Select a Girl")]
            GirlSelect
        }

        public HS2_GirlsEntrance()
        {
            //Patch Everything
            Harmony.CreateAndPatchAll(typeof(HS2_GirlsEntrance));

            //Config Panel Settings
            Enabled = Config.Bind("General", "Enabled", true, "Whether the plugin is enabled");
            MakeEntrance = Config.Bind("Settings", "When make an entrance", EntranceOption.FirstTime, "When the entrance animation plays");
            WherePlay = Config.Bind("Settings", "Where play the animation", WherePlayAnimation.MapSelect, "Which place do you want to play the animation");
        }

        //Pay the animation on girls's select
        [HarmonyPostfix, HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), "OnValueChange")]
        public static void SetupGirlsEntrance()
        {
            if (Enabled.Value && WherePlay.Value == WherePlayAnimation.GirlSelect)
            {
                EnterAnimation();
            }

        }

        //Play the animation on map select
        [HarmonyPrefix, HarmonyPatch(typeof(LobbyMapSelectUI), "InitList")]
        public static void LoadOnMapSelect()
        {
            if (Enabled.Value && WherePlay.Value == WherePlayAnimation.MapSelect)
            {
                EnterAnimation();
            }
        }

        //Calls the animation
        public static void EnterAnimation()
        {
            //Instance of LobbySceneManager
            LobbySceneManager scene = Singleton<LobbySceneManager>.Instance;
            int sexTimes = scene.heroines[0].gameinfo2.hCount;

            if (MakeEntrance.Value == EntranceOption.FirstTime && sexTimes == 0 ||
                MakeEntrance.Value == EntranceOption.EveryTime)
            {
                //Set ADV file to load
                string bundle = "adv/scenario/op/50/entrance.unity3d";

                //Set Name/PathID inside the file
                string asset = "0";

                //Set first Girl
                Heroine heroine = scene.heroines[0];

                //No idea
                Action onEnd = null;

                //Open the scene
                scene.OpenADV(bundle, asset, heroine, onEnd);
            }
        }
    }
}
