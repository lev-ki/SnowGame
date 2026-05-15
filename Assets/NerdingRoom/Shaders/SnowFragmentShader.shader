Shader "Custom/SnowFragmentShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _OldSnowColor("Old Snow Color", Color) = (1, 1, 1, 1)
        _FreshSnowColor("Fresh Snow Color", Color) = (1, 1, 1, 1)
        _MaskedColor("Masked Color", Color) = (1, 1, 1, 0.1)
        _MaskedMap("Masked Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            TEXTURE2D(_MaskedMap);
            SAMPLER(sampler_BaseMap);
            SAMPLER(sampler_MaskedMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _MaskedColor;
                half4 _OldSnowColor;
                half4 _FreshSnowColor;
                float4 _BaseMap_ST;
                float4 _MaskedMap_ST;
                float _Segments[512];
                float _TempSegments[512];
                float _InitializationProgress;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float CatmullRom(float p0, float p1, float p2, float p3, float t)
            {
                float t2 = t * t;
                float t3 = t2 * t;

                return 0.5 * (
                    (2.0 * p1) +
                    (-p0 + p2) * t +
                    (2.0*p0 - 5.0*p1 + 4.0*p2 - p3) * t2 +
                    (-p0 + 3.0*p1 - 3.0*p2 + p3) * t3
                );
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                const int num_of_segnemts = 512;
                
                float global_t = IN.uv.x * (num_of_segnemts - 1);
                
                int p0 = floor(global_t);
                int p1 = min(p0 + 1, num_of_segnemts);
                
                // int p1 = (int)floor(global_t);
                // int p0 = max(p1 - 1, 0);
                // int p2 = min(p1 + 1, num_of_segnemts - 1);
                // int p3 = min(p1 + 2, num_of_segnemts - 1);

                float t = frac(global_t); // local 0..1

                float cutoff_y = lerp(
                    lerp(_TempSegments[p0].x, _Segments[p0].x, _InitializationProgress),
                    lerp(_TempSegments[p1].x, _Segments[p1].x, _InitializationProgress),
                    t);
                // float cutoff_y = CatmullRom(
                //     _Segments[p0].x,
                //     _Segments[p1].x,
                //     _Segments[p2].x,
                //     _Segments[p3].x,
                //     t
                // );
                
                if (IN.uv.y < cutoff_y)
                {
                    half age = SAMPLE_TEXTURE2D(_MaskedMap, sampler_MaskedMap, IN.uv).r;

                    // Fresh → Old over time
                    half4 snowColor = lerp(_FreshSnowColor, _OldSnowColor, age);

                    // Optional: keep your existing mask alpha
                    // snowColor.a *= _MaskedColor.a;

                    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * snowColor * _BaseColor;
                    return color;
                }
                
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _MaskedColor;
                return color;
            }
            ENDHLSL
        }
    }
}
