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
    internal class RG_MaterialMod_HScenes
    {
        internal class Hooks_HScenes
        {
            // Coordinate menu
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSceneSpriteCoordinatesCard), nameof(HSceneSpriteCoordinatesCard.Start))]
            private static void HSceneSpriteCoordinatesCard_Start(HSceneSpriteCoordinatesCard __instance)
            {
                //Debug.Log("HSceneSpriteCoordinatesCard_Start");
                Manager.HSceneManager hSceneManager = __instance.hSceneManager;

                Button selectCoordinate = __instance.DecideCoode;
                selectCoordinate.onClick.AddListener((UnityAction)onClick);
                void onClick()
                {
                    // Copied from HS2
                    ChaControl chaControl = (hSceneManager.NumFemaleClothCustom < 2) ? __instance.females[hSceneManager.NumFemaleClothCustom] : __instance.males[hSceneManager.NumFemaleClothCustom - 2];
                    string characterName = chaControl.name;
                    CharacterContent characterContent = CharactersLoaded[characterName];
                    ResetCoordinateTextures(characterContent);
                    characterContent.enableLoadCard = false;
                    chaControl.Reload();
                    characterContent.enableLoadCard = true;
                }

                Button originalCoordinate = __instance.BeforeCoode;
                originalCoordinate.onClick.AddListener((UnityAction)onClick2);
                void onClick2()
                {
                    // Copied from HS2
                    ChaControl chaControl = (hSceneManager.NumFemaleClothCustom < 2) ? __instance.females[hSceneManager.NumFemaleClothCustom] : __instance.males[hSceneManager.NumFemaleClothCustom - 2];
                    string characterName = chaControl.name;
                    LoadCard(CharactersLoaded[characterName]);
                    chaControl.Reload();
                }
            }
        }
    }
}
