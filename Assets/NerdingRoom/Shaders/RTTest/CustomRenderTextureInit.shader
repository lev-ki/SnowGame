Shader "CustomRenderTexture/CustomTextureInit"
{
    Properties
    {
        _BaseValueMin ("Base Value Min", Range(0, 1)) = 0.25
        _BaseValueMax ("Base Value Max", Range(0, 1)) = 0.5
        _StartVelocity ("Start Velocity", Range(0, 1)) = 0.1
        _Acceleration ("Acceleration", Range(0, 1)) = 0.01
        _Seed ("Seed", Float) = 123.456
    }

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            HLSLPROGRAM
            #include "UnityCustomRenderTexture.cginc"

            #pragma vertex InitCustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float _BaseValueMin;
            float _BaseValueMax;
            float _StartVelocity;
            float _Acceleration;
            float _Seed;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(hash(i + float2(0.0, 0.0)), 
                                 hash(i + float2(1.0, 0.0)), u.x),
                            lerp(hash(i + float2(0.0, 1.0)), 
                                 hash(i + float2(1.0, 1.0)), u.x), u.y);
            }

            float4 frag(v2f_init_customrendertexture IN) : COLOR
            {
                float2 uv = IN.texcoord.xy;
                
                // We use _Acceleration to control the frequency (roughness)
                // We use _StartVelocity to control the amplitude of variation
                
                float freq = 2.0 + _Acceleration * 20.0;
                float amp = _StartVelocity;
                
                float n = noise(uv * freq + _Seed);
                
                // Layered noise for more "heightmap" feel
                n += 0.5 * noise(uv * freq * 2.0 + _Seed * 1.1);
                n /= 1.5;

                float range = _BaseValueMax - _BaseValueMin;
                float height = _BaseValueMin + n * range;
                
                return float4(height, height, height, 1.0);
            }
            ENDHLSL
        }
    }
}