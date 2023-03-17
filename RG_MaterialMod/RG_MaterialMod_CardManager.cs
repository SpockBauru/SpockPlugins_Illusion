using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using MessagePack;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UniRx;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils;
using BepInEx.IL2CPP.Utils.Collections;
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
using System.Text;

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

            //var bytes = Encoding.ASCII.GetBytes((string)byteString);
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
