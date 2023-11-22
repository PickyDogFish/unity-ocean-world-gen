
float3 underwaterFogColor(float3 fogColor, float fogIntensity, float distance, float3 backgroundColor)
{
    float fogFactor = exp(-fogIntensity * distance);
    return fogFactor * backgroundColor + (1- fogFactor) * fogColor;
}