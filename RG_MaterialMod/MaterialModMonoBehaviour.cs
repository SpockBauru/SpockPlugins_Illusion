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
    /// <summary>
    /// Delayed funcions goes here
    /// </summary>
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
            SetMaterialAlphas(chaControl);
            coroutineIsRunning = false;
        }

        ///// <summary>
        ///// Make character naked, reset skin and put clothes again
        ///// </summary>
        ///// <param name="chaControl"></param>
        //internal static void ResetSkin(ChaControl chaControl)
        //{
        //    instance.StartCoroutine(instance.ResetSkinCoroutine(chaControl).WrapToIl2Cpp());
        //}
        //private IEnumerator ResetSkinCoroutine(ChaControl chaControl)
        //{
        //    List<byte> oldClothesState = new List<byte>();
        //    var clothesStatus = chaControl.FileStatus.clothesState;
        //    // Save current clothes state and take off
        //    for (int i = 0; i < clothesStatus.Count; i++)
        //    {
        //        oldClothesState.Add(clothesStatus[i]);
        //        clothesStatus[i] = (byte)3;
        //    }
        //    yield return null;
        //    // Actually resset the body skin
        //    chaControl.SetBodyBaseMaterial();
        //    yield return null;
        //    // put clothes on again
        //    for (int i = 0; i < clothesStatus.Count; i++)
        //    {
        //        clothesStatus[i] = oldClothesState[i];
        //    }
        //    // Fix invisible bug in clothes
        //    MakeBodyVisible(chaControl);
        //}

        internal static void ResetFaceSkin(ChaControl chaControl)
        {
            instance.StartCoroutine(instance.ResetFaceSkinCoroutine(chaControl).WrapToIl2Cpp());
        }

        private IEnumerator ResetFaceSkinCoroutine(ChaControl chaControl)
        {
            yield return null;
            SetMaterialAlphas(chaControl);
            chaControl.SetFaceBaseMaterial();

        }
        internal static void SetMaterialAlphas(ChaControl chaControl)
        {
            //Debug.Log("SetMaterialAlphas");
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


        private static bool garbageBeingCollected = false;
        /// <summary>
        /// Destroy GarbageTextures, one per frame 
        /// </summary>
        internal static void DestroyGarbage()
        {
            if (!garbageBeingCollected) instance.StartCoroutine(instance.DestroyGarbageCoroutine().WrapToIl2Cpp());
        }
        private IEnumerator DestroyGarbageCoroutine()
        {
            garbageBeingCollected = true;
            // Destroy textures, one per frame
            for (int i = 0; i < RG_MaterialMod.GarbageTextures.Count; i++)
            {
                UnityEngine.Object.Destroy(RG_MaterialMod.GarbageTextures[i]);
                yield return null;
            }

            RG_MaterialMod.GarbageTextures.Clear();
            garbageBeingCollected = false;
        }

        internal static void LoadFileDelayed(Material material, RG_MaterialMod.CharacterContent characterContent, RG_MaterialMod.TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            instance.StartCoroutine(instance.LoadFileDelayedCoroutine(material, characterContent, texDictionary, kindIndex, renderIndex, textureName, miniature, sizeText).WrapToIl2Cpp());
        }
        private IEnumerator LoadFileDelayedCoroutine(Material material, RG_MaterialMod.CharacterContent characterContent, RG_MaterialMod.TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {


            // Check for Full Screen. Set windowed mod if in FullScreen, otherwise the game can softlock
            FullScreenMode fullScreenMode = Screen.fullScreenMode;
            if (fullScreenMode == FullScreenMode.FullScreenWindow || fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = FullScreenMode.Windowed;
            yield return null;

            // Load from file
            string path = Path.GetFullPath(".") + "\\UserData\\MaterialMod_Textures";
            string[] files = OpenFileDialog.ShowOpenDialog("Open File", path, "PNG Image (*.png)|*.png", OpenFileDialog.SingleFileFlags, OpenFileDialog.NativeMethods.GetActiveWindow());

            if (fullScreenMode == FullScreenMode.FullScreenWindow || fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = fullScreenMode;
            yield return null;

            if (files == null) yield break;

            Texture2D texture = new Texture2D(2, 2);
            byte[] bytes = File.ReadAllBytes(files[0]);
            texture.LoadImage(bytes);
            if (texture.width > 4096 || texture.height > 4096)
            {
                RG_MaterialMod.Log.LogMessage("MaterialMod: WARNING! Max texture size is 4096 x 4096");
                RG_MaterialMod.GarbageTextures.Add(texture);
                MaterialModMonoBehaviour.DestroyGarbage();
                yield break;
            }

            int coordinateType = (int)characterContent.currentCoordinate;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;

            // Getting the texture dictionary
            if (texDictionary == RG_MaterialMod.TextureDictionaries.clothesTextures) dicTextures = characterContent.clothesTextures;
            else if (texDictionary == RG_MaterialMod.TextureDictionaries.accessoryTextures) dicTextures = characterContent.accessoryTextures;
            else if (texDictionary == RG_MaterialMod.TextureDictionaries.hairTextures) dicTextures = characterContent.hairTextures;
            else if (texDictionary == RG_MaterialMod.TextureDictionaries.bodySkinTextures) dicTextures = characterContent.bodySkinTextures;
            else if (texDictionary == RG_MaterialMod.TextureDictionaries.faceSkinTextures) dicTextures = characterContent.faceSkinTextures;
            else yield break;

            // Create each dictionary if doesn't exist.
            // Texture = characterContent.clothesTextures[coordinate][kind][renderIndex][TextureName]
            if (!dicTextures.ContainsKey(coordinateType)) dicTextures.Add(coordinateType, new Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>());
            if (!dicTextures[coordinateType].ContainsKey(kindIndex)) dicTextures[coordinateType].Add(kindIndex, new Dictionary<int, Dictionary<string, byte[]>>());
            if (!dicTextures[coordinateType][kindIndex].ContainsKey(renderIndex)) dicTextures[coordinateType][kindIndex].Add(renderIndex, new Dictionary<string, byte[]>());
            if (!dicTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) dicTextures[coordinateType][kindIndex][renderIndex].Add(textureName, null);

            // Update Texture dictionary
            // From normal maps to Illusion pre-processed pink maps
            if (textureName.Contains("Bump")) texture = TextureTools.NormalToPink(texture);
            // Weatering mask must have the same dimensions
            if (textureName.Contains("_Weathering"))
            {
                int newWidth = texture.width;
                int newHeight = texture.height;
                Texture oldTexture = material.GetTexture(textureName);
                int oldWidth = oldTexture.width;
                int oldHeight = oldTexture.height;
                if (newWidth != oldWidth || newHeight != oldHeight)
                {
                    RG_MaterialMod.Log.LogMessage("MaterialMod: ERROR! Texture dimensions must match");
                    yield break;
                }
            }
            dicTextures[coordinateType][kindIndex][renderIndex][textureName] = texture.EncodeToPNG();


            // ======================================= Texture is set here ===========================================
            // Cleaning old textures. Not for skin, they need further investigation
            //if (texture != material.GetTexture(textureName) &&
            //    texDictionary != TextureDictionaries.bodySkinTextures &&
            //    texDictionary != TextureDictionaries.faceSkinTextures)
            //{
            //    GarbageTextures.Add(material.GetTexture(textureName));
            //}

            material.SetTexture(textureName, texture);
            RG_MaterialMod.UpdateMiniature(miniature, texture, textureName);

            MaterialModMonoBehaviour.DestroyGarbage();

            RG_MaterialMod.Log.LogMessage("MaterialMod: File Loaded");
        }

        internal static void ExportFileDelayed(Material material, RG_MaterialMod.CharacterContent characterContent, RG_MaterialMod.TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            instance.StartCoroutine(instance.ExportFileDelayedCoroutine(material, characterContent, texDictionary, kindIndex, renderIndex, textureName, miniature, sizeText).WrapToIl2Cpp());
        }
        private IEnumerator ExportFileDelayedCoroutine(Material material, RG_MaterialMod.CharacterContent characterContent, RG_MaterialMod.TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            // Check for Full Screen. Set windowed mod if in FullScreen, otherwise the game can softlock
            FullScreenMode fullScreenMode = Screen.fullScreenMode;
            if (fullScreenMode == FullScreenMode.FullScreenWindow || fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = FullScreenMode.Windowed;
            yield return null;

            // Save to file
            string path = Path.GetFullPath(".") + "\\UserData\\MaterialMod_Textures";
            string[] files = OpenFileDialog.ShowSaveDialog("Export File", path, "PNG Image (*.png)|*.png", OpenFileDialog.SingleFileFlags, OpenFileDialog.NativeMethods.GetActiveWindow());
            
            if (fullScreenMode == FullScreenMode.FullScreenWindow || fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = fullScreenMode;
            yield return null;

            if (files == null) yield break;
            if (!files[0].EndsWith(".png")) files[0] = files[0] + ".png";

            Texture2D texture = TextureTools.ToTexture2D(material.GetTexture(textureName));
            // From pink maps to regular normal maps
            if (textureName.Contains("Bump")) texture = TextureTools.PinkToNormal(texture);

            File.WriteAllBytes(files[0], texture.EncodeToPNG());

            RG_MaterialMod.GarbageTextures.Add(texture);
            MaterialModMonoBehaviour.DestroyGarbage();

            RG_MaterialMod.Log.LogMessage("MaterialMod: File Saved");
        }

        internal static void ExportUVDelayed(Renderer renderer, int index)
        {
            instance.StartCoroutine(instance.ExportUVDelayedCoroutine(renderer, index).WrapToIl2Cpp());
        }
        private IEnumerator ExportUVDelayedCoroutine(Renderer renderer, int index)
        {
            // Check for Full Screen. Set windowed mod if in FullScreen, otherwise the game can softlock
            FullScreenMode fullScreenMode = Screen.fullScreenMode;
            if (fullScreenMode == FullScreenMode.FullScreenWindow || fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = FullScreenMode.Windowed;
            yield return null;

            // Save to file
            string path = Path.GetFullPath(".") + "\\UserData\\MaterialMod_Textures";
            string[] files = OpenFileDialog.ShowSaveDialog("Export File", path, "PNG Image (*.png)|*.png", OpenFileDialog.SingleFileFlags, OpenFileDialog.NativeMethods.GetActiveWindow());

            if (fullScreenMode == FullScreenMode.FullScreenWindow || fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = fullScreenMode;
            yield return null;

            if (files == null) yield break;
            if (!files[0].EndsWith(".png")) files[0] = files[0] + ".png";


            // Getting size of main texture
            int width, height;
            Texture mainTexture = renderer.material.GetTexture("_MainTex");
            if (mainTexture != null)
            {
                width = mainTexture.width;
                height = mainTexture.height;
            }
            else width = height = 1024;

            List<Texture2D> UVRenderers = new List<Texture2D>();
            MeshRenderer meshRenderer = renderer.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null) UVRenderers = UVMap.GetUVMaps(meshRenderer, width, height);
            SkinnedMeshRenderer skinnedMeshRenderer = renderer.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null) UVRenderers.AddRange(UVMap.GetUVMaps(skinnedMeshRenderer, width, height));

            Texture2D UVtexture = UVRenderers[index];
            File.WriteAllBytes(files[0], UVtexture.EncodeToPNG());
            RG_MaterialMod.Log.LogMessage("MaterialMod: File Saved");

            // Cleaning textures
            for (int i = 0; i < UVRenderers.Count; i++)
                RG_MaterialMod.GarbageTextures.Add(UVRenderers[i]);
            MaterialModMonoBehaviour.DestroyGarbage();
        }
    }
}
