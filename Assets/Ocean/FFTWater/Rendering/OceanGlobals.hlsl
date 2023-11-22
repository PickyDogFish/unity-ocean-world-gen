            
            
TEXTURE2D(_OceanDisplacementTex);
SAMPLER(sampler_OceanDisplacementTex);


TEXTURE2D(Ocean_CameraSubmergenceTexture);
SAMPLER(samplerOcean_CameraSubmergenceTexture);

float4x4 Ocean_InverseProjectionMatrix;

float3 Ocean_FogColor;
float Ocean_FogIntensity;

float Ocean_WaveScale;