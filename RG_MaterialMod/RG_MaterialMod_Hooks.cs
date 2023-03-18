using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// BepInEx
using HarmonyLib;

// Unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Game Specific
using Chara;
using CharaCustom;


namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        internal class Hooks
        {
            // ================================================== CharaControl Section ==================================================
            // Initialize Character
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            private static void ChaControlInitialize(ChaControl __instance)
            {
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                ChaFile chaFile = chaControl.ChaFile;
                CharacterContent characterContent;

                if (!CharactersLoaded.ContainsKey(characterName))
                {
                    CharactersLoaded.Add(characterName, new CharacterContent());
                }
                characterContent = CharactersLoaded[characterName];
                characterContent.characterObject = characterObject;

                GameObject optionsToggleObject = GameObject.Find("tglOption");
                Toggle optionsToggle = optionsToggleObject.GetComponent<Toggle>();
                optionsToggle.onValueChanged.AddListener((UnityAction<bool>)delegate
                {
                    SavePluginData(characterContent, chaFile, optionsToggle);
                });
            }

            private static void SavePluginData(CharacterContent characterContent, ChaFile chaFile, Toggle toggle)
            {
                if (!toggle.isOn) return;
                SaveCard(chaFile, characterContent);

            }

            // Reload Character Prefix
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPre(ChaControl __instance)
            {
                string characterName = __instance.gameObject.name;
                Debug.Log("== ChaControlReload: " + __instance.name + " ==");
                ResetAllClothes(__instance.name);
            }


            // Reload Character Postfix
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPost(ChaControl __instance)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int coordinateType = cvsC_Clothes.coordinateType;

                MakeClothesDropdown(cvsC_Clothes);

                CharacterContent characterContent = CharactersLoaded[characterName];
                ChaFile chaFile = cvsC_Clothes.chaCtrl.ChaFile;
                LoadCard(chaFile, characterContent);

                SetAllClothesTextures(characterName);
            }

            // Destroy Character
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.DeleteChara))]
            private static void ChaControlDestroy(ChaControl cha)
            {
                bool wasRemoved = CharactersLoaded.Remove(cha.name);
            }

            // Get when clothes are updated
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), typeof(int), typeof(int), typeof(bool))]
            private static void ClothesChanged(ChaControl __instance, int kind)
            {
                Debug.Log("== ClothesChanged ==");
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int coordinateType = cvsC_Clothes.coordinateType;

                ResetKind(characterName, coordinateType, kind);
                SetClothesKind(characterName, coordinateType, kind);
            }

            // Get when material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void MaterialChanged(ChaControl __instance, int kind)
            {
                GameObject characterObject = __instance.gameObject;
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int coordinateType = cvsC_Clothes.coordinateType;

                // Update textures of piece "kind"
                SetClothesKind(__instance.gameObject.name, coordinateType, kind);
            }

            // ================================================== Clothes Submenu ==================================================
            // Get when clothing type change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.ChangeMenuFunc))]
            private static void PieceUpdated(CvsC_Clothes __instance)
            {
                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                int coordinate = __instance.coordinateType;
                int kind = __instance.SNo;

                SetClothesKind(characterName, coordinate, kind);

                if (clothesTab.isOn) MakeClothesDropdown(__instance);
                DestroyGarbage();
            }

            // Initializing clothes tab in Chara Maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.Initialize))]
            private static void StartClothesMenu(CvsC_Clothes __instance)
            {
                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                // Add character
                if (!CharactersLoaded.ContainsKey(__instance.chaCtrl.gameObject.name))
                {
                    CharactersLoaded.Add(characterName, new CharacterContent());
                    CharacterContent characterContent = CharactersLoaded[characterName];
                    characterContent.characterObject = characterObject;
                }

                // Resize Setting Window
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                RG_MaterialModUI.ChangeWindowSize(502f, settingWindow);

                // Create clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                clothesSelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/SelectMenu");
                clothesSettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/Setting");
                (clothesTab, clothesTabContent) = RG_MaterialModUI.CreateMakerTab(clothesSelectMenu, clothesSettingsGroup);

                clothesTab.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    if (isOn) MakeClothesDropdown(__instance);
                }

                MakeClothesDropdown(__instance);
            }
        }
    }
}
