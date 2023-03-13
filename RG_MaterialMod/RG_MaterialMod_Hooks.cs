using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Chara;
using CharaCustom;
using UnityEngine;
using UnityEngine.Events;

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
                if (!CharactersLoaded.ContainsKey(__instance.gameObject.name))
                    CharactersLoaded.Add(__instance.gameObject.name, new CharacterContent());
            }

            // Reload Character
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControlReload(ChaControl __instance)
            {
                //Debug.Log("== ChaControlReload: " + __instance.name + " ==");
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
                ResetKind(__instance.gameObject.name, kind);
                RefreshClothesMaterial(__instance.gameObject.name, kind);
            }

            // Get when material is updated
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
            private static void MaterialChanged(ChaControl __instance, int kind)
            {
                // Update textures of piece "kind"
                RefreshClothesMaterial(__instance.gameObject.name, kind);
            }

            // ================================================== Clothes Section ==================================================
            // Get when clothing type change
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.ChangeMenuFunc))]
            private static void PieceUpdated(CvsC_Clothes __instance)
            {
                RefreshClothesMaterial(__instance.chaCtrl.gameObject.name, __instance.SNo);
                if (RG_MaterialModUI.clothesToggle.isOn) MakeClothesContent(__instance);
                DestroyGarbage();
            }

            // Initializing clothes tab
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsC_Clothes), nameof(CvsC_Clothes.Initialize))]
            private static void StartClothesMenu(CvsC_Clothes __instance)
            {
                if (!CharactersLoaded.ContainsKey(__instance.chaCtrl.gameObject.name))
                {
                    CharactersLoaded.Add(__instance.chaCtrl.gameObject.name, new CharacterContent());
                }

                RG_MaterialModUI.MakeClothesTab();
                RG_MaterialModUI.clothesToggle.onValueChanged.AddListener((UnityAction<bool>)Make);
                void Make(bool isOn)
                {
                    if (isOn) MakeClothesContent(__instance);
                }

                MakeClothesContent(__instance);
            }
        }
    }
}
