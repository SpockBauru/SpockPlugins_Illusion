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

        // Maker Objects: Clothes Tab
        public static GameObject clothesSelectMenu;
        public static UI_ToggleEx clothesTab;

        public static GameObject clothesSettingsGroup;
        public static GameObject clothesTabContent;
        

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
            /// <br> MaterialContent = clothes[kind][renderIndex]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, MaterialContent>> clothes = new Dictionary<int, Dictionary<int, MaterialContent>>();
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
            if (!characterContent.clothes.ContainsKey(kind)) return;
            Dictionary<int, MaterialContent> dicMaterials = characterContent.clothes[kind];

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



        public static void ResetKind(string characterName, int kind)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            if (!characterContent.clothes.ContainsKey(kind)) return;
            Dictionary<int, MaterialContent> dicMaterials = characterContent.clothes[kind];

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
