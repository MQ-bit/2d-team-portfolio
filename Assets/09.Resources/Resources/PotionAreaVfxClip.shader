Shader "Potion/Clipped VFX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _AreaClipMask ("Clipped Footprint", 2D) = "white" {}
        [HideInInspector] _AreaClipRect ("Footprint World Bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _AreaClipEnabled ("Clip Enabled", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 clipPositionWS : TEXCOORD1;
            };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _AreaClipRect;
                float _AreaClipEnabled;
            CBUFFER_END
            TEXTURE2D(_AreaClipMask);
            SAMPLER(sampler_AreaClipMask);
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.clipPositionWS = TransformObjectToWorld(input.positionOS).xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (_AreaClipEnabled > 0.5)
                {
                    float2 uv = (input.clipPositionWS - _AreaClipRect.xy) / _AreaClipRect.zw;
                    clip(min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y)));
                    clip(SAMPLE_TEXTURE2D(_AreaClipMask, sampler_AreaClipMask, uv).r - 0.5);
                }
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
