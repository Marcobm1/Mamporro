// Números de daño (DamageNumbers.ts): glifos del atlas pixelado con contorno, color por vértice
// con transparencia, sin prueba de profundidad (siempre encima, como la web) y sin luz ni niebla.
// En Resources para que se incluya en la build sin referencias de escena.
Shader "Mamporro/DamageNumbers"
{
    Properties { _MainTex("Atlas", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZTest Always ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_point_clamp);
            struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS.xyz));
                o.color = input.color; o.uv = input.uv;
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv);
                clip(texel.a - 0.5);
                return half4(texel.rgb * input.color.rgb, input.color.a);
            }
            ENDHLSL
        }
    }
}
