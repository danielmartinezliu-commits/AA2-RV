Shader "Custom/StylizedGlass"
{
    Properties
    {
        // ── Base glass ────────────────────────────────────────────────────────
        [HDR] _Color        ("Glass Tint",          Color)           = (0.85, 0.95, 1.0, 0.12)

        // ── Fresnel rim ───────────────────────────────────────────────────────
        [HDR] _RimColor     ("Rim Color",           Color)           = (0.9, 0.97, 1.0, 0.85)
        _RimPower           ("Rim Power",            Range(1.0, 8.0)) = 3.5
        _RimScale           ("Rim Intensity",        Range(0.0, 2.0)) = 1.0

        // ── Specular highlight ────────────────────────────────────────────────
        [HDR] _SpecColor    ("Specular Color",       Color)           = (1, 1, 1, 1)
        _SpecPower          ("Specular Sharpness",   Range(10, 1024)) = 256
        _SpecScale          ("Specular Intensity",   Range(0.0, 2.0)) = 0.9

        // ── Interior (back faces = thickness illusion) ────────────────────────
        _InteriorColor      ("Interior Tint",        Color)           = (0.55, 0.75, 0.9, 0.35)

        // ── Subtle environment tint ───────────────────────────────────────────
        _EnvReflectScale    ("Env Reflect",          Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"   // renders after the liquid (Transparent+0)
            "RenderPipeline" = "UniversalPipeline"
        }

        // ─────────────────────────────────────────────────────────────────────
        // Single pass — Cull Off lets us shade interior and exterior separately
        // via SV_IsFrontFace without an extra draw call.
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "StylizedGlassForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull    Off
            ZWrite  Off
            Blend   SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Uniforms ──────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RimColor;
                float _RimPower;
                float _RimScale;
                half4 _SpecColor;
                float _SpecPower;
                float _SpecScale;
                half4 _InteriorColor;
                float _EnvReflectScale;
            CBUFFER_END

            // ── Vertex I/O ────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Vertex shader ─────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posData    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalData = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posData.positionCS;
                OUT.positionWS = posData.positionWS;
                OUT.normalWS   = normalData.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(posData.positionWS);
                return OUT;
            }

            // ── Fragment shader ───────────────────────────────────────────────
            half4 frag(Varyings IN, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 V = normalize(IN.viewDirWS);
                float3 N = normalize(frontFace ? IN.normalWS : -IN.normalWS);

                // ── Interior (back faces) ─────────────────────────────────────
                // Shows a darkened tinted layer → simulates glass thickness.
                if (!frontFace)
                    return _InteriorColor;

                // ── Exterior (front faces) ────────────────────────────────────

                // Fresnel: glass is transparent straight-on, opaque/reflective at rim
                float NdotV    = saturate(dot(N, V));
                float fresnel  = saturate(pow(1.0 - NdotV, _RimPower) * _RimScale);

                // Blinn-Phong specular — sharp spot for a polished glass surface
                Light  light   = GetMainLight();
                float3 H       = normalize(V + light.direction);
                float  NdotH   = saturate(dot(N, H));
                float  spec    = pow(NdotH, _SpecPower) * _SpecScale;

                // Environment reflection via built-in SH / reflection probe
                float3 reflDir  = reflect(-V, N);
                half3  envColor = GlossyEnvironmentReflection(
                                      reflDir,
                                      IN.positionWS,
                                      0.0,   // perceptual roughness — 0 = mirror
                                      1.0    // occlusion
                                  );

                // ── Colour assembly ───────────────────────────────────────────
                // Base tint → rim colour at edges
                half3 col  = lerp(_Color.rgb, _RimColor.rgb, fresnel);

                // Add environment reflection (subtle on flat surfaces, stronger at rim)
                col       += envColor * _EnvReflectScale * fresnel;

                // Specular highlight on top
                col       += _SpecColor.rgb * spec * light.color;

                // Alpha: nearly transparent in center, opaque at rim
                half alpha = lerp(_Color.a, _RimColor.a, fresnel);

                return half4(col, alpha);
            }
            ENDHLSL
        }

        // Depth-only shadow pass (writes just the silhouette of the bottle)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest  LEqual
            Cull   Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttribs  { float4 posOS : POSITION; float3 normOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct ShadowVaryings { float4 posCS : SV_POSITION; };

            ShadowVaryings shadowVert(ShadowAttribs IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                ShadowVaryings OUT;
                float3 posWS  = TransformObjectToWorld(IN.posOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                OUT.posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, lightDir));
                return OUT;
            }

            half4 shadowFrag(ShadowVaryings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}
