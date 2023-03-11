using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// BepInEx
using BepInEx;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using HarmonyLib;

// Unity 
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Room Girl
using RG;
using CharaCustom;

namespace IllusionPlugins
{
    internal class RG_MaterialModUI
    {
        // Tab of the MaterialMod in clothes sub menu
        public static UI_ToggleEx clothesToggle;

        // Content of the MaterialMod in clothes sub menu
        public static GameObject clothesTabContent;

        /// <summary>
        /// Create a new tab on Chara Maker Clothing window
        /// </summary>
        public static void MakeClothesTab()
        {
            SettingWindowSize(502f);
            string tabName = "Green";

            GameObject selectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/SelectMenu");

            // ======================================== Creating the toogle tab ==========================================           
            GameObject originalToggle = selectMenu.transform.GetChild(0).gameObject;
            GameObject toggleObject = UnityEngine.Object.Instantiate(originalToggle, selectMenu.transform);
            toggleObject.transform.localScale = Vector3.one;

            // Naming the toggle
            toggleObject.name = "tgl0" + selectMenu.transform.childCount;

            // Removing old UI_ToggleEx component and add a empty one
            UnityEngine.Object.DestroyImmediate(toggleObject.GetComponent<UI_ToggleEx>());

            // =====
            clothesToggle = toggleObject.AddComponent<UI_ToggleEx>();

            // Things copied from UnityExplorer
            clothesToggle.group = selectMenu.GetComponent<ToggleGroup>();
            clothesToggle._usedTextColor = true;
            clothesToggle._hasSelection_k__BackingField = false;
            clothesToggle.selectedColor = new Color(0, 0.3686f, 0.6549f, 1);

            // Colors
            ColorBlock colorblock = new ColorBlock();
            colorblock.normalColor =      new Color(0, 0.3686f, 0.6549f, 1);
            colorblock.highlightedColor = new Color(0, 0.6353f, 0.7922f, 1);
            colorblock.pressedColor =     new Color(0, 0.6353f, 0.7922f, 1);
            colorblock.selectedColor =    new Color(0, 0.3686f, 0.6549f, 1);
            colorblock.disabledColor =    new Color(0, 0.3686f, 0.6549f, 1);
            colorblock.colorMultiplier = 1;
            colorblock.fadeDuration = 0.1f;
            clothesToggle.TextColor = colorblock;

            // Add images to the new toggle
            clothesToggle.targetGraphic = toggleObject.GetComponent<Image>();
            clothesToggle.overImage = toggleObject.transform.Find("imgSel").GetComponent<Image>();
            clothesToggle.graphic = toggleObject.transform.Find("imgOn").GetComponent<Image>();

            // Changing tab name
            Text text = toggleObject.transform.GetComponentInChildren<Text>();
            text.text = tabName;

            // ======================================== Creating Content Panel ==========================================
            GameObject SettingsParent = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/Setting");
            GameObject originalSetting = SettingsParent.transform.GetChild(SettingsParent.transform.childCount - 1).gameObject;
            GameObject newSetting = UnityEngine.Object.Instantiate(originalSetting, SettingsParent.transform);

            // Naming the object
            newSetting.name = "Setting0" + SettingsParent.transform.childCount;

            // Set clothesTabContent for the whole class
            clothesTabContent = newSetting.GetComponentInChildren<ContentSizeFitter>().gameObject;

            // Cleaning content
            for (int i = clothesTabContent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(clothesTabContent.transform.GetChild(i).gameObject);
            }

            // Replacing vertical layout group with grid layout group
            GameObject.DestroyImmediate(clothesTabContent.GetComponent<VerticalLayoutGroup>());
            GridLayoutGroup grid = clothesTabContent.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230, 295);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(5, 5);

            // ====== Enabling when set ======
            clothesToggle.onValueChanged.AddListener((UnityAction<bool>)delegate
            {
                OnClothesToggleEnabled(clothesToggle, newSetting.GetComponent<CanvasGroup>());
            });
        }

        public static Image CreateTextureImage(Texture2D texture, int size)
        {
            GameObject imageObject = new GameObject("TextureImage");
            //imageObject.transform.SetParent(clothesTabContent.transform);
            //imageObject.transform.localScale = Vector3.one;

            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);

