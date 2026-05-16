Shader "Custom/Shader_SnowSlice_v1"
{   
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Heightmap("Heightmap", 2D) = "white" {}
        _Height("Height", Float) = 0.0
        _AlphaClipThreshold ("Alpha Clip Threshold", Range(0,1)) = 0.5
//        _AgeSpeed("AgeSpeed", Float) = 0.5
//        _SmoothingStep("SmoothingStep", Float) = 0.001
//        _SmoothingThreshold("SmoothingThreshold", Float) = 0.001
    }
    
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="AlphaTest"
        }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
//            Name "Shader_SnowSlice_v1"
            
            Name "ForwardLit"

            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_Heightmap);
            SAMPLER(sampler_Heightmap);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;
                float4 _Heightmap_ST;

                float _Height;
                float _AlphaClipThreshold;

            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Sample textures
                half4 albedo =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv);

                float height =
                    SAMPLE_TEXTURE2D(
                        _Heightmap,
                        sampler_Heightmap,
                        uv).r;

                // Compare against slice height
                float visible = step(_Height, height);

                // Alpha clip
                clip(visible - _AlphaClipThreshold);

                return albedo;
            }            
            ENDHLSL
        }
    }
}
