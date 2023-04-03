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
    [BepInDependency("com.bogus.RGExtendedSave")]
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class RG_MaterialMod : BasePlugin
    {
        // Plugin consts
        public const string GUID = "SpockBauru.MaterialMod";
        public const string PluginName = "MaterialMod";
        public const string Version = "0.1.1";
        public const string PluginNameInternal = Constants.Prefix + "_MaterialMod";

        // Maker Objects: Clothes Tab - Initialized in Hooks
        internal static GameObject clothesSelectMenu;
        internal static UI_ToggleEx clothesTab;
        internal static GameObject clothesSettingsGroup;
        internal static GameObject clothesTabContent;
        // Maker Objects: Accessory Tab - Initialized in Hooks
        internal static GameObject accessorySelectMenu;
        internal static UI_ToggleEx accessoryTab;
        internal static GameObject accessorySettingsGroup;
        internal static GameObject accessoryTabContent;
        // Maker Objects: Hair Tab - Initialized in Hooks
        internal static GameObject hairSelectMenu;
        internal static UI_ToggleEx hairTab;
        internal static GameObject hairSettingsGroup;
        internal static GameObject hairTabContent;
        // Maker Objects: Body Skin Tab - Initialized in Hooks
        internal static GameObject bodySkinSelectMenu;
        internal static UI_ToggleEx bodySkinTab;
        internal static GameObject bodySkinSettingsGroup;
        internal static GameObject bodySkinTabContent;
        // Maker Objects: Face skin Tab - Initialized in Hooks
        internal static GameObject faceSkinSelectMenu;
        internal static UI_ToggleEx faceSkinTab;
        internal static GameObject faceSkinSettingsGroup;
        internal static GameObject faceSkinTabContent;

        // Unity don't destroy textures automatically, need to do manually
        internal static List<Texture> GarbageTextures = new List<Texture>();
        //internal static List<Image> GarbageImages = new List<Image>();

        // Miniatures
        internal static int miniatureSize = 180;
        internal static List<Texture2D> dropdownTextures = new List<Texture2D>();

        /// <summary>
        /// Key: Name of Character's GameObject, Value: class CharacterContent
        /// </summary>
        internal static Dictionary<string, CharacterContent> CharactersLoaded = new Dictionary<string, CharacterContent>();


        internal static new ManualLogSource Log;
        public static GameObject SpockBauru;
        public override void Load()
        {
            Log = base.Log;
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<MaterialModMonoBehaviour>();

            // Add the monobehavior component to your personal GameObject. Try to not duplicate.
            SpockBauru = GameObject.Find("SpockBauru");
            if (SpockBauru == null)
            {
                SpockBauru = new GameObject("SpockBauru");
                GameObject.DontDestroyOnLoad(SpockBauru);
                SpockBauru.hideFlags = HideFlags.DontSave;
                SpockBauru.AddComponent<MaterialModMonoBehaviour>();
            }
            else SpockBauru.AddComponent<MaterialModMonoBehaviour>();
        }


        /// <summary>
        /// Every MaterialMod content for this character goes here
        /// </summary>
        [Serializable]
        internal class CharacterContent
        {
            public GameObject gameObject;
            public string name;
            public ChaControl chaControl;
            public ChaFile chafile;
            public bool enableSetKind = true;
            public bool enableLoadCard = true;
            public ChaFileDefine.CoordinateType currentCoordinate = ChaFileDefine.CoordinateType.Outer;

            /// <summary>
            /// <br> TextureByte = clothesTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> clothesTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            /// <summary>
            /// <br> TextureByte = accessoryTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> accessoryTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            /// <summary>
            /// <br> TextureByte = hairTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> hairTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            /// <summary>
            /// <br> TextureByte = bodySkinTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> bodySkinTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            /// <summary>
            /// <br> TextureByte = faceSkinTextures[coordinate][kind][renderIndex][TextureName]</br>
            /// <br> TextureByte is an PNG encoded byte[]</br>
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> faceSkinTextures = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

        }

        internal enum TextureDictionaries
        {
            clothesTextures,
            accessoryTextures,
            hairTextures,
            bodySkinTextures,
            faceSkinTextures,
        }

        internal static void SetAllTextures(CharacterContent characterContent)
        {
            //Debug.Log("= SetAllTextures");
            ChaControl chaControl = characterContent.chaControl;

            var objects = chaControl.ObjClothes.ToList();
            SetAllDictionary(characterContent, objects, characterContent.clothesTextures, "Clothes");

            objects = chaControl.ObjAccessory.ToList();
            SetAllDictionary(characterContent, objects, characterContent.accessoryTextures, "Accessory");

            objects = chaControl.ObjHair.ToList();
            SetAllDictionary(characterContent, objects, characterContent.hairTextures, "Hair");

            // Skin is special
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
            if (objects != null) SetAllDictionary(characterContent, objects, characterContent.bodySkinTextures, "Skin");

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
            if (objects != null) SetAllDictionary(characterContent, objects, characterContent.faceSkinTextures, "Face");

            // Fixing missing body parts bug
            MaterialModMonoBehaviour.MakeBodyVisible(chaControl);
        }

        internal static void SetAllDictionary(CharacterContent characterContent, List<GameObject> objects, Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures, string origin)
        {
            //Debug.Log("= SetAllDictionary: " + characterContent.name + " dict " + origin);
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

        internal static void SetKind(CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex)
        {
            //Debug.Log("SetKind: " + characterContent.name + " dict " + texDictionary.ToString() + " kind " + kindIndex);
            if (!characterContent.enableSetKind) return;
            GameObject characterObject = characterContent.gameObject;
            ChaControl chaControl = characterObject.GetComponent<ChaControl>();
            int coordinateIndex = (int)characterContent.currentCoordinate;

            // Fix invisible body parts
            MaterialModMonoBehaviour.MakeBodyVisible(chaControl);

            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;
            GameObject itemObject = null;
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
            else if (texDictionary == TextureDictionaries.hairTextures)
            {
                dicTextures = characterContent.hairTextures;
                itemObject = chaControl.ObjHair[kindIndex];
            }
            else if (texDictionary == TextureDictionaries.bodySkinTextures)
            {
                dicTextures = characterContent.bodySkinTextures;
                GameObject body = chaControl.ObjBody;
                // Search for skin object
                SkinnedMeshRenderer[] meshRenderers = body.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    if (meshRenderers[i].gameObject.name.StartsWith("o_body_c"))
                    {
                        itemObject = meshRenderers[i].gameObject;
                        break;
                    }
                }
            }
            else if (texDictionary == TextureDictionaries.faceSkinTextures)
            {
                dicTextures = characterContent.faceSkinTextures;
                GameObject head = chaControl.ObjHead;
                // Search for skin object
                SkinnedMeshRenderer[] meshRenderers = head.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    if (meshRenderers[i].gameObject.name.StartsWith("o_head"))
                    {
                        itemObject = meshRenderers[i].gameObject;
                        break;
                    }
                }
            }
            else
            {
                Log.LogMessage("MaterialMod: ERROR! Character piece not recognized");
                return;
            }

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

        internal static void ResetCoordinateTextures(CharacterContent characterContent)
        {
            ResetAllDictionary(characterContent.clothesTextures, "Clothes Coordinate");
            ResetAllDictionary(characterContent.accessoryTextures, "Accessory Coordinate");
            ResetAllDictionary(characterContent.hairTextures, "Hair Coordinate");
        }

        internal static void ResetAllTextures(CharacterContent characterContent)
        {
            ChaControl chaControl = characterContent.chaControl;
            ResetAllDictionary(characterContent.clothesTextures, "Clothes All");
            ResetAllDictionary(characterContent.accessoryTextures, "Accessory All");
            ResetAllDictionary(characterContent.hairTextures, "Hair All");

            chaControl.SetBodyBaseMaterial();
            ResetAllDictionary(characterContent.bodySkinTextures, "BodySkin All");

            chaControl.SetFaceBaseMaterial();
            ResetAllDictionary(characterContent.faceSkinTextures, "FaceSkin All");
        }

        private static void ResetAllDictionary(Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures, string origin)
        {
            //Debug.Log("ResetAllDictionary: " + origin);
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
                        }
                        dicTextures[coordinateIndex][kindIndex][rendererIndex].Clear();
                    }
                    dicTextures[coordinateIndex][kindIndex].Clear();
                }
                dicTextures[coordinateIndex].Clear();
            }
            dicTextures.Clear();
        }

        internal static void ResetKind(CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex)
        {
            //Debug.Log("ResetKind: " + characterContent.name + " dict " + texDictionary.ToString() + " kind " + kindIndex);
            int coordinateIndex = (int)characterContent.currentCoordinate;

            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;
            if (texDictionary == TextureDictionaries.clothesTextures) dicTextures = characterContent.clothesTextures;
            else if (texDictionary == TextureDictionaries.accessoryTextures) dicTextures = characterContent.accessoryTextures;
            else if (texDictionary == TextureDictionaries.hairTextures) dicTextures = characterContent.hairTextures;
            else if (texDictionary == TextureDictionaries.bodySkinTextures) dicTextures = characterContent.bodySkinTextures;
            else if (texDictionary == TextureDictionaries.faceSkinTextures) dicTextures = characterContent.faceSkinTextures;
            else
            {
                Log.LogMessage("MaterialMod: ERROR! Character piece not recognized");
                return;
            }

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
                }
                dicTextures[coordinateIndex][kindIndex][rendererIndex].Clear();
            }
            dicTextures[coordinateIndex][kindIndex].Clear();
        }
    }
}
