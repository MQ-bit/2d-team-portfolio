Shader "Custom/WobbleEffect"
{
    Properties
    {
        [Header(Wobble)]
        _Strength (
            "Strength (Pixels)",
            Range(0, 10)
        ) = 1.25

        _Speed (
            "Speed",
            Range(0, 10)
        ) = 1.5

        _Frequency (
            "Frequency",
            Range(1, 30)
        ) = 10
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
            Name "LightweightThirstWobble"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One Zero

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Speed;
                float _Frequency;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half2 uv =
                    (half2)input.texcoord;

                float time =
                    _Time.y * _Speed;

                // 세로 방향에 따라 좌우로 일렁임
                half horizontalWave =
                    (half)sin(
                        (float)uv.y *
                        _Frequency +
                        time
                    );

                // 가로 방향에 따라 약하게 상하로 일렁임
                half verticalWave =
                    (half)cos(
                        (float)uv.x *
                        (_Frequency * 0.73) -
                        time * 0.8
                    );

                // 상하 움직임은 좌우 움직임보다 약하게 적용
                half2 waveOffset = half2(
                    horizontalWave,
                    verticalWave * 0.35h
                );

                // 화면 가장자리로 갈수록 왜곡을 제거한다.
                // smoothstep과 거리 계산 없이 곱셈만 사용한다.
                half2 edgeFade = saturate(
                    uv *
                    (1.0h - uv) *
                    16.0h
                );

                half edgeMask =
                    edgeFade.x *
                    edgeFade.y;

                float2 distortion =
                    (float2)waveOffset *
                    _Strength *
                    _BlitTexture_TexelSize.xy *
                    edgeMask;

                float2 distortedUV =
                    input.texcoord +
                    distortion;

                // 화면 밖 샘플링 방지
                float2 halfTexel =
                    _BlitTexture_TexelSize.xy *
                    0.5;

                distortedUV = clamp(
                    distortedUV,
                    halfTexel,
                    1.0 - halfTexel
                );

                return SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    distortedUV,
                    0.0
                );
            }

            ENDHLSL
        }
    }

    Fallback Off
}