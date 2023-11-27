Shader "Ocean/SunShafts"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SunShafts"

            HLSLPROGRAM
            #pragma vertex ProceduralFullscreenVert
            #pragma fragment sunshaftFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "FullscreenVert.hlsl"
            #include "OceanGlobals.hlsl"

            struct Attributes{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float3 worldPositionFromDepth(float2 screenUV){
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    // Adjust z to match NDC for OpenGL
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                return ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
            }

            float sunshaftFrag(v2f input) : SV_Target
            {
                float4 positionCS = float4(input.uv * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
                float4 positionVS = mul(Ocean_InverseProjectionMatrix, positionCS);
                positionVS = positionVS / positionVS.w;
                float4 positionWS = mul(UNITY_MATRIX_I_V, positionVS);
                return 1;
            }

            ENDHLSL
        }
    }
}