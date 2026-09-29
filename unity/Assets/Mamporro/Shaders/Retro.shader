Shader "Mamporro/Retro"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END
            float _RetroSnap, _RetroDither;
            float4 _RetroSize;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float light:TEXCOORD0; float distance:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                float2 ndc = o.positionCS.xy / o.positionCS.w;
                float2 snapped = floor(ndc * _RetroSize.xy * .5 + .5) / (_RetroSize.xy * .5);
                o.positionCS.xy = lerp(ndc, snapped, _RetroSnap) * o.positionCS.w;
                o.light = .48 + .52 * saturate(dot(TransformObjectToWorldNormal(input.normalOS),normalize(float3(-.4,.85,-.3))));
                o.distance = distance(world,GetCameraPositionWS());
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 color = _BaseColor.rgb * input.light;
                color = lerp(color,float3(.19,.24,.28),saturate((input.distance-26)/65));
                uint2 p = (uint2)input.positionCS.xy & 3;
                static const float bayer[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                float d = (bayer[p.x+p.y*4]/16-.5)/31;
                color = lerp(color,floor(saturate(color+d)*31+.5)/31,_RetroDither);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
