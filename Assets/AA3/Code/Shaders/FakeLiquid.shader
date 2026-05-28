Shader "Custom/FakeLiquid"
{
    Properties
    {
        _Color        ("Liquid Color", Color) = (0.1, 0.5, 0.9, 0.85)
        _TopColor     ("Surface Color", Color) = (0.4, 0.75, 1.0, 1.0)
        _FillAmount   ("Fill Amount", Range(0.0, 1.0)) = 0.5
        _LocalBoundMin("Bound Min Y", Float) = -0.5
        _LocalBoundMax("Bound Max Y", Float) =  0.5
        _WobbleX      ("Wobble X", Float) = 0.0
        _WobbleZ      ("Wobble Z", Float) = 0.0
        _SurfaceWidth ("Surface Width",   Range(0.001, 0.08)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ── Single pass, both faces visible ──────────────────────────────────
        Pass
        {
            Name "LiquidForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Uniforms ─────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _TopColor;
                float _FillAmount;
                float _LocalBoundMin;
                float _LocalBoundMax;
                float _WobbleX;
                float _WobbleZ;
                float _SurfaceWidth;
            CBUFFER_END

            // ── Vertex I/O ───────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;   // needed for virtual-height clip
                float3 normalWS    : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Vertex shader ────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posData    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalData = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posData.positionCS;
                OUT.positionWS = posData.positionWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS   = normalData.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(posData.positionWS);
                return OUT;
            }

            // ── Fragment shader ──────────────────────────────────────────────
            half4 frag(Varyings IN, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // --- Virtual-height clipping (orientation-aware) --------------
                //
                // Transform world-up into object space.  The result points "up"
                // in local space regardless of how the bottle is rotated.
                // Normalising makes virtualH comparable to localBound values.
                float3 worldUpOS = normalize(mul((float3x3)unity_WorldToObject,
                                                   float3(0.0, 1.0, 0.0)));

                // "How high is this vertex along the world-up direction?" (local units)
                float virtualH = dot(IN.positionOS, worldUpOS);

                // Fill threshold in the same local-unit space
                float fillH = lerp(_LocalBoundMin, _LocalBoundMax, _FillAmount);

                // Wobble: world-space sine wave → convert to local-unit offset.
                // invScale ≈ 1/uniformScale, maps world-Y amplitude to local-Y amplitude.
                float invScale   = length(mul((float3x3)unity_WorldToObject,
                                               float3(0.0, 1.0, 0.0)));
                float t = _Time.y;
                float wobbleWS = sin(IN.positionWS.x * 8.0 + t * 4.0) * _WobbleX * 0.04
                                  + sin(IN.positionWS.z * 8.0 + t * 3.5) * _WobbleZ * 0.04;
                float wobbleLS = wobbleWS * invScale;

                float surface = fillH + wobbleLS;

                // Discard anything above the liquid surface
                clip(surface - virtualH);

                // --- Surface highlight ----------------------------------------
                float distToSurface = abs(surface - virtualH);
                float surfaceMask = 1.0 - smoothstep(0.0, _SurfaceWidth, distToSurface);

                // --- Simple lighting ------------------------------------------
                float3 N = normalize(frontFace ? IN.normalWS : -IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(N, mainLight.direction));
                float3 lighting = mainLight.color * (0.35 + 0.65 * NdotL);

                // Subtle Fresnel rim
                float fresnel = pow(1.0 - saturate(dot(N, V)), 2.5) * 0.25;

                // --- Final colour ---------------------------------------------
                half4 col = lerp(_Color, _TopColor, surfaceMask);
                col.rgb = col.rgb * lighting + fresnel;

                // Interior (back) faces are slightly darker
                if (!frontFace) col.rgb *= 0.70;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}