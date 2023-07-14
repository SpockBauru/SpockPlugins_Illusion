using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using MessagePack;
using MessagePack.Resolvers;
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
using ExtensibleSaveFormat;

// Game Specific
using RG;
using Chara;
using CharaCustom;


namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        internal static void SaveCard(CharacterContent characterContent)
        {
            ChaFile chaFile = characterContent.chafile;

            // Cleaning previews plugin data
            ExtendedSave.SetExtendedDataById(chaFile, GUID, null);

            PluginData pluginData = new PluginData();
            pluginData.version = 1;

            // Clothes
            var clothesTextures = characterContent.clothesTextures;
            if (clothesTextures.Count >= 0) SaveTextureDictionary(pluginData, chaFile, TextureDictionaries.clothesTextures.ToString(), clothesTextures);

            // Accesories
            var accessoryTextures = characterContent.accessoryTextures;
            if (accessoryTextures.Count >= 0) SaveTextureDictionary(pluginData, chaFile, TextureDictionaries.accessoryTextures.ToString(), accessoryTextures);

            // Hair
            var hairTextures = characterContent.hairTextures;
            if (hairTextures.Count >= 0) SaveTextureDictionary(pluginData, chaFile, TextureDictionaries.hairTextures.ToString(), hairTextures);

            // Body Skin
            var bodySkinTextures = characterContent.bodySkinTextures;
            if (bodySkinTextures.Count >= 0)
            {
                SaveTextureDictionary(pluginData, chaFile, TextureDictionaries.bodySkinTextures.ToString(), bodySkinTextures);
            }

            // Face Skin
            var faceSkinTextures = characterContent.faceSkinTextures;
            if (faceSkinTextures.Count >= 0)
            {
                SaveTextureDictionary(pluginData, chaFile, TextureDictionaries.faceSkinTextures.ToString(), faceSkinTextures);
            }

            ExtendedSave.SetExtendedDataById(chaFile, GUID, pluginData);
        }

        private static void SaveTextureDictionary(PluginData pluginData, ChaFile chaFile, string dicName, Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures)
        {
            // Converting the big boy to byte[]
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, dicTextures);
            byte[] outputByte = stream.ToArray();
            stream.Close();

            // Saving
            pluginData.data.Add(dicName, outputByte);
        }

        internal static void LoadCard(CharacterContent characterContent)
        {
            if (characterContent.enableLoadCard == false) return;
            ChaFile chaFile = characterContent.chafile;

            bool hasData;
            object texturesObject;
            byte[] bytes;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;

            PluginData pluginData;
            pluginData = ExtendedSave.GetExtendedDataById(chaFile, GUID);
            if (pluginData == null)
            {
                return;
            }

            // Clothes
            hasData = pluginData.data.TryGetValue(TextureDictionaries.clothesTextures.ToString(), out texturesObject);
            if (hasData)
            {
                bytes = (byte[])texturesObject;
                dicTextures = LoadTexturesDictionary(bytes);
                characterContent.clothesTextures = dicTextures;
            }

            // Accessories
            hasData = pluginData.data.TryGetValue(TextureDictionaries.accessoryTextures.ToString(), out texturesObject);
            if (hasData)
            {
                bytes = (byte[])texturesObject;
                dicTextures = LoadTexturesDictionary(bytes);
                characterContent.accessoryTextures = dicTextures;
            }

            // Hair
            hasData = pluginData.data.TryGetValue(TextureDictionaries.hairTextures.ToString(), out texturesObject);
            if (hasData)
            {
                bytes = (byte[])texturesObject;
                dicTextures = LoadTexturesDictionary(bytes);
                characterContent.hairTextures = dicTextures;
            }

            // Body Skin
            hasData = pluginData.data.TryGetValue(TextureDictionaries.bodySkinTextures.ToString(), out texturesObject);
            if (hasData)
            {
                bytes = (byte[])texturesObject;
                dicTextures = LoadTexturesDictionary(bytes);
                characterContent.bodySkinTextures = dicTextures;
            }

            // Face Skin
            hasData = pluginData.data.TryGetValue(TextureDictionaries.faceSkinTextures.ToString(), out texturesObject);
            if (hasData)
            {
                bytes = (byte[])texturesObject;
                dicTextures = LoadTexturesDictionary(bytes);
                characterContent.faceSkinTextures = dicTextures;
            }
        }

        private static Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> LoadTexturesDictionary(byte[] bytes)
        {
            // Deserializing
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream(bytes);
            stream.Position = 0;

            var clothesTexturesByte = (Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>>)formatter.Deserialize(stream);
            stream.Close();

            return clothesTexturesByte;
        }
    }
}
