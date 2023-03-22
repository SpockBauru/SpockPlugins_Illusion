using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
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


namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        public static void MakeClothesDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject clothesPiece = chaControl.ObjClothes[kindIndex];
            GameObject settingsGroup = clothesSettingsGroup;
            GameObject tabContent = clothesTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.clothesTextures;

            MakeDropdown(characterContent, texDictionary, clothesPiece, settingsGroup, clothesTabContent, kindIndex);
        }

        public static void MakeAccessoryDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject accessoryPiece = chaControl.ObjAccessory[kindIndex];
            GameObject settingsGroup = accessorySettingsGroup;
            GameObject tabContent = accessoryTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.accessoryTextures;

            // if no object is sleected, display error text
            if (accessoryPiece == null)
            {
                RG_MaterialModUI.ResetMakerDropdown(settingsGroup);
                // Cleaning UI content
                for (int i = tabContent.transform.childCount - 1; i >= 0; i--)
                    GameObject.Destroy(tabContent.transform.GetChild(i).gameObject);

                Text text = RG_MaterialModUI.CreateText("No accessory selected", 20, 200, 30);
                text.alignment = TextAnchor.UpperLeft;
                text.transform.SetParent(tabContent.transform, false);
                return;
            }
            else
            {
                Text text = tabContent.GetComponentInChildren<Text>();
                if (text != null && text.name == "AccessoryError") UnityEngine.Object.Destroy(text);
            }

            MakeDropdown(characterContent, texDictionary, accessoryPiece, settingsGroup, tabContent, kindIndex);
        }

        public static void MakeDropdown(CharacterContent characterContent, TextureDictionaries texDictionary, GameObject clothesPiece, GameObject settingsGroup, GameObject tabContent, int kindIndex)
        {
            // Create one button for each material
            rendererList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            // Purging Dropdown for materials because of Unity bugs in 2020.3
            RG_MaterialModUI.ResetMakerDropdown(settingsGroup);
            Dropdown clothesDropdown = settingsGroup.GetComponentInChildren<Dropdown>();

            // Populating dropdown
            Il2CppSystem.Collections.Generic.List<string> dropdownOptions = new Il2CppSystem.Collections.Generic.List<string>();
            dropdownOptions.Clear();
            dropdownOptions.Add("Select Texture");

            // Getting Texture list from material
            for (int i = 0; i < rendererList.Length; i++)
            {
                Material material = rendererList[i].material;
                string piecePartName = rendererList[i].transform.parent.name;
                if (piecePartName.EndsWith("_a")) piecePartName = piecePartName.Replace("_a", " (On)");
                if (piecePartName.EndsWith("_b")) piecePartName = piecePartName.Remove(piecePartName.Length - 2, 2) + " (Half)";
                string materialName = "<b>Material: </b>" + material.name.Replace("(Instance)", "").Trim() + "<b> Piece: </b>" + piecePartName;
                dropdownOptions.Add(materialName);
            }
            clothesDropdown.AddOptions(dropdownOptions);
            clothesDropdown.RefreshShownValue();
            clothesDropdown.onValueChanged.AddListener((UnityAction<int>)delegate
            {
                DrawTexturesGrid(clothesDropdown, tabContent, rendererList, characterContent, texDictionary, kindIndex);
            });
            lastDropdownSetting = -1;
            DrawTexturesGrid(clothesDropdown, tabContent, rendererList, characterContent, texDictionary, kindIndex);
        }

        private static int lastDropdownSetting = -1;
        private static Renderer[] rendererList;
        public static void DrawTexturesGrid(Dropdown dropdown, GameObject tabContent, Renderer[] rendererList, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex)
        {
            // Fixing Unity bug for duplicated calls
            if (dropdown.value == lastDropdownSetting) return;
            lastDropdownSetting = dropdown.value;

            // Cleaning UI content
            for (int i = tabContent.transform.childCount - 1; i >= 0; i--)
                GameObject.Destroy(tabContent.transform.GetChild(i).gameObject);

            // Cleaning old miniatures
            for (int i = 0; i < miniatureTextures.Count; i++)
            {
                GarbageTextures.Add(miniatureTextures[i]);
                GarbageImages.Add(miniatureImages[i]);
            }
            miniatureTextures.Clear();

            // Don't create texture block on dropdown 0 (empty)
            if (dropdown.value < 1) return;

            // RenderIndex 0 is dropdown.value 1
            int renderIndex = dropdown.value - 1;

            // Getting Texture list from material
            Material material = rendererList[renderIndex].material;
            string materialName = material.name.Replace("(Instance)", "").Trim() + "-" + rendererList[renderIndex].transform.parent.name;
            
            Dictionary<string, (Vector2, Texture2D)> TexNamesAndMiniatures = GetTexNamesAndMiniatures(material, miniatureSize);

            // Creating one texture block for each texture
            for (int i = 0; i < TexNamesAndMiniatures.Count; i++)
            {
                string textureName = TexNamesAndMiniatures.ElementAt(i).Key;
                Vector2 texOriginalSize = TexNamesAndMiniatures[textureName].Item1;
                Texture2D miniatureTexture = TexNamesAndMiniatures[textureName].Item2;
                CreateTextureBlock(material, miniatureTexture, texOriginalSize, tabContent, characterContent, texDictionary, kindIndex, renderIndex, textureName);
            }
            DestroyGarbage();
        }

        public static void CreateTextureBlock(Material material, Texture2D miniatureTexture, Vector2 texOriginalSize, GameObject parent, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + textureName);
            textureGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Clothes Image
            Image miniature = RG_MaterialModUI.CreateImage(miniatureTexture.width, miniatureTexture.height);
            miniature.transform.SetParent(textureGroup.transform, false);
            UpdateMiniature(miniature, miniatureTexture, textureName);

            // Text with size
            string textContent = "Size: " + texOriginalSize.x.ToString() + "x" + texOriginalSize.y.ToString();
            Text text = RG_MaterialModUI.CreateText(textContent, 17, 180, 20);
            text.transform.SetParent(textureGroup.transform, false);

            // Clothes Set Button
            Button buttonSet = RG_MaterialModUI.CreateButton("Green  " + textureName.Replace("_", ""), 15, 180, 35);
            buttonSet.onClick.AddListener((UnityAction)delegate
            {
                SetTextureButton(material, characterContent, texDictionary, kindIndex, renderIndex, textureName, miniature, text);
            });
            buttonSet.transform.SetParent(textureGroup.transform, false);

            // Clothes Reset Button
            Button buttonReset = RG_MaterialModUI.CreateButton("Reset " + textureName.Replace("_", ""), 15, 180, 35);
            buttonReset.onClick.AddListener((UnityAction)delegate
            {
                ResetTextureButton(material, characterContent, texDictionary, kindIndex, renderIndex, textureName, miniature, text);
            });
            buttonReset.transform.SetParent(textureGroup.transform, false);

            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void UpdateMiniature(Image miniature, Texture2D texture, string textureName)
        {
            Texture2D miniatureTexture;
            // resize textures bigger than miniature
            if (texture.width > miniatureSize || texture.height > miniatureSize)
            {
                // Getting miniature size maintaining proportions
                int width, height;
                width = height = miniatureSize;
                if (texture.height > texture.width) width = height * texture.width / texture.height;
                else height = width * texture.height / texture.width;

                miniatureTexture = Resize(texture, width, height, false);
            }
            else
            {
                miniatureTexture = new Texture2D(texture.width, texture.height);
                Graphics.CopyTexture(texture, miniatureTexture);
            }

            // From pink maps to regular normal maps
            if (textureName.Contains("Bump"))
            {
                miniatureTexture = PinkToNormal(miniatureTexture);
            }

            miniature.sprite = Sprite.Create(miniatureTexture, new Rect(0, 0, miniatureTexture.width, miniatureTexture.height), new Vector2(), 100);
            miniatureTextures.Add(miniatureTexture);
            miniatureImages.Add(miniature);
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void SetTextureButton(Material material, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            // In the future the load texture will be here
            Texture2D texture = new Texture2D(2, 2);
            texture = GreenTexture(256, 256);
            characterContent.enableSetTextures = true;
            //Texture2D texture = new Texture2D(512, 512);
            //byte[] spockBytes = File.ReadAllBytes("Spock.jpg");
            //texture.LoadImage(spockBytes);

            int coordinateType = (int)characterContent.currentCoordinate;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicOriginalTextures;
            if (texDictionary == TextureDictionaries.clothesTextures)
            {
                dicTextures = characterContent.clothesTextures;
                dicOriginalTextures = characterContent.originalClothesTextures;
            }
            else if (texDictionary == TextureDictionaries.accessoryTextures)
            {
                dicTextures = characterContent.accessoryTextures;
                dicOriginalTextures = characterContent.originalAccessoryTextures;
            }
            else return;

            // Texture = characterContent.clothesTextures[coordinate][kind][renderIndex][TextureName]
            if (!dicTextures.ContainsKey(coordinateType)) dicTextures.Add(coordinateType, new Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>());
            if (!dicTextures[coordinateType].ContainsKey(kindIndex)) dicTextures[coordinateType].Add(kindIndex, new Dictionary<int, Dictionary<string, byte[]>>());
            if (!dicTextures[coordinateType][kindIndex].ContainsKey(renderIndex)) dicTextures[coordinateType][kindIndex].Add(renderIndex, new Dictionary<string, byte[]>());
            if (!dicTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) dicTextures[coordinateType][kindIndex][renderIndex].Add(textureName, null);

            // Texture = characterContent.clothesTextures[coordinate][kind][renderIndex][TextureName]
            if (!dicOriginalTextures.ContainsKey(coordinateType)) dicOriginalTextures.Add(coordinateType, new Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>());
            if (!dicOriginalTextures[coordinateType].ContainsKey(kindIndex)) dicOriginalTextures[coordinateType].Add(kindIndex, new Dictionary<int, Dictionary<string, byte[]>>());
            if (!dicOriginalTextures[coordinateType][kindIndex].ContainsKey(renderIndex)) dicOriginalTextures[coordinateType][kindIndex].Add(renderIndex, new Dictionary<string, byte[]>());
            if (!dicOriginalTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) dicOriginalTextures[coordinateType][kindIndex][renderIndex].Add(textureName, null);

            // Store original texture
            if (dicOriginalTextures[coordinateType][kindIndex][renderIndex][textureName] == null)
            {
                Texture originalTexture = material.GetTexture(textureName);
                Texture2D originalTexture2D = ToTexture2D(originalTexture);
                dicOriginalTextures[coordinateType][kindIndex][renderIndex][textureName] = originalTexture2D.EncodeToPNG();
            }

            // Reset old texture
            //if (!(characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName] == null)) GarbageTextures.Add(characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName]);

            // Update Texture
            //texture.Compress(false);
            dicTextures[coordinateType][kindIndex][renderIndex][textureName] = texture.EncodeToPNG();
            material.SetTexture(textureName, texture);
            sizeText.text = "Size: " + texture.width.ToString() + "x" + texture.height.ToString();

            UpdateMiniature(miniature, texture, textureName);

            //DestroyGarbage();
        }

        public static void ResetTextureButton(Material material, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            int coordinateType = (int)characterContent.currentCoordinate;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicOriginalTextures;
            if (texDictionary == TextureDictionaries.clothesTextures)
            {
                dicTextures = characterContent.clothesTextures;
                dicOriginalTextures = characterContent.originalClothesTextures;
            }
            else if (texDictionary == TextureDictionaries.accessoryTextures)
            {
                dicTextures = characterContent.accessoryTextures;
                dicOriginalTextures = characterContent.originalAccessoryTextures;
            }
            else return;

            if (!dicTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) return;

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(dicOriginalTextures[coordinateType][kindIndex][renderIndex][textureName]);
            material.SetTexture(textureName, texture);
            sizeText.text = "Size: " + texture.width.ToString() + "x" + texture.height.ToString();
            UpdateMiniature(miniature, texture, textureName);

            // cleaning texture and entrances
            //GarbageTextures.Add(characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName]);
            dicTextures[coordinateType][kindIndex][renderIndex].Remove(textureName);

            //DestroyGarbage();
        }
    }
}
