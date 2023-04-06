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
    internal partial class RG_MaterialMod_Maker
    {
        internal class Hooks_Maker
        {
            // ================================================== Initialize Section ==================================================
            // Initialize Character
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Normal)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Initialize))]
            private static void ChaControlInitialize(ChaControl __instance)
            {
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;

                characterContent.enableSetKind = true;

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
            [HarmonyPriority(Priority.Normal)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPre(ChaControl __instance)
            {
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;

                // Don't reset clothes when in clothes menu
                Canvas clothesDefaultWin = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/").GetComponent<Canvas>();
                Toggle clothesMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglClothes/").GetComponent<Toggle>();
                if (!clothesDefaultWin.enabled || !clothesMainToggle.isOn) ResetAllTextures(characterContent);
            }

            // Reload Character Postfix
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Normal)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPost(ChaControl __instance)
            {
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;

                // Make clothes tab
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int kindIndex = cvsC_Clothes.SNo;
                MakeClothesDropdown(characterContent, kindIndex);

                // Don't load cards when in clothes menu
                Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                if (!clothesMainToggle.isOn)
                {
                    LoadCard(characterContent);
                    MaterialModMonoBehaviour.SetAllTexturesDelayed(characterContent);
                }
            }

            // ================================================== Clothes Submenu ==================================================
            // Initializing clothes tab in Chara Maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.Initialize))]
            private static void StartClothesMenu(CvsC_Clothes __instance)
            {
                //Debug.Log("StartClothesMenu");
                CvsC_Clothes cvsC_Clothes = __instance;
                GameObject characterObject = cvsC_Clothes.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsC_Clothes.SNo;

                // Create clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                clothesSelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/SelectMenu");
                clothesSettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/Setting");
                (clothesTab, clothesTabContent) = UITools.CreateMakerTab(clothesSelectMenu, clothesSettingsGroup);

                clothesTab.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    kindIndex = cvsC_Clothes.SNo;
                    if (isOn) MakeClothesDropdown(characterContent, kindIndex);
                }
                MakeClothesDropdown(characterContent, kindIndex);

                // Make when entering clothes section
                Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                clothesMainToggle.onValueChanged.AddListener((UnityAction<bool>)Deselect);
                void Deselect(bool isOn)
                {
                    if (isOn)
                    {
                        if (clothesSelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5)
                            UITools.ChangeWindowSize(502f, settingWindow);
                        else UITools.ChangeWindowSize(428, settingWindow);
                        MakeClothesDropdown(characterContent, kindIndex);
                    }
                }

                // Save and Exit button when you edit a girl in-game
                Button saveAndExit = GameObject.Find("CharaCustom/CustomControl/CanvasMain/btnExit").GetComponent<Button>();
                saveAndExit.onClick.AddListener((UnityAction)delegate { SaveCard(characterContent); });
            }

            // Get when submenu tabs change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.RestrictClothesMenu))]
            private static void ResizeClothesWindow(CvsC_Clothes __instance)
            {
                //Debug.Log("ResizeClothesWindow");
                // Make settings size bigger if there's more than 5 tabs
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                if (clothesSelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5) UITools.ChangeWindowSize(502f, settingWindow);
                else UITools.ChangeWindowSize(428f, settingWindow);
            }

            // Get when clothing or accessory type change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.ChangeMenuFunc))]
            private static void PieceUpdated(CvsC_Clothes __instance)
            {
                //Debug.Log("PieceUpdated");
                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = __instance.SNo;
                
                Canvas winClothes = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes").GetComponent<Canvas>();

                if (winClothes.enabled) SetKind(characterContent, TextureDictionaries.clothesTextures, kindIndex);
                if (clothesTab.isOn && winClothes.enabled) MakeClothesDropdown(characterContent, kindIndex);

                // == Accessory Section ==
                Canvas winAccessory = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory").GetComponent<Canvas>();
                if (winAccessory.enabled) MakeAccessoryDropdown(characterContent, kindIndex);
            }

            // Get when change the coordinate type (outer, house, bath)
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Normal)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinateTypePost(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;

                // Make Clothes dropdown when click on outer/house/bath toggles
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int kindIndex = cvsC_Clothes.SNo;
                if (clothesTab.isOn) MakeClothesDropdown(characterContent, kindIndex);
            }

            // Used when dressing/undressing clothes or half state
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            private static void SetClothesState(ChaControl __instance)
            {
                //Debug.Log("SetClothesState");
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;
                MaterialModMonoBehaviour.MakeBodyVisible(__instance);
            }

            // Get when change the clothes in right submenu
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), typeof(int), typeof(int), typeof(bool))]
            private static void ClothesChangedPre(ChaControl __instance, int id, int kind)
            {
                //Debug.Log("ClothesChangedPre");
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                ResetKind(characterContent, TextureDictionaries.clothesTextures, kind);
            }

            // ================================================== Accessory Submenu ==================================================
            // Initializing Accessory tab in Chara Maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsA_Slot), nameof(CvsA_Slot.Initialize))]
            private static void StartAccessoryMenu(CvsA_Slot __instance)
            {
                //Debug.Log("StartAccessoryMenu");
                CvsA_Slot cvsA_Slot = __instance;
                GameObject characterObject = cvsA_Slot.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsA_Slot.SNo;

                // Accessory clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                accessorySelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory/A_Slot/SelectMenu");
                accessorySettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory/A_Slot/Setting");
                (accessoryTab, accessoryTabContent) = UITools.CreateMakerTab(accessorySelectMenu, accessorySettingsGroup);

                accessoryTab.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    kindIndex = cvsA_Slot.SNo;
                    if (isOn) MakeAccessoryDropdown(characterContent, kindIndex);
                }

                // Make when entering clothes section
                Toggle accrssoryMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglAccessory").GetComponent<Toggle>();
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                accrssoryMainToggle.onValueChanged.AddListener((UnityAction<bool>)Deselect);
                void Deselect(bool isOn)
                {
                    if (isOn)
                    {
                        if (accessorySelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5)
                            UITools.ChangeWindowSize(502f, settingWindow);
                        else UITools.ChangeWindowSize(428, settingWindow);
                        MakeAccessoryDropdown(characterContent, kindIndex);
                    }
                }
            }

            // Get when submenu tabs change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsA_Slot), nameof(CvsA_Slot.RestrictAcsMenu))]
            private static void ResizeAccessoryWindow()
            {
                //Debug.Log("ResizeAccessoryWindow");
                // Make settings size bigger if there's more than 5 tabs
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                if (accessorySelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5) UITools.ChangeWindowSize(502f, settingWindow);
                else UITools.ChangeWindowSize(428f, settingWindow);
            }

            [HarmonyPostfix]
            [HarmonyPriority(Priority.Normal)]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryParent))]
            private static void AccessoryChanged(int slotNo, ChaControl __instance)
            {
                //Debug.Log("AccessoryChanged");
                ChaControl chaControl = __instance;
                string characterName = chaControl.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;

                // Reset when accessory change
                Toggle accessoryMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglAccessory").GetComponent<Toggle>();
                Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                if (accessoryMainToggle.isOn && !clothesMainToggle.isOn)
                {
                    ResetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
                }
                else SetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
            }

            // ================================================== Hair Submenu ==================================================
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsH_Hair), nameof(CvsH_Hair.Initialize))]
            private static void StartHairMenu(CvsH_Hair __instance)
            {
                //Debug.Log("StartHairMenu");
                CvsH_Hair cvsH_Hair = __instance;
                GameObject characterObject = cvsH_Hair.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsH_Hair.SNo;

                // Hair clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                hairSelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinHair/H_Hair/SelectMenu");
                hairSettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinHair/H_Hair/Setting");
                (hairTab, hairTabContent) = UITools.CreateMakerTab(hairSelectMenu, hairSettingsGroup);

                hairTab.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    kindIndex = cvsH_Hair.SNo;
                    if (isOn) MakeHairDropdown(characterContent, kindIndex);
                }

                // Make when entering hair section
                Toggle hairMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglHair").GetComponent<Toggle>();
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                hairMainToggle.onValueChanged.AddListener((UnityAction<bool>)valueChanged);
                void valueChanged(bool isOn)
                {
                    if (isOn)
                    {
                        if (hairSelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5)
                            UITools.ChangeWindowSize(502f, settingWindow);
                        else UITools.ChangeWindowSize(428, settingWindow);
                        MakeHairDropdown(characterContent, kindIndex);
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsH_Hair), nameof(CvsH_Hair.SetDrawSettingByHair))]
            private static void SetDrawSettingByHair(CvsH_Hair __instance)
            {
                //Debug.Log("SetDrawSettingByHair");
                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = __instance.SNo;

                MakeHairDropdown(characterContent, kindIndex);

                // Change setting window size
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                Toggle hairMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglHair").GetComponent<Toggle>();
                if (hairMainToggle.isOn && hairSelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5)
                {
                    UITools.ChangeWindowSize(502f, settingWindow);
                }
                else
                {
                    UITools.ChangeWindowSize(428f, settingWindow);
                }

                //SetAllTextures(characterName);
                for (int i = 0; i < 4; i++) SetKind(characterContent, TextureDictionaries.hairTextures, i);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHair), typeof(int), typeof(int), typeof(bool))]
            private static void ChangeHair(ChaControl __instance, int kind, int id)
            {
                //Debug.Log("ChangeHair");
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                ResetKind(characterContent, TextureDictionaries.hairTextures, kind);
            }

            // ================================================== Body Skin Submenu ==================================================
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsB_Skin), nameof(CvsB_Skin.Initialize))]
            private static void CvsB_SkinInitialize(CvsB_Skin __instance)
            {
                //Debug.Log("CvsB_SkinInitialize");
                CvsB_Skin cvsB_Skin = __instance;
                GameObject characterObject = cvsB_Skin.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsB_Skin.SNo;

                // Skin clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                bodySkinSelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinBody/B_Skin/SelectMenu");
                bodySkinSettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinBody/B_Skin/Setting");
                (bodySkinTab, bodySkinTabContent) = UITools.CreateMakerTab(bodySkinSelectMenu, bodySkinSettingsGroup);

                bodySkinTab.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    kindIndex = cvsB_Skin.SNo;
                    if (isOn) MakeBodySkinDropdown(characterContent, kindIndex);
                }

                // Make when entering skin section
                UI_ButtonEx skinButton = GameObject.Find("CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuBody/Scroll View/Viewport/Content/Category/CategoryTop/SkinType").GetComponent<UI_ButtonEx>();
                skinButton.onClick.AddListener((UnityAction)onClick);
                void onClick()
                {
                    MakeBodySkinDropdown(characterContent, kindIndex);
                }

                // Make when entering body section
                Toggle bodyMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglBody").GetComponent<Toggle>();
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                bodyMainToggle.onValueChanged.AddListener((UnityAction<bool>)valueChanged);
                void valueChanged(bool isOn)
                {
                    if (isOn) UITools.ChangeWindowSize(428f, settingWindow);
                    if (isOn && bodySkinTab.isOn)
                    {
                        MakeBodySkinDropdown(characterContent, kindIndex);
                    }
                }
            }

            // ================================================== Facial Type Submenu ==================================================
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsF_FaceType), nameof(CvsF_FaceType.Initialize))]
            private static void CvsF_FaceType_Initialize(CvsF_FaceType __instance)
            {
                //Debug.Log("CvsF_FaceType_Initialize");
                CvsF_FaceType cvsF_FaceType = __instance;
                GameObject characterObject = cvsF_FaceType.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                int kindIndex = cvsF_FaceType.SNo;

                // Skin clothes Tab in chara maker: GET TAB TOGGLE AND WINDOW CONTENT!
                faceSkinSelectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinFace/F_FaceType/SelectMenu");
                faceSkinSettingsGroup = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinFace/F_FaceType/Setting");
                (faceSkinTab, faceSkinTabContent) = UITools.CreateMakerTab(faceSkinSelectMenu, faceSkinSettingsGroup);

                faceSkinTab.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    kindIndex = cvsF_FaceType.SNo;
                    if (isOn) MakeFaceSkinDropdown(characterContent, kindIndex);
                }

                // Make when entering Face Type section
                UI_ButtonEx faceSkinButton = GameObject.Find("CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuFace/Scroll View/Viewport/Content/Category/CategoryTop/FaceType").GetComponent<UI_ButtonEx>();
                faceSkinButton.onClick.AddListener((UnityAction)onClick);
                void onClick()
                {
                    MakeFaceSkinDropdown(characterContent, kindIndex);
                }

                // Make when entering Face section
                Toggle faceMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglFace").GetComponent<Toggle>();
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                faceMainToggle.onValueChanged.AddListener((UnityAction<bool>)valueChanged);
                void valueChanged(bool isOn)
                {
                    if (isOn) UITools.ChangeWindowSize(428f, settingWindow);
                    if (isOn && faceSkinTab.isOn)
                    {
                        MakeFaceSkinDropdown(characterContent, kindIndex);
                    }
                }

                MaterialModMonoBehaviour.ResetFaceSkin(characterContent.chaControl);
            }
        }
    }
}
