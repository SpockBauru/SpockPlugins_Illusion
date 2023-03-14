using System;
using System.Collections;
using System.IO;
using System.Diagnostics;

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

// Game Specific
using RG;
using Chara;
using CharaCustom;
using System.Collections.Generic;
using System.Linq;

namespace IllusionPlugins
{
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class RG_MaterialMod : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.MaterialMod";
        public const string PluginName = "MaterialMod";
        public const string Version = "0.1";
        public const string PluginNameInternal = Constants.Prefix + "MaterialMod";

        // Unity don't destroy textures automatically, need to do manually
        static List<Texture2D> GarbageTextures = new List<Texture2D>();
        static List<Image> GarbageImages = new List<Image>();

        // Miniatures
        static int miniatureSize = 200;
        static List<Texture2D> miniatureTextures = new List<Texture2D>();
        static List<Image> miniatureImages = new List<Image>();

        /// <summary>
        /// Key: Name of Character's GameObject, Value: class CharacterTextures
        /// </summary>
        public static Dictionary<string, CharacterContent> CharactersLoaded = new Dictionary<string, CharacterContent>();

        // Everything MaterialMod content for this character goes here

        /// <summary>
        /// Every MaterialMod content for this character goes here
        /// </summary>
        public class CharacterContent
        {
            /// <summary>
            /// <br>Key: Texture material number</br>
            /// <br>Value: MaterialTextures class</br>
            /// </summary>
            public Dictionary<int, MaterialContent> clothesTop = new Dictionary<int, MaterialContent>();
            public Dictionary<int, MaterialContent> clothesBottom = new Dictionary<int, MaterialContent>();
        }

        /// <summary>
        /// All content regarding one material
        /// </summary>
        public class MaterialContent
        {
            /// <summary>
            /// Key: Texture name
            /// </summary>
            public Dictionary<string, Texture2D> currentTextures;

            /// <summary>
            /// Key: Texture name
            /// </summary>
            public Dictionary<string, Texture2D> originalTextures;
        }

        public enum TextureType
        {
            generic,   // Generic RGBA texture
            normalMap, // Need to be converted between DXT5nm (pink) and regular Normal Map
            splitMap   // Each channel have a meaning and need to be disassembled
        };

