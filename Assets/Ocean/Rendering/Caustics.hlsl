half3 sampleCaustics(half2 uv, float colorSplit){
    half r = SAMPLE_TEXTURE2D(Ocean_CausticsTexture, samplerOcean_CausticsTexture, uv).r;
    half g = SAMPLE_TEXTURE2D(Ocean_CausticsTexture, samplerOcean_CausticsTexture, uv + half2(0, colorSplit)).r;
    half b = SAMPLE_TEXTURE2D(Ocean_CausticsTexture, samplerOcean_CausticsTexture, uv + half2(colorSplit, colorSplit)).r;

    return half3(r,g,b);
}


float3 CalcCaustics(float3 positionWS){
    half2 panDir = float2(cos(Ocean_WindAngle), sin(Ocean_WindAngle));
    half2 uv = mul(positionWS, Ocean_MainLightDirection).xy;
    half2 uv1 = _Time * panDir * 0.12 + uv * 0.37 / Ocean_WaveScale * 6;
    half2 uv2 = _Time * panDir * float2(-0.1, -0.13) + uv * 0.43 / Ocean_WaveScale * 6;
    half3 caustics1 = sampleCaustics(uv1, Ocean_ColorSplit);
    half3 caustics2 = sampleCaustics(uv2, Ocean_ColorSplit);
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    half light = MainLightRealtimeShadow(shadowCoord);
    half heightPercent = 1 - saturate(-positionWS.y / Ocean_CausticsMaxDepth);
    half topFade = saturate((Ocean_TopFade + positionWS.y)/Ocean_TopFade);

    return min(caustics1*2, caustics2*2) * light * (saturate(heightPercent - topFade));
}
