using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Linq;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils;
using BepInEx.IL2CPP.Utils.Collections;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// Extended Save
using RGExtendedSave;

// Game Specific
using RG;
using Chara;
using CharaCustom;
using BepInEx.Logging;
namespace IllusionPlugins
{
    public class MaterialModMonoBehaviour : MonoBehaviour
    {
        // Constructor needed to use Start, Update, etc...
        public MaterialModMonoBehaviour(IntPtr handle) : base(handle) { }

        private static MaterialModMonoBehaviour instance;

        static bool coroutineIsRunning = false;

        private void Awake() 
        {
            instance = this;
        }

        /// <summary>
        /// Make all clothes and body parts visible at the end of the frame
        /// </summary>
        internal static void MakeBodyVisible(ChaControl chaControl)
        {
            if (!coroutineIsRunning) instance.StartCoroutine(instance.MakeBodyVisibleCoroutine(chaControl).WrapToIl2Cpp());
        }
        private IEnumerator MakeBodyVisibleCoroutine(ChaControl chaControl)
        {
            coroutineIsRunning = true;

            yield return new WaitForEndOfFrame();

            // Set body visible in material
            chaControl.CustomMatBody.SetFloat(ChaShader.alpha_c, 1f);
            if (chaControl.RendBra != null && chaControl.RendBra[0] != null && chaControl.RendBra[0].material != null) chaControl.RendBra[0].material.SetFloat(ChaShader.alpha_c, 1f);
            
            chaControl.CustomMatBody.SetFloat(ChaShader.alpha_d, 0f);
            if (chaControl.RendInnerTB != null) chaControl.RendInnerTB.material.SetFloat(ChaShader.alpha_d, 0f);
            if (chaControl.RendInnerB != null) chaControl.RendInnerB.material.SetFloat(ChaShader.alpha_d, 0f);
            if (chaControl.RendPanst != null) chaControl.RendPanst.material.SetFloat(ChaShader.alpha_d, 0f);

            coroutineIsRunning = false;
        }

        /// <summary>
        /// Make character naked, reset skin and put clothes again
        /// </summary>
        /// <param name="chaControl"></param>
        internal static void ResetSkin(ChaControl chaControl)
        {
            instance.StartCoroutine(instance.ResetSkinCoroutine(chaControl).WrapToIl2Cpp());
        }
        private IEnumerator ResetSkinCoroutine(ChaControl chaControl)
        {
            List<byte> oldClothesState = new List<byte>();
            var clothesStatus = chaControl.FileStatus.clothesState;

            // Save current clothes state and take off
            for (int i = 0; i < clothesStatus.Count; i++)
            {
                oldClothesState.Add(clothesStatus[i]);
                clothesStatus[i] = (byte)3;
            }
            yield return null;

            // Actually resset the body skin
            chaControl.SetBodyBaseMaterial();
            yield return null;

            // put clothes on again
            for (int i = 0; i < clothesStatus.Count; i++)
            {
                clothesStatus[i] = oldClothesState[i];
            }

            // Fix invisible bug in clothes
            MakeBodyVisible(chaControl);
        }

        internal static void SetAllTexturesDelayed(string characterName)
        {
            instance.StartCoroutine(instance.SetAllTexturesCoroutine(characterName).WrapToIl2Cpp());
        }

        private IEnumerator SetAllTexturesCoroutine(string characterName)
        {
            yield return new WaitForEndOfFrame();
            RG_MaterialMod.SetAllTextures(characterName);

        }
    }
}
