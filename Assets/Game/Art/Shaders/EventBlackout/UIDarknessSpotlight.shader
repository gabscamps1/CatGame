Shader "UI/DarknessSpotlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture (nao usado)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Darkness)]
        _DarkColor ("Cor da Escuridao (A = opacidade)", Color) = (0, 0, 0, 0.97)
        _Intensity ("Intensidade (fade in/out)", Range(0, 1)) = 1

        [Header(Light)]
        _LightTint ("Cor da Luz (A = forca do brilho)", Color) = (1, 0.85, 0.55, 0)
        _Radius ("Raio (em alturas de tela)", Range(0.02, 1.5)) = 0.22
        _Softness ("Suavidade da Borda", Range(0, 1)) = 0.5
        _Falloff ("Curva da Borda", Range(0.2, 4)) = 1

        [Header(Movement)]
        _Progress ("Progresso (0 = esquerda, 1 = direita)", Range(0, 1)) = 0.5
        _PosY ("Altura Central", Range(0, 1)) = 0.5
        _RangeMin ("Limite Esquerdo do Percurso (UV)", Range(-0.5, 1)) = 0.05
        _RangeMax ("Limite Direito do Percurso (UV)", Range(0, 1.5)) = 0.95
        _WobbleAmp ("Balanco Vertical (amplitude)", Range(0, 0.5)) = 0.06
        _WobbleFreq ("Balanco Vertical (ciclos)", Float) = 1.5

        [Header(Life)]
        _PulseAmount ("Pulsar (amplitude)", Range(0, 0.3)) = 0.03
        _PulseSpeed ("Pulsar (velocidade)", Float) = 2
        _FlickerAmount ("Tremida da Luz", Range(0, 0.2)) = 0.015

        [Header(Reveal)]
        _Reveal ("Revelar Tela (0 = luz normal, 1 = tela toda)", Range(0, 1)) = 0

        [Header(Layout)]
        _Aspect ("Aspect (Largura / Altura)", Float) = 1.7778

        [Header(Time)]
        [Toggle] _UseCustomTime ("Usar Tempo Customizado (unscaled)", Float) = 0
        _CustomTime ("Tempo Customizado", Float) = 0

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

            fixed4 _DarkColor;
            fixed4 _LightTint;
            float _Intensity;
            float _Radius;
            float _Softness;
            float _Falloff;
            float _Progress;
            float _PosY;
            float _RangeMin;
            float _RangeMax;
            float _Reveal;
            float _WobbleAmp;
            float _WobbleFreq;
            float _PulseAmount;
            float _PulseSpeed;
            float _FlickerAmount;
            float _Aspect;
            float _UseCustomTime;
            float _CustomTime;

            static const float TAU = 6.28318530718;

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
                float aspect = max(_Aspect, 0.0001);

                // Percurso entre _RangeMin e _RangeMax
                float2 center;
                center.x = lerp(_RangeMin, _RangeMax, _Progress);
                center.y = _PosY + _WobbleAmp * sin(_Progress * TAU * _WobbleFreq);

                // Raio com pulsar suave + tremida irregular
                float flicker = sin(t * 23.0) * 0.5 + sin(t * 37.0 + 1.3) * 0.5;
                float R = _Radius * (1.0 + _PulseAmount * sin(t * _PulseSpeed)
                                         + _FlickerAmount * flicker);

                // Revelar: o raio cresce ate cobrir a tela inteira a partir do centro atual
                float2 farDist = float2(max(center.x, 1.0 - center.x) * aspect,
                                        max(center.y, 1.0 - center.y));
                float rFull = length(farDist) / max(1.0 - _Softness, 0.05) + 0.05;
                R = lerp(R, max(rFull, R), _Reveal);

                // Distancia em "alturas de tela" (circulo perfeito em qualquer aspecto)
                float d = length((IN.texcoord - center) * float2(aspect, 1.0));

                // Borda com suavidade + antialiasing
                float aa = fwidth(d);
                float inner = min(R * (1.0 - _Softness), R - aa);
                float m = smoothstep(inner, R, d);
                m = pow(saturate(m), _Falloff);

                // Brilho opcional dentro da luz (A da cor da luz)
                float glow = (1.0 - smoothstep(0.0, R, d)) * _LightTint.a * (1.0 - _Reveal);

                fixed4 col;
                col.rgb = lerp(_LightTint.rgb, _DarkColor.rgb, m);
                col.a = lerp(glow, _DarkColor.a, m) * _Intensity;
                col *= fixed4(IN.color.rgb, IN.color.a);

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
