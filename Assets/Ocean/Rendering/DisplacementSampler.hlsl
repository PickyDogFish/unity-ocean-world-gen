

float3 SampleDisplacement(float2 worldUV){
    float3 displacement = _OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, worldUV/Ocean_WaveScale, 0.0f)* (Ocean_WaveScale/100);
    return displacement.yxz;
}

float SampleHeight(float2 worldUV)
{
    //worldUV /= Ocean_WaveScale;
    float3 displacement = SampleDisplacement(worldUV);//_OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, worldUV, 0.0f)* (Ocean_WaveScale/100);
    //_OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, (worldUV - displacement.xz), 0.0f)* (Ocean_WaveScale/100);
    displacement = SampleDisplacement(worldUV-displacement.xz);
    displacement = SampleDisplacement(worldUV-displacement.xz);
    displacement = SampleDisplacement(worldUV-displacement.xz);
    return displacement.y;
}

float3 SampleNormal(float2 worldUV){
    return _OceanNormalTex.SampleLevel(sampler_OceanNormalTex, worldUV / Ocean_WaveScale, 0.0f);
}