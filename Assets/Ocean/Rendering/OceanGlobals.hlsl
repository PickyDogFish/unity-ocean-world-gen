#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(_OceanDisplacementTex);
SAMPLER(sampler_OceanDisplacementTex);


TEXTURE2D(Ocean_CameraSubmergenceTexture);
SAMPLER(samplerOcean_CameraSubmergenceTexture);

float4x4 Ocean_InverseProjectionMatrix;

// environment maps
TEXTURECUBE(Ocean_CubeMap);
SAMPLER(samplerOcean_CubeMap);
float4 Ocean_CubeMap_HDR;

float3 Ocean_FogColor;
float Ocean_FogIntensity;
float Ocean_RefractionIntensity;

float Ocean_WaveScale;


float3 SampleOceanCubeMap(float3 dir)
{
    float4 envSample = SAMPLE_TEXTURECUBE_LOD(Ocean_CubeMap, samplerOcean_CubeMap, dir, 0);
    return DecodeHDREnvironment(envSample, Ocean_CubeMap_HDR);
}