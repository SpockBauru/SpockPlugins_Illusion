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
        public const string Version = "0.5";

        // User Configurations
        private static ConfigEntry<bool> Enabled;
        private static ConfigEntry<EntranceOption> MakeEntrance;
        private static ConfigEntry<WherePlayAnimation> WherePlay;

        // See if animation is still playing
        public static bool isPlaying = false;

        // Custon verion of OpenAdv to call the animation in background
        static OpenADV EntranceObj = new OpenADV();

        //Store the orignal text for enter H
        public static string originalText;

        public HS2_GirlsEntrance()
        {
            // Config Panel Settings
            Enabled = Config.Bind("General", "Enabled", true, "Whether the plugin is enabled");
            MakeEntrance = Config.Bind("Settings", "When make an entrance", EntranceOption.FirstTime, "When the entrance animation plays");
            WherePlay = Config.Bind("Settings", "Where play the animation", WherePlayAnimation.MapSelect, "Which place do you want to play the animation");

            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(HS2_GirlsEntrance));
        }

        //============================Configuration Manager=====================================
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

        //=======================================Hooks=============================================
        // Avoid change animation on girl's change
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), "OnValueChange")]
        [HarmonyPrefix]
        public static void EnableGirlChange(ref bool __1)
        {
            ref bool isOn = ref __1;

            if (isPlaying) isOn = false;
        }

        // Avoid enter H-scene while animation is playing
        [HarmonyPostfix, HarmonyPatch(typeof(LobbyMapSelectInfoScrollController), "OnSnapTargetChanged")]
        public static void EnableStartH(Button ___btnStart)
        {
            var buttonStartText = ___btnStart.GetComponentsInChildren<Text>();
            if (originalText == null) originalText = buttonStartText[0].text;

            if (isPlaying)
            {
                ___btnStart.interactable = false;
                buttonStartText[0].text = "Wait the Girl Enter";
            }
            else buttonStartText[0].text = originalText;
        }

        // Pay the animation on girls's select
        [HarmonyPostfix, HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), "OnValueChange")]
        public static void SetupGirlsEntrance()
        {
            if (!isPlaying && Enabled.Value && WherePlay.Value == WherePlayAnimation.GirlSelect)
                EnterAnimation();
        }

        // Play the animation on map select
        [HarmonyPrefix, HarmonyPatch(typeof(LobbyMapSelectUI), "InitList")]
        public static void LoadOnMapSelect()
        {
            if (!isPlaying && Enabled.Value && WherePlay.Value == WherePlayAnimation.MapSelect)
                EnterAnimation();
        }

        public static void EnterAnimation()
        {
            // Instance of LobbySceneManager
            LobbySceneManager scene = Singleton<LobbySceneManager>.Instance;

            // Exit if the girl is Fur
            string heroineID = scene.heroines[0].ChaName;
            if (heroineID == "c-1") return;

            // Number of times the selected girl had sex
            int sexTimes = scene.heroines[0].gameinfo2.hCount;

            if (MakeEntrance.Value == EntranceOption.FirstTime && sexTimes == 0 ||
                MakeEntrance.Value == EntranceOption.EveryTime)
            {
                // Set ADV file to load
                string bundle = "adv/scenario/op/50/entrance.unity3d";
                //string bundle = "adv/scenario/op/50/04.unity3d";

                // Set Name/PathID inside the file
                string asset = "0";

                // Set first Girl
                Heroine heroine = scene.heroines[0];

                // What to do after the animation stop playing
                Action onEnd = null;

                // Open the scene using game's OpenADV -> Plays on foreground
                if (WherePlay.Value == WherePlayAnimation.MapSelect)
                    scene.OpenADV(bundle, asset, heroine, onEnd);

                // Open the scene using custon OpenADV -> Plays on background
                if (WherePlay.Value == WherePlayAnimation.GirlSelect)
                    EntranceObj.OpenADVScene(bundle, asset, heroine, scene, onEnd);
            }
        }
    }
}
