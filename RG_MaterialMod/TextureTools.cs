using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils;
using BepInEx.IL2CPP.Utils.Collections;
using HarmonyLib;
using UnhollowerRuntimeLib;

// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

// Game Specific
using RG;
using Chara;
using CharaCustom;


namespace IllusionPlugins
{
    internal class TextureTools
    {
        // ================================================== Texture Tools ==================================================
        /// <summary>
        /// Converts Illusion Pink map to Normal map. Warning: CPU heavy
        /// </summary>
        public static Texture2D PinkToNormal(Texture2D texture)
        {
            Color[] colorArray = texture.GetPixels(0);
            float x, y, z, polyfit;

            for (int i = 0; i < colorArray.Length; i++)
            {
                // DXT5nm channel swap
                colorArray[i].r = colorArray[i].a;
                colorArray[i].a = 1;

                // Taking off Illusion processing
                y = colorArray[i].g;
                polyfit = (-0.142436f * y * y) + 0.146477f * y - 0.001472f;  // Got this from polynomial fit (Excel File in project root)
                colorArray[i].g = (y - polyfit) * (y - polyfit);

                // Recovering z axis
                x = colorArray[i].r * 2 - 1;
                y = colorArray[i].g * 2 - 1;
                z = Mathf.Sqrt(1 - (x * x) - (y * y));
                colorArray[i].b = z * 0.5f + 0.5f;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(true);
            return texture;
        }

        public static Texture2D NormalToPink(Texture2D texture)
        {
            Color[] colorArray = texture.GetPixels(0);
            float y, polyfit;

            for (int i = 0; i < colorArray.Length; i++)
            {
                // Applying Illusion processing
                y = colorArray[i].g;
                polyfit = (-0.142436f * y * y) + 0.146477f * y - 0.001472f;  // Got this from polynomial fit (Excel File in project root)
                colorArray[i].g = Mathf.Sqrt(y) + polyfit;

                // DXT5nm channel swap
                colorArray[i].a = colorArray[i].r;
                colorArray[i].b = colorArray[i].g;
                colorArray[i].r = 1;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(true);

            return texture;
        }

        // Was so slow that deserved a specialized method
        public static Dictionary<string, (Vector2, Texture2D)> GetTexNamesAndMiniatures(Material material, int maxSize)
        {
            //Shader shader = material.shader;
            //int propertyCount = shader.GetPropertyCount();
            //for (int i = 0; i < propertyCount; i++)
            //{
            //    if (shader.GetPropertyType(i) == ShaderPropertyType.Float)
            //    {
            //        string name = shader.GetPropertyName(i);
            //        float value = material.GetFloat(name);
            //        //Debug.Log("Property: " + name + " value: " + value);
            //    }
            //}

            Dictionary<string, (Vector2, Texture2D)> dicTexture = new Dictionary<string, (Vector2, Texture2D)>();
            string[] textureaNames = material.GetTexturePropertyNames();

            for (int i = 0; i < textureaNames.Length; i++)
            {
                string textureName = textureaNames[i];
                Texture texture = material.GetTexture(textureName);

                // REVIEW THIS
                if (texture == null) continue;
                if (string.IsNullOrEmpty(textureName)) continue;

                Vector2 originalSize = new Vector2(texture.width, texture.height);

                // Getting miniature size maintaining proportions
                int width, height;
                width = height = maxSize;
                if (texture.height > texture.width) width = height * texture.width / texture.height;
                else height = width * texture.height / texture.width;

                Texture2D texture2D = Resize(texture, width, height, false);

                dicTexture.Add(textureName, (originalSize, texture2D));
            }
            return dicTexture;
        }

        /// <summary>
        /// Resize texture in the GPU
        /// </summary>
        public static Texture2D Resize(Texture texture, int width, int height, bool updateMipMap)
        {
            Texture2D result = new Texture2D(width, height);
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0);
            RenderTexture currentRT = RenderTexture.active;

            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;

            result.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);

            result.Apply(updateMipMap);
            return result;
        }
        /// <summary>
        /// Resize texture in the GPU
        /// </summary>
        public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY, bool updateMipMap)
        {
            Texture2D result = new Texture2D(targetX, targetY);
            RenderTexture renderTexture = RenderTexture.GetTemporary(targetX, targetY, 0);
            RenderTexture currentRT = RenderTexture.active;

            Graphics.Blit(texture2D, renderTexture);
            RenderTexture.active = renderTexture;
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0, false);

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            result.Apply(updateMipMap);
            return result;
        }

        /// <summary>
        /// Generate a square green texture with the desired size
        /// </summary>
        static Texture2D GreenTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(2, 2);

            // Making the Texture2D Green
            Color[] colorArray = texture.GetPixels(0);
            for (int x = 0; x < colorArray.Length; x++)
            {
                colorArray[x] = Color.green;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(false);
            texture = Resize(texture, width, height, true);

            return texture;
        }

        /// <summary>
        /// Converts Texture into Texture2D. Texture2D can be applyed directly to the material later
        /// </summary>
        public static Texture2D ToTexture2D(Texture texture)
        {
            Texture2D texture2D = new Texture2D(texture.width, texture.height);
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0);
            RenderTexture currentRT = RenderTexture.active;

            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            texture2D.Apply(true);
            return texture2D;
        }
    }
}
