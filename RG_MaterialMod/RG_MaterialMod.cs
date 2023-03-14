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
        public class CharacterContent
        {
            /// <summary>
            /// <br>Key: Texture material number</br>
            /// <br>Value: TextureContent</br>
            /// </summary>
            public Dictionary<int, List<TextureContent>> clothesTop = new Dictionary<int, List<TextureContent>>();
        }

        public class TextureContent
        {
            public string textureName;
            public Texture2D currentTexture;
            public Texture2D originalTexture;
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

        public static void RefreshClothesMaterial(string characterName, int kind)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            Dictionary<int, List<TextureContent>> dicTexture;
            if (kind == 0) dicTexture = characterContent.clothesTop;
            else return;

            if (dicTexture == null) return;

            ChaControl charaControl = GameObject.Find(characterName).GetComponent<ChaControl>();
            GameObject clothesPiece = GetClothes(charaControl, kind);
            Renderer[] rendererList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < rendererList.Length; i++)
            {
                Material material = rendererList[i].material;

                // in the future will loop with the textures inside material
                if (!dicTexture.ContainsKey(i)) continue;
                List<TextureContent> textureList = dicTexture[i];

                for (int j = 0; j < textureList.Count; j++) 
                {
                    TextureContent texture = textureList[i];
                    if (texture == null) continue;
                    SetModTexture(material, texture);
                }
            }
        }

        public static GameObject GetClothes(ChaControl chaControl, int kind)
        {
            return chaControl.ObjClothes[kind];
        }

        public static void SetModTexture(Material material, TextureContent textureContent)
        {
            material.SetTexture(textureContent.textureName, textureContent.currentTexture);
        }

        public static void SetOriginalTexture(Material material, TextureContent textureContent)
        {
            material.SetTexture(textureContent.textureName, textureContent.originalTexture);
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

            // List of stored textures for this kind (piece) of clothing
            CharacterContent characterContent = CharactersLoaded[characterName];
            Dictionary<int, List<TextureContent>> dicMaterialsTextures;
            if (kind == 0) dicMaterialsTextures = characterContent.clothesTop;
            else return;

            // Create one button for each material
            Renderer[] renderList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderList.Length; i++)
            {
                // Getting Texture list from material
                Material material = renderList[i].material;
                Debug.Log("\r\n===== Material number: " + i.ToString());
                List<TextureContent> materialTextures = GetMaterialTextures(material);

                // Getting stored texture list
                List<TextureContent> storedTextures;
                if (!dicMaterialsTextures.ContainsKey(i))
                {
                    dicMaterialsTextures.Add(i, new List<TextureContent>());
                    storedTextures = dicMaterialsTextures[i];
                    for (int j = 0; j < materialTextures.Count; j++)
                        storedTextures.Add(new TextureContent());
                }
                else
                {
                    storedTextures = dicMaterialsTextures[i];
                }

                // Creating one texture block for each texture
                for (int j = 0; j < materialTextures.Count; j++)
                    CreateTexturesBlock(material, materialTextures[j], storedTextures[j], clothesTabContent);
            }
        }


        public static void CreateTexturesBlock(Material material, TextureContent materialTexture, TextureContent storedTexture, GameObject parent)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + materialTexture.textureName);
            textureGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Clothes Image
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * materialTexture.currentTexture.width / materialTexture.currentTexture.height;
            else height = width * materialTexture.currentTexture.height / materialTexture.currentTexture.width;

            Image miniature = RG_MaterialModUI.CreateImage(width, height);
            miniature.transform.SetParent(textureGroup.transform, false);
            UpdateMiniature(miniature, materialTexture);

            // Text with size
            string textContent = "Size: " + materialTexture.currentTexture.width.ToString() + "x" + materialTexture.currentTexture.height.ToString();
            Text text = RG_MaterialModUI.CreateText(textContent, 17, 200, 35);
            text.transform.SetParent(textureGroup.transform, false);

            // Clothes Set Button
            Button buttonSet = RG_MaterialModUI.CreateButton("Green  " + materialTexture.textureName, 16, 200, 35);
            buttonSet.onClick.AddListener((UnityAction)delegate { SetTextureButton(material, materialTexture, storedTexture, miniature); });
            buttonSet.transform.SetParent(textureGroup.transform, false);

            // Clothes Reset Button
            Button buttonReset = RG_MaterialModUI.CreateButton("Reset " + materialTexture.textureName, 16, 200, 35);
            buttonReset.onClick.AddListener((UnityAction)delegate { ResetTextureButton(material, materialTexture, storedTexture, miniature); });
            buttonReset.transform.SetParent(textureGroup.transform, false);

            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void UpdateMiniature(Image miniature, TextureContent textureContent)
        {
            Texture2D texture2D = textureContent.currentTexture;

            // maitaining proportions
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * texture2D.width / texture2D.height;
            else height = width * texture2D.height / texture2D.width;

            Texture2D scaledTexture = Resize(texture2D, width, height);

            // Trom pink maps to regular normal maps
            if (textureContent.textureName.Contains("Bump"))
            {
                scaledTexture = DXT2nmToNormal(scaledTexture);
            }

            miniature.sprite = Sprite.Create(scaledTexture, new Rect(0, 0, width, height), new Vector2());
            GarbageTextures.Add(texture2D);
            miniatureTextures.Add(scaledTexture);
            miniatureImages.Add(miniature);
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void SetTextureButton(Material material, TextureContent materialTexture, TextureContent storedTexture, Image miniature)
        {
            // In the future the load texture will be here
            Texture2D texture = new Texture2D(512, 512);
            texture = GreenTexture(512, 512);

            // Storing original texture
            if (storedTexture.originalTexture == null) storedTexture.originalTexture = materialTexture.currentTexture;

            // Reset old texture
            if (!(storedTexture.currentTexture == null)) GarbageTextures.Add(storedTexture.currentTexture);

            // Update Texture
            storedTexture.currentTexture = texture;
            storedTexture.textureName = materialTexture.textureName;
            SetModTexture(material, storedTexture);

            // Update miniature
            UpdateMiniature(miniature, storedTexture);
        }

        // ================================================== Cleaning Section ==================================================
        public static void ResetTextureButton(Material material, TextureContent materialTexture, TextureContent storedTexture, Image miniature)
        {
            SetOriginalTexture(material, storedTexture);

            // cleaning texture and entrances
            if (storedTexture.currentTexture != null) GarbageTextures.Add(storedTexture.currentTexture);
            storedTexture.currentTexture = null;

            // Updating miniature
            UpdateMiniature(miniature, materialTexture);
        }

        public static void ResetKind(string characterName, int Kind)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            var dicTexture = characterContent.clothesTop;

            if (dicTexture == null) return;
            if (!dicTexture.ContainsKey(Kind)) return;

            var textureList = dicTexture[Kind];

            for (int i = textureList.Count - 1; i >= 0; i--)
            {
                var textureContent = textureList[i];
                GarbageTextures.Add(textureContent.currentTexture);
                textureContent.currentTexture = null;
            }

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
                UnityEngine.Object.Destroy(GarbageImages[i], i * 0.034f);
            }

            GarbageTextures.Clear();
            GarbageImages.Clear();
        }

        // ================================================== Texture Tools ==================================================
        public static Texture2D DXT2nmToNormal(Texture2D texture)
        {
            Color[] colorArray = texture.GetPixels(0);
            float x, y, z, polyfit;

            for (int i = 0; i < colorArray.Length; i++)
            {
                // DXT5nm channel swap
                colorArray[i].r = colorArray[i].a;
                colorArray[i].a = 1;

                // Taking off Illusion processing
                y = colorArray[i].g;
                polyfit = (-0.142436f * y * y) + 0.146477f * y - 0.001472f;  // Got this from polynomial fit (Excel File in project root)
                colorArray[i].g = (y - polyfit) * (y - polyfit);

                // Recovering z axis
                x = colorArray[i].r * 2 - 1;
                y = colorArray[i].g * 2 - 1;
                z = Mathf.Sqrt(1 - (x * x) - (y * y));
                colorArray[i].b = z * 0.5f + 0.5f;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(true);

            return texture;
        }

        public static Texture2D NormalToDXT2nm(Texture2D texture)
        {
            Color[] colorArray = texture.GetPixels(0);
            float y, polyfit;

            for (int i = 0; i < colorArray.Length; i++)
            {
                // Applying Illusion processing
                y = colorArray[i].g;
                polyfit = (-0.142436f * y * y) + 0.146477f * y - 0.001472f;  // Got this from polynomial fit (Excel File in project root)
                colorArray[i].g = Mathf.Sqrt(y) + polyfit;

                // DXT5nm channel swap
                colorArray[i].a = colorArray[i].r;
                colorArray[i].b = colorArray[i].g;
                colorArray[i].r = 1;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(true);

            return texture;
        }

        /// <summary>
        /// Get all textures from material and turns into a list of TextureContents
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static List<TextureContent> GetMaterialTextures(Material material)
        {
            List<TextureContent> textureContentList = new List<TextureContent>();

            Shader shader = material.shader;

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string propertyName = shader.GetPropertyName(i);
                var propertyType = shader.GetPropertyType(i);

                //if (propertyType == UnityEngine.Rendering.ShaderPropertyType.Range)
                //{
                //    var shaderFloat = material.GetFloat(propertyName);
                //    Debug.Log("propertyType:" + propertyType + " propertyName: " + propertyName + " Value: " + shaderFloat);
                //}

                if (propertyType == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    Texture texture = material.GetTexture(propertyName);
                    if (texture == null) continue;

                    TextureContent textureContent = new TextureContent();
                    textureContent.textureName = propertyName;
                    Texture2D texture2D = ToTexture2D(texture);
                    textureContent.currentTexture = texture2D;
                    textureContentList.Add(textureContent);

                    GarbageTextures.Add(texture2D);
                }
            }
            return textureContentList;
        }

        /// <summary>
        /// Converts Texture into Texture2D. Texture2D can be applyed directly to the material later
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static Texture2D ToTexture2D(Texture texture)
        {
            Texture2D texture2D = new Texture2D(texture.width, texture.height);
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0);
            RenderTexture currentRT = RenderTexture.active;

            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            texture2D.Apply(true);
            return texture2D;
        }

        public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            Texture2D result = new Texture2D(targetX, targetY);
            RenderTexture renderTexture = RenderTexture.GetTemporary(targetX, targetY, 0);
            RenderTexture currentRT = RenderTexture.active;

            Graphics.Blit(texture2D, renderTexture);
            RenderTexture.active = renderTexture;
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            result.Apply(true);
            return result;
        }

        /// <summary>
        /// Generate a square texture with the desired size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        static Texture2D GreenTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);

            // Making the Texture2D Green
            Color[] colorArray = texture.GetPixels(0);
            for (int x = 0; x < colorArray.Length; x++)
            {
                colorArray[x].r = 0;
                colorArray[x].g = 1;
                colorArray[x].b = 0;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(true);

            return texture;
        }
    }
}
