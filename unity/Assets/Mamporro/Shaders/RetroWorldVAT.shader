// B0.5 (spike, solo QA): RetroWorld con animación de vértices horneada en textura (VAT).
// Cada texel (vértice, fotograma) guarda la posición local del vértice en esa pose; el
// vértice interpola entre dos fotogramas según el reloj global _B0AnimTime y la fase de la
// instancia (_Phase, 0–1). Mismo ajuste de vértices, dithering, niebla y normales planas.
Shader "Mamporro/RetroWorldVAT"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _VatTex("Posiciones por fotograma", 2D) = "black" {}
        _VatFrames("Fotogramas", Float) = 20
        _VatFps("Fotogramas por segundo", Float) = 30
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _VatTex_TexelSize;
            float _VatFrames, _VatFps;
            CBUFFER_END
            TEXTURE2D(_VatTex);
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Phase)
            UNITY_INSTANCING_BUFFER_END(Props)
            float _RetroSnap, _RetroDither, _RetroFogNear, _RetroFogFar, _B0AnimTime;
            float4 _RetroSize, _RetroFogColor, _RetroSunDir, _RetroSky, _RetroGround, _RetroSun;
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; uint id:SV_VertexID; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float3 world:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                float phase = UNITY_ACCESS_INSTANCED_PROP(Props, _Phase);
                float f = frac(_B0AnimTime * _VatFps / _VatFrames + phase) * _VatFrames;
                uint f0 = (uint)floor(f) % (uint)_VatFrames, f1 = (f0 + 1) % (uint)_VatFrames;
                float3 p0 = LOAD_TEXTURE2D_LOD(_VatTex, uint2(input.id, f0), 0).xyz;
                float3 p1 = LOAD_TEXTURE2D_LOD(_VatTex, uint2(input.id, f1), 0).xyz;
                float3 world = TransformObjectToWorld(lerp(p0, p1, frac(f)));
                o.world = world;
                o.positionCS = TransformWorldToHClip(world);
                float2 ndc = o.positionCS.xy / o.positionCS.w;
                float2 snapped = floor(ndc * _RetroSize.xy * .5 + .5) / (_RetroSize.xy * .5);
                o.positionCS.xy = lerp(ndc, snapped, _RetroSnap) * o.positionCS.w;
                o.color = input.color * _BaseColor;
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 n = normalize(cross(ddy(input.world), ddx(input.world)));
                float3 sun = normalize(_RetroSunDir.xyz);
                float hemi = n.y * 0.5 + 0.5;
                float3 light = lerp(_RetroGround.rgb, _RetroSky.rgb, hemi) + _RetroSun.rgb * saturate(dot(n, sun));
                float3 color = input.color.rgb * light;
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
