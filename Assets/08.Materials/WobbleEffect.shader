Shader "Custom/WobbleEffect"
{
    Properties
    {
        // 화면이 흔들리는 최대 거리(픽셀 단위)
        _Strength ("Strength (Pixels)", Range(0, 20)) = 4

        // 움직이는 속도
        _Speed ("Speed", Range(0, 10)) = 1.5

        // 물결의 촘촘함
        _Frequency ("Frequency", Range(1, 50)) = 12

        // 가장자리에서 효과가 사라지는 범위
        _EdgeFade ("Edge Fade", Range(0.001, 0.2)) = 0.04
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
            Name "FullscreenWobble"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Speed;
                float _Frequency;
                float _EdgeFade;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // 가로세로 해상도 차이 때문에 파형이 찌그러지지 않도록 보정
                float aspect =
                    _BlitTexture_TexelSize.z *
                    _BlitTexture_TexelSize.y;

                float2 position = uv - 0.5;
                position.x *= aspect;

                float time = _Time.y * _Speed;

                // 서로 다른 방향과 속도를 가진 파형을 합성
                float waveX =
                    sin(position.y * _Frequency + time);

                waveX +=
                    sin(
                        (position.y + position.x * 0.35) *
                        (_Frequency * 1.73) -
                        time * 1.37
                    ) * 0.45;

                float waveY =
                    cos(
                        position.x *
                        (_Frequency * 0.85) -
                        time * 0.83
                    );

                waveY +=
                    sin(
                        (position.x - position.y) *
                        (_Frequency * 1.31) +
                        time * 1.11
                    ) * 0.35;

                // Strength를 픽셀 단위에서 UV 단위로 변환
                float2 distortion =
                    float2(waveX, waveY) *
                    _Strength *
                    _BlitTexture_TexelSize.xy;

                // 화면 가장자리에서 왜곡을 서서히 제거
                float2 distanceToEdge = min(uv, 1.0 - uv);
                float nearestEdge =
                    min(distanceToEdge.x, distanceToEdge.y);

                float edgeMask = smoothstep(
                    0.0,
                    max(_EdgeFade, 0.0001),
                    nearestEdge
                );

                float2 distortedUV =
                    uv + distortion * edgeMask;

                // 텍스처 바깥을 샘플링하는 현상 방지
                float2 halfTexel =
                    _BlitTexture_TexelSize.xy * 0.5;

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