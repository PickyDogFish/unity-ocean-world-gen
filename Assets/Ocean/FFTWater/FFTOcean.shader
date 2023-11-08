// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/FFTWater"
{

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1"}

        Pass
        {
            Name "Ocean pass"
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            // Pull in URP library functions and our own common functions
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


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
                float3 positionOS : POSITION1;
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };


            v2f vert(Attributes input){
                v2f output;
                float3 normal = _NormalMap.SampleLevel(sampler_NormalMap, input.uv, 0.0f);
                output.normalWS = normal;
                
                float3 height = _HeightMap.SampleLevel(sampler_HeightMap, input.uv, 0.0f);
                input.position += height.yxz;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.position);

                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _HeightMap);
                output.positionWS = posInputs.positionWS;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output; 
            }

            float4 frag(v2f input) : SV_TARGET{
                float3 diffuseColor = float3(0.1,0.2,0.8);
                float4 specularColor = float4(diffuseColor, 1);
                float3 ambientColor = diffuseColor;

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                //saturate because dot() is negative half the time
                float3 lambert = diffuseColor * saturate(dot(_MainLightPosition, input.normalWS));
                float3 specular = LightingSpecular(_MainLightColor.rgb, _MainLightPosition, input.normalWS, viewDir, specularColor, 25);
                float3 finalColor = ambientColor + lambert + specular;

                float depth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.screenPos);

                return saturate(float4(finalColor,0));
            }
            ENDHLSL
        }
    }
}