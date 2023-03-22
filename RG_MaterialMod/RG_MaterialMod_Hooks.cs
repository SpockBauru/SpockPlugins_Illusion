// BepInEx
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.IO;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Game Specific
using Chara;
using CharaCustom;
using Il2CppSystem.Collections.Generic;

namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        internal class Hooks
        {
            // ================================================== Load/Reload Section ==================================================
            // Initialize Character
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            private static void ChaControlInitialize(ChaControl __instance)
            {
                // Initialize Character
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                ChaFile chaFile = chaControl.ChaFile;
                CharacterContent characterContent;

                if (!CharactersLoaded.ContainsKey(characterName))
                    CharactersLoaded.Add(characterName, new CharacterContent());
                characterContent = CharactersLoaded[characterName];
                characterContent.characterObject = characterObject;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;
                characterContent.enableSetTextures = true;
                ResetAllClothes(characterName);

                // Chara Maker stuff
                GameObject charaCustom = GameObject.Find("CharaCustom");
                if (charaCustom == null) return;

                // Save when click on options toggle
                GameObject optionsToggleObject = GameObject.Find("tglOption");
                Toggle optionsToggle = optionsToggleObject.GetComponent<Toggle>();
                optionsToggle.onValueChanged.AddListener((UnityAction<bool>)delegate
                {
                    SavePluginData(characterContent, optionsToggle);
                });
            }

            private static void SavePluginData(CharacterContent characterContent, Toggle toggle)
            {
                if (!toggle.isOn) return;
                SaveCard(characterContent);
            }

            // Reload Character Prefix
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPre(ChaControl __instance)
            {
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                ChaFile chaFile = chaControl.ChaFile;
                CharacterContent characterContent;

                if (!CharactersLoaded.ContainsKey(characterName))
                    CharactersLoaded.Add(characterName, new CharacterContent());
                characterContent = CharactersLoaded[characterName];
                characterContent.characterObject = characterObject;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;

                // Something bad happens between the ChaControlReloadPre and the ChaControlReloadPost
                characterContent.enableSetTextures = false;

                // ==== Chara Maker Section ===
                GameObject customControl = GameObject.Find("CustomControl");
                if (customControl == null) return;
                // Don't reset clothes when in clothes menu
                Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();

                if (clothesMainToggle.isOn) return;
                ResetAllClothes(characterName);
            }

            // Reload Character Postfix
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPost(ChaControl __instance)
            {
                ChaControl chaControl = __instance;
                GameObject characterObject = chaControl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                characterContent.enableSetTextures = true;

                // If not in the chara maker, just load card and set textures
                GameObject customControl = GameObject.Find("CustomControl");
                if (customControl == null)
                {
                    LoadCard(characterContent);
                    SetAllTextures(characterName);
                }
                else
                {
                    // Make clothes tab
                    CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                    int kindIndex = cvsC_Clothes.SNo;
                    MakeClothesDropdown(characterContent, kindIndex);

                    // Don't load cards when in clothes menu
                    Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                    if (!clothesMainToggle.isOn)
                    {
                        LoadCard(characterContent);
                        SetAllTextures(characterName);
                    }
                }
            }

            // Get when change the coordinate type (outer, house, bath)
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinatePre(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                characterContent.currentCoordinate = type;

                // Chara Maker Section
                GameObject customControl = GameObject.Find("CustomControl");
                if (customControl == null) return;
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int kindIndex = cvsC_Clothes.SNo;
                if (clothesTab.isOn) MakeClothesDropdown(characterContent, kindIndex);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinatePost(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                SetAllTextures(characterName);
            }


            // ================================================== Clothes Submenu ==================================================
            // Initializing clothes tab in Chara Maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.Initialize))]
            private static void StartClothesMenu(CvsC_Clothes __instance)
            {
                CvsC_Clothes cvsC_Clothes = __instance;
                GameObject characterObject = cvsC_Clothes.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsC_Clothes.SNo;

                // Create clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                clothesSelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/SelectMenu");
                clothesSettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/Setting");
                (clothesTab, clothesTabContent) = RG_MaterialModUI.CreateMakerTab(clothesSelectMenu, clothesSettingsGroup);

                clothesTab.onValueChanged.AddListener((UnityAction<bool>)Make);

                void Make(bool isOn)
                {
                    kindIndex = cvsC_Clothes.SNo;
                    if (isOn) MakeClothesDropdown(characterContent, kindIndex);
                }
                MakeClothesDropdown(characterContent, kindIndex);

                // Deselect tab when in another section
                Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                clothesMainToggle.onValueChanged.AddListener((UnityAction<bool>)Deselect);
                void Deselect(bool isOn)
                {
                    if (!isOn) clothesTab.isOn = false;
                }

                // Save and Exit button when you edit a girl in-game
                Button saveAndExit = GameObject.Find("CharaCustom/CustomControl/CanvasMain/btnExit").GetComponent<Button>();
                saveAndExit.onClick.AddListener((UnityAction)delegate { SaveCard(characterContent); });
            }

            // Get when submenu tabs change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.RestrictClothesMenu))]
            private static void ResizeClothesWindow()
            {
                // Make settings size bigger if there's more than 5 tabs
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                if (clothesSelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5) RG_MaterialModUI.ChangeWindowSize(502f, settingWindow);
                else RG_MaterialModUI.ChangeWindowSize(428f, settingWindow);
            }

            // Get when change the clothes in right submenu
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), typeof(int), typeof(int), typeof(bool))]
            private static void ClothesChangedPre(ChaControl __instance, int id, int kind)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                characterContent.enableSetTextures = true;

                ResetKind(characterName, TextureDictionaries.clothesTextures, kind);
            }

            // Get when clothes material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void MaterialChanged(ChaControl __instance, int kind)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                if (!characterContent.enableSetTextures) return;

                //SetAllClothesTextures(characterName);
                SetClothesKind(characterName, TextureDictionaries.clothesTextures, kind);
            }

            // Get when clothing or accessory type change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.ChangeMenuFunc))]
            private static void PieceUpdated(CvsC_Clothes __instance)
            {
                Canvas winClothes = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes").GetComponent<Canvas>();
                //if (!winClothes.enabled) return;

                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                ChaControl chaControl = characterContent.chaControl;
                characterContent.enableSetTextures = true;
                int kindIndex = __instance.SNo;

                if (winClothes.enabled) SetClothesKind(characterName, TextureDictionaries.clothesTextures, kindIndex);
                if (clothesTab.isOn && winClothes.enabled)
                {
                    MakeClothesDropdown(characterContent, kindIndex);
                }

                // == Accessory Section ==
                Canvas winAccessory = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory").GetComponent<Canvas>();
                GameObject accessoryObject = chaControl.ObjAccessory[kindIndex];

                if (winAccessory.enabled) MakeAccessoryDropdown(characterContent, kindIndex);
            }

            // ================================================== Accessory Submenu ==================================================
            // Initializing Accessory tab in Chara Maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsA_Slot), nameof(CvsA_Slot.Initialize))]
            private static void StartAccessoryMenu(CvsA_Slot __instance)
            {
                CvsA_Slot cvsA_Slot = __instance;
                GameObject characterObject = cvsA_Slot.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsA_Slot.SNo;

                // Create clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                accessorySelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory/A_Slot/SelectMenu");
                accessorySettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory/A_Slot/Setting");
                (accessoryTab, accessoryTabContent) = RG_MaterialModUI.CreateMakerTab(accessorySelectMenu, accessorySettingsGroup);

                accessoryTab.onValueChanged.AddListener((UnityAction<bool>)Make);

                void Make(bool isOn)
                {
                    kindIndex = cvsA_Slot.SNo;
                    if (isOn) MakeAccessoryDropdown(characterContent, kindIndex);
                }
            }

            // Get when submenu tabs change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsA_Slot), nameof(CvsA_Slot.RestrictAcsMenu))]
            private static void ResizeAccessoryWindow()
            {
                // Make settings size bigger if there's more than 5 tabs
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                if (clothesSelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5) RG_MaterialModUI.ChangeWindowSize(502f, settingWindow);
                else RG_MaterialModUI.ChangeWindowSize(428f, settingWindow);
            }


            //Get when Accessory material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
            private static void AccessoryMaterialChanged(int slotNo, ChaControl __instance)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                if (!characterContent.enableSetTextures) return;

                //SetAllClothesTextures(characterName);
                SetClothesKind(characterName, TextureDictionaries.accessoryTextures, slotNo);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryParent))]
            private static void AccessoryChanged(int slotNo, ChaControl __instance)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                characterContent.enableSetTextures = true;

                // If not in chara maker, just set accessories
                GameObject customControl = GameObject.Find("CustomControl");
                if (customControl != null)
                {
                    Toggle accessoryMainToggle = GameObject.Find("tglAccessory").GetComponent<Toggle>();
                    Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                    if (accessoryMainToggle.isOn && !clothesMainToggle.isOn)
                    {
                        ResetKind(characterName, TextureDictionaries.accessoryTextures, slotNo);
                    }
                    else SetClothesKind(characterName, TextureDictionaries.accessoryTextures, slotNo);
                }
                else SetClothesKind(characterName, TextureDictionaries.accessoryTextures, slotNo);
            }
        }
    }
}
