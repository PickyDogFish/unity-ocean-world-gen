float3 RefractionCoords(float refractionStrength, float4 positionNDC, float viewDepth, float3 normal)
{
	//float2 uvOffset = normal.xz * refractionStrength;
	//uvOffset.y *= _CameraDepthTexture_TexelSize.z * abs(_CameraDepthTexture_TexelSize.y);
	//float2 refractedScreenUV = (positionNDC.xy + uvOffset) / positionNDC.w;
    //float rawDepth = SampleSceneDepth(refractedScreenUV);
    //float refractedDepthDiff = LinearEyeDepth(rawDepth, _ZBufferParams) - viewDepth;
	//uvOffset *= saturate(refractedDepthDiff);
	//refractedScreenUV = (positionNDC.xy + uvOffset) / positionNDC.w;
    //rawDepth = SampleSceneDepth(refractedScreenUV);
	//return float3(refractedScreenUV, rawDepth);
}