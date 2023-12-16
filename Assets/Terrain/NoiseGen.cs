using System;
using UnityEngine;
public static class NoiseGen
{
    public struct TerrainGenData{
        public RenderTexture heightMap;
        public RenderTexture splatMap;
    }

    public static float[,] GetNoiseArray(Vector2Int tileCoords, ComputeShader noiseCS, int size, float scale){
        float[,] finalValues = new float[size,size];
        ComputeBuffer noiseBuffer = new ComputeBuffer(size*size, sizeof(float));
        noiseCS.SetFloat("_scale", scale);
        noiseCS.SetInt("_size", size);
        noiseCS.SetInts("_tileCoords", tileCoords.x, tileCoords.y);
        noiseCS.SetBuffer(0, "_noiseValues", noiseBuffer);
        noiseCS.Dispatch(0, size/8, size/8, 1);
        noiseBuffer.GetData(finalValues);
        noiseBuffer.Release();
        return finalValues;
    }

    public static RenderTexture GetNoiseRT(Vector2Int tileCoords, ComputeShader noiseCS, int tileSize, int size, float scale){
        RenderTextureDescriptor rtDescriptor = new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGBFloat);
        RenderTexture result = new RenderTexture(rtDescriptor);
        result.enableRandomWrite = true;
        noiseCS.SetFloat("_scale", scale);
        noiseCS.SetInt("_size", tileSize);
        noiseCS.SetInts("_tileCoords", tileCoords.x, tileCoords.y);
        noiseCS.SetTexture(1,"_noiseTexture", result);
        noiseCS.Dispatch(1, size/8, size/8, 1);
        return result;
    }

    public static TerrainGenData GetTerrainRT(Vector2Int tileCoords, ComputeShader noiseCS, int tileSize, int size, float scale){
        RenderTextureDescriptor rtDescriptor = new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGBFloat);
        RenderTexture heightMap = new RenderTexture(rtDescriptor);
        RenderTexture splatMap = new RenderTexture(rtDescriptor);
        heightMap.enableRandomWrite = true;
        splatMap.enableRandomWrite = true;
        noiseCS.SetFloat("_scale", scale);
        noiseCS.SetInt("_size", tileSize);
        noiseCS.SetInts("_tileCoords", tileCoords.x, tileCoords.y);
        noiseCS.SetTexture(2,"_heightMap", heightMap);
        noiseCS.SetTexture(2,"_splatMap", splatMap);
        noiseCS.Dispatch(2, size/8, size/8, 1);

        //Packing data into a struct
        TerrainGenData resultingData = new TerrainGenData();
        resultingData.heightMap = heightMap;
        resultingData.splatMap = splatMap;

        return resultingData;
    }

}