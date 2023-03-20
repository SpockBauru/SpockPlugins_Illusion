// BepInEx
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.IO;

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
                Debug.Log("ChaControlInitialize: " + __instance.name);
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
                Debug.Log("ChaControlReloadPre: " + __instance.name + " ==");
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
                Debug.Log("== ChaControlReloadPost:" + __instance.name + " ==");
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
                    SetAllClothesTextures(characterName);
                }
                else
                {
                    // Make clothes tab
                    CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                    MakeMaterialDropdown(cvsC_Clothes);

                    // Don't load cards when in clothes menu
                    Toggle clothesMainToggle = GameObject.Find("tglClothes").GetComponent<Toggle>();
                    if (!clothesMainToggle.isOn)
                    {
                        LoadCard(characterContent);
                        SetAllClothesTextures(characterName);
                    }
                }
            }

            // Get when change the clothes in right submenu
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeClothes), typeof(int), typeof(int), typeof(bool))]
            private static void ClothesChanged(ChaControl __instance, int id, int kind)
            {
                Debug.Log("ClothesChanged ID: " + id);

                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                characterContent.enableSetTextures = true;

                ResetKind(characterName, kind);
            }

            // Get when material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void MaterialChanged(ChaControl __instance, int kind)
            {
                Debug.Log("MaterialChanged: " + kind.ToString());
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                if (!characterContent.enableSetTextures) return;

                //SetAllClothesTextures(characterName);
                SetClothesKind(characterName, kind);

            }

            // Get when change the coordinate type (outer, house, bath)
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool), typeof(bool))]
            private static void ChangeCoordinateType(ChaControl __instance, ChaFileDefine.CoordinateType type)
            {
                Debug.Log("====== Assign Coordinate: " + type.ToString());
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];

                characterContent.currentCoordinate = type;

                SetAllClothesTextures(characterName);

                // Chara Maker Section
                GameObject customControl = GameObject.Find("CustomControl");
                if (customControl == null) return;
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                if (clothesTab.isOn) MakeMaterialDropdown(cvsC_Clothes);
            }


            // ================================================== Character Section ==================================================
            // Destroy Character
            //[HarmonyPrefix]
            //[HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.DeleteChara))]
            //private static void ChaControlDestroy(ChaControl cha)
            //{
            //    ResetAllClothes(cha.name);
            //}


            // ================================================== Clothes Submenu ==================================================
            // Initializing clothes tab in Chara Maker
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.Initialize))]
            private static void StartClothesMenu(CvsC_Clothes __instance)
            {
                Debug.Log("StartClothesMenu");

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
                    if (isOn) MakeMaterialDropdown(__instance);
                }

                MakeMaterialDropdown(__instance);

                // Save and exit button when you edit a girl make in-game
                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                Button saveAndExit = GameObject.Find("CharaCustom/CustomControl/CanvasMain/btnExit").GetComponent<Button>();
                Debug.Log(saveAndExit.name);
                saveAndExit.onClick.AddListener((UnityAction)delegate { SaveCard(characterContent); });
            }

            // Get when clothing type change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.ChangeMenuFunc))]
            private static void PieceUpdated(CvsC_Clothes __instance)
            {
                Debug.Log("ChangeMenuFunc");
                GameObject characterObject = __instance.chaCtrl.gameObject;
                string characterName = characterObject.name;
                CharacterContent characterContent = CharactersLoaded[characterName];
                characterContent.enableSetTextures = true;
                int coordinateType = (int)characterContent.currentCoordinate;
                int kind = __instance.SNo;

                SetClothesKind(characterName, kind);
                //SetAllClothesTextures(characterName);

                if (clothesTab.isOn) MakeMaterialDropdown(__instance);
                //DestroyGarbage();
            }
        }
    }
}
