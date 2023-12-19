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

            #include "UnityCG.cginc"

            float unity_noise_randomValue (float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
            }

            float2 betterSmooth(float2 uv){
                return uv*uv*uv*(10.0 + uv * (-15.0 + 6*uv));
            }

            float2 betterSmoothDerivative(float2 uv){
                return 30 *uv*uv*(1 + uv*(-2 + uv));
            }

            float3 myValueNoise(float2 uv){
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = betterSmooth(f);
                float2 df = betterSmoothDerivative(f);
                //get the locations of the nearby "pixels"
                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);
                //get the random noise at "pixel" locations
                float a = unity_noise_randomValue(c0);
                float b = unity_noise_randomValue(c1);
                float c = unity_noise_randomValue(c2);
                float d = unity_noise_randomValue(c3);

                float k1 = b-a;
                float k2 = c-a;
                float k3 = a-b-c+d;

                float3 noiseAndDerivatives = 0.0;
                noiseAndDerivatives.x = (a + f.x * k1 + f.y * k2 + f.x * f.y * k3);
                noiseAndDerivatives.y = ((k1 + f.y * k3) * df.x);// /1.875; //so its between 0 and 1;
                noiseAndDerivatives.z = ((k2 + f.x * k3) * df.y);///1.875;
                return noiseAndDerivatives;
            }

            float3 myFbmValueNoise(float2 uv, uint numOctaves){
                float valueSum = 0;
                float2 derivativeSum = 0;
                float amplitude = 0.6;
                float frequency = 1;
                float3 noise = 0;
                for (uint i = 0; i < numOctaves; i++){
                    noise = myValueNoise(uv * frequency);
                    valueSum += (noise.x-0.5) * amplitude  / (1.0 + 2 * dot(derivativeSum,derivativeSum));
                    derivativeSum += noise.yz * amplitude / (1.0 + 2* dot(derivativeSum,derivativeSum));
                    frequency *= 2.01;
                    amplitude *= 0.49;
                }
                return float3(valueSum+0.5, derivativeSum);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                v.uv *= 4;
                float3 noise = myFbmValueNoise(v.uv,4);
                v.vertex.y += noise.x*25;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 noise =  myFbmValueNoise(i.uv,4);
                float derLen = length(noise.yz);

                return fixed4(0, derLen.xx ,1);
            }
            ENDHLSL
        }
    }
}
