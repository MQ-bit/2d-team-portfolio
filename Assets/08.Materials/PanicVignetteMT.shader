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
        ) = (0.5, 0.0, 0.0, 1)

        _VignetteIntensity (
            "Vignette Intensity",
            Range(0, 1)
        ) = 0.7

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
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PainVignette"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One Zero

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            // Pulse가 꺼져 있으면 관련 연산이 제거된 버전 사용
            #pragma shader_feature_local_fragment _VIGNETTE_PULSE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _VignetteColor;

                float _EffectAmount;
                float _VignetteIntensity;
                float _VignetteRadius;
                float _VignetteSoftness;

                float _EnablePulse;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half4 color = SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv,
                    0.0
                );

                // 중앙에서의 거리 제곱.
                // length()를 사용하지 않아 제곱근 연산을 피한다.
                half2 centeredUV =
                    (half2)uv - half2(0.5h, 0.5h);

                // 중앙 0, 모서리 약 1
                half distanceSquared =
                    dot(centeredUV, centeredUV) * 2.0h;

                half radius =
                    (half)_VignetteRadius;

                half outerRadius = saturate(
                    (half)(
                        _VignetteRadius +
                        _VignetteSoftness
                    )
                );

                half innerSquared =
                    radius * radius;

                half outerSquared =
                    outerRadius * outerRadius;

                half vignetteMask = smoothstep(
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

                    // 맥박이 순간적으로 강해지는 형태
                    pulse =
                        pulse *
                        pulse *
                        pulse;

                    pulseMultiplier +=
                        pulse *
                        (half)_PulseAmount;

                #endif

                half vignetteAmount = saturate(
                    vignetteMask *
                    (half)_VignetteIntensity *
                    (half)_EffectAmount *
                    pulseMultiplier *
                    (half)_VignetteColor.a
                );

                // 가장자리 색상을 지정된 색으로 혼합
                color.rgb = lerp(
                    color.rgb,
                    (half3)_VignetteColor.rgb,
                    vignetteAmount
                );

                return color;
            }

            ENDHLSL
        }
    }

    Fallback Off
}