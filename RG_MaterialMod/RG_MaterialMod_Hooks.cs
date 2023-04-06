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
    public partial class RG_MaterialMod
    {
        internal class Hooks
        {
            // ================================================== Initialize Section ==================================================
            // Initialize Character
            
            [HarmonyPostfix]
            [HarmonyPriority(Priority.High)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            private static void ChaControlInitialize(ChaControl __instance)
            {
                //Debug.Log("ChaControlInitialize");
                // Initialize Character
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                ChaFile chaFile = chaControl.ChaFile;
                CharacterContent characterContent;

                if (!CharactersLoaded.ContainsKey(characterName))
                    CharactersLoaded.Add(characterName, new CharacterContent());
                characterContent = CharactersLoaded[characterName];
                characterContent.gameObject = characterObject;
                characterContent.name = characterName;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;
                characterContent.enableSetKind = false;
                characterContent.enableLoadCard = true;
                characterContent.currentCoordinate = ChaFileDefine.CoordinateType.Outer;
                ResetAllTextures(characterContent);
            }

            // Reload Character Prefix
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPre(ChaControl __instance)
            {
                //Debug.Log("ChaControlReloadPre");
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                ChaFile chaFile = chaControl.ChaFile;
                CharacterContent characterContent;

                if (!CharactersLoaded.ContainsKey(characterName))
                    CharactersLoaded.Add(characterName, new CharacterContent());
                characterContent = CharactersLoaded[characterName];
                characterContent.gameObject = characterObject;
                characterContent.name = characterName;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;
                characterContent.enableSetKind = false;
                //characterContent.currentCoordinate = ChaFileDefine.CoordinateType.Outer;

                // if not in chara maker, just reset textures
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") ResetAllTextures(characterContent);
            }

            // Reload Character Postfix
            [HarmonyPostfix]
            [HarmonyPriority(Priority.High)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPost(ChaControl __instance)
            {
                //Debug.Log("ChaControlReloadPost");
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                ChaFile chaFile = chaControl.ChaFile;

                CharacterContent characterContent = CharactersLoaded[characterName];
                characterContent.gameObject = characterObject;
                characterContent.name = characterName;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;
                characterContent.enableSetKind = false;
                //characterContent.currentCoordinate = ChaFileDefine.CoordinateType.Outer;

                // If not in the chara maker, just load card and set textures
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom")
                {
                    LoadCard(characterContent);
                    MaterialModMonoBehaviour.SetAllTexturesDelayed(characterContent);
                }
                characterContent.enableLoadCard = true;
            }

            // Get when change the coordinate type (outer, house, bath)
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinateTypePre(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                //Debug.Log("ChangeCoordinateTypePre");
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                characterContent.currentCoordinate = type;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinateTypePost(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                //Debug.Log("ChangeCoordinateTypePost");
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                MaterialModMonoBehaviour.SetAllTexturesDelayed(characterContent);
            }

            // Clean CharactersLoaded when ChaControl is destroyed
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.OnDestroy))]
            private static void ChaControlOnDestroy(ChaControl __instance)
            {
                //Debug.Log("ChaControlOnDestroy");
                for (int i = CharactersLoaded.Count - 1; i >= 0; i--)
                {
                    CharacterContent characterContent = CharactersLoaded.ElementAt(i).Value;
                    if (characterContent.chaControl.name == "Delete_Reserve : DeleteChara")
                    {
                        ResetAllTextures(characterContent);
                        CharactersLoaded.Remove(characterContent.name);
                    }
                }
            }

            // ================================================== Clothes Submenu ==================================================
            // Get when clothes material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void ChangeCustomClothesPost(ChaControl __instance, int kind)
            {
                //Debug.Log("ChangeCustomClothesPost");
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                SetKind(characterContent, TextureDictionaries.clothesTextures, kind);
            }

            // ================================================== Accessory Submenu ==================================================
            //Get when Accessory material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
            private static void AccessoryMaterialChanged(int slotNo, ChaControl __instance)
            {
                //Debug.Log("AccessoryMaterialChanged");
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                SetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.High)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryParent))]
            private static void AccessoryChanged(int slotNo, ChaControl __instance)
            {
                //Debug.Log("AccessoryChanged");
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                // If not in chara maker, just set accessories
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") SetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
            }
        }
    }
}
