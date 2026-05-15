Shader "Custom/SnowAge"
{
    Properties
    {
        _MainSnow("Main Snow", 2D) = "white" {}
        _SecondarySnow("Meta Snow", 2D) = "white" {}
        _DeltaTime("DeltaTime", Float) = 0.016
        _AgeSpeed("AgeSpeed", Float) = 0.5
        _SmoothingStep("SmoothingStep", Float) = 0.001
        _SmoothingThreshold("SmoothingThreshold", Float) = 0.001
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite Off
            Cull Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainSnow;
            sampler2D _SecondarySnow;
            float _DeltaTime;
            float _AgeSpeed;
            float _SmoothingStep;
            float _SmoothingThreshold;
            float4 _MainSnow_TexelSize; // x = 1/width, y = 1/height

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Read current height
                float h = tex2D(_MainSnow, i.uv).r;

                // Neighbor heights (1D horizontal smoothing)
                float left  = tex2D(_MainSnow, i.uv + float2(-_MainSnow_TexelSize.x, 0)).r;
                float right = tex2D(_MainSnow, i.uv + float2( _MainSnow_TexelSize.x, 0)).r;

                float deltaL = left - h;
                float deltaR = right - h;

                if (abs(deltaL) > abs(deltaR) && abs(deltaL) > _SmoothingThreshold)
                {
                    h += sign(deltaL) * _SmoothingStep;
                }
                else if (abs(deltaR) > _SmoothingThreshold)
                {
                    h += sign(deltaR) * _SmoothingStep;
                }

                // Age snow
                float age = tex2D(_SecondarySnow, i.uv).r;
                age = min(age + _DeltaTime * _AgeSpeed, 1.0);

                // Output combined (can output height in R, age in G if desired)
                return float4(h, age, 0, 0);
            }
            ENDCG
        }
    }
}
