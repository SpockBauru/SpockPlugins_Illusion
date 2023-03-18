using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using MessagePack;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UniRx;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils;
using BepInEx.IL2CPP.Utils.Collections;
using Il2CppSystem.Text;
using HarmonyLib;
using UnhollowerRuntimeLib;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;


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
    public partial class RG_MaterialMod
    {
        [HideFromIl2Cpp]
        private PluginData CreateNewPluginData(Il2CppSystem.Object obj)
        {
            PluginData pluginData = new PluginData();
            pluginData.data.Add("testKey", obj.GetIl2CppType().FullName);

            return pluginData;
        }

        public static void SaveCard(ChaFile chaFile, CharacterContent characterContent)
        {
            Debug.Log("Save Card");
            // Cleaning previews plugin data
            ExtendedSave.SetExtendedDataById(chaFile, GUID, null);

            // Populating plugin data
            PluginData pluginData = new PluginData();

            // clothesTexturesByte is Imitating clothesTextures
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> clothesTexturesByte = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>();

            var clothesTextures = characterContent.clothesTextures;
            if (clothesTextures.Count == 0) return;

            // Texture = clothesTextures[coordinate][kind][renderIndex][TextureName]
            for (int i = 0; i < clothesTextures.Count; i++)
            {
                if (clothesTextures.Count == 0) continue;
                int coordinateIndex = clothesTextures.ElementAt(i).Key;
                var coordinate = clothesTextures[coordinateIndex];
                if (!clothesTexturesByte.ContainsKey(coordinateIndex))
                    clothesTexturesByte.Add(coordinateIndex, new Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>());

                for (int j = 0; j < coordinate.Count; j++)
                {
                    if (coordinate.Count == 0) continue;
                    int kindIndex = coordinate.ElementAt(j).Key;
                    var kind = coordinate[kindIndex];
                    if (!clothesTexturesByte[coordinateIndex].ContainsKey(kindIndex))
                        clothesTexturesByte[coordinateIndex].Add(kindIndex, new Dictionary<int, Dictionary<string, byte[]>>());

                    for (int k = 0; k < kind.Count; k++)
                    {
                        if (kind.Count == 0) continue;
                        int rendererIndex = kind.ElementAt(k).Key;
                        var renderer = kind[rendererIndex];
                        
                        if (!clothesTexturesByte[coordinateIndex][kindIndex].ContainsKey(rendererIndex))
                            clothesTexturesByte[coordinateIndex][kindIndex].Add(rendererIndex, new Dictionary<string, byte[]>());

                        // Finally adding textures to byte[]
                        for (int l = 0; l < renderer.Count; l++)
                        {
                            if (renderer.Count == 0) continue;
                            string textureName = renderer.ElementAt(l).Key;
                            Texture2D texture = renderer[textureName];

                            // transforming Texture2D into byte[]
                            byte[] bytes = texture.EncodeToPNG();

                            clothesTexturesByte[coordinateIndex][kindIndex][rendererIndex].Add(textureName, bytes);

                            string testetexto = "";
                            for (int txt = 0; txt < 10; txt++)
                                testetexto = testetexto + bytes[txt] + " ";

                            Debug.Log("textureName: " + textureName + " First 10: " + testetexto);
                        }
                    }
                }
            }

            // Converting the big boy to string
            BinaryFormatter formatter1 = new BinaryFormatter();
            MemoryStream stream1 = new MemoryStream();
            formatter1.Serialize(stream1, clothesTexturesByte);
            byte[] outputByte = stream1.ToArray();
            stream1.Close();

            // Encoding to string
            string bytesString = Encoding.Latin1.GetString(outputByte);

            // Finally saving
            pluginData.data.Add("chothesTextures", bytesString);
            ExtendedSave.SetExtendedDataById(chaFile, GUID, pluginData);
        }


        public static void LoadCard(ChaFile chaFile, CharacterContent characterContent)
        {
            PluginData pluginData;
            pluginData = ExtendedSave.GetExtendedDataById(chaFile, GUID);
            if (pluginData == null)
            {
                Debug.Log("Plugin data is null");
                return;
            }

            object objectString;
            pluginData.data.TryGetValue("chothesTextures", out objectString);
            string byteString = (string)objectString;


            // Deserializing
            var outputByte = Encoding.Latin1.GetBytes(byteString);
            BinaryFormatter formatter1 = new BinaryFormatter();
            MemoryStream stream1 = new MemoryStream(outputByte);
            stream1.Position = 0;

            var clothesTexturesByte = (Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>)formatter1.Deserialize(stream1);
            stream1.Close();

            // Populating clothesTextures
            var clothesTextures = characterContent.clothesTextures;

            // Texture = clothesTextures[coordinate][kind][renderIndex][TextureName]
            for (int i = 0; i < clothesTexturesByte.Count; i++)
            {
                if (clothesTexturesByte.ElementAt(i).Value == null) continue;
                int coordinateIndex = clothesTexturesByte.ElementAt(i).Key;
                var coordinate = clothesTexturesByte[coordinateIndex];
                if (!clothesTextures.ContainsKey(coordinateIndex))
                    clothesTextures.Add(coordinateIndex, new Dictionary<int, Dictionary<int, Dictionary<string, Texture2D>>>());

                for (int j = 0; j < coordinate.Count; j++)
                {
                    if (coordinate.ElementAt(j).Value == null) continue;
                    int kindIndex = coordinate.ElementAt(j).Key;
                    var kind = coordinate[kindIndex];
                    if (!clothesTextures[coordinateIndex].ContainsKey(kindIndex))
                        clothesTextures[coordinateIndex].Add(kindIndex, new Dictionary<int, Dictionary<string, Texture2D>>());

                    for (int k = 0; k < kind.Count; k++)
                    {
                        if (kind.ElementAt(k).Value == null) continue;
                        int rendererIndex = kind.ElementAt(k).Key;
                        var renderer = kind[rendererIndex];
                        if (!clothesTextures[coordinateIndex][kindIndex].ContainsKey(rendererIndex))
                            clothesTextures[coordinateIndex][kindIndex].Add(rendererIndex, new Dictionary<string, Texture2D>());

                        // Finally loading textures from byte[]
                        for (int l = 0; l < renderer.Count; l++)
                        {
                            if (renderer.ElementAt(l).Value == null) continue;
                            string textureName = renderer.ElementAt(l).Key;
                            byte[] bytes = renderer[textureName];

                            // transforming bytes into Texture2D
                            Texture2D texture = new Texture2D(2, 2);
                            texture.LoadImage(bytes);

                            clothesTextures[coordinateIndex][kindIndex][rendererIndex].Add(textureName, texture);


                            string testetexto = "";
                            for (int txt = 0; txt < 10; txt++)
                                testetexto = testetexto + bytes[txt] + " ";

                            Debug.Log("textureName: " + textureName + " First 10: " + testetexto);
                        }
                    }
                }
            }
        }


        public static void SaveData(ChaFile chaFile, string key, string data)
        {
            //byte[] bytes = { 0, 7, 10 };

            //string bytesString = Encoding.ASCII.GetString(bytes);

            // Plugin Data Root
            PluginData pluginDataRoot = new PluginData();
            pluginDataRoot.data.Add(key, data);
            Debug.Log("Data count: " + data.Length);
            ExtendedSave.SetExtendedDataById(chaFile, GUID, pluginDataRoot);
        }

        private static string LoadData(ChaFile chaFile, string key)
        {
            PluginData pluginDataRoot = ExtendedSave.GetExtendedDataById(chaFile, GUID);
            if (pluginDataRoot == null)
            {
                Debug.Log("Plugin data é null");
                return null;
            }

            object byteString;
            pluginDataRoot.data.TryGetValue(key, out byteString);

            return (string)byteString;
        }

        private void ClearData()
        {
            GameObject obj = GameObject.Find("CharaCustom");
            if (obj == null)
            {
                Log.LogError("CharaCustom not found! Are you in the editor?");
                return;
            }

            CharaCustom.CharaCustom charaCustom = obj.GetComponent<CharaCustom.CharaCustom>();
            ChaFile chaFile = charaCustom.customCtrl.chaFile;

            ExtendedSave.SetExtendedDataById(chaFile, GUID, null);

            Log.LogMessage("Data cleared");
        }

        [HideFromIl2Cpp]
        private void TestPluginData(PluginData plginData, Il2CppSystem.Object obj)
        {
            TestNotNull(plginData, obj);
            if (plginData.data.TryGetValue("testKey", out object actual))
            {
                if (obj.GetIl2CppType().FullName.Equals(actual))
                {
                    return;
                }
            }

            throw new Exception($"PluginData values incorrect for {obj.GetIl2CppType().Name} -- actual testKey value was {actual.GetType()} {actual}");
        }

        [HideFromIl2Cpp]
        private void TestValue(PluginData pluginData, string key, object expected)
        {
            if (pluginData.data.TryGetValue(key, out object actual))
            {
                if (expected.Equals(actual))
                {
                    return;
                }
            }

            throw new Exception($"Value for key {key} should equal {expected.GetType()} {expected} -- got {actual.GetType()} {actual}");
        }

        [HideFromIl2Cpp]
        private void TestNotNull(PluginData pluginData, Il2CppSystem.Object obj)
        {
            if (pluginData == null)
            {
                throw new Exception($"Plugin data for {obj.GetIl2CppType().Name} not found");
            }
        }
    }
}
