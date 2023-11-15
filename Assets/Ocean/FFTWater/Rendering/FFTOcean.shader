// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/FFTOcean"
{

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags {"RenderPipeline" = "UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}

        Pass
        {
            //can be used to tell unity not to render the pass on a frame/draw it during a given call to ScriptableRenderContext.DrawRendereres
            Tags { "LightMode" = "OceanMain"}
            Name "Ocean pass"
            Cull Off //turn off backface culling
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


            float _Displacement;

            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);
            float4 _HeightMap_ST;

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

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
                float3 normal = _NormalMap.SampleLevel(sampler_NormalMap, input.uv, 0.0f);
                output.normalWS = normal;
                
                float3 height = _HeightMap.SampleLevel(sampler_HeightMap, input.uv, 0.0f);
                input.position += height.yxz;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.position);

                output.positionHCS = TransformObjectToHClip(input.position.xyz);;
                output.uv = TRANSFORM_TEX(input.uv, _HeightMap);
                output.positionWS = posInputs.positionWS;

                return output; 
            }

            float3 ColorThroughWater(float4 fogColor, float3 backgroundColor, float depth){
                float fogFactor = exp2(-fogColor.a * depth);
                return lerp(fogColor.rgb, backgroundColor, fogFactor);
            }

            float4 frag(v2f IN, float facing : VFACE) : SV_TARGET{
                float3 diffuseColor = float3(0.1,0.2,0.8);
                float4 specularColor = float4(diffuseColor, 1);
                float3 ambientColor = diffuseColor;

                float4 fogColor = float4(diffuseColor, 0.05);


                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    // Adjust z to match NDC for OpenGL
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                float3 WPFromDepth = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float depthDif = length(WPFromDepth - IN.positionWS);

                float3 backgroundColor = SampleSceneColor(screenUV);
                float3 colorThroughWater = ColorThroughWater(fogColor, backgroundColor, depthDif);

                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                //saturate because dot() is negative half the time
                float3 lambert = colorThroughWater * saturate(dot(_MainLightPosition, IN.normalWS));
                float3 specular = LightingSpecular(_MainLightColor.rgb, _MainLightPosition, IN.normalWS, viewDir, specularColor, 25);
                //float3 finalColor = ambientColor + lambert + specular;
                float3 finalColor = colorThroughWater + specular;
                
                //If looking at the back face
                if (facing < 0 ){
                    finalColor = diffuseColor;
                }
                return saturate(float4(finalColor,1));
            
            }
            ENDHLSL
        }
    }
}