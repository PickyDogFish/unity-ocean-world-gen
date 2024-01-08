Shader "Custom/Terrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
                // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque"}

        HLSLINCLUDE

        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile_fragment _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION


        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "psrdnoise2D.hlsl"

        float3 ClipMap_ViewerPosition;
        float _displacement;
        float _scale;
        float _verticalOffset;

        float3 getNoise(float2 pos, int octaves){
            //return myValueNoise(pos/_scale);
            return myMorphedFbmNoise(pos/_scale, octaves);
        }

        ENDHLSL

        Pass
        {
            Tags{"LightMode" = "UniversalForward"}
            Name "Terrain pass"

            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM

            #include "ColorSpaceConversion.hlsl"

            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag

            int _normalNoiseOctaves;
            float _percentUnderwater;

            TEXTURE2D_ARRAY(_groundTextures);
            SAMPLER(sampler_groundTextures);

            struct Attributes{
                float3 position : POSITION;
            };

            struct v2f{
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };




            float3 CalcNormal2s(float2 id, float noise){
                // read neighbor heights using small offset
                float2 off = float2(0.01, 0);
                float hR = getNoise(id + off.xy, _normalNoiseOctaves).x;
                float hU = getNoise(id + off.yx, _normalNoiseOctaves).x;
                // deduce terrain normal
                float3 N;
                N.x = noise - hR;
                N.y = 0.00005;
                N.z = noise - hU;
                return normalize(N);
            }

            float3 CalcNormal4s(float2 id){
                // read neighbor heights using small offset
                float2 off = float2(0.0001, 0);
                float hR = getNoise(id + off.xy, _normalNoiseOctaves).x;
                float hL = getNoise(id - off.xy, _normalNoiseOctaves).x;
                float hU = getNoise(id + off.yx, _normalNoiseOctaves).x;
                float hD = getNoise(id - off.yx, _normalNoiseOctaves).x;
                // deduce terrain normal
                float3 N; 
                N.x = hL-hR;
                N.y = 0.000001;
                N.z = hD-hU;
                return normalize(N);
            }

            float3 ClipMapVertex(float3 positionOS){
                float tileSize = 1; 
                float3 snappedViewerPos = float3(floor(ClipMap_ViewerPosition.x / tileSize) * tileSize, 0, floor(ClipMap_ViewerPosition.z / tileSize) * tileSize);
                float3 worldPos = float3(snappedViewerPos.x + positionOS.x, 0, snappedViewerPos.z + positionOS.z);
                return worldPos;
            }

            v2f vert(Attributes IN){
                v2f output;
                output.positionWS = ClipMapVertex(IN.position);//TransformObjectToWorld(IN.position);
                float3 noise = getNoise(output.positionWS.xz, 10);
                output.positionWS.y += noise.x * _displacement * _scale + _verticalOffset * _scale;
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                //float2 normalX = normalize(float2(noise.y, 1));
                //float2 normalZ = normalize(float2(noise.z, 1));
                //output.normalWS = cross(float3(normalX,0), float3(normalZ,0).zyx);//normalize(float3(-1/(noise.y+0.0001), 1, -1/noise.z));
                output.normalWS = CalcNormal4s(output.positionWS.xz);
                return output;
            }

            float4 sumOne(float4 input){
                return input / (input.x + input.y + input.z + input.w);
            }

            float4 splatData(float3 normal, float height){
                float steepness = betterSmooth(normal.y);
                float sandMask = saturate(-(height-_percentUnderwater/2 - 0.002)*1000) * steepness*2;
                float grassMask = saturate((height-_percentUnderwater/2 - 0.002)*1000) * steepness*2;
                float rockMask = 1-steepness;
                float snowMask = saturate((height - 0.45)*100) * steepness; 
                float4 map =  float4(sandMask, grassMask, rockMask, snowMask); 
                return sumOne(map);
            }

            float4 colorFromNormal(float3 normal, float3 posWS){
                float4 splatValues = splatData(normal, posWS.y);
                float4 albedo = 0;
                float maxSplat = max(splatValues.x, splatValues.y);
                maxSplat = max(maxSplat, splatValues.z);
                maxSplat = max(maxSplat, splatValues.w);
                for (uint i = 0; i < 4; i++){
                    if (splatValues[i] == maxSplat){
                        albedo = SAMPLE_TEXTURE2D_ARRAY(_groundTextures, sampler_groundTextures, posWS.xz, i);
                    }
                }
                albedo.w = 0;
                return albedo;
            }

            half4 frag(v2f IN) : SV_TARGET{
                //initializes to all 0
                InputData lightingData = (InputData)0;
                lightingData.normalWS = IN.normalWS;//CalcNormal4s(IN.positionWS.xz);//float3(0,1,0)
                //lightingData.normalWS = float3(0,1,0);
                lightingData.positionWS = IN.positionWS;
                lightingData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightingData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);

                SurfaceData surfaceInput = (SurfaceData)0;
                surfaceInput.albedo = colorFromNormal(lightingData.normalWS, IN.positionWS);//lightingData.normalWS;
                surfaceInput.alpha = 0;
                surfaceInput.specular = 0.5;
                surfaceInput.smoothness = 0.1;
                half4 pbrColor = UniversalFragmentPBR(lightingData, surfaceInput);
                //half4 pbrColor = UniversalFragmentBlinnPhong(lightingData, surfaceInput);
                return pbrColor;//float4(VertexLighting(IN.positionWS, normalize(float3(0.5,IN.positionWS.y,0))), 1);
            }

            ENDHLSL

        }

        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode" = "DepthNormalsOnly"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            float3 ClipMapVertex(float3 positionOS){
                float tileSize = 1; 
                float3 snappedViewerPos = float3(floor(ClipMap_ViewerPosition.x / tileSize) * tileSize, 0, floor(ClipMap_ViewerPosition.z / tileSize) * tileSize);
                float3 worldPos = float3(snappedViewerPos.x + positionOS.x, 0, snappedViewerPos.z + positionOS.z);
                return worldPos;
            }

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = ClipMapVertex(input.positionOS);
                float3 noise = getNoise(positionWS, 10);
                positionWS.y += noise.x * _displacement * _scale + _verticalOffset * _scale;
                float3 normalWS = normalize(float3(abs(noise.y), 1, abs(noise.z)));

                float3 lightDirectionWS = _LightDirection;

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                return positionCS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }

            ENDHLSL
        }

    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
