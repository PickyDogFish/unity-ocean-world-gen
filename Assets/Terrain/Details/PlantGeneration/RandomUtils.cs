using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Windows;

public static class RandomUtils
{

    const string SAVE_LOCATION = "E:/pifko/Pictures/diploma/";
    public static Vector2 RandomVector2(System.Random random)
    {
        return new Vector2(RandomFloat11(random), RandomFloat11(random));
    }
    public static Vector3 RandomVector3(System.Random random)
    {
        return new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
    }

    /// <summary>
    /// returns random float between -1 and 1
    /// </summary>
    public static float RandomFloat11(System.Random random)
    {
        return ((float)random.NextDouble() - 0.5f) * 2;
    }

    public static Mesh CopyMesh(Mesh mesh)
    {
        Mesh newmesh = new Mesh();
        newmesh.vertices = mesh.vertices;
        newmesh.triangles = mesh.triangles;
        newmesh.uv = mesh.uv;
        newmesh.normals = mesh.normals;
        newmesh.colors = mesh.colors;
        newmesh.tangents = mesh.tangents;
        return newmesh;
    }

    public static void SaveTexture(Texture2D tex, string fileName)
    {
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(SAVE_LOCATION + fileName, bytes);
        Debug.Log("Saved texture to " + SAVE_LOCATION + fileName);
    }

    public static void SaveTexture(RenderTexture rTex, string fileName)
    {
        Texture2D tex = ToTexture2D(rTex);
        SaveTexture(tex, fileName);
    }

    public static Texture2D ToTexture2D(RenderTexture rTex, int res=0, int channels=3)
    {
        Texture2D tex;
        if (res != 0){
            tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
        } else {
            tex = new Texture2D(rTex.width, rTex.height, TextureFormat.ARGB32, false);
        }
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();

        if (channels == 2){
            Texture2D modTex = new Texture2D(tex.width, tex.height);
            Color[] pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                pixel.b = 0;
                pixels[i] = pixel;
            }
            modTex.SetPixels(pixels);
            modTex.Apply();
            return modTex;
        }
        
        return tex;
    }

    public static Texture2D ToBW(Texture2D inTex, int channel = 0, float multiplier = 1, bool avg = false)
    {
        Texture2D outTex = new Texture2D(inTex.width, inTex.height, TextureFormat.RGB24, false);

        Color[] pixels = inTex.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float color = 0;
            if (avg){
                for (int j = 0; j < channel; j++)
                    color += Mathf.Abs(pixels[i][j]);
                color /= channel;
                color *= multiplier;
            } else {
                color = Mathf.Abs(pixels[i][channel]) * multiplier;
            }
            pixels[i] = new Color(color, color, color, 1);
        }

        outTex.SetPixels(0,0,outTex.width, outTex.height, pixels);
        outTex.Apply();
        return outTex;
    }

}
