using System;
using System.Collections.Generic;
using System.IO;

// Unity
using UnityEngine;
using UnityEngine.Rendering;


namespace IllusionPlugins
{
    // Inspired on KKPlugins MaterialEditor: https://github.com/IllusionMods/KK_Plugins/blob/master/src/MaterialEditor.Base/Export.UV.cs
    internal class UVMap
    {
        /// <summary>
        /// Get the UV map(s) of the SkinnedMeshRenderer or MeshRenderer
        /// </summary>
        public static List<Texture2D> GetUVMaps(Renderer renderer, int width, int height)
        {
            List<Texture2D> textures = new List<Texture2D>();
            Shader shader = Shader.Find("Hidden/Internal-Colored");

            Material lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);

            Mesh mesh;
            if (renderer is MeshRenderer meshRenderer)
                mesh = meshRenderer.GetComponent<MeshFilter>().mesh;
            else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                mesh = skinnedMeshRenderer.sharedMesh;
            else return null;

            for (int x = 0; x < mesh.subMeshCount; x++)
            {
                RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
                RenderTexture currentRT = RenderTexture.active;
                RenderTexture.active = renderTexture;

                var uvs = mesh.uv;
                var tris = mesh.GetTriangles(x);

                Color lineColor = Color.black;
                GL.PushMatrix();
                GL.LoadOrtho();
                GL.Clear(false, true, Color.clear);

                lineMaterial.SetPass(0);
                //GL.Begin(GL.LINES);     not in IL2CPP... Get the number by your own
                GL.Begin(1);
                GL.Color(lineColor);

                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector2 v = new Vector2(Reduce(uvs[tris[i]].x), Reduce(uvs[tris[i]].y));
                    Vector2 n1 = new Vector2(Reduce(uvs[tris[i + 1]].x), Reduce(uvs[tris[i + 1]].y));
                    Vector2 n2 = new Vector2(Reduce(uvs[tris[i + 2]].x), Reduce(uvs[tris[i + 2]].y));

                    GL.Vertex(v);
                    GL.Vertex(n1);

                    GL.Vertex(v);
                    GL.Vertex(n2);

                    GL.Vertex(n1);
                    GL.Vertex(n2);
                }

                GL.End();
                GL.PopMatrix();
                
                Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height);
                texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0, false);
                texture2D.Apply(false);

                RenderTexture.active = currentRT;
                RenderTexture.ReleaseTemporary(renderTexture);
                
                textures.Add(texture2D);
            }
            return textures;
        }

        /// <summary>
        /// Trim any floats outside of 0-1 range so only the decimal place remains. For moving UVs to the main unit square if they are outside of it.
        /// Probably a better way to do this. Probably breaks if one or two points of the tri are on a different UV square.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static float Reduce(float num)
        {
            if (num > 1f)
            {
                num -= 1f;
                return Reduce(num);
            }
            if (num < 0f)
            {
                num += 1f;
                return Reduce(num);
            }
            return num;
        }

        // Turn readable a non readable mesh. Works only on Unity 2021+...
        // https://forum.unity.com/threads/950170/
        //public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh)
        //{
        //    Mesh meshCopy = new Mesh();
        //    meshCopy.indexFormat = nonReadableMesh.indexFormat;

        //    // Handle vertices
        //    GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
        //    int totalSize = verticesBuffer.stride * verticesBuffer.count;
        //    byte[] data = new byte[totalSize];
        //    verticesBuffer.GetData(data);
        //    meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
        //    meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
        //    verticesBuffer.Release();

        //    // Handle triangles
        //    meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
        //    GraphicsBuffer indexesBuffer = nonReadableMesh.GetIndexBuffer();
        //    int tot = indexesBuffer.stride * indexesBuffer.count;
        //    byte[] indexesData = new byte[tot];
        //    indexesBuffer.GetData(indexesData);
        //    meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
        //    meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
        //    indexesBuffer.Release();

        //    // Restore submesh structure
        //    uint currentIndexOffset = 0;
        //    for (int i = 0; i < meshCopy.subMeshCount; i++)
        //    {
        //        uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
        //        meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
        //        currentIndexOffset += subMeshIndexCount;
        //    }

        //    // Recalculate normals and bounds
        //    meshCopy.RecalculateNormals();
        //    meshCopy.RecalculateBounds();

        //    return meshCopy;
        //}
    }
}
