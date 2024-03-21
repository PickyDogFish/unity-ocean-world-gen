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

            float2 _rayTexSize;
            int _stepCount;
            float _anisotrophy;
            float _maxDistance;
            float _intensityMultiplier;
            float _extinctionCoefficient;
            float _scatteringCoefficient;
            
        ENDHLSL

        Pass
        {
            Name "Ray marching"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            
            #pragma vertex ProceduralFullscreenVert
            #pragma fragment NewRayMarchFrag

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
                return SampleHeight(positionWS.xz);
            }

            float random01( float2 p )
            {
                return frac(sin(dot(p, float2(41, 289)))*45758.5453 ); 
            }

            //The henyey-greenstein phase function.
            float HenyeyGreenstein(float cosTheta)
            {
                float anisotrophySquared = _anisotrophy * _anisotrophy;
                float numerator = 1.0f - anisotrophySquared;
                float denominator = pow(1.0f + anisotrophySquared - 2.0f * _anisotrophy * cosTheta, 1.5f);
                return 1.0f / (4.0f * 3.14159265359f) * numerator / denominator;
            }

            float3 NewRayMarchFrag(Varyings input) : SV_TARGET{
                float3 rayEndWorldPos = worldPositionFromDepth(input.uv);

                float mainLightLuminosity = 10;

                float3 startPosition = _WorldSpaceCameraPos;
                float3 rayVector = rayEndWorldPos - startPosition;
                float3 rayDirection =  normalize(rayVector);
                float rayLength = length(rayVector);
                rayLength = min(rayLength, _maxDistance);

                float stepLength = rayLength / _stepCount;
                float3 step = rayDirection * stepLength;
                rayEndWorldPos = startPosition + rayDirection * rayLength;

                //adding randomness to avoid artefacts from always sampling at same depth
                float3 currentPosition = rayEndWorldPos - random01(input.uv) * step;
                float lightSum = 0;

                for (int i = 1; i < _stepCount; i++){
                    float3 surfacePositionWS = currentPosition - currentPosition.y * (_MainLightPosition.xyz / _MainLightPosition.y); // _MainLightPosition is a normalized vector pointing towards the light. Scaling it to have y = 1. Result is point at world y = 0.
                    float3 surfaceNormal = SampleNormal(surfacePositionWS.xz);


                    float distFromSurface = length(surfacePositionWS - currentPosition);
                    //                                                                                                      Beer-Lambert law L * e^(-K*d)
                    float inscattering = _scatteringCoefficient * HenyeyGreenstein(dot(rayDirection, _MainLightPosition)) * mainLightLuminosity * exp(-_extinctionCoefficient * distFromSurface) * saturate(dot(surfaceNormal, _MainLightPosition.xyz));
                    float extinction = lightSum * exp(-_extinctionCoefficient * stepLength);
                    lightSum = lightSum + inscattering - extinction;
                
                    currentPosition -= step;
                }
                return saturate(lightSum * _intensityMultiplier);
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
                rayLength = min(rayLength, _maxDistance);

                float stepLength = rayLength / _stepCount;
                float3 step = rayDirection * stepLength;

                float3 currentPosition = startPosition + random01(input.uv) * stepLength;
                float lightSum = 0;

                for (int i = 0; i < _stepCount; i++){
                    float3 surfacePositionWS = currentPosition - currentPosition.y * (_MainLightPosition.xyz / _MainLightPosition.y); // _MainLightPosition is a normalized vector pointing towards the light. Scaling it to have y = 1. Result is point at world y = 0.
                    float3 normal = SampleNormal(surfacePositionWS.xz);
                    float distFromSurface = length(surfacePositionWS - currentPosition);
                    float lightAtCurrentPosition = HenyeyGreenstein(dot(normal, _MainLightPosition)) * pow(saturate(1 - distFromSurface/50),2);

                    float distFromCamera = length(_WorldSpaceCameraPos - currentPosition);
                    float light = lightAtCurrentPosition * HenyeyGreenstein(dot(rayDirection, _MainLightPosition.xyz)) * pow(saturate(1-distFromCamera/50), 2);
                    lightSum += saturate(light);
                    currentPosition += step;
                } 
                //to avoid rays being much brighter for closer objects at low step count
                lightSum *= (rayLength / _maxDistance);
                lightSum /= _stepCount;
                return saturate(lightSum * _intensityMultiplier);
            }

            ENDHLSL
        }

        Pass {
            Name "Blur x"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment BlurXFrag

            static const float weight[5] = {0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162};
            //static const float weight[8] = { 0.14446445, 0.13543542, 0.11153505, 0.08055309, 0.05087564, 0.02798160, 0.01332457, 0.00545096};

            half4 BlurXFrag(Varyings input) : SV_TARGET
            {
                float step = 1.0 / _rayTexSize.x;

                float color = SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, input.uv).r  * weight[0];
                for (int i=1; i < 5; i++) {
                    float2 sampleUV = saturate(input.uv + float2(0.0, i * step));
                    color += SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, sampleUV).r * weight[i];
                    sampleUV = saturate(input.uv - float2(0.0, i * step));
                    color += SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, sampleUV).r * weight[i];
                }
                return color;
            }

            ENDHLSL
        }

        Pass {
            Name "Blur y"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment BlurYFrag

            static const float weight[5] = {0.2270270270, 0.1945945946, 0.1216216216, 0.0540540541, 0.0162162162};
            //static const float weight[8] = { 0.14446445, 0.13543542, 0.11153505, 0.08055309, 0.05087564, 0.02798160, 0.01332457, 0.00545096};

            TEXTURE2D(_blurXTarget);
            SAMPLER(sampler_blurXTarget);

            half4 BlurYFrag(Varyings input) : SV_TARGET
            {
                float step = 1.0 / _rayTexSize.y;

                float color = SAMPLE_TEXTURE2D(_blurXTarget, sampler_blurXTarget, input.uv).r * weight[0];
                for (int i=1; i < 5; i++) {
                    float2 sampleUV = saturate(input.uv + float2(i * step, 0.0));
                    color += SAMPLE_TEXTURE2D(_blurXTarget, sampler_blurXTarget, sampleUV).r * weight[i];
                    sampleUV = saturate(input.uv - float2(i * step, 0.0));
                    color += SAMPLE_TEXTURE2D(_blurXTarget, sampler_blurXTarget, sampleUV).r * weight[i];
                }
                return color;
            }

            ENDHLSL
        }


        Pass
        {
            Name "Preview"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

            #pragma vertex ProceduralFullscreenVert
            #pragma fragment UnderwaterPostEffectFrag


            // render only if preview is enabled
            half4 UnderwaterPostEffectFrag(Varyings input) : SV_Target
            {

                float shafts = SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, input.uv).r;
                //float rawDepth = SampleSceneDepth(input.uv);
                //float3 WPFromDepth = ComputeWorldSpacePosition(input.uv, rawDepth, UNITY_MATRIX_I_VP);
                //float viewDist = length(WPFromDepth - _WorldSpaceCameraPos);


                //float3 backgroundColor = SampleSceneColor(input.uv);
                //float3 finalColor =  backgroundColor + shafts;
                return shafts;
            }
            ENDHLSL
        }
    }
}