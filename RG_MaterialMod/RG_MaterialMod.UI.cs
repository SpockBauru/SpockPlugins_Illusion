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
using System.IO;
using UnityEngine.EventSystems;

namespace IllusionPlugins
{
    /// <summary>
    /// Methods for make UI. Try to make it as less dependent on game instance as possible
    /// </summary>
    internal class RG_MaterialModUI
    {

        /// <summary>
        /// Create text object
        /// </summary>
        /// <param name="textContent"></param>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        public static Text CreateText(string textContent, int fontSize, int width, int height)
        {
            GameObject textObject = new GameObject("Text");
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            textObject.AddComponent<CanvasRenderer>();
            LayoutElement layout = textObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.minHeight = height;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0, 0.3686f, 0.6549f, 1);
            text.raycastTarget = false;

            text.text = textContent;
            text.fontSize = fontSize;


            return text;
        }

        /// <summary>
        /// Create an empty image object
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Image CreateImage(int width, int height)
        {
            GameObject imageObject = new GameObject("Image");
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            CanvasRenderer canvasRenderer = imageObject.AddComponent<CanvasRenderer>();
            LayoutElement layout = imageObject.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.minHeight = height;

            Image image = imageObject.AddComponent<Image>();
            image.preserveAspect = true;

            return image;
        }

        /// <summary>
        /// Create button object with text and image as separate child objects
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fontSize"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Button CreateButton(string text, int fontSize, int width, int height)
        {
            GameObject buttonObject = new GameObject("Button");
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = height;

            // Creating image and resizing, resize image will also resize the button
            Image image = CreateImage(width, height);
            image.transform.SetParent(buttonObject.transform, false);
            image.preserveAspect = false;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;

            // Image content
            Texture2D texture = new Texture2D(0, 0);
            texture.LoadImage(MaterialModResources.btnNormal_png);
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 pivot = Vector2.zero;
            Vector4 borders = new Vector4(25, 25, 25, 25);
            Sprite sprite = Sprite.Create(texture, rect, pivot, 100f, 1, SpriteMeshType.FullRect, borders);

            image.sprite = sprite;

            // Button Text object
            Text buttonText = CreateText(text, fontSize, 500, 50);
            buttonText.transform.SetParent(buttonObject.transform, false);

            // Creating the button itself
            Button button = buttonObject.AddComponent<Button>();
            button.image = image;

            // Colors
            ColorBlock colorblock = new ColorBlock();
            colorblock.normalColor = new Color(1, 1f, 1f, 1);
            colorblock.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1);
            colorblock.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1);
            colorblock.selectedColor = new Color(1f, 1f, 1f, 1);
            colorblock.disabledColor = new Color(0.3f, 0.3f, 0.3f, 1);
            colorblock.colorMultiplier = 1;
            colorblock.fadeDuration = 0.1f;
            button.colors = colorblock;

            // Unselect after click
            button.onClick.AddListener((UnityAction)DeselectButton);

            return button;
        }

        private static void DeselectButton()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        public static void ChangeWindowSize(float xSize, GameObject window)
        {
            RectTransform settingRect = window.GetComponent<RectTransform>();
            Vector2 position = settingRect.anchoredPosition;
            Vector2 size = settingRect.sizeDelta;

            position.x += size.x - xSize;
            settingRect.anchoredPosition = position;

            size.x = xSize;
            settingRect.sizeDelta = size;
        }

        /// <summary>
        /// <br></br>Create a new tab on Chara Maker sub-menu</br>
        /// Outputs (Toggle Tab, GameObject Window Content)
        /// </summary>
        public static (UI_ToggleEx, GameObject) CreateMakerTab(GameObject selectMenu, GameObject settingsGroup)
        {
            string tabName = "Green";

            // ======================================== Creating the toogle tab ==========================================           
            GameObject originalToggle = selectMenu.GetComponentInChildren<UI_ToggleEx>().gameObject;
            GameObject toggleObject = UnityEngine.Object.Instantiate(originalToggle, selectMenu.transform);
            toggleObject.transform.localScale = Vector3.one;

            // Naming the toggle
            toggleObject.name = "tgl0" + selectMenu.transform.childCount;

            // GETTING TOGGLE
            UI_ToggleEx ui_toggleEx = toggleObject.GetComponent<UI_ToggleEx>();

            // Things copied from UnityExplorer
            ui_toggleEx.group = selectMenu.GetComponent<ToggleGroup>();

            // Changing tab name
            Text text = toggleObject.transform.GetComponentInChildren<Text>();
            text.text = tabName;

            // ======================================== Creating Content Panel ==========================================
            GameObject originalSetting = settingsGroup.transform.GetChild(settingsGroup.transform.childCount - 1).gameObject;
            GameObject newSetting = UnityEngine.Object.Instantiate(originalSetting, settingsGroup.transform);

            // Naming the object
            newSetting.name = "Setting0" + settingsGroup.transform.childCount;

            // GETTING TAB CONTENT
            GameObject tabContent = newSetting.GetComponentInChildren<ContentSizeFitter>().gameObject;

            // Cleaning content
            for (int i = tabContent.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(tabContent.transform.GetChild(i).gameObject);
            }

            // Dropdown will go here
            Text TESTE = CreateText("TESTE TESTE", 20, 0, 0);
            TESTE.transform.SetParent(newSetting.transform, false);
            TESTE.alignment = TextAnchor.UpperCenter;
            RectTransform testeRect = TESTE.GetComponent<RectTransform>();
            testeRect.anchorMax = new Vector2(1, 1);
            testeRect.anchorMin = new Vector2(0, 1);

            // spacing scroll view
            GameObject scrollview = newSetting.GetComponentInChildren<ScrollRect>().gameObject;
            RectTransform scrollRect = scrollview.GetComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -8);
            scrollRect.sizeDelta = new Vector2(-26, -45);

            // Making the grid layout group
            UnityEngine.Object.DestroyImmediate(tabContent.GetComponent<VerticalLayoutGroup>());
            GridLayoutGroup grid = tabContent.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(230, 300);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(5, 5);


            // ====== Enable/disable when set ======
            ui_toggleEx.onValueChanged.AddListener((UnityAction<bool>)delegate
            {
                OnClothesToggleEnabled(ui_toggleEx, newSetting.GetComponent<CanvasGroup>());
            });

            // ======  Finalizing =========
            UI_ToggleEx originalUI = originalToggle.GetComponent<UI_ToggleEx>();
            originalUI.isOn = true;

            return (ui_toggleEx, tabContent);
        }

        private static void OnClothesToggleEnabled(Toggle toggle, CanvasGroup canvas)
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


    }
}
