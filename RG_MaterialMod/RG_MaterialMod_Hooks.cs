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
                characterContent.gameObject = characterObject;
                characterContent.name = characterName;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;
                characterContent.enableSetKind = false;
                characterContent.enableLoadCard = true;
                characterContent.currentCoordinate = ChaFileDefine.CoordinateType.Outer;
                ResetAllTextures(characterContent);

                // Chara Maker stuff
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
                characterContent.gameObject = characterObject;
                characterContent.name = characterName;
                characterContent.chaControl = chaControl;
                characterContent.chafile = chaFile;
                characterContent.enableSetKind = false;
                //characterContent.currentCoordinate = ChaFileDefine.CoordinateType.Outer;

                // if not in chara maker, just reset textures
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom")
                {
                    ResetAllTextures(characterContent);
                }
                else
                {
                    // Don't reset clothes when in clothes menu
                    Canvas clothesDefaultWin = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/").GetComponent<Canvas>();
                    Toggle clothesMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglClothes/").GetComponent<Toggle>();
                    if (!clothesDefaultWin.enabled || !clothesMainToggle.isOn) ResetAllTextures(characterContent);
                }
            }

            // Reload Character Postfix
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReloadPost(ChaControl __instance)
            {
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
                else
                {
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

                characterContent.enableLoadCard = true;
            }

            // Get when change the coordinate type (outer, house, bath)
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinateTypePre(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                characterContent.currentCoordinate = type;

                // Chara Maker Section
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int kindIndex = cvsC_Clothes.SNo;
                if (clothesTab.isOn) MakeClothesDropdown(characterContent, kindIndex);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinateTypePost(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                MaterialModMonoBehaviour.SetAllTexturesDelayed(characterContent);
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.OnDestroy))]
            private static void ChaControlOnDestroy(ChaControl __instance)
            {
                ChaControl chaControl = __instance;
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
                Canvas winClothes = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes").GetComponent<Canvas>();
                //if (!winClothes.enabled) return;

                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                ChaControl chaControl = characterContent.chaControl;
                int kindIndex = __instance.SNo;

                if (winClothes.enabled) SetKind(characterContent, TextureDictionaries.clothesTextures, kindIndex);
                if (clothesTab.isOn && winClothes.enabled)
                {
                    MakeClothesDropdown(characterContent, kindIndex);
                }

                // == Accessory Section ==
                Canvas winAccessory = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinAccessory").GetComponent<Canvas>();
                GameObject accessoryObject = chaControl.ObjAccessory[kindIndex];

                if (winAccessory.enabled) MakeAccessoryDropdown(characterContent, kindIndex);
            }

            // Get when change the clothes in right submenu
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), typeof(int), typeof(int), typeof(bool))]
            private static void ClothesChangedPre(ChaControl __instance, int id, int kind)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                ResetKind(characterContent, TextureDictionaries.clothesTextures, kind);

            }

            // Get when clothes material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void ChangeCustomClothesPost(ChaControl __instance, int kind)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                //if (!characterContent.enableSetKind) return;

                //SetAllClothesTextures(characterName);
                SetKind(characterContent, TextureDictionaries.clothesTextures, kind);
            }

            // Used when dressing/undressing clothes or half state
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
            private static void SetClothesState(ChaControl __instance)
            {
                string scene = SceneManager.GetActiveScene().name;
                if (scene != "CharaCustom") return;
                MaterialModMonoBehaviour.MakeBodyVisible(__instance);
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
                // Make settings size bigger if there's more than 5 tabs
                GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
                if (accessorySelectMenu.GetComponentsInChildren<UI_ToggleEx>(false).Count > 5) UITools.ChangeWindowSize(502f, settingWindow);
                else UITools.ChangeWindowSize(428f, settingWindow);
            }

            //Get when Accessory material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryColor))]
            private static void AccessoryMaterialChanged(int slotNo, ChaControl __instance)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                //if (!characterContent.enableSetKind) return;

                //SetAllClothesTextures(characterName);
                SetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryParent))]
            private static void AccessoryChanged(int slotNo, ChaControl __instance)
            {
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                // If not in chara maker, just set accessories
                string scene = SceneManager.GetActiveScene().name;
                if (scene == "CharaCustom")
                {
                    Toggle accessoryMainToggle = GameObject.Find("CharaCustom/CustomControl/CanvasMain/MainMenu/tglAccessory").GetComponent<Toggle>();
                    Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                    if (accessoryMainToggle.isOn && !clothesMainToggle.isOn)
                    {
                        ResetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
                    }
                    else SetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
                }
                else SetKind(characterContent, TextureDictionaries.accessoryTextures, slotNo);
            }

            // ================================================== Hair Submenu ==================================================
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsH_Hair), nameof(CvsH_Hair.Initialize))]
            private static void StartHairMenu(CvsH_Hair __instance)
            {
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
            private static void ChangeHair1(ChaControl __instance, int kind, int id)
            {
                string characterName = __instance.gameObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                ResetKind(characterContent, TextureDictionaries.hairTextures, kind);
            }

            // ================================================== Body Skin Submenu ==================================================
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsB_Skin), nameof(CvsB_Skin.Initialize))]
            private static void CvsB_SkinInitialize(CvsB_Skin __instance)
            {
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

            // ================================================== Facial Type  Submenu ==================================================
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsF_FaceType), nameof(CvsF_FaceType.Initialize))]
            private static void CvsF_FaceType_Initialize(CvsF_FaceType __instance)
            {
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
            }


            // ================================================== H-Scenes ==================================================
            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSceneSpriteCoordinatesCard), nameof(HSceneSpriteCoordinatesCard.Start))]
            private static void HSceneSpriteCoordinatesCard_Start(HSceneSpriteCoordinatesCard __instance)
            {
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

            // ================================================== Action Scene ==================================================
            static string currentCharacter = "";

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RG.Scene.Action.UI.ActionUI), nameof(RG.Scene.Action.UI.ActionUI.OpenCoordinateSelectUI))]
            private static void OpenCoordinateSelectUI(RG.Scene.Action.UI.ActionUI __instance)
            {
                CharacterContent characterContent = CharactersLoaded[currentCharacter];
                characterContent.enableLoadCard = false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(RG.Scene.Action.UI.CharaSelectOption), nameof(RG.Scene.Action.UI.CharaSelectOption.ChangeButtonState))]
            private static void UpdateUI(RG.Scene.Action.UI.CharaSelectOption.ButtonState btnState, RG.Scene.Action.UI.CharaSelectOption __instance)
            {
                if (__instance.Owner == null) return;
                if (btnState != RG.Scene.Action.UI.CharaSelectOption.ButtonState.Select) return;

                currentCharacter = __instance.Owner.Chara.name;
            }
        }
    }
}
