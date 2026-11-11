Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size (px)", Range(0, 16)) = 2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent"
               "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float  _OutlineSize;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;   // cor do SpriteRenderer
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float2 d = _MainTex_TexelSize.xy * _OutlineSize;

                // 8 vizinhos (cantos deixam o contorno mais redondo)
                half a = 0;
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( d.x, 0)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-d.x, 0)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0,  d.y)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, -d.y)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( d.x,  d.y)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-d.x,  d.y)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( d.x, -d.y)).a);
                a = max(a, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-d.x, -d.y)).a);

                half outline = a * (1 - tex.a);   // só onde o sprite NÃO está

                half4 col;
                col.rgb = tex.rgb * tex.a + _OutlineColor.rgb * outline;
                col.a   = saturate(tex.a + outline * _OutlineColor.a);
                col.rgb /= max(col.a, 0.0001);    // desfaz o pré-multiplicado
                return col;
            }
            ENDHLSL
        }
    }
}