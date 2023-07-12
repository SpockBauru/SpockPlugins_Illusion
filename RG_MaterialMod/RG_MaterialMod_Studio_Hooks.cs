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

namespace IllusionPlugins
{
    internal class RG_MaterialMod_Studio_Hooks
    {
        // ================================================== Initialize Section ==================================================
        // Load Character
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Load))]
        private static void Load(ChaControl __instance)
        {
            Debug.Log("ChaControl.Load");
            // Initialize Character
            ChaControl chaControl = __instance;
            GameObject characterObject = chaControl.gameObject;
            string characterName = characterObject.name;
            ChaFile chaFile = chaControl.ChaFile;
            RG_MaterialMod.CharacterContent characterContent;

            if (!RG_MaterialMod.CharactersLoaded.ContainsKey(characterName))
                RG_MaterialMod.CharactersLoaded.Add(characterName, new RG_MaterialMod.CharacterContent());
            characterContent = RG_MaterialMod.CharactersLoaded[characterName];
            characterContent.gameObject = characterObject;
            characterContent.name = characterName;
            characterContent.chaControl = chaControl;
            characterContent.chafile = chaFile;
            characterContent.enableSetKind = false;
            characterContent.enableLoadCard = true;
            characterContent.currentCoordinate = ChaFileDefine.CoordinateType.Outer;
            RG_MaterialMod.ResetAllTextures(characterContent);
            RG_MaterialMod.LoadCard(characterContent);
            MaterialModMonoBehaviour.SetAllTexturesDelayed(characterContent);
        }

        // Clean CharactersLoaded when ChaControl is destroyed
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.OnDestroy))]
        private static void ChaControlOnDestroy(ChaControl __instance)
        {
            Debug.Log("ChaControl.OnDestroy");
            Debug.Log(__instance.gameObject.name);
            for (int i = RG_MaterialMod.CharactersLoaded.Count - 1; i >= 0; i--)
            {
                RG_MaterialMod.CharacterContent characterContent = RG_MaterialMod.CharactersLoaded.ElementAt(i).Value;
                if (characterContent.chaControl.name.StartsWith("Delete_Reserve : DeleteChara"))
                {
                    RG_MaterialMod.ResetAllTextures(characterContent);
                    RG_MaterialMod.CharactersLoaded.Remove(characterContent.name);
                }
            }
        }
    }
}
