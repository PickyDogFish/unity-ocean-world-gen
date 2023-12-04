Shader "Ocean/UnderwaterEffect"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
    
        HLSLINCLUDE
            #include "FullscreenVert.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "OceanGlobals.hlsl"
            #include "DisplacementSampler.hlsl"
            #include "OceanVolume.hlsl"
            
        ENDHLSL

        Pass
        {
            Name "Camera Submergence"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            
            #pragma vertex ProceduralFullscreenVert
            #pragma fragment SubmergenceFrag

            float3 SubmergenceFrag(Varyings input) : SV_Target
            {
                float4 positionCS = float4(input.uv * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
                float4 positionVS = mul(Ocean_InverseProjectionMatrix, positionCS);
                positionVS = positionVS / positionVS.w;
                float4 positionWS = mul(UNITY_MATRIX_I_V, positionVS);
                float waterHeight = SampleHeight(positionWS.xz, Ocean_WaveScale);//ShoreModulation(SampleShore(pos.xz).r));
                return positionWS.y - waterHeight + 0.5;
            }

            ENDHLSL
        }

        Pass
        {
            Name "Underwater Post Effect"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment UnderwaterPostEffectFrag

            // calculates the color of underwater objects when underwater
        half4 UnderwaterPostEffectFrag(Varyings input) : SV_Target
        {

            float submergence = SAMPLE_TEXTURE2D(Ocean_CameraSubmergenceTexture, samplerOcean_CameraSubmergenceTexture, input.uv).r;
            float safetyMargin = 0.05;
            clip(-(submergence - 0.5 > safetyMargin));
            float rawDepth = SampleSceneDepth(input.uv);
            float3 WPFromDepth = ComputeWorldSpacePosition(input.uv, rawDepth, UNITY_MATRIX_I_VP);
            float viewDist = length(WPFromDepth - _WorldSpaceCameraPos);
            //viewDir /= viewDist;
            //float4 positionWS = mul(Ocean_InverseViewMatrix, positionVS);


            //adding caustics to scene color

            //float3 backgroundColor = AddCaustics(positionWS, SampleSceneColor(input.uv));

            //Light mainLight = GetMainLight();
            //float3 volume = UnderwaterFogColor(viewDir, mainLight.direction, _WorldSpaceCameraPos.y);
            //float3 color = ColorThroughWater(backgroundColor, volume, viewDist - _ProjectionParams.y, -positionWS.y);

            float3 forward = mul(UNITY_MATRIX_V, float4(0, 0, 1, 0)).xyz;

            float3 backgroundColor = SampleSceneColor(input.uv);
            float sunshafts = SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, input.uv).r;
            float3 finalColor = underwaterFogColor(Ocean_FogColor, Ocean_FogIntensity, viewDist, backgroundColor, sunshafts);
            return float4(finalColor, 0);// * submergence +  float3(0.3,0.5,0.9) * (1-submergence), 1);
        }
            ENDHLSL
        }
    }
}