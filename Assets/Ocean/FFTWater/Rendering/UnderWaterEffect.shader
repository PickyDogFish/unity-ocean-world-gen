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

            TEXTURE2D(Ocean_CameraSubmergenceTexture);
            SAMPLER(samplerOcean_CameraSubmergenceTexture);
        ENDHLSL

        Pass
        {
            Name "Camera Submergence"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            
            #pragma vertex ProceduralFullscreenVert
            #pragma fragment SubmergenceFrag

            float SubmergenceFrag(Varyings input) : SV_Target
            {
                float4 positionCS = float4(input.uv * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
                float4 positionVS = mul(UNITY_MATRIX_I_VP, positionCS);
                positionVS = positionVS / positionVS.w;
                float4 positionWS = mul(UNITY_MATRIX_I_V, positionVS);
                float waterHeight = 0.5;//SampleHeight(positionWS.xz, 1, 1);//ShoreModulation(SampleShore(pos.xz).r));
                return positionWS.y;// - waterHeight + 0.5;
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
            //float safetyMargin = 0.05;
            //clip(-(submergence - 0.5 > safetyMargin));
            //float rawDepth = SampleSceneDepth(input.uv);
            //float4 positionCS = float4(input.uv * 2 - 1, rawDepth, 1);
            //float4 positionVS = mul(Ocean_InverseProjectionMatrix, positionCS);
            //positionVS /= positionVS.w;
            //float3 viewDir = -mul(Ocean_InverseViewMatrix, float4(positionVS.xyz, 0)).xyz;
            //float viewDist = length(positionVS);
            //viewDir /= viewDist;
            //float4 positionWS = mul(Ocean_InverseViewMatrix, positionVS);
//
            //safetyMargin *= saturate((viewDir.y * 1.3 + 1) * 0.5);
            //clip(-(submergence - 0.5 > safetyMargin));


            //adding caustics to scene color

            //float3 backgroundColor = AddCaustics(positionWS, SampleSceneColor(input.uv));

            //Light mainLight = GetMainLight();
            //float3 volume = UnderwaterFogColor(viewDir, mainLight.direction, _WorldSpaceCameraPos.y);
            //float3 color = ColorThroughWater(backgroundColor, volume, viewDist - _ProjectionParams.y, -positionWS.y);
            return float4(submergence, submergence, submergence, 1);
        }
            ENDHLSL
        }
    }
}