        public override void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
        }

        public static void SetClothesTextures(string characterName, int kind)
        {
            ChaControl charaControl = GameObject.Find(characterName).GetComponent<ChaControl>();
            GameObject clothesPiece = GetClothes(charaControl, kind);
            Renderer[] rendererList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            CharacterContent characterContent = CharactersLoaded[characterName];
            Dictionary<int, MaterialContent> dicMaterials;

            if (kind == 0) dicMaterials = characterContent.clothesTop;
            else if (kind == 1) dicMaterials = characterContent.clothesBottom;
            else dicMaterials = null;

            if (dicMaterials == null) return;

            // Search for all materials
            for (int i = 0; i < rendererList.Length; i++)
            {
                if (!dicMaterials.ContainsKey(i)) continue;

                Material material = rendererList[i].material;
                MaterialContent materialContent = dicMaterials[i];
                Dictionary<string, Texture2D> dicTexture = materialContent.currentTextures;

                // Set all textures
                for (int j = 0; j < dicTexture.Count; j++)
                {
                    string name = dicTexture.ElementAt(j).Key;
                    Texture2D texture = dicTexture.ElementAt(j).Value;
                    if (texture == null) continue;
                    material.SetTexture(name, texture);
                }
            }
        }

        public static GameObject GetClothes(ChaControl chaControl, int kind)
        {
            return chaControl.ObjClothes[kind];
        }

        // ================================================== Construct Section ==================================================
        public static void MakeClothesContent(CvsC_Clothes clothesControl)
        {
            // Cleaning UI content
            for (int i = clothesTabContent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(clothesTabContent.transform.GetChild(i).gameObject);
            }

            // Cleaning old miniatures
            for (int i = 0; i < miniatureTextures.Count; i++)
            {
                GarbageTextures.Add(miniatureTextures[i]);
                GarbageImages.Add(miniatureImages[i]);
            }
            miniatureTextures.Clear();

            ChaControl charaControl = clothesControl.chaCtrl;
            string characterName = charaControl.gameObject.name;

            // index according to enum ChaFileDefine.ClothesKind
            int kind = clothesControl.SNo;
            GameObject clothesPiece = GetClothes(charaControl, kind);

            // Stored textures for this kind (piece) of clothing
            CharacterContent characterContent = CharactersLoaded[characterName];
            Dictionary<int, MaterialContent> dicMaterials;
            if (kind == 0) dicMaterials = characterContent.clothesTop;
            else if (kind == 1) dicMaterials = characterContent.clothesBottom;
            else return;

            // Create one button for each material
            Renderer[] renderList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderList.Length; i++)
            {
                // Getting Texture list from material
                Material material = renderList[i].material;
                string materialName = material.name.Replace("(Instance)", "").Trim() + "-" + renderList[i].transform.parent.name;
                Debug.Log("\r\n===== Material Name: " + materialName);
                Dictionary<string, Texture2D> materialTextures = GetMaterialTextures(material);


                if (!dicMaterials.ContainsKey(i)) dicMaterials.Add(i, new MaterialContent());
                MaterialContent materialContent = dicMaterials[i];
                if (materialContent.currentTextures == null) materialContent.currentTextures = new Dictionary<string, Texture2D>();
                Dictionary<string, Texture2D> storedTextues = materialContent.currentTextures;
                if (materialContent.originalTextures == null) materialContent.originalTextures = new Dictionary<string, Texture2D>();
                Dictionary<string, Texture2D> originalTextures = materialContent.currentTextures;

                // Creating one texture block for each texture
                for (int j = 0; j < materialTextures.Count; j++)
                {
                    string textureName = materialTextures.ElementAt(j).Key;
                    Texture2D materialTexture = materialTextures[textureName];

                    CreateTexturesBlock(material, materialTexture, textureName, materialContent, clothesTabContent);
                }
            }
        }

        public static void CreateTexturesBlock(Material material, Texture2D materialTexture, string textureName, MaterialContent materialContent, GameObject parent)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + textureName);
            textureGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Clothes Image
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * materialTexture.width / materialTexture.height;
            else height = width * materialTexture.height / materialTexture.width;

            Image miniature = RG_MaterialModUI.CreateImage(width, height);
            miniature.transform.SetParent(textureGroup.transform, false);
            UpdateMiniature(miniature, materialTexture, textureName);

            // Text with size
            string textContent = "Size: " + materialTexture.width.ToString() + "x" + materialTexture.height.ToString();
            Text text = RG_MaterialModUI.CreateText(textContent, 17, 200, 20);
            text.transform.SetParent(textureGroup.transform, false);

            // Clothes Set Button
            Button buttonSet = RG_MaterialModUI.CreateButton("Green  " + textureName, 16, 200, 35);
            buttonSet.onClick.AddListener((UnityAction)delegate { SetTextureButton(material, materialTexture, textureName, materialContent, miniature); });
            buttonSet.transform.SetParent(textureGroup.transform, false);

            // Clothes Reset Button
            Button buttonReset = RG_MaterialModUI.CreateButton("Reset " + textureName, 16, 200, 35);
            buttonReset.onClick.AddListener((UnityAction)delegate { ResetTextureButton(material, materialTexture, textureName, materialContent, miniature); });
            buttonReset.transform.SetParent(textureGroup.transform, false);

            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void UpdateMiniature(Image miniature, Texture2D texture, string textureName)
        {
            // maitaining proportions
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * texture.width / texture.height;
            else height = width * texture.height / texture.width;

            Texture2D scaledTexture = Resize(texture, width, height);

            // Trom pink maps to regular normal maps
            if (textureName.Contains("Bump"))
            {
                scaledTexture = DXT2nmToNormal(scaledTexture);
            }

            miniature.sprite = Sprite.Create(scaledTexture, new Rect(0, 0, width, height), new Vector2());
            miniatureTextures.Add(scaledTexture);
            miniatureImages.Add(miniature);
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void SetTextureButton(Material material, Texture2D materialTexture, string textureName, MaterialContent materialContent, Image miniature)
        {
            // In the future the load texture will be here
            Texture2D texture = new Texture2D(512, 512);
            texture = GreenTexture(512, 512);

            if (!materialContent.currentTextures.ContainsKey(textureName)) materialContent.currentTextures.Add(textureName, null);
            if (!materialContent.originalTextures.ContainsKey(textureName)) materialContent.originalTextures.Add(textureName, null);

            // Storing original texture
            if (materialContent.originalTextures[textureName] == null) materialContent.originalTextures[textureName] = materialTexture;

            // Reset old texture
            if (!(materialContent.currentTextures[textureName] == null)) GarbageTextures.Add(materialContent.currentTextures[textureName]);

            // Update Texture
            materialContent.currentTextures[textureName] = texture;
            material.SetTexture(textureName, materialContent.currentTextures[textureName]);

            // Update miniature
            UpdateMiniature(miniature, materialContent.currentTextures[textureName], textureName);

            DestroyGarbage();
        }

        // ================================================== Cleaning Section ==================================================
        public static void ResetTextureButton(Material material, Texture2D materialTexture, string textureName, MaterialContent materialContent, Image miniature)
        {
            if (!materialContent.currentTextures.ContainsKey(textureName)) return;

            material.SetTexture(textureName, materialContent.originalTextures[textureName]);
            UpdateMiniature(miniature, materialContent.originalTextures[textureName], textureName);

            // cleaning texture and entrances
            if (materialContent.currentTextures.ContainsKey(textureName)) GarbageTextures.Add(materialContent.currentTextures[textureName]);
            materialContent.currentTextures.Remove(textureName);

            DestroyGarbage();
        }

        public static void ResetKind(string characterName, int Kind)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            Dictionary<int, MaterialContent> dicMaterials;
            if (Kind == 0) dicMaterials = characterContent.clothesTop;
            else if (Kind == 1) dicMaterials = characterContent.clothesBottom;
            else return;

            if (dicMaterials.Count <= 0) return;

            for (int i = dicMaterials.Count -1; i >= 0; i--)
            {
                MaterialContent materialContent = dicMaterials.ElementAt(i).Value;
                Dictionary<string, Texture2D> storedTextures = materialContent.currentTextures;
                Dictionary<string, Texture2D> originalTextures = materialContent.originalTextures;

                for (int j = storedTextures.Count - 1; j >= 0; j--)
                {
                    string textureName = storedTextures.ElementAt(j).Key;
                    Texture2D storedTexture = storedTextures[textureName];
                    GarbageTextures.Add(storedTexture);
                    storedTexture = null;
                    storedTextures.Remove(textureName);
                }
                storedTextures = null;

                for (int j = originalTextures.Count - 1; j >= 0; j--)
                {
                    string textureName = originalTextures.ElementAt(j).Key;
                    Texture2D originalTexture = originalTextures[textureName];
                    GarbageTextures.Add(originalTexture);
                    originalTexture = null;
                    originalTextures.Remove(textureName);
                }
                originalTextures = null;

                dicMaterials.Remove(i);
            }

            dicMaterials = null;

            DestroyGarbage();
        }

        static void DestroyGarbage()
        {
            // Destroy textures, up to 30 per second
            for (int i = 0; i < GarbageTextures.Count; i++)
            {
                UnityEngine.Object.Destroy(GarbageTextures[i], i * 0.034f);
            }

            // Destroy images, up to 30 per second
            for (int i = 0; i < GarbageImages.Count; i++)
            {
                UnityEngine.Object.Destroy(GarbageImages[i], i * 0.034f + 0.017f);
            }

            GarbageTextures.Clear();
            GarbageImages.Clear();
        }


    }
}
