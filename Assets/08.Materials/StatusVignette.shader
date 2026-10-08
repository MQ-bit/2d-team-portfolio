// 상태이상 화면 테두리(StatusScreenVignette)용 UI 셰이더.
// 색과 진하기는 Image.color(정점 색)로 받고, _Style에 따라 테두리의 모양과 움직임을 다르게 그린다.
// 0 = 불(위로 일렁이는 불길), 1 = 독(느리게 맥동하는 얼룩), 2 = 얼음(결정 무늬와 반짝임), 3 = 전기(번쩍이는 전류)
Shader "UI/StatusVignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Style ("Style", Float) = 0
        _Thickness ("Thickness", Range(0.01, 1)) = 0.1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float _Style;
            float _Thickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0;
                float amplitude = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    value += amplitude * ValueNoise(p);
                    p *= 2.03;
                    amplitude *= 0.5;
                }
                return value;
            }

            // 화면 가장자리까지의 거리(화면 높이 단위). 모서리는 부드러운 min으로 살짝 둥글고 조금 더 두껍게.
            // 부드러운 정도를 두께에 맞춰서 테두리가 얇아도 모서리만 뭉툭하게 커지지 않게 한다.
            float EdgeDistance(float2 uv, float aspect, float thickness)
            {
                float ex = min(uv.x, 1 - uv.x) * aspect;
                float ey = min(uv.y, 1 - uv.y);
                float k = 3 / max(thickness, 0.01);
                return -log(exp(-k * ex) + exp(-k * ey)) / k;
            }

            float4 frag (v2f i) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 q = float2(i.uv.x * aspect, i.uv.y); // 노이즈가 화면비에 늘어나지 않게
                float e = EdgeDistance(i.uv, aspect, _Thickness);
                float t = _Time.y;
                float th = _Thickness;
                // 일렁임 폭은 두께에 비례시켜서 테두리를 얇게 해도 안쪽으로 튀어나오지 않게 한다.

                float3 col = i.color.rgb;
                float alpha = 0;

                if (_Style < 0.5)
                {
                    // 불: 노이즈를 위로 흘려 불길이 일렁이며 타오르는 테두리. 가장자리일수록 노랗게 달아오른다.
                    float n = Fbm(float2(q.x * 5, q.y * 3 - t * 1.6));
                    float n2 = Fbm(float2(q.x * 11, q.y * 7 - t * 2.7));
                    float m = 1 - smoothstep(0, th, e + ((n - 0.5) * 0.8 + (n2 - 0.5) * 0.35) * th);
                    float flicker = 0.85 + 0.15 * ValueNoise(float2(t * 9, 0));

                    col = lerp(col, float3(1, 0.75, 0.25), smoothstep(0.6, 1, m) * 0.7);
                    alpha = m * m * flicker;
                }
                else if (_Style < 1.5)
                {
                    // 독: 느리게 꿈틀대는 테두리가 숨쉬듯 맥동하고, 짙은 얼룩이 천천히 번진다.
                    float pulse = 0.5 + 0.5 * sin(t * 2.2);
                    float n = Fbm(q * 3 + float2(t * 0.15, t * 0.1));
                    float m = 1 - smoothstep(0, th * (0.8 + 0.25 * pulse), e + (n - 0.5) * th);
                    float blot = smoothstep(0.55, 0.75, Fbm(q * 9 + float2(0, t * 0.4)));

                    col = lerp(col, col * 0.4, blot);
                    alpha = m * m * (0.7 + 0.3 * pulse);
                }
                else if (_Style < 2.5)
                {
                    // 얼음: 움직이지 않는 날카로운 결정 경계와 갈라진 얼음 결, 가끔 반짝이는 서리 알갱이.
                    float ridge = 1 - abs(Fbm(q * 6) * 2 - 1);
                    float jag = ValueNoise(q * 28);
                    float m = 1 - smoothstep(th * 0.55, th, e + ((jag - 0.5) * 0.3 - ridge * 0.3) * th);
                    float haze = (1 - smoothstep(0, th * 1.3, e)) * 0.2;
                    float crack = pow(ridge, 6);

                    float2 grid = q * 40;
                    float h = Hash21(floor(grid));
                    float dotShape = smoothstep(0.3, 0, length(frac(grid) - 0.5));
                    float sparkle = step(0.96, h) * dotShape * (0.5 + 0.5 * sin(t * 4 + h * 50)) * m;

                    col = lerp(col, float3(0.9, 0.97, 1), saturate(crack * m * 1.2 + smoothstep(0.7, 1, m) * 0.4));
                    col += sparkle;
                    alpha = max(m * (0.75 + 0.25 * crack), haze) + sparkle * 0.5;
                }
                else
                {
                    // 전기: 가장자리를 따라 가는 전류 줄기가 가끔 모양을 바꾸고, 이따금 살짝 번쩍인다.
                    float stepTime = floor(t * 4);
                    float ridge = 1 - abs(Fbm(q * 7 + stepTime * 1.7) * 2 - 1);
                    float arc = pow(saturate(ridge), 24);
                    float m = 1 - smoothstep(0, th, e);
                    float flash = Hash21(float2(stepTime, 7.1)) > 0.8 ? 1 : 0.8;

                    col = lerp(col, float3(0.95, 0.9, 1), saturate(arc * 1.2));
                    alpha = (m * m * 0.6 + arc * smoothstep(0.3, 0.9, m) * 0.8) * flash;
                }

                return float4(col, saturate(alpha) * i.color.a);
            }
            ENDCG
        }
    }
}
