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
using BepInEx.Logging;
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


namespace IllusionPlugins
{
    public class MaterialModMonoBehaviour : MonoBehaviour
    {
        // Constructor needed to use Start, Update, etc...
        public MaterialModMonoBehaviour(IntPtr handle) : base(handle) { }

        private static MaterialModMonoBehaviour instance;

        static bool coroutineIsRunning = false;
        static string oldCharacter = "";
        static string newCharacter = "";

        private void Awake() 
        {
            instance = this;
        }

        /// <summary>
        /// Make all clothes and body parts visible at the end of the frame
        /// </summary>
        internal static void MakeBodyVisible(ChaControl chaControl)
        {
            newCharacter = chaControl.name;
            if (newCharacter == oldCharacter && coroutineIsRunning) return;
            oldCharacter = newCharacter;
            instance.StartCoroutine(instance.MakeBodyVisibleCoroutine(chaControl).WrapToIl2Cpp());
        }
        private IEnumerator MakeBodyVisibleCoroutine(ChaControl chaControl)
        {
            coroutineIsRunning = true;

            yield return new WaitForEndOfFrame();

            // Set body visible in material
            chaControl.CustomMatBody.SetFloat(ChaShader.alpha_c, 1f);
            for (int i = 0; i < chaControl.RendBra.Count; i++)
            {
                if (chaControl.RendBra != null && chaControl.RendBra[i] != null && chaControl.RendBra[i].material != null) chaControl.RendBra[i].material.SetFloat(ChaShader.alpha_c, 1f);
            }

            chaControl.CustomMatBody.SetFloat(ChaShader.alpha_d, 0f);
            if (chaControl.RendInnerTB != null) chaControl.RendInnerTB.material.SetFloat(ChaShader.alpha_d, 0f);
            if (chaControl.RendInnerB != null) chaControl.RendInnerB.material.SetFloat(ChaShader.alpha_d, 0f);
            if (chaControl.RendPanst != null) chaControl.RendPanst.material.SetFloat(ChaShader.alpha_d, 0f);

            coroutineIsRunning = false;
            Debug.Log("= MakeBodyVisibleCoroutine: " + chaControl.name);
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

            Debug.Log("== ResetSkin: " + chaControl.name);
            // Fix invisible bug in clothes
            MakeBodyVisible(chaControl);
        }

        internal static void SetAllTexturesDelayed(RG_MaterialMod.CharacterContent characterContent)
        {
            instance.StartCoroutine(instance.SetAllTexturesCoroutine(characterContent).WrapToIl2Cpp());
        }

        private IEnumerator SetAllTexturesCoroutine(RG_MaterialMod.CharacterContent characterContent)
        {
            if (!characterContent.enableSetTextures) yield break;
            
            ChaControl chaControl = characterContent.chaControl;

            yield return null;
            var objects = chaControl.ObjClothes.ToList();
            RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.clothesTextures, characterContent.name + " Clothes Delayed");

            yield return null;
            objects = chaControl.ObjAccessory.ToList();
            RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.accessoryTextures, characterContent.name + " Accessory Delayed");

            yield return null;
            objects = chaControl.ObjHair.ToList();
            RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.hairTextures, characterContent.name + " Hair Delayed");

            // Reseting skin before update
            yield return null;
            chaControl.SetBodyBaseMaterial();
            yield return null;
            
            GameObject body = chaControl.ObjBody;
            // Search for skin object
            SkinnedMeshRenderer[] meshRenderers = body.GetComponentsInChildren<SkinnedMeshRenderer>();
            objects.Clear();
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                GameObject thisObject = meshRenderers[i].gameObject;
                if (thisObject.name.StartsWith("o_body_c"))
                {
                    objects.Add(thisObject);
                    break;
                }
            }
            if (objects != null) RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.bodySkinTextures, characterContent.name + " Skin Delayed");

            // Reseting skin before update
            yield return null;
            chaControl.SetFaceBaseMaterial();
            yield return null;

            // Head Skin
            GameObject head = chaControl.ObjHead;
            // Search for skin object
            meshRenderers = head.GetComponentsInChildren<SkinnedMeshRenderer>();
            objects.Clear();
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                GameObject thisObject = meshRenderers[i].gameObject;
                if (thisObject.name.StartsWith("o_head"))
                {
                    objects.Add(thisObject);
                    break;
                }
            }
            if (objects != null) RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.headSkinTextures, characterContent.name + " Head Delayed");

            // Fixing missing body parts bug
            MakeBodyVisible(chaControl);
        }
    }
}
