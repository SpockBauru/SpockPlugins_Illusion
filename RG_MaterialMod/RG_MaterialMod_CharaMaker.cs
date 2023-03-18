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

        public static void MakeClothesDropdown(CvsC_Clothes clothesControl)
        {
            GameObject characterObject = clothesControl.chaCtrl.gameObject;
            string characterName = characterObject.name;
            int coordinateType = clothesControl.coordinateType;
            int kindIndex = clothesControl.SNo;
            CharacterContent characterContent = CharactersLoaded[characterName];
            ChaFile chaFile = clothesControl.chaCtrl.ChaFile;
            ChaControl chaControl = clothesControl.chaCtrl;
            GameObject clothesPiece = chaControl.ObjClothes[kindIndex];


            // Create one button for each material
            rendererList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            // Purging Dropdown for materials because of Unity bugs in 2020.3
            GameObject dropDownObject = clothesSettingsGroup.GetComponentInChildren<Dropdown>().gameObject;
            GameObject dropDownParent = dropDownObject.transform.parent.gameObject;
            UnityEngine.Object.DestroyImmediate(dropDownObject);
            Dropdown clothesDropdown = RG_MaterialModUI.CreateDropdown(470, 35, 18);
            clothesDropdown.transform.SetParent(dropDownParent.transform, false);
            clothesDropdown.ClearOptions();
            RectTransform dropdownRect = clothesDropdown.gameObject.GetComponent<RectTransform>();
            dropdownRect.anchoredPosition = new Vector2(0, -10);
            GameObject dropContentObj = clothesDropdown.gameObject.GetComponentInChildren<ScrollRect>(true).gameObject;
            RectTransform dropContentRect = dropContentObj.GetComponent<RectTransform>();
            dropContentRect.sizeDelta = new Vector2(0, 500);

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
                DrawTexturesGrid(clothesDropdown, rendererList, characterContent, coordinateType, kindIndex, chaFile);
            });
            lastDropdownSetting = -1;
            DrawTexturesGrid(clothesDropdown, rendererList, characterContent, coordinateType, kindIndex, chaFile);
        }

        private static int lastDropdownSetting = -1;
        private static Renderer[] rendererList;
        public static void DrawTexturesGrid(Dropdown dropdown, Renderer[] rendererList, CharacterContent characterContent, int coordinateType, int kindIndex, ChaFile chaFile)
        {
            // Fixing Unity bug for duplicated calls
            if (dropdown.value == lastDropdownSetting) return;
            lastDropdownSetting = dropdown.value;

            // Cleaning UI content
            for (int i = clothesTabContent.transform.childCount - 1; i >= 0; i--)
                GameObject.Destroy(clothesTabContent.transform.GetChild(i).gameObject);

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
            Debug.Log("Material Name: " + materialName);

            Dictionary<string, Texture2D> materialTextures = GetMaterialTextures(material);

            // Creating one texture block for each texture
            for (int i = 0; i < materialTextures.Count; i++)
            {
                string textureName = materialTextures.ElementAt(i).Key;
                Texture2D materialTexture = materialTextures[textureName];

                CreateTextureBlock(material, materialTexture, clothesTabContent, characterContent, coordinateType, kindIndex, renderIndex, textureName, chaFile);
            }
        }

        public static void CreateTextureBlock(Material material, Texture2D materialTexture, GameObject parent, CharacterContent characterContent, int coordinateType, int kindIndex, int renderIndex, string textureName, ChaFile chaFile)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + textureName);
            textureGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Clothes Image
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * materialTexture.width / materialTexture.height;
            else height = width * materialTexture.height / materialTexture.width;

            Image miniature = RG_MaterialModUI.CreateImage(width, height);
            miniature.transform.SetParent(textureGroup.transform, false);
            UpdateMiniature(miniature, materialTexture, textureName);

            // Text with size
            string textContent = "Size: " + materialTexture.width.ToString() + "x" + materialTexture.height.ToString();
            Text text = RG_MaterialModUI.CreateText(textContent, 17, 200, 20);
            text.transform.SetParent(textureGroup.transform, false);

            // Clothes Set Button
            Button buttonSet = RG_MaterialModUI.CreateButton("Green  " + textureName, 16, 200, 35);
            buttonSet.onClick.AddListener((UnityAction)delegate
            {
                SetTextureButton(material, characterContent, coordinateType, kindIndex, renderIndex, textureName, miniature, chaFile);
            });
            buttonSet.transform.SetParent(textureGroup.transform, false);

            // Clothes Reset Button
            Button buttonReset = RG_MaterialModUI.CreateButton("Reset " + textureName, 16, 200, 35);
            buttonReset.onClick.AddListener((UnityAction)delegate
            {
                ResetTextureButton(material, characterContent, coordinateType, kindIndex, renderIndex, textureName, miniature);
            });
            buttonReset.transform.SetParent(textureGroup.transform, false);

            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void UpdateMiniature(Image miniature, Texture2D texture, string textureName)
        {
            // maitaining proportions
            int width, height;
            width = height = miniatureSize;
            if (height > width) width = height * texture.width / texture.height;
            else height = width * texture.height / texture.width;

            Texture2D scaledTexture = Resize(texture, width, height);

            // Trom pink maps to regular normal maps
            if (textureName.Contains("Bump"))
            {
                scaledTexture = DXT2nmToNormal(scaledTexture);
            }

            miniature.sprite = Sprite.Create(scaledTexture, new Rect(0, 0, width, height), new Vector2());
            miniatureTextures.Add(scaledTexture);
            miniatureImages.Add(miniature);
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        public static void SetTextureButton(Material material, CharacterContent characterContent, int coordinateType, int kindIndex, int renderIndex, string textureName, Image miniature, ChaFile chaFile)
        {
            // In the future the load texture will be here
            Texture2D texture = new Texture2D(512, 512);
            texture = GreenTexture(512, 512);


            //Debug.Log("Material Name: " + material.name.Replace("(Instance)", "").Trim() + " Coord: " + coordinateType + " Kind: " + kindIndex + " render: " + renderIndex + " Texture: " + textureName);

            if (!characterContent.clothesTextures.ContainsKey(coordinateType)) characterContent.clothesTextures.Add(coordinateType, new Dictionary<int, Dictionary<int, Dictionary<string, Texture2D>>>());
            if (!characterContent.clothesTextures[coordinateType].ContainsKey(kindIndex)) characterContent.clothesTextures[coordinateType].Add(kindIndex, new Dictionary<int, Dictionary<string, Texture2D>>());
            if (!characterContent.clothesTextures[coordinateType][kindIndex].ContainsKey(renderIndex)) characterContent.clothesTextures[coordinateType][kindIndex].Add(renderIndex, new Dictionary<string, Texture2D>());
            if (!characterContent.clothesTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) characterContent.clothesTextures[coordinateType][kindIndex][renderIndex].Add(textureName, null);

            // Stored original textures 
            // Texture = characterContent.clothesTextures[coordinate][kind][renderIndex][TextureName]
            if (!characterContent.originalClothesTextures.ContainsKey(coordinateType)) characterContent.originalClothesTextures.Add(coordinateType, new Dictionary<int, Dictionary<int, Dictionary<string, Texture2D>>>());
            if (!characterContent.originalClothesTextures[coordinateType].ContainsKey(kindIndex)) characterContent.originalClothesTextures[coordinateType].Add(kindIndex, new Dictionary<int, Dictionary<string, Texture2D>>());
            if (!characterContent.originalClothesTextures[coordinateType][kindIndex].ContainsKey(renderIndex)) characterContent.originalClothesTextures[coordinateType][kindIndex].Add(renderIndex, new Dictionary<string, Texture2D>());
            if (!characterContent.originalClothesTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) characterContent.originalClothesTextures[coordinateType][kindIndex][renderIndex].Add(textureName, null);

            // Getting material all textures
            Dictionary<string, Texture2D> materialTextures = GetMaterialTextures(material);

            // Store original texture
            if (characterContent.originalClothesTextures[coordinateType][kindIndex][renderIndex][textureName] == null) characterContent.originalClothesTextures[coordinateType][kindIndex][renderIndex][textureName] = materialTextures[textureName];

            // Reset old texture
            //if (!(characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName] == null)) GarbageTextures.Add(characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName]);

            // Update Texture
            characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName] = texture;
            material.SetTexture(textureName, characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName]);

            // Update miniature
            UpdateMiniature(miniature, characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName], textureName);

            DestroyGarbage();
        }

        public static void ResetTextureButton(Material material, CharacterContent characterContent, int coordinateType, int kindIndex, int renderIndex, string textureName, Image miniature)
        {
            if (!characterContent.clothesTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) return;

            material.SetTexture(textureName, characterContent.originalClothesTextures[coordinateType][kindIndex][renderIndex][textureName]);
            UpdateMiniature(miniature, characterContent.originalClothesTextures[coordinateType][kindIndex][renderIndex][textureName], textureName);

            // cleaning texture and entrances
            GarbageTextures.Add(characterContent.clothesTextures[coordinateType][kindIndex][renderIndex][textureName]);
            characterContent.clothesTextures[coordinateType][kindIndex][renderIndex].Remove(textureName);

            DestroyGarbage();
        }

    }
}
