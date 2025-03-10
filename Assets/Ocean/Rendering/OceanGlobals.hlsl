#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(_OceanDisplacementTex);
SAMPLER(sampler_OceanDisplacementTex);

TEXTURE2D(_OceanNormalTex);
SAMPLER(sampler_OceanNormalTex);


TEXTURE2D(Ocean_CameraSubmergenceTexture);
SAMPLER(samplerOcean_CameraSubmergenceTexture);

TEXTURE2D(Ocean_SunShaftsTexture);
SAMPLER(samplerOcean_SunShaftsTexture);

//Caustics
TEXTURE2D(Ocean_CausticsTexture);
SAMPLER(samplerOcean_CausticsTexture);
half4x4 Ocean_MainLightDirection;
float Ocean_LuminanceMaskStrength;
float Ocean_ColorSplit;
float Ocean_CausticsMaxDepth;
float Ocean_TopFade;

float4x4 Ocean_InverseProjectionMatrix;

// environment maps
TEXTURECUBE(Ocean_CubeMap);
SAMPLER(samplerOcean_CubeMap);
float4 Ocean_CubeMap_HDR;

float3 Ocean_FogColor;
float Ocean_FogIntensity;
float Ocean_RefractionIntensity;

float Ocean_WindAngle;
float Ocean_WindMagnitude;

float Ocean_WaveScale;

float Ocean_NormalStrength;

float3 Ocean_ViewerPosition;


float3 SampleOceanCubeMap(float3 dir)
{
    float4 envSample = SAMPLE_TEXTURECUBE_LOD(Ocean_CubeMap, samplerOcean_CubeMap, dir, 0);
    return DecodeHDREnvironment(envSample, Ocean_CubeMap_HDR);
}