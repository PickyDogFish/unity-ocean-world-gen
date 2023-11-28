Shader "Ocean/SunShafts"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
    
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "FullscreenVert.hlsl"
            #include "OceanGlobals.hlsl"
            #include "DisplacementSampler.hlsl"
            #include "OceanVolume.hlsl"
            
        ENDHLSL

        Pass
        {
            Name "Ray marching"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            
            #pragma vertex ProceduralFullscreenVert
            #pragma fragment RayMarchFrag

            float3 worldPositionFromDepth(float2 screenUV){
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    // Adjust z to match NDC for OpenGL
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                return ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
            }

            float calcWaterHeight(float2 screenUV){
                float4 positionCS = float4(screenUV * 2 - 1, UNITY_NEAR_CLIP_VALUE, 1);
                float4 positionVS = mul(Ocean_InverseProjectionMatrix, positionCS);
                positionVS = positionVS / positionVS.w;
                float4 positionWS = mul(UNITY_MATRIX_I_V, positionVS);
                return SampleHeight(positionWS.xz, Ocean_WaveScale);
            }

            float3 RayMarchFrag(Varyings input) : SV_Target
            {
                //dont render rays when above the water surface
                if (_WorldSpaceCameraPos.y > calcWaterHeight(input.uv)){
                    return 0;
                }
                
                float3 rayEndWorldPos = worldPositionFromDepth(input.uv);

                float3 startPosition = _WorldSpaceCameraPos;
                float3 rayVector = rayEndWorldPos - startPosition;
                float3 rayDirection =  normalize(rayVector);
                float rayLength = length(rayVector);
                rayLength = min(rayLength, 75); //75 is the hardcoded max distance for now

                int stepCount = 25;
                float stepLength = rayLength / stepCount; // 25 is the hardcoded amount of steps for now
                float3 step = rayDirection * stepLength;

                float3 currentPosition = startPosition;
                float lightSum = 0;

                float3 testResult;

                for (int i = 0; i < stepCount; i++){
                    float3 surfacePositionWS = currentPosition - currentPosition.y * (_MainLightPosition.xyz / _MainLightPosition.y); // _MainLightPosition is a normalized vector pointing towards the light. Scaling it to have y = 1. Result is point at world y = 0.
                    float3 normal = SampleNormal(surfacePositionWS.xz);
                    float distThroughWater = length(surfacePositionWS - currentPosition);

                    float light = dot(normal, _MainLightPosition) * pow(saturate(1 - distThroughWater/50),2);
                    lightSum += saturate(light);
                    currentPosition += step;
                } 

                lightSum /= stepCount;
                return lightSum;
            }

            ENDHLSL
        }

        Pass {
            Name "Blur x"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment BlurXFrag

            half4 BlurXFrag(Varyings input) : SV_TARGET
            {
                float shafts = SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, input.uv).r;
                return shafts;
            }

            ENDHLSL
        }

        Pass {
            Name "Blur y"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment BlurYFrag

            TEXTURE2D(_blurXTarget);
            SAMPLER(sampler_blurXTarget);

            half4 BlurYFrag(Varyings input) : SV_TARGET
            {
                float shafts = SAMPLE_TEXTURE2D(_blurXTarget, sampler_blurXTarget, input.uv).r;
                return shafts;
            }

            ENDHLSL
        }


        Pass
        {
            Name "Composit"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment UnderwaterPostEffectFrag


            // calculates the color of underwater objects when underwater
            half4 UnderwaterPostEffectFrag(Varyings input) : SV_Target
            {

                float shafts = SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, input.uv).r;
                float rawDepth = SampleSceneDepth(input.uv);
                float3 WPFromDepth = ComputeWorldSpacePosition(input.uv, rawDepth, UNITY_MATRIX_I_VP);
                float viewDist = length(WPFromDepth - _WorldSpaceCameraPos);


                float3 backgroundColor = SampleSceneColor(input.uv);
                float3 finalColor =  backgroundColor + shafts;// underwaterFogColor(Ocean_FogColor, Ocean_FogIntensity, viewDist, backgroundColor);
                return float4(finalColor, 0);// * submergence +  float3(0.3,0.5,0.9) * (1-submergence), 1);
            }
            ENDHLSL
        }
    }
}