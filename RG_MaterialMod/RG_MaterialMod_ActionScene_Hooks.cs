using System.Linq;

// BepInEx
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.IO;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Game Specific
using Chara;
using CharaCustom;

// Plugin Specific
using static IllusionPlugins.RG_MaterialMod;

namespace IllusionPlugins
{
    internal class RG_MaterialMod_ActionScene
    {
        internal class Hooks_ActionScene
        {
            // ================================================== Action Scene ==================================================
            // Current character on the UI
            static string currentCharacter = "";

            // Get the current selcted character when update the UI
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RG.Scene.Action.UI.CharaSelectOption), nameof(RG.Scene.Action.UI.CharaSelectOption.ChangeButtonState))]
            private static void UpdateUI(RG.Scene.Action.UI.CharaSelectOption.ButtonState btnState, RG.Scene.Action.UI.CharaSelectOption __instance)
            {
                //Debug.Log("UpdateUI");
                if (__instance.Owner == null) return;
                if (btnState != RG.Scene.Action.UI.CharaSelectOption.ButtonState.Select) return;

                currentCharacter = __instance.Owner.Chara.name;
            }

            // Don't load card when changing clothes to coordinates
            [HarmonyPostfix]
            [HarmonyPatch(typeof(RG.Scene.Action.UI.ActionUI), nameof(RG.Scene.Action.UI.ActionUI.OpenCoordinateSelectUI))]
            private static void OpenCoordinateSelectUI(RG.Scene.Action.UI.ActionUI __instance)
            {
                //Debug.Log("OpenCoordinateSelectUI");
                CharacterContent characterContent = CharactersLoaded[currentCharacter];
                characterContent.enableLoadCard = false;
            }
        }
    }
}
