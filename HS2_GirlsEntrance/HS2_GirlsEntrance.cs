using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine.UI;
using HS2;
using Manager;
using Actor;
using System.ComponentModel;

namespace HS2_GirlsEntrance
{
    [BepInProcess("HoneySelect2")]
    [BepInPlugin("spockbauru.hs2.girlsentrance", "HS2_GirlsEntrance", Version)]
    public class HS2_GirlsEntrance : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "1.0";

        // User Configurations
        private static ConfigEntry<bool> Enabled;
        private static ConfigEntry<EntranceOption> MakeEntrance;
        private static ConfigEntry<WherePlayAnimation> WherePlay;

        // See if animation is still playing
        public static bool isPlaying = false;

        // Custom verion of OpenAdv to call the animation in background
        private static OpenADVmod EntranceObj = new OpenADVmod();

        //Store the orignal text for enter H
        private static string originalText;

        public HS2_GirlsEntrance()
        {
            // Config Panel Settings
            Enabled = Config.Bind("General", "Enabled", true, "Whether the plugin is enabled");
            MakeEntrance = Config.Bind("Settings", "When make an entrance", EntranceOption.FirstTime, "When the entrance animation plays");
            WherePlay = Config.Bind("Settings", "Where play the animation", WherePlayAnimation.MapSelect, "Which place do you want to play the animation");

            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(HS2_GirlsEntrance));
        }

        //=======================================Configuration Manager=======================================
        // User can define girl's state to play animation
        private enum EntranceOption
        {
            [Description("On Girl's First Time")]
            FirstTime,
            [Description("Every Time")]
            EveryTime
        }

        // User can define in which place he wants to play the animation
        private enum WherePlayAnimation
        {
            [Description("Before Map Select")]
            MapSelect,
            [Description("When Select a Girl")]
            GirlSelect
        }

        //===============================================Hooks===============================================
        // Play the animation on girls's select
        [HarmonyPostfix, HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), "OnValueChange")]
        private static void LoadOnSelect()
        {
            if (!isPlaying && Enabled.Value && WherePlay.Value == WherePlayAnimation.GirlSelect)
                EnterAnimation();
        }

        // Play the animation on map select
        [HarmonyPrefix, HarmonyPatch(typeof(LobbyMapSelectUI), "InitList")]
        private static void LoadOnMapSelect()
        {
            if (!isPlaying && Enabled.Value && WherePlay.Value == WherePlayAnimation.MapSelect)
                EnterAnimation();
        }

        // Avoid change animation on girl's change
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), "OnValueChange")]
        [HarmonyPrefix]
        private static void EnableGirlChange(ref bool __1)
        {
            ref bool _isOn = ref __1;

            if (isPlaying)
                _isOn = false;
        }

        // Avoid enter H-scene while animation is playing
        [HarmonyPostfix, HarmonyPatch(typeof(LobbyMapSelectInfoScrollController), "OnSnapTargetChanged")]
        private static void EnableStartH(Button ___btnStart)
        {
            //text inside button to start h-scene in map select
            Text[] buttonStartText = ___btnStart.GetComponentsInChildren<Text>();

            if (originalText == null)
                originalText = buttonStartText[0].text;

            if (isPlaying)
            {
                ___btnStart.interactable = false;
                buttonStartText[0].text = "Wait the Girl Enter";
            }
            else buttonStartText[0].text = originalText;
        }

        //=========================================Play the Animation========================================
        private static void EnterAnimation()
        {
            // Instance of LobbySceneManager
            LobbySceneManager sceneLobby = Singleton<LobbySceneManager>.Instance;

            // Exit if the girl is Fur
            string heroineID = sceneLobby.heroines[0].ChaName;
            if (heroineID == "c-1") return;

            // Number of times the selected girl had sex
            int sexTimes = sceneLobby.heroines[0].gameinfo2.hCount;

            if (MakeEntrance.Value == EntranceOption.FirstTime && sexTimes == 0 ||
                MakeEntrance.Value == EntranceOption.EveryTime)
            {
                // Set ADV file to load
                string bundle = "adv/scenario/op/50/entrance.unity3d";

                // Set Name/PathID inside the file
                string asset = "0";

                // Set first Girl
                Heroine heroine = sceneLobby.heroines[0];

                // What to do after the animation stop playing
                Action onEnd = null;

                // Open the scene using game's OpenADV -> Plays on foreground
                if (WherePlay.Value == WherePlayAnimation.MapSelect)
                    sceneLobby.OpenADV(bundle, asset, heroine, onEnd);

                // Open the scene using custon OpenADV -> Plays on background
                if (WherePlay.Value == WherePlayAnimation.GirlSelect)
                    EntranceObj.OpenADVScene(bundle, asset, heroine, sceneLobby, onEnd);
            }
        }
    }
}
