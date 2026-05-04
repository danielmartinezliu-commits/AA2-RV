// Billboard quad que dibuja un círculo suave coloreado como líquido.
// No necesita textura — la forma se calcula por UV en el fragment shader.
Shader "Custom/LiquidDroplet"
{
    Properties
    {
        [HDR] _Color    ("Drop Color",     Color)          = (0.1, 0.5, 0.9, 0.9)
        _Softness       ("Edge Softness",  Range(0.0, 0.5)) = 0.12
        _SquishY        ("Squish Y",       Range(0.5, 1.0)) = 0.85  // aplana la gota levemente
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
            Name "DropletForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Softness;
                float _SquishY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // UV centrado en (0,0), rango [-1, 1]
                float2 p = (IN.uv - 0.5) * 2.0;
                p.y /= _SquishY;    // aplana verticalmente → forma de gota

                float dist  = length(p);
                float alpha = smoothstep(1.0, 1.0 - _Softness * 2.0, dist);

                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}
