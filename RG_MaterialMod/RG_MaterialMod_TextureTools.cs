using System;
using System.Collections;
using System.IO;
using System.Diagnostics;

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
using Debug = UnityEngine.Debug;

// Game Specific
using RG;
using Chara;
using CharaCustom;
using System.Collections.Generic;
using System.Linq;

namespace IllusionPlugins
{
    public partial class RG_MaterialMod
    {
        // ================================================== Texture Tools ==================================================
        public static Texture2D DXT2nmToNormal(Texture2D texture)
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

        public static Texture2D NormalToDXT2nm(Texture2D texture)
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

        /// <summary>
        /// Get all textures from material and turns into a dictionary of TextureContents
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        public static Dictionary<string, Texture2D> GetMaterialTextures(Material material)
        {
            Dictionary<string, Texture2D> dicTexture = new Dictionary<string, Texture2D>();

            Shader shader = material.shader;

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string propertyName = shader.GetPropertyName(i);
                var propertyType = shader.GetPropertyType(i);

                //if (propertyType == UnityEngine.Rendering.ShaderPropertyType.Range)
                //{
                //    var shaderFloat = material.GetFloat(propertyName);
                //    Debug.Log("propertyType:" + propertyType + " propertyName: " + propertyName + " Value: " + shaderFloat);
                //}

                //if (propertyName == "_WeatheringMask") Debug.Log(shader.GetPropertyAttributes(i).ToString());

                if (propertyType == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    Texture texture = material.GetTexture(propertyName);



                    // REVIEW THIS
                    if (texture == null) continue;
                    if (string.IsNullOrEmpty(propertyName)) continue;



                    MaterialContent materialContent = new MaterialContent();
                    string textureName = propertyName;
                    Texture2D texture2D = ToTexture2D(texture);
                    dicTexture.Add(textureName, texture2D);

                    //GarbageTextures.Add(texture2D);
                }
            }
            return dicTexture;
        }

        /// <summary>
        /// Converts Texture into Texture2D. Texture2D can be applyed directly to the material later
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
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

        public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            Texture2D result = new Texture2D(targetX, targetY);
            RenderTexture renderTexture = RenderTexture.GetTemporary(targetX, targetY, 0);
            RenderTexture currentRT = RenderTexture.active;

            Graphics.Blit(texture2D, renderTexture);
            RenderTexture.active = renderTexture;
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            result.Apply(true);
            return result;
        }

        /// <summary>
        /// Generate a square texture with the desired size
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        static Texture2D GreenTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);

            // Making the Texture2D Green
            Color[] colorArray = texture.GetPixels(0);
            for (int x = 0; x < colorArray.Length; x++)
            {
                colorArray[x].r = 0;
                colorArray[x].g = 1;
                colorArray[x].b = 0;
            }

            texture.SetPixels(colorArray, 0);
            texture.Apply(true);

            return texture;
        }
    }
}
