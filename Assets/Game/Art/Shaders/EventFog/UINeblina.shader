Shader "UI/Neblina"
{
    Properties
    {
        // O RawImage coloca aqui a imagem da tela (RenderTexture da camera)
        [PerRendererData] _MainTex ("Tela (RenderTexture da camera)", 2D) = "white" {}
        _Color ("Tint (so o alpha e usado)", Color) = (1,1,1,1)

        [Header(Geral)]
        _Intensity ("Intensidade Geral (fade)", Range(0, 1)) = 1
        _Seed ("Semente", Float) = 0
        _Aspect ("Aspect (Largura / Altura)", Float) = 1.7778
        [Toggle] _UseBackground ("Usar Tela de Fundo (RenderTexture)", Float) = 1

        [Header(Desfoque e Embacado)]
        [KeywordEnum(Low, Medium, High)] _BlurQ ("Qualidade do Desfoque", Float) = 1
        _Blur ("Desfoque da Tela", Range(0, 1)) = 0.35
        _Haze ("Embacado (veu claro)", Range(0, 1)) = 0.12
        _HazeColor ("Cor do Embacado", Color) = (0.8, 0.85, 0.9, 1)

        [Header(Neblina)]
        _FogColor ("Cor da Neblina", Color) = (0.84, 0.87, 0.9, 1)
        _FogOpacity ("Opacidade Maxima (1 = esconde tudo)", Range(0, 1)) = 0.95
        _FogShade ("Variacao de Tom", Range(0, 1)) = 0.25
        _FogBlur ("Desfoque Extra na Neblina", Range(0, 1)) = 0.6
        [IntRange] _CloudCount ("Quantidade de Nuvens", Range(0, 12)) = 4
        _CloudSize ("Tamanho (raio, em alturas de tela)", Range(0.05, 1.5)) = 0.35
        _CloudSizeVariation ("Variacao de Tamanho", Range(0, 0.8)) = 0.35
        _CloudSoftness ("Suavidade da Borda", Range(0.05, 1)) = 0.6
        _Wispiness ("Irregularidade (fiapos)", Range(0, 2)) = 0.8
        _NoiseScale ("Escala do Ruido", Range(0.5, 8)) = 2.5
        _CloudSpeed ("Velocidade do Vento", Range(0, 0.5)) = 0.04
        _WindAngle ("Direcao do Vento (graus)", Range(-180, 180)) = 0
        _CloudLife ("Vida de Cada Nuvem (s)", Float) = 12

        [Header(Chuva na Tela)]
        _RainAmount ("Quantidade Geral de Chuva", Range(0, 1)) = 0.8
        _StaticDensity ("Gotas Grandes: Densidade", Range(2, 20)) = 7
        _StaticAmount ("Gotas Grandes: Quantidade", Range(0, 1)) = 0.5
        _StaticSize ("Gotas Grandes: Tamanho", Range(0.1, 1)) = 0.9
        _StaticLife ("Gotas: Velocidade do Ciclo", Float) = 0.07
        _MistDensity ("Gotinhas: Densidade", Range(8, 60)) = 24
        _MistAmount ("Gotinhas: Quantidade", Range(0, 1)) = 0.55
        _MistSize ("Gotinhas: Tamanho", Range(0.1, 1)) = 0.7
        _SlideColumns ("Escorrendo: Colunas (por altura)", Range(1, 16)) = 5
        _SlideAmount ("Escorrendo: Quantidade", Range(0, 1)) = 0.5
        _SlideSize ("Escorrendo: Tamanho da Gota", Range(0.002, 0.04)) = 0.012
        _SlideSpeed ("Escorrendo: Velocidade", Range(0.01, 1)) = 0.12
        _SlideLength ("Escorrendo: Comprimento do Percurso", Range(0.1, 1.5)) = 0.55
        _TrailStrength ("Escorrendo: Forca do Rastro", Range(0, 1)) = 0.7

        [Header(Otica das Gotas)]
        _Refraction ("Refracao (ampliacao invertida)", Range(0, 4)) = 2.2
        _DropClear ("Gota Limpa o Vidro (desfoque/veu)", Range(0, 1)) = 0.9
        _DropSpecular ("Brilho Especular", Range(0, 2)) = 1
        _DropRim ("Borda Escura", Range(0, 1)) = 0.5
        _LightAngle ("Direcao da Luz (graus)", Range(-180, 180)) = 135

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
            #pragma multi_compile_local _BLURQ_LOW _BLURQ_MEDIUM _BLURQ_HIGH

            #if defined(_BLURQ_LOW)
                #define BLUR_TAPS 8
            #elif defined(_BLURQ_HIGH)
                #define BLUR_TAPS 24
            #else
                #define BLUR_TAPS 14
            #endif

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

            float _Intensity, _Seed, _Aspect, _UseBackground;
            float _Blur, _Haze;
            fixed4 _HazeColor;

            fixed4 _FogColor;
            float _FogOpacity, _FogShade, _FogBlur;
            float _CloudCount, _CloudSize, _CloudSizeVariation, _CloudSoftness;
            float _Wispiness, _NoiseScale, _CloudSpeed, _WindAngle, _CloudLife;

            float _RainAmount;
            float _StaticDensity, _StaticAmount, _StaticSize, _StaticLife;
            float _MistDensity, _MistAmount, _MistSize;
            float _SlideColumns, _SlideAmount, _SlideSize, _SlideSpeed, _SlideLength, _TrailStrength;

            float _Refraction, _DropClear, _DropSpecular, _DropRim, _LightAngle;

            float _UseCustomTime, _CustomTime;

            // ------------------------------------------------------------
            // Ruido
            // ------------------------------------------------------------
            float hash21(float2 p)
            {
                float3 q = frac(float3(p.x, p.y, p.x) * float3(0.1031, 0.1030, 0.0973));
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.03 + float2(17.1, 9.2);
                    a *= 0.5;
                }
                return v / 0.9375;
            }

            // ------------------------------------------------------------
            // Desfoque (disco de Vogel, 8 / 14 / 24 amostras)
            // ------------------------------------------------------------
            float3 BlurSample(float2 uv, float radius, float aspect)
            {
                if (radius < 0.0002)
                    return tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb;

                float2 scale = float2(radius / aspect, radius);
                float3 sum = 0;
                [unroll]
                for (int i = 0; i < BLUR_TAPS; i++)
                {
                    float r = sqrt((i + 0.5) / BLUR_TAPS);
                    float a = i * 2.39996323;
                    float2 o = float2(cos(a), sin(a)) * r * scale;
                    sum += tex2Dlod(_MainTex, float4(uv + o, 0, 0)).rgb;
                }
                return sum / BLUR_TAPS;
            }

            // ------------------------------------------------------------
            // Nuvens de neblina
            // ------------------------------------------------------------
            float CloudDensity(float2 pA, float aspect, float t, out float shadeN)
            {
                float2 wind = float2(cos(radians(_WindAngle)), sin(radians(_WindAngle)));
                float2 q = pA * _NoiseScale;

                // dois campos de ruido compartilhados por todas as nuvens
                float n = fbm(q + wind * t * _CloudSpeed * 2.0 + _Seed * 3.7);
                shadeN = fbm(q * 2.1 - wind * t * _CloudSpeed + 11.3 + _Seed);

                int count = (int)(_CloudCount + 0.5);
                float dens = 0.0;

                [loop]
                for (int i = 0; i < 12; i++)
                {
                    if (i >= count) break;

                    float fi = (float)i;
                    float ph = hash21(float2(fi, _Seed + 3.1));
                    float cyc = t / max(_CloudLife, 0.1) + ph;
                    float cycId = floor(cyc);
                    float lt = frac(cyc);

                    // nasce, cresce, some; a cada ciclo reaparece em outro lugar
                    float env = smoothstep(0.0, 0.35, lt) * (1.0 - smoothstep(0.6, 1.0, lt));

                    float2 sd = float2(fi * 7.13 + cycId * 3.77, _Seed + fi);
                    float2 c = float2(lerp(-0.1, 1.1, hash21(sd)) * aspect,
                                      lerp(-0.1, 1.1, hash21(sd + 5.2)));
                    c += wind * _CloudSpeed * (lt - 0.5) * _CloudLife;

                    float sz = _CloudSize * lerp(1.0 - _CloudSizeVariation,
                                                 1.0 + _CloudSizeVariation,
                                                 hash21(sd + 9.1));
                    sz *= lerp(0.6, 1.0, env);

                    float d = length(pA - c) / max(sz, 0.01);
                    float shape = 1.0 - d + (n - 0.5) * _Wispiness;
                    float cd = smoothstep(0.0, max(_CloudSoftness, 0.01), shape) * env;

                    dens = 1.0 - (1.0 - dens) * (1.0 - cd);
                }
                return dens;
            }

            // ------------------------------------------------------------
            // Gotas
            // ------------------------------------------------------------
            struct DropOut
            {
                float2 p;   // posicao relativa ao centro (altura de tela)
                float r;    // raio
                float m;    // mascara 0..1
            };

            DropOut NoDrop()
            {
                DropOut o;
                o.p = float2(0, 0);
                o.r = 1.0;
                o.m = 0.0;
                return o;
            }

            // Gotas paradas: uma por celula, nascem, crescem e evaporam
            DropOut StaticDrop(float2 pA, float density, float amount, float sizeMul,
                               float rate, float t, float seed)
            {
                DropOut o = NoDrop();

                float2 g = pA * density;
                float2 id = floor(g);
                float2 f = frac(g);

                float h0 = hash21(id + seed);
                float cyc = t * rate + h0 * 5.0;
                float cid = floor(cyc);
                float lt = frac(cyc);

                float2 cs = id + seed + cid * 7.13;
                float h1 = hash21(cs + 3.3);
                float pres = saturate((amount - h1) * 8.0);
                if (pres <= 0.0) return o;

                float h2 = hash21(cs + 8.8);
                float h3 = hash21(cs + 15.5);
                float h4 = hash21(cs + 22.2);

                float2 pos = float2(0.3 + 0.4 * h2, 0.3 + 0.4 * h3);
                float env = smoothstep(0.0, 0.12, lt) * (1.0 - smoothstep(0.7, 1.0, lt));
                float rel = lerp(0.1, 0.28, h4) * saturate(sizeMul) * env * pres;
                float r = max(rel / density, 1e-5);

                float2 p = (f - pos) / density;
                o.p = p;
                o.r = r;
                o.m = 1.0 - smoothstep(r * 0.85, r, length(p));
                return o;
            }

            // Gotas que escorrem: gota principal + rastro + gotinhas deixadas para tras
            void SlideDrops(float2 pA, float t, float rain,
                            out DropOut mainD, out DropOut trailD, out DropOut beadD)
            {
                mainD = NoDrop();
                trailD = NoDrop();
                beadD = NoDrop();

                float cols = max(_SlideColumns, 0.5);
                float colF = pA.x * cols;
                float col = floor(colF);
                float fx = frac(colF);

                float hc = hash21(float2(col, 3.7 + _Seed));
                float rowH = max(_SlideLength, 0.05);
                float yy = pA.y + hc * rowH * 7.0;         // desalinha as colunas
                float rowF = yy / rowH;
                float row = floor(rowF);
                float ly = frac(rowF);                      // 0 = base, 1 = topo

                float2 cell = float2(col, row) + _Seed * 1.3;
                float hA = hash21(cell + 11.1);
                float hB = hash21(cell + 23.3);

                float cyc = t * _SlideSpeed * (0.7 + 0.6 * hA) + hB;
                float cid = floor(cyc);
                float s = frac(cyc);

                float2 cs = cell + cid * 5.31;
                float h1 = hash21(cs + 41.7);
                float pres = saturate((_SlideAmount * rain - h1) * 8.0);
                if (pres <= 0.0) return;

                float h2 = hash21(cs + 53.9);
                float h3 = hash21(cs + 67.1);
                float hr = hash21(cs + 77.7);

                float ends = smoothstep(0.0, 0.08, s) * (1.0 - smoothstep(0.88, 1.0, s));

                // desce em "trancos" (gruda e solta)
                float stick = saturate(s + 0.025 * sin(s * 31.0 + h3 * 6.2832));
                float dropY = 0.95 - stick * 0.9;

                float x0 = 0.3 + 0.4 * h2;
                float ph = h3 * 6.2832;
                float xd = x0 + 0.12 * sin((row + dropY) * rowH * 14.0 + ph);
                float xt = x0 + 0.12 * sin(yy * 14.0 + ph);

                float dy = (ly - dropY) * rowH;             // > 0 = acima da gota
                float rd = _SlideSize * (0.75 + 0.5 * hr) * lerp(1.0, 0.75, s) * ends * pres;
                rd = max(rd, 1e-5);

                // gota principal (levemente alongada)
                float2 pd = float2((fx - xd) / cols, dy);
                float dist = length(float2(pd.x, pd.y / 1.25));
                mainD.p = pd;
                mainD.r = rd;
                mainD.m = 1.0 - smoothstep(rd * 0.85, rd, dist);

                // rastro de agua
                float L = max((1.0 - dropY) * rowH, 1e-4);
                float wT = rd * 0.45;
                float dxT = (fx - xt) / cols;
                float along = saturate(dy / L);
                float tm = 1.0 - smoothstep(wT * 0.4, wT, abs(dxT));
                tm *= smoothstep(0.0, rd * 0.5, dy) * pow(1.0 - along, 1.5) * ends * _TrailStrength;
                trailD.p = float2(dxT, 0.0);
                trailD.r = max(wT, 1e-5);
                trailD.m = tm;

                // gotinhas deixadas no rastro
                float bs = max(rd * 4.5, 1e-4);
                float bi = floor(dy / bs);
                float hbd = hash21(float2(bi, h2 * 91.0 + cell.x));
                float rb = max(rd * (0.25 + 0.3 * hbd), 1e-5);
                float2 bc = float2((hash21(float2(bi, h3 * 57.0)) - 0.5) * wT * 1.2, (bi + 0.5) * bs);
                float2 pb = float2(dxT, dy) - bc;
                float bm = 1.0 - smoothstep(rb * 0.8, rb, length(pb));
                bm *= step(0.45, hbd) * step(0.0, bi) * (1.0 - smoothstep(0.7, 1.0, along)) * ends;
                beadD.p = pb;
                beadD.r = rb;
                beadD.m = bm;
            }

            // Soma a contribuicao optica de uma gota (lente + brilho + borda)
            void AddDrop(DropOut d, float gain, float2 L2, float3 L3,
                         inout float2 refr, inout float clearM, inout float spec, inout float rim)
            {
                if (d.m <= 0.0001) return;

                float2 n = d.p / d.r;
                float nl = length(n);
                if (nl > 1.0) { n /= nl; nl = 1.0; }
                float3 n3 = float3(n, sqrt(max(0.0, 1.0 - nl * nl)));

                float w = d.m * gain;
                refr -= d.p * _Refraction * w;                       // imagem invertida, como numa lente
                clearM = max(clearM, d.m * min(1.0, gain * 1.5));
                spec += pow(saturate(dot(n3, L3)), 24.0) * w;
                rim += saturate(-dot(n, L2)) * pow(nl, 2.5) * w;
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
                float I = _Intensity;
                float aspect = max(_Aspect, 0.0001);

                float2 uv = IN.texcoord;
                float2 pA = float2(uv.x * aspect, uv.y);   // unidades de altura de tela

                // ---------------- Neblina ----------------
                float shadeN;
                float dens = CloudDensity(pA, aspect, t, shadeN);

                // ---------------- Gotas ----------------
                float2 refr = float2(0, 0);
                float clearM = 0.0;
                float spec = 0.0;
                float rim = 0.0;

                float rain = _RainAmount * I;
                if (rain > 0.001)
                {
                    float2 L2 = float2(cos(radians(_LightAngle)), sin(radians(_LightAngle)));
                    float3 L3 = normalize(float3(L2 * 0.7, 0.7));

                    DropOut s1 = StaticDrop(pA, _StaticDensity, _StaticAmount * rain,
                                            _StaticSize, _StaticLife, t, 1.0 + _Seed);
                    AddDrop(s1, 1.0, L2, L3, refr, clearM, spec, rim);

                    DropOut s2 = StaticDrop(pA, _MistDensity, _MistAmount * rain,
                                            _MistSize, _StaticLife * 1.4, t, 7.0 + _Seed);
                    AddDrop(s2, 0.8, L2, L3, refr, clearM, spec, rim);

                    DropOut dm, dt, db;
                    SlideDrops(pA, t, rain, dm, dt, db);
                    AddDrop(dm, 1.0, L2, L3, refr, clearM, spec, rim);
                    AddDrop(dt, 0.3, L2, L3, refr, clearM, spec, rim);
                    AddDrop(db, 0.9, L2, L3, refr, clearM, spec, rim);
                }

                // ---------------- Composicao ----------------
                float2 uvS = uv + refr / float2(aspect, 1.0);
                float clearK = clearM * _DropClear;

                float blurAmt = saturate(_Blur * I + dens * _FogBlur * I) * (1.0 - clearK);

                float3 col;
                float acc = 0.0;
                if (_UseBackground > 0.5)
                    col = BlurSample(uvS, blurAmt * 0.02, aspect);
                else
                    col = _HazeColor.rgb;

                // embacado (veu claro); gotas limpam o vidro
                float hz = _Haze * I * (1.0 - clearK);
                col = lerp(col, _HazeColor.rgb, hz);
                acc = 1.0 - (1.0 - acc) * (1.0 - hz);

                // nuvens de neblina
                float fogA = saturate(dens * _FogOpacity) * I * (1.0 - clearK * 0.5);
                float3 fogCol = lerp(_FogColor.rgb * (1.0 - _FogShade), _FogColor.rgb, shadeN);
                col = lerp(col, fogCol, fogA);
                acc = 1.0 - (1.0 - acc) * (1.0 - fogA);

                // sombreamento das gotas: borda escura + brilho especular
                float rimV = saturate(rim) * _DropRim * 0.5;
                float specV = saturate(spec) * _DropSpecular * 0.6;
                col *= 1.0 - rimV;
                col += specV;
                acc = 1.0 - (1.0 - acc) * (1.0 - saturate(rimV + specV));

                float alpha = _UseBackground > 0.5 ? 1.0 : saturate(acc);

                fixed4 outCol = fixed4(col, alpha * IN.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                outCol.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(outCol.a - 0.001);
                #endif

                return outCol;
            }
            ENDCG
        }
    }
}
