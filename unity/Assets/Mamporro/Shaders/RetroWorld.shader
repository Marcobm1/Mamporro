// Variante del shader retro de U1 para el mundo de U3: mismo ajuste de vértices,
// dithering y niebla, más color de vértice, textura de detalle con filtro puntual,
// normales planas (como flatShading de la web), color por instancia y viento.
Shader "Mamporro/RetroWorld"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _MainTex("Detalle", 2D) = "white" {}
        _TexMeters("Metros por repetición", Float) = 4
        _Wind("Viento (amplitud, inicio, velocidad)", Vector) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _WorldUV("UV en metros del mundo (0/1)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MainTex_ST;
            float _TexMeters;
            float4 _Wind;
            float _WorldUV;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_point_repeat);
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)
            float _RetroSnap, _RetroDither, _RetroTime, _RetroFogNear, _RetroFogFar;
            float4 _RetroSize, _RetroFogColor, _RetroSunDir, _RetroSky, _RetroGround, _RetroSun;
            // TEXCOORD1: origen de la instancia (x, z), altura local sin escalar y escala (mallas combinadas).
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 wind:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                // Viento: se mueve más cuanto más alto está el vértice (como uWind de la web).
                if (_Wind.x > 0)
                {
                    float h = max(0.0, input.wind.z - _Wind.y) * input.wind.w;
                    float phase = _RetroTime * _Wind.z + dot(input.wind.xy, float2(0.13, 0.17));
                    world.x += sin(phase) * h * _Wind.x;
                    world.z -= cos(phase * 0.83) * h * _Wind.x * 0.6;
                }
                o.world = world;
                o.positionCS = TransformWorldToHClip(world);
                float2 ndc = o.positionCS.xy / o.positionCS.w;
                float2 snapped = floor(ndc * _RetroSize.xy * .5 + .5) / (_RetroSize.xy * .5);
                o.positionCS.xy = lerp(ndc, snapped, _RetroSnap) * o.positionCS.w;
                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                o.color = input.color * _BaseColor * (tint.a > 0 ? tint : float4(1,1,1,1));
                o.uv = _WorldUV > 0.5 ? world.xz / _TexMeters : input.uv;
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // Normal plana del triángulo (flatShading).
                float3 n = normalize(cross(ddy(input.world), ddx(input.world)));
                float3 sun = normalize(_RetroSunDir.xyz);
                float hemi = n.y * 0.5 + 0.5;
                float3 light = lerp(_RetroGround.rgb, _RetroSky.rgb, hemi) + _RetroSun.rgb * saturate(dot(n, sun));
                float3 detail = SAMPLE_TEXTURE2D(_MainTex, sampler_point_repeat, input.uv).rgb;
                float3 color = input.color.rgb * detail * light;
                float fog = saturate((distance(input.world, GetCameraPositionWS()) - _RetroFogNear) / max(1, _RetroFogFar - _RetroFogNear));
                color = lerp(color, _RetroFogColor.rgb, fog);
                uint2 p = (uint2)input.positionCS.xy & 3;
                static const float bayer[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                float d = (bayer[p.x+p.y*4]/16-.5)/31;
                color = lerp(color, floor(saturate(color+d)*31+.5)/31, _RetroDither);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
