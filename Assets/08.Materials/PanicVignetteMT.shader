Shader "Custom/PanicVignette"
{
    Properties
    {
        [Header(Master)]
        _EffectAmount (
            "Effect Amount",
            Range(0, 1)
        ) = 1

        [Header(Vignette)]
        _VignetteColor (
            "Vignette Color",
            Color
        ) = (0.55, 0.0, 0.0, 1)

        _VignetteIntensity (
            "Vignette Intensity",
            Range(0, 1)
        ) = 0.75

        _VignetteRadius (
            "Vignette Radius",
            Range(0, 1)
        ) = 0.35

        _VignetteSoftness (
            "Vignette Softness",
            Range(0.01, 1)
        ) = 0.5

        [Header(Pulse)]
        [Toggle(_VIGNETTE_PULSE)]
        _EnablePulse (
            "Enable Pulse",
            Float
        ) = 1

        _PulseSpeed (
            "Pulse Speed",
            Range(0, 10)
        ) = 1.2

        _PulseAmount (
            "Pulse Amount",
            Range(0, 1)
        ) = 0.3

        // Unity UI 공통 설정
        [PerRendererData]
        _MainTex (
            "Sprite Texture",
            2D
        ) = "white" {}

        _Color (
            "UI Tint",
            Color
        ) = (1, 1, 1, 1)

        _StencilComp (
            "Stencil Comparison",
            Float
        ) = 8

        _Stencil (
            "Stencil ID",
            Float
        ) = 0

        _StencilOp (
            "Stencil Operation",
            Float
        ) = 0

        _StencilWriteMask (
            "Stencil Write Mask",
            Float
        ) = 255

        _StencilReadMask (
            "Stencil Read Mask",
            Float
        ) = 255

        _ColorMask (
            "Color Mask",
            Float
        ) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip (
            "Use Alpha Clip",
            Float
        ) = 0
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
            Name "ProceduralPainVignette"

            CGPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _VIGNETTE_PULSE

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 worldPosition : TEXCOORD0;
                half4 color : COLOR;
                half2 uv : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _VignetteColor;

                float _EffectAmount;
                float _VignetteIntensity;
                float _VignetteRadius;
                float _VignetteSoftness;

                float _EnablePulse;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            float4 _ClipRect;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition =
                    input.positionOS;

                output.positionCS =
                    UnityObjectToClipPos(
                        input.positionOS
                    );

                output.uv =
                    input.uv;

                // Image.color와 Material의 UI Tint 반영
                output.color =
                    input.color *
                    (half4)_Color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 화면 중앙을 기준으로 한 UV
                half2 centeredUV =
                    input.uv -
                    half2(0.5h, 0.5h);

                // 중앙은 0, 모서리는 약 1
                // sqrt를 사용하지 않는 거리 제곱 방식
                half distanceSquared =
                    dot(
                        centeredUV,
                        centeredUV
                    ) * 2.0h;

                half radius =
                    (half)_VignetteRadius;

                half outerRadius =
                    saturate(
                        (half)(
                            _VignetteRadius +
                            _VignetteSoftness
                        )
                    );

                half innerSquared =
                    radius * radius;

                half outerSquared =
                    outerRadius *
                    outerRadius;

                half vignetteMask =
                    smoothstep(
                        innerSquared,
                        max(
                            outerSquared,
                            innerSquared + 0.0001h
                        ),
                        distanceSquared
                    );

                half pulseMultiplier = 1.0h;

                #if defined(_VIGNETTE_PULSE)

                    half pulse =
                        sin(
                            (float)_Time.y *
                            _PulseSpeed *
                            6.2831853
                        ) * 0.5h + 0.5h;

                    // 완만한 사인파를 짧고 강한 맥박으로 변경
                    pulse =
                        pulse *
                        pulse *
                        pulse;

                    pulseMultiplier +=
                        pulse *
                        (half)_PulseAmount;

                #endif

                half alpha =
                    vignetteMask *
                    (half)_VignetteIntensity *
                    (half)_EffectAmount *
                    pulseMultiplier *
                    (half)_VignetteColor.a *
                    input.color.a;

                alpha = saturate(alpha);

                // Image.color로 추가 색상 조절 가능
                half3 finalColor =
                    (half3)_VignetteColor.rgb *
                    input.color.rgb;

                half4 outputColor =
                    half4(
                        finalColor,
                        alpha
                    );

                // RectMask2D 및 UI Mask 영역 반영
                #ifdef UNITY_UI_CLIP_RECT

                    outputColor.a *=
                        UnityGet2DClipping(
                            input.worldPosition.xy,
                            _ClipRect
                        );

                #endif

                #ifdef UNITY_UI_ALPHACLIP

                    clip(
                        outputColor.a -
                        0.001h
                    );

                #endif

                return outputColor;
            }

            ENDCG
        }
    }
}