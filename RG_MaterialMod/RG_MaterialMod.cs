using System;
using System.Collections;
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

        // Miniature cache
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
            /// <br>Key: Texture kind from rom enum ChaFileDefine.ClothesKind</br>
            /// <br>Value: TextureContent</br>
            /// </summary>
            public Dictionary<int, List<TextureContent>> clothesTextures = new Dictionary<int, List<TextureContent>>();
        }

        public class TextureContent
        {
            public int textureType; // Get from the enum TextureType
            public int kind;        //Texture kind (clothing piece) from enum ChaFileDefine.ClothesKind
            public Texture2D texture;
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
            var dicTexture = characterContent.clothesTextures;
            if (dicTexture == null) return;
            if (!dicTexture.ContainsKey(kind)) return;
            var textureList = dicTexture[kind];

            ChaControl charaControl = GameObject.Find(characterName).GetComponent<ChaControl>();
            GameObject clothesPiece = GetClothes(charaControl, kind);
            Renderer[] rendererList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < rendererList.Length; i++)
            {
                Material material = rendererList[i].material;

                // in the future will loop with the textures inside material
                var textureContent = textureList[i];

                Texture2D texture = textureContent.texture;
                if (texture == null) continue;
                SetTexture(material, texture);
            }
        }

        public static GameObject GetClothes(ChaControl chaControl, int kind)
        {
            return chaControl.ObjClothes[kind];
        }

        public static void SetTexture(Material material, Texture2D texture)
        {
            material.mainTexture = texture;
        }

        // ================================================== Construct Section ==================================================
        public static void MakeClothesContent(CvsC_Clothes clothesControl)
        {
            // Cleaning UI content
            for (int i = RG_MaterialModUI.clothesTabContent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(RG_MaterialModUI.clothesTabContent.transform.GetChild(i).gameObject);
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
            var dicKindTextures = characterContent.clothesTextures;
            if (!dicKindTextures.ContainsKey(kind)) dicKindTextures.Add(kind, new List<TextureContent>());
            var textureList = dicKindTextures[kind];
            for (int i = 0; i <= 8; i++) textureList.Add(new TextureContent());

            // Create one button for each material
            Renderer[] renderList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderList.Length; i++)
            {
                Material material = renderList[i].material;

                CreateClothesBlock(material, characterName, kind, i);
            }
        }

        public static void CreateClothesBlock(Material material, string characterName, int kind, int textureNumber)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + textureNumber);
            textureGroup.transform.SetParent(RG_MaterialModUI.clothesTabContent.transform);
            textureGroup.transform.localScale = Vector3.one;
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;

            // Clothes Image
            int size = 200;
            Texture2D mainTexture2D = ToTexture2D(material.mainTexture);
            Texture2D scaledTexture = Resize(mainTexture2D, size, size);
            Image textureImage = RG_MaterialModUI.CreateTextureImage(scaledTexture, size);

            GarbageTextures.Add(mainTexture2D);
            miniatureTextures.Add(scaledTexture);
            miniatureImages.Add(textureImage);

            textureImage.transform.SetParent(textureGroup.transform);
            textureImage.transform.localScale = Vector3.one;

            // Clothes Set Button
            Button buttonSet = RG_MaterialModUI.CreateClothesButton("Set Green Texture " + textureNumber.ToString());
            buttonSet.onClick.AddListener((UnityAction)delegate { SetClothesTexture(characterName, kind, textureNumber); });
            buttonSet.transform.SetParent(textureGroup.transform);
            buttonSet.transform.localScale = Vector3.one;

            // Clothes Reset Button
            Button buttonReset = RG_MaterialModUI.CreateClothesButton("Reset Texture " + textureNumber.ToString());
            buttonReset.onClick.AddListener((UnityAction)delegate { RemoveKindTexture(characterName, kind, textureNumber); });
            buttonReset.transform.SetParent(textureGroup.transform);
            buttonReset.transform.localScale = Vector3.one;
        }

        public static void SetClothesTexture(string characterName, int kind, int textureNumber)
        {
            // In the future the load texture will be here
            Texture2D texture = new Texture2D(512, 512);
            texture = GreenTexture(512, 512);

            CharacterContent characterContent = CharactersLoaded[characterName];
            var textureList = characterContent.clothesTextures[kind];
            TextureContent textureContent = textureList[textureNumber];

            // set old texture to destroy
            if (textureContent.texture != null) GarbageTextures.Add(textureContent.texture);

            // Updated the texture data
            textureContent.texture = texture;
            textureContent.kind = kind;
            textureContent.textureType = (int)TextureType.generic;

            RefreshClothesMaterial(characterName, kind);
        }

        // ================================================== Cleaning Section ==================================================
        public static void CreateClothesResetButton(string characterName, int kind, int textureNumber)
        {
            // Creating Button
            Button button = RG_MaterialModUI.CreateClothesButton("Reset Texture " + textureNumber.ToString());
            button.onClick.AddListener((UnityAction)delegate { RemoveKindTexture(characterName, kind, textureNumber); });
        }

        public static void RemoveKindTexture(string characterName, int kind, int textureNumber)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            var dicTexture = characterContent.clothesTextures;

            if (dicTexture == null) return;
            if (!dicTexture.ContainsKey(kind)) return;

            var textureList = dicTexture[kind];
            TextureContent textureContent = textureList[textureNumber];

            // cleaning texture and entrances
            if (textureContent.texture != null) GarbageTextures.Add(textureContent.texture);
            textureContent.texture = null;
            textureContent.textureType = -1;

            ChaControl charaControl = GameObject.Find(characterName).GetComponent<ChaControl>();
            charaControl.ChangeCustomClothes(kind, true, false, false, false);

            DestroyGarbage();
        }

        public static void ResetKind(string characterName, int Kind)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            var dicTexture = characterContent.clothesTextures;

            if (dicTexture == null) return;
            if (!dicTexture.ContainsKey(Kind)) return;

            var textureList = dicTexture[Kind];

            for (int i = textureList.Count - 1; i >= 0; i--)
            {
                var textureContent = textureList[i];

                GarbageTextures.Add(textureContent.texture);

                textureContent.texture = null;
                textureContent.textureType = -1;
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
