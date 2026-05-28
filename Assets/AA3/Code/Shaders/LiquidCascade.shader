// Stream de líquido para LineRenderer.
//
// UV del LineRenderer (Unity):
//   uv.x = 0..1  A LO LARGO de la línea (inicio → fin)
//   uv.y = 0..1  A LO ANCHO  del ribbon (borde → borde)
//
// El alpha base viene de _Color.a (no depende de UV),
// así el chorro nunca desaparece por error de mapeo.
// El efecto de borde suave viene del ancho del LineRenderer (startWidth/endWidth).

Shader "Custom/LiquidCascade"
{
    Properties
    {
        [HDR] _Color      ("Stream Color",  Color)             = (0.15, 0.55, 0.95, 0.88)
        _ScrollSpeed      ("Flow Speed",    Float)             = 3.0
        _Brightness       ("Brightness",    Range(0.5, 2.0))   = 1.15
        _EdgeSoftness     ("Edge Softness", Range(0.0, 0.49))  = 0.20
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+2"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CascadeForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _ScrollSpeed;
                float _Brightness;
                float _EdgeSoftness;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv    = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ── Scroll a lo largo de la línea (uv.x) ─────────────────────
                float alongLine = IN.uv.x - _Time.y * _ScrollSpeed;
                float flow = 0.82 + 0.18 * sin(alongLine * 14.0);

                // ── Bordes suaves a lo ancho (uv.y) ──────────────────────────
                // Si _EdgeSoftness = 0, edge = 1 siempre (sin fade lateral).
                float edge = (_EdgeSoftness > 0.001)
                    ? smoothstep(0.0, _EdgeSoftness, IN.uv.y)
                      * smoothstep(0.0, _EdgeSoftness, 1.0 - IN.uv.y)
                    : 1.0;

                half3 col = _Color.rgb * (flow * _Brightness);
                return half4(col, _Color.a * edge);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
