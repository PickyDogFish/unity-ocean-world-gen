
float3 underwaterFogColor(float3 fogColor, float fogIntensity, float distance, float3 backgroundColor, float sunshaftIntensity, float depth)
{
    float fogFactor = exp(-fogIntensity * distance);
    return exp(-clamp((-depth)/200, 0, 100)) * (fogFactor * backgroundColor + (1- fogFactor) * (saturate(fogColor + sunshaftIntensity)));
}