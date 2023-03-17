using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Chara;
using CharaCustom;
using UnityEngine;
using UnityEngine.Events;
using MessagePack;
using Il2CppSystem.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
                GameObject characterObject = __instance.gameObject;
                string characterName = characterObject.name;

                if (!CharactersLoaded.ContainsKey(characterName))
                {
                    CharactersLoaded.Add(characterName, new CharacterContent());
                    CharacterContent characterContent = CharactersLoaded[characterName];
                    characterContent.characterObject = characterObject;
                }
            }

            // Reload Character
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReload(ChaControl __instance)
            {
                string characterName = __instance.gameObject.name;
                Debug.Log("== ChaControlReload: " + __instance.name + " ==");
                ResetAllClothes(__instance.name);


                // =============== TEST TEST ====================
                //ChaFile chaFile = __instance.ChaFile;
                //string byteString = LoadData(chaFile, "firstKey");
                //var bytes = Encoding.Latin1.GetBytes(byteString);
                //string texto1 = "";
                //for (int i = 0; i < 10; i++)
                //    texto1 = texto1 + bytes[i] + " ";
                //Debug.Log("Bytes Firsts: " + texto1);


                //BinaryFormatter formatter = new BinaryFormatter();
                //MemoryStream stream = new MemoryStream(bytes);
                //stream.Position = 0;
                //Debug.Log("Stream Length: " + stream.Length.ToString());
                //var teste = (Dictionary<string, byte[]>)formatter.Deserialize(stream);
                //stream.Close();


                //var textoTeste = teste.ElementAt(0).Key;
                //Debug.Log("Loaded: " + textoTeste);



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
                GameObject characterObject = __instance.gameObject;
                GameObject customControl = GameObject.Find("CustomControl");
                CvsC_Clothes cvsC_Clothes = customControl.GetComponentInChildren<CvsC_Clothes>();
                int coordinateType = cvsC_Clothes.coordinateType;

                ResetKind(characterObject.name, coordinateType,kind);
                SetClothesKind(__instance.gameObject.name, coordinateType, kind);
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

            // ================================================== Clothes Section ==================================================
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
