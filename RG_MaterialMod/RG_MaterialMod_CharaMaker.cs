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
using UniRx.Triggers;


namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        internal static void MakeClothesDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject clothesPiece = chaControl.ObjClothes[kindIndex];
            GameObject settingsGroup = clothesSettingsGroup;
            GameObject tabContent = clothesTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.clothesTextures;

            MakeDropdown(characterContent, texDictionary, clothesPiece, settingsGroup, tabContent, kindIndex);
        }

        internal static void MakeAccessoryDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject accessoryPiece = chaControl.ObjAccessory[kindIndex];
            GameObject settingsGroup = accessorySettingsGroup;
            GameObject tabContent = accessoryTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.accessoryTextures;

            // if no object is sleected, display error text
            if (accessoryPiece == null)
            {
                UITools.ResetMakerDropdown(settingsGroup);
                // Cleaning UI content
                for (int i = tabContent.transform.childCount - 1; i >= 0; i--)
                    GameObject.Destroy(tabContent.transform.GetChild(i).gameObject);

                Text text = UITools.CreateText("No accessory selected", 20, 200, 30);
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

        internal static void MakeHairDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject hairPiece = chaControl.ObjHair[kindIndex];
            GameObject settingsGroup = hairSettingsGroup;
            GameObject tabContent = hairTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.hairTextures;

            MakeDropdown(characterContent, texDictionary, hairPiece, settingsGroup, tabContent, kindIndex);
        }

        internal static void MakeBodySkinDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject body = chaControl.ObjBody;

            // Make Rest Skin Button
            Transform buttonParent = bodySkinTabContent.transform.parent.parent.parent;
            Transform resetSkinObject = buttonParent.FindChild("ResetSkinButton");
            if (resetSkinObject == null)
            {
                Button resetSkin = UITools.CreateButton("Reset Skin", 19, 400, 40);
                resetSkin.gameObject.name = "ResetSkinButton";
                resetSkin.transform.SetParent(buttonParent, false);
                RectTransform resetRect = resetSkin.gameObject.GetComponent<RectTransform>();
                resetRect.anchorMin = new Vector3(0.5f, -0.05f);
                resetRect.anchorMax = new Vector3(0.5f, -0.05f);

                resetSkin.onClick.AddListener((UnityAction)ResetSkin);
                void ResetSkin()
                {
                    ResetKind(characterContent, TextureDictionaries.bodySkinTextures, kindIndex);

                    // Reset body to defaults
                    MaterialModMonoBehaviour.SetMaterialAlphas(chaControl);
                    chaControl.SetBodyBaseMaterial();
                    MakeBodySkinDropdown(characterContent, kindIndex);
                }

                //// Warning to not set when clothed
                //Text text = UITools.CreateText("WARNING: Don't use clothes\r\nwhen changing skin textures!", 20, 400, 30);
                //text.gameObject.name = "ClothesWarning";
                //text.transform.SetParent(buttonParent, false);
                //text.color = Color.red;
                //Outline outline = text.gameObject.AddComponent<Outline>();
                //outline.effectDistance = new Vector2(1.3f, 1.3f);
                //outline.effectColor = new Color(1, 1, 1, 0.5f);
                //RectTransform textRect = text.gameObject.GetComponent<RectTransform>();
                //textRect.anchorMin = new Vector3(0.5f, -0.16f);
                //textRect.anchorMax = new Vector3(0.5f, -0.16f);
            }

            // Search for skin object
            GameObject skin = null;
            SkinnedMeshRenderer[] meshRenderers = body.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i].gameObject.name.StartsWith("o_body_c"))
                {
                    skin = meshRenderers[i].gameObject;
                    break;
                }
            }

            if (skin == null) return;

            GameObject settingsGroup = bodySkinSettingsGroup;
            GameObject tabContent = bodySkinTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.bodySkinTextures;

            MakeDropdown(characterContent, texDictionary, skin, settingsGroup, tabContent, kindIndex);
        }

        internal static void MakeFaceSkinDropdown(CharacterContent characterContent, int kindIndex)
        {
            ChaControl chaControl = characterContent.chaControl;
            GameObject head = chaControl.ObjHead;

            Transform buttonParent = faceSkinTabContent.transform.parent.parent.parent;
            Transform resetSkinObject = buttonParent.FindChild("ResetSkinButton");
            if (resetSkinObject == null)
            {
                Button resetSkin = UITools.CreateButton("Reset Skin", 19, 400, 40);
                resetSkin.gameObject.name = "ResetSkinButton";
                resetSkin.transform.SetParent(buttonParent, false);
                RectTransform resetRect = resetSkin.gameObject.GetComponent<RectTransform>();
                resetRect.anchorMin = new Vector3(0.5f, -0.05f);
                resetRect.anchorMax = new Vector3(0.5f, -0.05f);

                resetSkin.onClick.AddListener((UnityAction)ResetSkin);
                void ResetSkin()
                {
                    ResetKind(characterContent, TextureDictionaries.faceSkinTextures, kindIndex);
                    MaterialModMonoBehaviour.SetMaterialAlphas(chaControl);
                    chaControl.SetFaceBaseMaterial();
                    MakeFaceSkinDropdown(characterContent, kindIndex);
                }
            }

            // Search for skin object
            GameObject skin = null;
            SkinnedMeshRenderer[] meshRenderers = head.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i].gameObject.name.StartsWith("o_head"))
                {
                    skin = meshRenderers[i].gameObject;
                    break;
                }
            }

            if (skin == null) return;

            GameObject settingsGroup = faceSkinSettingsGroup;
            GameObject tabContent = faceSkinTabContent;
            TextureDictionaries texDictionary = TextureDictionaries.faceSkinTextures;

            MakeDropdown(characterContent, texDictionary, skin, settingsGroup, tabContent, kindIndex);
        }

        private static void MakeDropdown(CharacterContent characterContent, TextureDictionaries texDictionary, GameObject clothesPiece, GameObject settingsGroup, GameObject tabContent, int kindIndex)
        {
            // Create one button for each material
            rendererList = clothesPiece.GetComponentsInChildren<Renderer>(true);

            // Purging Dropdown for materials because of Unity bugs in 2020.3
            UITools.ResetMakerDropdown(settingsGroup);
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
        private static void DrawTexturesGrid(Dropdown dropdown, GameObject tabContent, Renderer[] rendererList, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex)
        {
            // Fixing Unity bug for duplicated calls
            if (dropdown.value == lastDropdownSetting) return;
            lastDropdownSetting = dropdown.value;

            // Cleaning old miniatures
            GarbageTextures.AddRange(dropdownTextures);
            dropdownTextures.Clear();

            // Cleaning UI content
            for (int i = tabContent.transform.childCount - 1; i >= 0; i--)
                GameObject.Destroy(tabContent.transform.GetChild(i).gameObject);

            // Don't create texture block on dropdown 0 (empty)
            if (dropdown.value < 1) return;

            // RenderIndex 0 is dropdown.value 1
            int renderIndex = dropdown.value - 1;

            // Getting Texture list from material
            Material material = rendererList[renderIndex].material;
            string materialName = material.name.Replace("(Instance)", "").Trim() + "-" + rendererList[renderIndex].transform.parent.name;

            // Creating UV Maps blocks
            List<Texture2D> UVRenderers = new List<Texture2D>();

            MeshRenderer meshRenderer = rendererList[renderIndex].gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Mesh mesh = meshRenderer.GetComponent<MeshFilter>().mesh;
                if (mesh.isReadable) UVRenderers = UVMap.GetUVMaps(meshRenderer, miniatureSize, miniatureSize);
            }

            SkinnedMeshRenderer skinnedMeshRenderer = rendererList[renderIndex].gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                if (mesh.isReadable) UVRenderers.AddRange(UVMap.GetUVMaps(skinnedMeshRenderer, miniatureSize, miniatureSize));
            }

            for (int i = 0; i < UVRenderers.Count; i++)
            {
                string mapName = "UV Map " + i + 1;
                if (UVRenderers[i].isReadable) CreateUVBlock(rendererList[renderIndex], i, UVRenderers[i], mapName, tabContent);
                dropdownTextures.Add(UVRenderers[i]);
            }

            // Creating Textures blocks
            Dictionary<string, (Vector2, Texture2D)> TexNamesAndMiniatures = TextureTools.GetTexNamesAndMiniatures(material, miniatureSize);

            // Creating one texture block for each texture
            for (int i = 0; i < TexNamesAndMiniatures.Count; i++)
            {
                string textureName = TexNamesAndMiniatures.ElementAt(i).Key;
                Vector2 texOriginalSize = TexNamesAndMiniatures[textureName].Item1;
                Texture2D miniatureTexture = TexNamesAndMiniatures[textureName].Item2;
                dropdownTextures.Add(miniatureTexture);
                CreateTextureBlock(material, miniatureTexture, texOriginalSize, tabContent, characterContent, texDictionary, kindIndex, renderIndex, textureName);
            }
            MaterialModMonoBehaviour.DestroyGarbage();
        }

        private static void CreateUVBlock(Renderer renderer, int index, Texture2D miniatureTexture, string mapName, GameObject parent)
        {
            // UI group
            GameObject UVGroup = new GameObject("UVGroup " + mapName);
            UVGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = UVGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Background image for getting the scroll
            RectTransform textureGroupRect = UVGroup.GetComponent<RectTransform>();
            textureGroupRect.sizeDelta = new Vector2(190, 280);
            CanvasRenderer canvasRenderer = UVGroup.AddComponent<CanvasRenderer>();
            Image background = UVGroup.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0);

            // Texture Image
            //Texture2D miniatureTexture = Resize(texture, miniatureSize, miniatureSize, false);
            Image miniature = UITools.CreateImage(miniatureTexture.width, miniatureTexture.height);
            miniature.transform.SetParent(UVGroup.transform, false);
            miniature.sprite = Sprite.Create(miniatureTexture, new Rect(0, 0, miniatureTexture.width, miniatureTexture.height), new Vector2(), 100);

            // Text with name
            Text text = UITools.CreateText(mapName, 17, 180, 20);
            text.transform.SetParent(UVGroup.transform, false);

            // Export Button
            Button exportButton = UITools.CreateButton("Export UV", 16, 180, 35);
            exportButton.transform.SetParent(UVGroup.transform, false);
            exportButton.onClick.AddListener((UnityAction)delegate
            {
                ExportUVButton(renderer, index);
            });
            dropdownTextures.Add(exportButton.gameObject.GetComponentInChildren<Image>().sprite.texture);
        }

        private static void ExportUVButton(Renderer renderer, int index)
        {
            string path = Path.GetFullPath(".") + "\\UserData\\MaterialMod_Textures";
            string[] files = OpenFileDialog.ShowSaveDialog("Export File", path, "PNG Image (*.png)|*.png", OpenFileDialog.SingleFileFlags, OpenFileDialog.NativeMethods.GetActiveWindow());
            if (files == null) return;
            if (!files[0].EndsWith(".png")) files[0] = files[0] + ".png";


            // Getting size of main texture
            int width, height;
            Texture mainTexture = renderer.material.GetTexture("_MainTex");
            if (mainTexture != null)
            {
                width = mainTexture.width;
                height = mainTexture.height;
            }
            else width = height = 1024;

            List<Texture2D> UVRenderers = new List<Texture2D>();
            MeshRenderer meshRenderer = renderer.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null) UVRenderers = UVMap.GetUVMaps(meshRenderer, width, height);
            SkinnedMeshRenderer skinnedMeshRenderer = renderer.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null) UVRenderers.AddRange(UVMap.GetUVMaps(skinnedMeshRenderer, width, height));

            Texture2D UVtexture = UVRenderers[index];
            File.WriteAllBytes(files[0], UVtexture.EncodeToPNG());
            Log.LogMessage("MaterialMod: File Saved");

            // Cleaning textures
            for (int i = 1; i < UVRenderers.Count; i++)
                GarbageTextures.Add(UVRenderers[i]);
            MaterialModMonoBehaviour.DestroyGarbage();
        }

        private static void CreateTextureBlock(Material material, Texture2D miniatureTexture, Vector2 texOriginalSize, GameObject parent, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName)
        {
            // UI group
            GameObject textureGroup = new GameObject("TextureGroup " + textureName);
            textureGroup.transform.SetParent(parent.transform, false);
            VerticalLayoutGroup verticalLayoutGroup = textureGroup.AddComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            // Background image for getting the scroll
            RectTransform textureGroupRect = textureGroup.GetComponent<RectTransform>();
            textureGroupRect.sizeDelta = new Vector2(190, 280);
            CanvasRenderer canvasRenderer = textureGroup.AddComponent<CanvasRenderer>();
            Image background = textureGroup.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0);
            //dropdownTextures.Add(background.sprite.texture);

            Image miniature = UITools.CreateImage(miniatureTexture.width, miniatureTexture.height);
            miniature.transform.SetParent(textureGroup.transform, false);
            UpdateMiniature(miniature, miniatureTexture, textureName);

            // Text with name
            string textContent = textureName.Replace("_", "");
            Text text = UITools.CreateText(textContent, 17, 180, 20);
            text.transform.SetParent(textureGroup.transform, false);

            // Load Button
            Button buttonSet = UITools.CreateButton("Load new texture", 18, 180, 35);
            buttonSet.transform.SetParent(textureGroup.transform, false);
            buttonSet.onClick.AddListener((UnityAction)delegate
            {
                LoadTextureButton(material, characterContent, texDictionary, kindIndex, renderIndex, textureName, miniature, text);
            });
            dropdownTextures.Add(buttonSet.gameObject.GetComponentInChildren<Image>().sprite.texture);

            // Export Button
            Button buttonReset = UITools.CreateButton("Export current texture", 16, 180, 35);
            buttonReset.transform.SetParent(textureGroup.transform, false);
            buttonReset.onClick.AddListener((UnityAction)delegate
            {
                ExportTextureButton(material, characterContent, texDictionary, kindIndex, renderIndex, textureName, miniature, text);
            });
            dropdownTextures.Add(buttonReset.gameObject.GetComponentInChildren<Image>().sprite.texture);

            //// Offset input
            //InputField offsetX, offsetY;
            //Button offsetButton;
            //(offsetX, offsetY, offsetButton) = UITools.InputVector2("Offset", 16, 60, 30, textureGroup.transform);
            //offsetX.text = material.GetTextureOffset(textureName).x.ToString();
            //offsetY.text = material.GetTextureOffset(textureName).y.ToString();
            //offsetButton.onClick.AddListener((UnityAction) delegate { SetOffset(material, textureName, offsetX, offsetY); });

            //// Scale input
            //InputField scaleX, scaleY;
            //Button scaleButton;
            //(scaleX, scaleY, scaleButton) = UITools.InputVector2("Scale", 16, 60, 30, textureGroup.transform);
            //scaleX.text = material.GetTextureScale(textureName).x.ToString();
            //scaleY.text = material.GetTextureScale(textureName).y.ToString();
            //scaleButton.onClick.AddListener((UnityAction)delegate { SetScale(material, textureName, scaleX, scaleY); });

            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        private static void SetOffset(Material material, string textureName, InputField xInput, InputField yInput)
        {
            xInput.text = xInput.text.Replace(",", ".");
            yInput.text = yInput.text.Replace(",", ".");
            float.TryParse(xInput.text, out float x);
            float.TryParse(yInput.text, out float y);
            Vector2 vector2 = new Vector2(x, y);

            material.SetTextureOffset(textureName, vector2);

            Vector2 internalScale = material.GetTextureScale(textureName);
        }

        private static void SetScale(Material material, string textureName, InputField xInput, InputField yInput)
        {
            xInput.text = xInput.text.Replace(",", ".");
            yInput.text = yInput.text.Replace(",", ".");
            float.TryParse(xInput.text, out float x);
            float.TryParse(yInput.text, out float y);
            Vector2 vector2 = new Vector2(x, y);

            material.SetTextureScale(textureName, vector2);

            Vector2 internalScale = material.GetTextureScale(textureName);
        }

        private static void UpdateMiniature(Image miniature, Texture2D texture, string textureName)
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

                miniatureTexture = TextureTools.Resize(texture, width, height, false);
            }
            else
            {
                miniatureTexture = new Texture2D(texture.width, texture.height);
                Graphics.CopyTexture(texture, miniatureTexture);
            }

            // From pink maps to regular normal maps
            if (textureName.Contains("Bump"))
            {
                miniatureTexture = TextureTools.PinkToNormal(miniatureTexture);
            }

            miniature.sprite = Sprite.Create(miniatureTexture, new Rect(0, 0, miniatureTexture.width, miniatureTexture.height), new Vector2(), 100);
            dropdownTextures.Add(miniatureTexture);
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());
        }

        private static void LoadTextureButton(Material material, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            // Load from file
            string path = Path.GetFullPath(".") + "\\UserData\\MaterialMod_Textures";
            string[] files = OpenFileDialog.ShowOpenDialog("Open File", path, "PNG Image (*.png)|*.png", OpenFileDialog.SingleFileFlags, OpenFileDialog.NativeMethods.GetActiveWindow());
            if (files == null) return;

            Texture2D texture = new Texture2D(2, 2);
            byte[] bytes = File.ReadAllBytes(files[0]);
            texture.LoadImage(bytes);
            if (texture.width > 4096 || texture.height > 4096)
            {
                Log.LogMessage("MaterialMod: WARNING! Max texture size is 4096 x 4096");
                GarbageTextures.Add(texture);
                return;
            }

            int coordinateType = (int)characterContent.currentCoordinate;
            Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>> dicTextures;

            // Getting the texture dictionary
            if (texDictionary == TextureDictionaries.clothesTextures) dicTextures = characterContent.clothesTextures;
            else if (texDictionary == TextureDictionaries.accessoryTextures) dicTextures = characterContent.accessoryTextures;
            else if (texDictionary == TextureDictionaries.hairTextures) dicTextures = characterContent.hairTextures;
            else if (texDictionary == TextureDictionaries.bodySkinTextures) dicTextures = characterContent.bodySkinTextures;
            else if (texDictionary == TextureDictionaries.faceSkinTextures) dicTextures = characterContent.faceSkinTextures;
            else return;

            // Texture = characterContent.clothesTextures[coordinate][kind][renderIndex][TextureName]
            if (!dicTextures.ContainsKey(coordinateType)) dicTextures.Add(coordinateType, new Dictionary<int, Dictionary<int, Dictionary<string, byte[]>>>());
            if (!dicTextures[coordinateType].ContainsKey(kindIndex)) dicTextures[coordinateType].Add(kindIndex, new Dictionary<int, Dictionary<string, byte[]>>());
            if (!dicTextures[coordinateType][kindIndex].ContainsKey(renderIndex)) dicTextures[coordinateType][kindIndex].Add(renderIndex, new Dictionary<string, byte[]>());
            if (!dicTextures[coordinateType][kindIndex][renderIndex].ContainsKey(textureName)) dicTextures[coordinateType][kindIndex][renderIndex].Add(textureName, null);

            // Update Texture dictionary
            // From normal maps to Illusion pre-processed pink maps
            if (textureName.Contains("Bump")) texture = TextureTools.NormalToPink(texture);
            // Weatering mask must have the same dimensions
            if (textureName.Contains("_Weathering"))
            {
                int newWidth = texture.width;
                int newHeight = texture.height;
                Texture oldTexture = material.GetTexture(textureName);
                int oldWidth = oldTexture.width;
                int oldHeight = oldTexture.height;
                if (newWidth != oldWidth || newHeight != oldHeight)
                {
                    Log.LogMessage("MaterialMod: ERROR! Texture dimensions must match");
                    return;
                }
            }
            dicTextures[coordinateType][kindIndex][renderIndex][textureName] = texture.EncodeToPNG();


            // ======================================= Texture is set here ===========================================
            // Cleaning old textures. Not for skin, they need further investigation
            //if (texture != material.GetTexture(textureName) &&
            //    texDictionary != TextureDictionaries.bodySkinTextures &&
            //    texDictionary != TextureDictionaries.faceSkinTextures)
            //{
            //    GarbageTextures.Add(material.GetTexture(textureName));
            //}


            material.SetTexture(textureName, texture);
            UpdateMiniature(miniature, texture, textureName);

            MaterialModMonoBehaviour.DestroyGarbage();

            Log.LogMessage("MaterialMod: File Loaded");
        }

        private static void ExportTextureButton(Material material, CharacterContent characterContent, TextureDictionaries texDictionary, int kindIndex, int renderIndex, string textureName, Image miniature, Text sizeText)
        {
            string path = Path.GetFullPath(".") + "\\UserData\\MaterialMod_Textures";
            string[] files = OpenFileDialog.ShowSaveDialog("Export File", path, "PNG Image (*.png)|*.png", OpenFileDialog.SingleFileFlags, OpenFileDialog.NativeMethods.GetActiveWindow());
            if (files == null) return;
            if (!files[0].EndsWith(".png")) files[0] = files[0] + ".png";

            Texture2D texture = TextureTools.ToTexture2D(material.GetTexture(textureName));
            // From pink maps to regular normal maps
            if (textureName.Contains("Bump")) texture = TextureTools.PinkToNormal(texture);

            File.WriteAllBytes(files[0], texture.EncodeToPNG());

            GarbageTextures.Add(texture);
            MaterialModMonoBehaviour.DestroyGarbage();

            Log.LogMessage("MaterialMod: File Saved");
        }
    }
}
