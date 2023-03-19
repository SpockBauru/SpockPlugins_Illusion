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

        public static void SaveCard(CharacterContent characterContent)
        {
            ChaFile chaFile = characterContent.chafile;

            Debug.Log("Save Card");
            // Cleaning previews plugin data
            ExtendedSave.SetExtendedDataById(chaFile, GUID, null);

            // Populating plugin data
            PluginData pluginData = new PluginData();
            pluginData.version = 1;
            
            var clothesTextures = characterContent.clothesTextures;
            if (clothesTextures.Count == 0) return;

            // Converting the big boy to string
            BinaryFormatter formatter1 = new BinaryFormatter();
            MemoryStream stream1 = new MemoryStream();
            formatter1.Serialize(stream1, clothesTextures);
            byte[] outputByte = stream1.ToArray();
            stream1.Close();

            // Encoding to string
            string bytesString = Encoding.Latin1.GetString(outputByte);

            // Finally saving
            pluginData.data.Add("chothesTextures", bytesString);
            ExtendedSave.SetExtendedDataById(chaFile, GUID, pluginData);
        }


        public static void LoadCard(CharacterContent characterContent)
        {
            ChaFile chaFile = characterContent.chafile;

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

            characterContent.clothesTextures = clothesTexturesByte;
        }
    }
}
