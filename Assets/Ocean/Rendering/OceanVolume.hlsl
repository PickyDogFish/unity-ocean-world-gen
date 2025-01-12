float3 underwaterFogColor(float3 viewDirection, float distance, float3 backgroundColor, float sunshaftIntensity, float depth)
{
    float fogFactor = exp(-Ocean_FogIntensity * distance);
    float depthFactor = exp(-clamp(depth/20, 0, 100));

    //return exp(-clamp((-depth)/50, 0, 100)) * (fogFactor * backgroundColor + (1- fogFactor) * (saturate(Ocean_FogColor + sunshaftIntensity)));
    return depthFactor * (fogFactor * backgroundColor + (1 - fogFactor) * saturate(fogFactor * Ocean_FogColor + sunshaftIntensity));
}

