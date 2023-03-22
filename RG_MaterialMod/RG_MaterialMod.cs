using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
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
    [BepInDependency("com.bogus.RGExtendedSave")]
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class RG_MaterialMod : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.MaterialMod";
        public const string PluginName = "MaterialMod";
        public const string Version = "0.1";
        public const string PluginNameInternal = Constants.Prefix + "MaterialMod";

        // Maker Objects: Clothes Tab - Initialized in Hooks
        public static GameObject clothesSelectMenu;
        public static UI_ToggleEx clothesTab;
        public static GameObject clothesSettingsGroup;
        public static GameObject clothesTabContent;
        // Maker Objects: Accessory Tab - Initialized in Hooks
        public static GameObject accessorySelectMenu;
        public static UI_ToggleEx accessoryTab;
        public static GameObject accessorySettingsGroup;
        public static GameObject accessoryTabContent;

        // Unity don't destroy textures automatically, need to do manually
        static List<Texture2D> GarbageTextures = new List<Texture2D>();
        static List<Image> GarbageImages = new List<Image>();

        // Miniatures
        static int miniatureSize = 180;
        static List<Texture2D> miniatureTextures = new List<Texture2D>();
        static List<Image> miniatureImages = new List<Image>();

        /// <summary>
        /// Key: Name of Character's GameObject, Value: class CharacterContent
        /// </summary>
        public static Dictionary<string, CharacterContent> CharactersLoaded = new Dictionary<string, CharacterContent>();

        /// <summary>
        /// Every MaterialMod content for this character goes here
        /// </summary>
        [Serializable]
        public class CharacterContent
        {
            // IMPORTANT: KEEP TRACK OF THIS ACCROSS FILES
            public bool enableSetTextures = true;
            public GameObject characterObject;
            public ChaControl chaControl;
            public ChaFile chafile;
            public ChaFileDefine.CoordinateType currentCoordinate = ChaFileDefine.CoordinateType.Outer;


            /// <summary>
            /// <br> TextureByte = clothesTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> clothesTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            /// <summary>
            /// <br> Original Texture = clothesTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> originalClothesTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();
        
            /// <summary>
            /// <br> TextureByte = clothesTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> accessoryTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            /// <summary>
            /// <br> Original Texture = clothesTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> originalAccessoryTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();
        }

        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
        }

        public enum TextureDictionaries
        {
            clothesTextures,
            accessoryTextures
        }

        public static void SetAllTextures(string characterName)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            ChaControl chaControl = characterContent.chaControl;


            var objects = chaControl.ObjClothes;
            SetAllDictionary(characterContent, objects, characterContent.clothesTextures);

            objects = chaControl.ObjAccessory;
            SetAllDictionary(characterContent, objects, characterContent.accessoryTextures);
        }

        private static void SetAllDictionary(CharacterContent characterContent, UnhollowerBaseLib.Il2CppReferenceArray<GameObject> objects, Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures)
        {
            if (dicTextures == null) return;

            int currentCoordinate = (int)characterContent.currentCoordinate;
            if (!dicTextures.ContainsKey(currentCoordinate)) return;

            var coordinate = dicTextures[(int)characterContent.currentCoordinate];

            for (int j = 0; j < coordinate.Count; j++)
            {
                if (coordinate.ElementAt(j).Value == null) continue;
                int kindIndex = coordinate.ElementAt(j).Key;
                var kind = coordinate[kindIndex];

                var rendererList = objects[kindIndex].GetComponentsInChildren<Renderer>(true);

                for (int k = 0; k < kind.Count; k++)
                {
                    if (kind.ElementAt(k).Value == null) continue;
                    int rendererIndex = kind.ElementAt(k).Key;
                    var storedRenderer = kind[rendererIndex];

                    Material material = rendererList[rendererIndex].material;

                    for (int l = 0; l < storedRenderer.Count; l++)
                    {
                        if (storedRenderer.ElementAt(l).Key == null) continue;
                        string textureName = storedRenderer.ElementAt(l).Key;

                        if (storedRenderer.ElementAt(l).Value == null) continue;

                        // Loading the bytes data that are encoded into png
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(storedRenderer[textureName]);
                        material.SetTexture(textureName, texture);
                    }
                }
            }
        }

        public static void SetClothesKind(string characterName, TextureDictionaries texDictionary, int kindIndex)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            GameObject characterObject = characterContent.characterObject;
            ChaControl chaControl = characterObject.GetComponent<ChaControl>();
            int coordinateIndex = (int)characterContent.currentCoordinate;

            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;
            GameObject itemObject;
            if (texDictionary == TextureDictionaries.clothesTextures)
            {
                dicTextures = characterContent.clothesTextures;
                itemObject = chaControl.ObjClothes[kindIndex];
            }
            else if (texDictionary == TextureDictionaries.accessoryTextures)
            {
                dicTextures = characterContent.accessoryTextures;
                itemObject = chaControl.ObjAccessory[kindIndex];
            }
            else return;


            if (!dicTextures.ContainsKey(coordinateIndex)) return;
            var coordinate = dicTextures[coordinateIndex];

            if (!coordinate.ContainsKey(kindIndex)) return;
            var kind = coordinate[kindIndex];

            var rendererList = itemObject.GetComponentsInChildren<Renderer>(true);

            for (int k = 0; k < kind.Count; k++)
            {
                if (kind.ElementAt(k).Value == null) continue;
                int rendererIndex = kind.ElementAt(k).Key;
                var storedRenderer = kind[rendererIndex];

                Material material = rendererList[rendererIndex].material;

                for (int l = 0; l < storedRenderer.Count; l++)
                {
                    if (storedRenderer.ElementAt(l).Key == null) continue;
                    string textureName = storedRenderer.ElementAt(l).Key;

                    if (storedRenderer.ElementAt(l).Value == null) continue;

                    // Loading the bytes data that are encoded into png
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(storedRenderer[textureName]);
                    material.SetTexture(textureName, texture);
                }
            }
        }

        public static void ResetAllClothes(string characterName)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            ResetAllDictionary(characterContent.clothesTextures);
            ResetAllDictionary(characterContent.accessoryTextures);
        }

        private static void ResetAllDictionary(Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures)
        {
            for (int i = 0; i < dicTextures.Count; i++)
            {
                int coordinateIndex = dicTextures.ElementAt(i).Key;
                var coordinate = dicTextures[coordinateIndex];

                for (int j = 0; j < coordinate.Count; j++)
                {
                    int kindIndex = coordinate.ElementAt(j).Key;
                    var kind = dicTextures[coordinateIndex][kindIndex];
                    for (int k = 0; k < kind.Count; k++)
                    {
                        int rendererIndex = kind.ElementAt(k).Key;
                        var renderer = dicTextures[coordinateIndex][kindIndex][rendererIndex];

                        for (int l = 0; l < renderer.Count; l++)
                        {
                            string textureIndex = renderer.ElementAt(l).Key;
                            renderer[textureIndex] = null;
                            //GarbageTextures.Add(characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex][textureIndex]);
                        }
                        dicTextures[coordinateIndex][kindIndex][rendererIndex].Clear();
                    }
                    dicTextures[coordinateIndex][kindIndex].Clear();
                }
                dicTextures[coordinateIndex].Clear();
            }
            dicTextures.Clear();
            DestroyGarbage();
        }

        public static void ResetKind(string characterName, TextureDictionaries texDictionary, int kindIndex)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            int coordinateIndex = (int)characterContent.currentCoordinate;

            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;
            if (texDictionary == TextureDictionaries.clothesTextures) dicTextures = characterContent.clothesTextures;
            else if (texDictionary == TextureDictionaries.accessoryTextures) dicTextures = characterContent.accessoryTextures;
            else return;

            if (!dicTextures.ContainsKey(coordinateIndex)) return;
            var coordinate = dicTextures[coordinateIndex];

            if (!coordinate.ContainsKey(kindIndex)) return;

            // cleaning textures 
            var kind = dicTextures[coordinateIndex][kindIndex];
            for (int k = 0; k < kind.Count; k++)
            {
                int rendererIndex = kind.ElementAt(k).Key;
                var renderer = dicTextures[coordinateIndex][kindIndex][rendererIndex];

                for (int l = 0; l < renderer.Count; l++)
                {
                    string textureIndex = renderer.ElementAt(l).Key;
                    renderer[textureIndex] = null;
                    //GarbageTextures.Add(characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex][textureIndex]);
                }
                dicTextures[coordinateIndex][kindIndex][rendererIndex].Clear();
            }
            dicTextures[coordinateIndex][kindIndex].Clear();

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
