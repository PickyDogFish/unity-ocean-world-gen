Shader "Custom/DepthVisualizationShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata_t
            {
                float4 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float3 posWS : TEXCOORD1;
            };

            v2f vert (appdata_t i)
            {
                v2f o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(i.positionOS);
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.posWS = TransformObjectToWorld(i.positionOS);
                return o;
            }


            float4 bwFloat4(float val){
                return float4(val,val,val,1);
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.positionHCS.xy / _ScaledScreenParams.xy;
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    // Adjust z to match NDC for OpenGL
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                float3 WPFromDepth = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                float depthDif = length(WPFromDepth - i.posWS);

                return bwFloat4(saturate(length(depthDif) /100));
            }
            ENDHLSL
        }
    }
}