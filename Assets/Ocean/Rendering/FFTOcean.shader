// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/FFTOcean"
{

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags {"RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            //can be used to tell unity not to render the pass on a frame/draw it during a given call to ScriptableRenderContext.DrawRendereres
            Tags { "LightMode" = "OceanMain"}
            Name "Ocean pass"
            Cull Off
            ZWrite On
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            // Pull in URP library functions and our own common functions
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "OceanGlobals.hlsl"
            #include "DisplacementSampler.hlsl"
            #include "OceanVolume.hlsl"
            #include "ClipMap.hlsl"


            float _Displacement;

            struct Attributes{
                float3 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };


            v2f vert(Attributes input){
                v2f output;

                output.positionWS = ClipMapVertex(input.position);//TransformObjectToWorld(input.position);
                float3 normal = _OceanNormalTex.SampleLevel(sampler_OceanNormalTex, output.positionWS.xz / Ocean_WaveScale, 0.0f);
                output.normalWS = normal;
                
                float3 displacement = _OceanDisplacementTex.SampleLevel(sampler_OceanDisplacementTex, output.positionWS.xz/Ocean_WaveScale, 0.0f);
                
                output.positionWS += displacement.yxz;
                
                float3 positionOS = TransformWorldToObject(output.positionWS);
                output.positionHCS = TransformObjectToHClip(positionOS);

                return output; 
            }



            float SchlickFresnel(float3 normal, float3 viewDir){
                float R0 = 0.308641975308642;
                float exponential = pow(1- saturate(dot(normal, viewDir)), 5);
                return (R0 + (1.0 - R0) * exponential);
            }

            float3 worldPositionFromDepth(float2 screenUV){
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    // Adjust z to match NDC for OpenGL
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                return ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
            }

            float4 frag(v2f IN, float facing : VFACE) : SV_TARGET{
                float3 diffuseColor = float3(0.1,0.2,0.8);
                float4 specularColor = float4(0.85,0.85,0.95, 1);
                float3 ambientColor = diffuseColor;

                float3 viewDirection = normalize(_WorldSpaceCameraPos - IN.positionWS);


                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                float3 refracted = refract(viewDirection, IN.normalWS, 1.0/1.33);
                screenUV += refracted.xz * Ocean_RefractionIntensity;

                float3 WPFromDepth = worldPositionFromDepth(screenUV);

                float depthDif = length(WPFromDepth - IN.positionWS);

                IN.normalWS = IN.normalWS;

                float3 finalColor = 0;

                float3 reflectionDir = reflect(viewDirection, IN.normalWS);
                float3 reflectionColor = SampleOceanCubeMap(reflectionDir);
                float fernel = SchlickFresnel(IN.normalWS, viewDirection);
            
                float3 backgroundColor = SampleSceneColor(screenUV);
                float3 colorThroughWater = underwaterFogColor(Ocean_FogColor, Ocean_FogIntensity, depthDif, backgroundColor, 0);
                finalColor = lerp(colorThroughWater, reflectionColor, fernel);


                //If looking at the back face
                if (facing < 0 ){
                    float sunshafts = SAMPLE_TEXTURE2D(Ocean_SunShaftsTexture, samplerOcean_SunShaftsTexture, screenUV).r;
                    finalColor = underwaterFogColor(Ocean_FogColor, Ocean_FogIntensity, length(IN.positionWS - _WorldSpaceCameraPos), finalColor, sunshafts);
                }
                //finalColor = reflectionColor;
                return saturate(float4(finalColor,1));
            
            }
            ENDHLSL
        }
    }
}