            CanvasRenderer canvasRenderer = imageObject.AddComponent<CanvasRenderer>();
            //VerticalLayoutGroup verticalLayoutGroup = imageObject.AddComponent<VerticalLayoutGroup>();
            //verticalLayoutGroup.childForceExpandHeight = false;

            LayoutElement layout = imageObject.AddComponent<LayoutElement>();
            layout.minHeight = size;

            Image image = imageObject.AddComponent<Image>();
            image.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2());
            image.preserveAspect = true;

            // Mark to rebuild in the next frame
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());

            return image;
        }

        /// <summary>
        /// Create a button on places there are managed with VerticalLayoutGroup 
        /// </summary>
        /// <param name="text">Button text</param>
        /// <returns></returns>
        public static Button CreateClothesButton(string text)
        {
            // Creating button object
            GameObject buttonObject = new GameObject("Button");
            //buttonObject.transform.SetParent(clothesTabContent.transform);
            //buttonObject.transform.localScale = Vector3.one;

            buttonObject.AddComponent<RectTransform>();
            buttonObject.AddComponent<CanvasRenderer>();
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 35;

            // Getting button images from the toggle
            GameObject selectMenu = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinClothes/DefaultWin/C_Clothes/SelectMenu");
            GameObject originalToggle = selectMenu.transform.GetChild(0).gameObject;
            GameObject buttonImageObject = UnityEngine.Object.Instantiate(originalToggle, buttonObject.transform);
            buttonImageObject.name = "Images";
            //originalToggle.GetComponent<UI_ToggleEx>().isOn = true;


            // Cleaning what is not an image
            UnityEngine.Object.Destroy(buttonImageObject.GetComponent<UI_ToggleEx>());
            UnityEngine.Object.Destroy(buttonImageObject.GetComponent<LayoutElement>());
            UnityEngine.Object.Destroy(buttonImageObject.GetComponent<UniRx.Triggers.ObservableDestroyTrigger>());

            // Getting image and resizing, resize image will also resize the button
            Image mainImage = buttonImageObject.GetComponent<Image>();
            RectTransform rect = buttonImageObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 35);

            // Finally, creating the button itself
            Button button = buttonObject.AddComponent<Button>();
            button.image = mainImage.GetComponentInChildren<Image>();

            // Button Text
            buttonObject.GetComponentInChildren<Text>().text = text;

            // Colors
            ColorBlock colorblock = new ColorBlock();
            colorblock.normalColor =      new Color(1, 1f, 1f, 1);
            colorblock.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1);
            colorblock.pressedColor =     new Color(0.5f, 0.5f, 0.5f, 1);
            colorblock.selectedColor =    new Color(1f, 1f, 1f, 1);
            colorblock.disabledColor =    new Color(0.3f, 0.3f, 0.3f, 1);
            colorblock.colorMultiplier = 1;
            colorblock.fadeDuration = 0.1f;
            button.colors = colorblock;

            // Mark to rebuild in the next frame
            LayoutRebuilder.MarkLayoutForRebuild(clothesTabContent.GetComponent<RectTransform>());

            return button;
        }



        static void OnClothesToggleEnabled(Toggle toggle, CanvasGroup canvas)
        {
            if (toggle.isOn)
            {
                // Disable every canvas, then enable just the current one
                Transform parent = canvas.transform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    CanvasGroup childcanvas = parent.GetChild(i).GetComponent<CanvasGroup>();
                    childcanvas.alpha = 0;
                    childcanvas.blocksRaycasts = false;
                    childcanvas.interactable = false;
                }
                canvas.alpha = 1;
                canvas.blocksRaycasts = true;
                canvas.interactable = true;
            }
            else
            {
                canvas.alpha = 0;
                canvas.blocksRaycasts = false;
                canvas.interactable = false;
            }
        }

        public static void SettingWindowSize(float xSize)
        {
            GameObject settingWindow = GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow");
            RectTransform settingRect = settingWindow.GetComponent<RectTransform>();
            Vector2 position = settingRect.anchoredPosition;
            Vector2 size = settingRect.sizeDelta;

            position.x += size.x - xSize;
            settingRect.anchoredPosition = position;

            size.x = xSize;
            settingRect.sizeDelta = size;
        }
    }
}
