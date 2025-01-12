Shader "Test/NoiseDerTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #include "psrdnoise2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Ocean/Rendering/OceanGlobals.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float3 testFbm(float2 uv, uint numOctaves){
                float2 p = uv;
            
                float2 derivativeSum = 0;
                float valueSum = 0;
                float amplitude = 0.5;
                float frequency = 2; 
                float3 noise = 0;
                for (uint i = 0; i < numOctaves; i++){
                    noise = myValueNoise(p);
                    valueSum = noise.x * amplitude;// / (1.0 + dot(derivativeSum,derivativeSum));
                    derivativeSum = noise.yz;// / (1.0 + dot(derivativeSum,derivativeSum));
                    amplitude *= 0.5;
                    //p = mul(myMat, p) * frequency;
                    p *= frequency;
                }
                return float3(valueSum, derivativeSum);
            }


            float3 getNoiseWrapper(float2 uv){
                //return myValueNoise(uv);
                //return myMorphedFbmNoise(uv, 4);
                return fbmValueNoise(uv, 4, 0);
                //return testFbm(uv, 2);
            }

            float3 CalcNormal(float2 uv){
                // # P.xy store the position for which we want to calculate the normals
                // # height() here is a function that return the height at a point in the terrain
            
                // read neightbor heights using small offset
                float2 off = float2(0.1, 0);
                float hR = getNoiseWrapper(uv + off.xy).x;

            
                float hU = getNoiseWrapper(uv + off.yx).x;

                float hID = getNoiseWrapper(uv);
            
                // deduce terrain normal
                float3 N;
                N.x = hID - hR;
                N.y = 0.01;
                N.z = hID - hU;
                return normalize(N);
            }

            v2f vert (appdata v)
            {
                v2f o;
                float3 wp = TransformObjectToWorld(v.vertex);
                float3 noise = getNoiseWrapper(wp.xz/25);
                wp.y += noise.x * 25;
                o.vertex = TransformWorldToHClip(wp);
                o.uv = wp.xz/25;
                o.normal = normalize(float3(noise.y, 0.1, noise.z));
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 noise = getNoiseWrapper(i.uv);
                float3 normal = i.normal;//normalize(float3(noise.y, 0.1, noise.z));
                normal = normal - CalcNormal(i.uv);
                //normal = cross(normalize(float3(noise.y, 1, 0)), normalize(float3(0, 1, noise.z)));
                //normal = normalize(normal);
                //if (normal.y > 0.99){
                //    return float4(0,1,0,0);
                //}
                //if (normal.y  < abs(normal.z)){
                //    return float4(0,0,1,0);
                //}

                //return float4(abs(CalcNormal(i.uv).yyy), 1);
                return float4(abs(i.normal.yyy), 1);
            }
            ENDHLSL
        }
    }
}
