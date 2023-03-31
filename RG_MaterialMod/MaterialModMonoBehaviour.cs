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

            //// Set body visible in material
            //chaControl.CustomMatBody.SetFloat(ChaShader.alpha_c, 1f);
            //for (int i = 0; i < chaControl.RendBra.Count; i++)
            //{
            //    if (chaControl.RendBra != null && chaControl.RendBra[i] != null && chaControl.RendBra[i].material != null) chaControl.RendBra[i].material.SetFloat(ChaShader.alpha_c, 1f);
            //}

            //chaControl.CustomMatBody.SetFloat(ChaShader.alpha_d, 0f);
            //if (chaControl.RendInnerTB != null) chaControl.RendInnerTB.material.SetFloat(ChaShader.alpha_d, 0f);
            //if (chaControl.RendInnerB != null) chaControl.RendInnerB.material.SetFloat(ChaShader.alpha_d, 0f);
            //if (chaControl.RendPanst != null) chaControl.RendPanst.material.SetFloat(ChaShader.alpha_d, 0f);
            SetMaterialAlphas(chaControl);

            coroutineIsRunning = false;
            Debug.Log("= MakeBodyVisibleCoroutine: " + chaControl.name);
        }

        internal static void SetMaterialAlphas(ChaControl chaControl)
        {
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
        }

        /// <summary>
        /// Set all textures type by type on different frames. The frame separation solves many issues
        /// </summary>
        /// <param name="characterContent"></param>
        internal static void SetAllTexturesDelayed(RG_MaterialMod.CharacterContent characterContent)
        {
            instance.StartCoroutine(instance.SetAllTexturesCoroutine(characterContent).WrapToIl2Cpp());
        }
        private IEnumerator SetAllTexturesCoroutine(RG_MaterialMod.CharacterContent characterContent)
        {
            ChaControl chaControl = characterContent.chaControl;
            characterContent.enableSetKind = false;
            yield return null;

            var objects = chaControl.ObjClothes.ToList();
            RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.clothesTextures, characterContent.name + " Clothes Delayed");
            yield return null;

            objects = chaControl.ObjAccessory.ToList();
            RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.accessoryTextures, characterContent.name + " Accessory Delayed");
            yield return null;

            objects = chaControl.ObjHair.ToList();
            RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.hairTextures, characterContent.name + " Hair Delayed");
            yield return null;

            // Fixing missing body parts bug. This must be after clothes and before skin
            SetMaterialAlphas(chaControl);
            yield return null;

            // ============== Body Skin Section ================
            // Rest body skin
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
            yield return null;

            // ============== Face Skin Section ================
            // Rest face skin
            chaControl.SetFaceBaseMaterial();
            yield return null;

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
            if (objects != null) RG_MaterialMod.SetAllDictionary(characterContent, objects, characterContent.faceSkinTextures, characterContent.name + " Face Delayed");

            characterContent.enableSetKind = true;
        }
    }
}
