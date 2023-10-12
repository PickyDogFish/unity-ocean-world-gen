// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/Water"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    {
        [HeightMap] _HeightMap("Height Map", 2D) = "white"
        _HeightMult("Height Multiplier", float) = 1
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

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

            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            float4 _HeightMap_ST;

            struct Attributes{
                float3 position : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct v2f{
                float3 positionOS : POSITION1;
                float4 positionCS : SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                //DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
            };


            v2f vert(Attributes input){
                v2f output;
                
                float height = _HeightMap.SampleLevel(sampler_HeightMap, input.uv, 0.0f);
                input.position += float3(5.0, height, 0.0);
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.position);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _HeightMap);
                output.positionWS = posInputs.positionWS;
                output.positionOS = input.position;
                return output; 
            }

            float4 frag(v2f input) : SV_TARGET{

                
                return float4(1,1,1,1);
            }
            ENDHLSL
        }
    }
}