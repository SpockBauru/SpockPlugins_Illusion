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


        // Unity don't destroy textures automatically, need to do manually
        static List<Texture2D> GarbageTextures = new List<Texture2D>();
        static List<Image> GarbageImages = new List<Image>();

        // Miniatures
        static int miniatureSize = 200;
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

        public static void SetAllClothesTextures(string characterName)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            ChaControl chaControl = characterContent.chaControl;

            var clothesTextures = characterContent.clothesTextures;
            if (clothesTextures == null) return;

            int currentCoordinate = (int)characterContent.currentCoordinate;
            Debug.Log("SetAllClothesTextures: currentCoordinate " + currentCoordinate.ToString());
            if (!clothesTextures.ContainsKey(currentCoordinate)) return;

            var coordinate = clothesTextures[(int)characterContent.currentCoordinate];

            for (int j = 0; j < coordinate.Count; j++)
            {
                if (coordinate.ElementAt(j).Value == null) continue;
                int kindIndex = coordinate.ElementAt(j).Key;
                var kind = coordinate[kindIndex];

                var rendererList = chaControl.ObjClothes[kindIndex].GetComponentsInChildren<Renderer>(true);

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
                        Debug.Log("coordinate: " + j + " kind: " + k + " storedRenderer: " + l + " textureName: " + textureName);
                    }
                }
            }
        }

        public static void SetClothesKind(string characterName, int kindIndex)
        {
            CharacterContent characterContent = CharactersLoaded[characterName];
            GameObject characterObject = characterContent.characterObject;
            ChaControl chaControl = characterObject.GetComponent<ChaControl>();
            var clothesTextures = characterContent.clothesTextures;
            int coordinateIndex = (int)characterContent.currentCoordinate;
            Debug.Log("SetClothesKind: Coordinate " + coordinateIndex + " kind " + kindIndex);

            if (!clothesTextures.ContainsKey(coordinateIndex)) return;
            var coordinate = clothesTextures[coordinateIndex];

            if (!coordinate.ContainsKey(kindIndex)) return;
            var kind = coordinate[kindIndex];

            var rendererList = chaControl.ObjClothes[kindIndex].GetComponentsInChildren<Renderer>(true);

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
                    Debug.Log("coordinate: " + coordinateIndex + " kind: " + kindIndex + " storedRenderer: " + l + " textureName: " + textureName);
                }
            }
        }

        public static void ResetAllClothes(string characterName)
        {
            Debug.Log("ResetAllClothes");
            CharacterContent characterContent = CharactersLoaded[characterName];
            GameObject characterObject = characterContent.characterObject;
            ChaControl chaControl = characterObject.GetComponent<ChaControl>();

            for (int i = 0; i < characterContent.clothesTextures.Count; i++)
            {
                int coordinateIndex = characterContent.clothesTextures.ElementAt(i).Key;
                var coordinate = characterContent.clothesTextures[coordinateIndex];

                for (int j = 0; j < coordinate.Count; j++)
                {
                    int kindIndex = coordinate.ElementAt(j).Key;
                    var kind = characterContent.clothesTextures[coordinateIndex][kindIndex];
                    for (int k = 0; k < kind.Count; k++)
                    {
                        int rendererIndex = kind.ElementAt(k).Key;
                        var renderer = characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex];

                        for (int l = 0; l < renderer.Count; l++)
                        {
                            string textureIndex = renderer.ElementAt(l).Key;
                            //GarbageTextures.Add(characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex][textureIndex]);
                        }
                        characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex].Clear();
                    }
                    characterContent.clothesTextures[coordinateIndex][kindIndex].Clear();
                }
                characterContent.clothesTextures[coordinateIndex].Clear();
            }
            characterContent.clothesTextures.Clear();
            DestroyGarbage();
        }

        public static void ResetKind(string characterName, int kindIndex)
        {
            Debug.Log("ResetKind");
            CharacterContent characterContent = CharactersLoaded[characterName];
            GameObject characterObject = characterContent.characterObject;
            ChaControl chaControl = characterObject.GetComponent<ChaControl>();
            var clothesTextures = characterContent.clothesTextures;
            int coordinateIndex = (int)characterContent.currentCoordinate;

            if (!clothesTextures.ContainsKey(coordinateIndex)) return;
            var coordinate = clothesTextures[coordinateIndex];

            if (!coordinate.ContainsKey(kindIndex)) return;

            // cleaning textures 
            var kind = characterContent.clothesTextures[coordinateIndex][kindIndex];
            for (int k = 0; k < kind.Count; k++)
            {
                int rendererIndex = kind.ElementAt(k).Key;
                var renderer = characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex];

                for (int l = 0; l < renderer.Count; l++)
                {
                    string textureIndex = renderer.ElementAt(l).Key;
                    //GarbageTextures.Add(characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex][textureIndex]);
                }
                characterContent.clothesTextures[coordinateIndex][kindIndex][rendererIndex].Clear();
            }
            characterContent.clothesTextures[coordinateIndex][kindIndex].Clear();

            DestroyGarbage();
        }

        static void DestroyGarbage()
        {
            Debug.Log("DestroyGarbage");
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
