Shader "UI/WaveStripes"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (nao usado)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Colors)]
        _ColorA ("Stripe Color A", Color) = (0.11, 0.12, 0.13, 1)
        _ColorB ("Stripe Color B", Color) = (0.02, 0.025, 0.03, 1)

        [Header(Layout)]
        _Aspect ("Aspect (Largura / Altura)", Float) = 1.7778
        _Rotation ("Rotacao Global (graus)", Range(-180, 180)) = 0

        [Header(Stripes)]
        _StripeCount ("Quantidade de Listras", Float) = 14
        _StripeAngle ("Angulo das Listras (graus)", Range(-180, 180)) = 35
        _StripeSpeed ("Velocidade de Rolagem", Float) = 0.1
        _StripeDuty ("Largura da Listra A", Range(0.05, 0.95)) = 0.5
        _Softness ("Suavidade da Borda", Range(0, 0.5)) = 0.03

        [Header(Waves)]
        _WaveAmplitude ("Amplitude (em listras)", Float) = 0.8
        _WaveFrequency ("Frequencia (ondas por altura)", Float) = 1.2
        _WaveSpeed ("Velocidade da Onda", Float) = 0.6
        _WaveAngle ("Direcao da Onda (graus)", Range(-180, 180)) = 0
        _WaveDetail ("Onda Secundaria", Range(0, 1)) = 0.35
        _Shade ("Sombreamento pela Onda", Range(0, 1)) = 0.25

        [Header(Time)]
        [Toggle] _UseCustomTime ("Usar Tempo Customizado (unscaled)", Float) = 0
        _CustomTime ("Tempo Customizado", Float) = 0

        // Suporte padrao de UI (mascaras, stencil, clip)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;
            sampler2D _MainTex;

            fixed4 _ColorA;
            fixed4 _ColorB;
            float _Aspect;
            float _Rotation;
            float _StripeCount;
            float _StripeAngle;
            float _StripeSpeed;
            float _StripeDuty;
            float _Softness;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _WaveAngle;
            float _WaveDetail;
            float _Shade;
            float _UseCustomTime;
            float _CustomTime;

            static const float TAU = 6.28318530718;

            float2 Rot(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float t = _UseCustomTime > 0.5 ? _CustomTime : _Time.y;

                // Coordenada centrada, corrigida pelo aspecto, com rotacao global
                float2 p = (IN.texcoord - 0.5) * float2(_Aspect, 1.0);
                p = Rot(p, radians(_Rotation));

                // Direcoes
                float sa = radians(_StripeAngle);
                float2 stripeNormal = float2(-sin(sa), cos(sa));
                float wa = radians(_WaveAngle);
                float2 waveDir1 = float2(cos(wa), sin(wa));
                float2 waveDir2 = Rot(waveDir1, 0.9);

                // Ondas (duas camadas para ficar organico)
                float w1 = sin(dot(p, waveDir1) * TAU * _WaveFrequency + t * _WaveSpeed);
                float w2 = sin(dot(p, waveDir2) * TAU * _WaveFrequency * 2.3 - t * _WaveSpeed * 1.4 + 1.7);
                float wave = (w1 + w2 * _WaveDetail) / (1.0 + _WaveDetail);

                // Coordenada da listra (periodo = 1), deslocada pela onda
                float s = dot(p, stripeNormal) * _StripeCount
                        + wave * _WaveAmplitude
                        + t * _StripeSpeed;

                // Antialiasing + suavidade
                float soft = max(_Softness, fwidth(s));
                soft = min(soft, 0.5 * min(_StripeDuty, 1.0 - _StripeDuty));

                float f = frac(s);
                float m = smoothstep(0.0, soft, f)
                        - smoothstep(_StripeDuty, _StripeDuty + soft, f);

                fixed4 col = lerp(_ColorB, _ColorA, m);
                col.rgb *= 1.0 + wave * _Shade;
                col *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
