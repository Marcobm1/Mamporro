// Cielo de la web (src/render/Sky.ts): degradado horizonte → cénit y disco del sol.
Shader "Mamporro/RetroSky"
{
    Properties
    {
        _Zenith("Cénit", Color) = (0.1,0.3,0.7,1)
        _Horizon("Horizonte", Color) = (0.6,0.7,0.8,1)
        _SunColor("Sol", Color) = (1,0.9,0.7,1)
        _SunDir("Dirección del sol", Vector) = (0.45,0.6,0.66,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Front ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Zenith, _Horizon, _SunColor, _SunDir;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 dir:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.dir = input.positionOS.xyz;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Siempre al plano lejano, como p.xyww en la web.
                #if UNITY_REVERSED_Z
                    o.positionCS.z = 0;
                #else
                    o.positionCS.z = o.positionCS.w;
                #endif
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 dir = normalize(input.dir);
                float h = saturate(dir.y);
                float3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(h, 0.55));
                float s = dot(dir, normalize(_SunDir.xyz));
                col += _SunColor.rgb * pow(max(s, 0.0), 48.0) * 0.35;
                col = lerp(col, _SunColor.rgb, step(0.9975, s));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
