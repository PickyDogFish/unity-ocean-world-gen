

float SampleHeight(float2 worldUV, float waveScale)
{
    float4 displacement = _OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, worldUV/waveScale, 0.0f);
    displacement = _OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, (worldUV - displacement.yz)/waveScale, 0.0f);
    displacement = _OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, (worldUV - displacement.yz)/waveScale, 0.0f);
    displacement = _OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, (worldUV - displacement.yz)/waveScale, 0.0f);
    return displacement.x;
}