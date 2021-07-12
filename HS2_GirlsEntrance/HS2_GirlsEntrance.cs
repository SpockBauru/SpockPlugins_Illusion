using HarmonyLib;
using UnityEngine;
using System;
using UnityEngine.UI;
using HS2;
using Manager;
using SceneAssist;
using Illusion.Game;
//using UniRx;
//using UniRx.Triggers;
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
using BepInEx;

namespace HS2_GirlsEntrance
{
    [BepInProcess("HoneySelect2")]
    [BepInPlugin("SpockBauru.hs2.GirlsEntrance", "HS2_GirlsEntrance", Version)]
    public class HS2_GirlsEntrance : BaseUnityPlugin
    {
        public const string Version = "0.1";
        public HS2_GirlsEntrance()
        {
            Harmony.CreateAndPatchAll(typeof(HS2_GirlsEntrance));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(LobbyMapSelectUI), "InitList")]
        public static void LobbyFashion()
        {
            Console.WriteLine("============AAAAAAAAAAAAAAAA=================");

            //Instance of LobbySceneManager is needed for all things. I coudn't open an existing one
            LobbySceneManager lm_mod = Singleton<LobbySceneManager>.Instance;
                        
            Console.WriteLine(lm_mod.heroines[0].gameinfo2.hCount);
            // How many times the girl had sex
            int sexTimes = lm_mod.heroines[0].gameinfo2.hCount;

            // If virgin, play animation
            if (sexTimes == 0)
                EnterAnimation(lm_mod);
        }

        public static void EnterAnimation(LobbySceneManager lm_mod)
        {
            Console.WriteLine("============BBBBBBBBBBBBBBBB=================");
            //Set ADV file to load
            string bundle_mod = "adv/scenario/op/30/04.unity3d";

            //Set Name/PathID inside the file
            string asset_mod = "0";

            //Set first Girl
            Heroine herone_mod = lm_mod.heroines[0];

            //no idea
            Action onEnd_mod = null;

            //Open the scene
            lm_mod.OpenADV(bundle_mod, asset_mod, herone_mod, onEnd_mod);
        }
    }
}
