using System;
using UnityEngine;
public static class NoiseGen
{

    public static float[,] GetNoiseArray(ComputeShader noiseCS, int size, float scale){
        float[,] finalValues = new float[size,size];
        ComputeBuffer noiseBuffer = new ComputeBuffer(size*size, sizeof(float));
        noiseCS.SetFloat("_scale", scale);
        noiseCS.SetInt("_size", size);
        noiseCS.SetBuffer(0, "_noiseValues", noiseBuffer);
        noiseCS.Dispatch(0, size/8, size/8, 1);
        noiseBuffer.GetData(finalValues);
        noiseBuffer.Release();
        Debug.Log(finalValues[size-1, size-1]);
        return finalValues;
    }

}