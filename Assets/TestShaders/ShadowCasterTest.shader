Shader "Custom/ShadowCasterTest" {
    Properties {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        //_SinTime("Sin Time", Vector) = (0, 0, 0, 0)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST; // Tiling & Offset, x = TilingX, y = TilingY, z = OffsetX, w = OffsetY
        float4 _BaseColor;
        float _Cutoff;
        CBUFFER_END

        ENDHLSL

        Pass {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                float4 shadowCoord  : TEXCOORD3;
                float4 clipPos      : TEXCOORD5;
            };

            Varyings vert(Attributes input) {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                #ifdef _MAIN_LIGHT_SHADOWS
                output.shadowCoord = TransformWorldToShadowCoord(output.positionCS.xyz);
                #endif

                output.viewDirWS = GetWorldSpaceViewDir(output.positionCS.xyz);
                output.clipPos = output.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #ifdef _ALPHATEST_ON
                clip(baseColor.a - _Cutoff);
                #endif

                half3 normalWS = input.normalWS;
                #ifdef _NORMALMAP
                normalWS = TangentSpaceNormalToWorld(normalWS, UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv)));
                #endif

                half3 viewDirWS = input.viewDirWS;
                half3 lightDirWS;
                half atten;
                half3 lightColor;

                return half4(1,1,1, 0);
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            ENDHLSL
        }

        Pass {
            Name "DepthOnly"
            
            //Would be depthOnly pass if normals werent enabled somewhere, but i honestly dont know where they get enabled.
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
