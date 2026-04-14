Shader "Custom/PulseOverlay"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        _PulseColor("Pulse Color", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
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
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PulseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RendererColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Flip)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 flip = UNITY_ACCESS_INSTANCED_PROP(Props, _Flip);
                float4 pos = input.positionOS;
                pos.xy = pos.xy * flip.xy;

                output.positionCS = TransformObjectToHClip(pos.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                float4 rendererColor = UNITY_ACCESS_INSTANCED_PROP(Props, _RendererColor);
                output.color = input.color * rendererColor;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 pulseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _PulseColor);

                // RGB is pulseColor RGB
                // A is pulseColor A multiplied by input texture A (and vertex/renderer color A)
                float4 color;
                color.rgb = pulseColor.rgb;
                color.a = pulseColor.a * texColor.a * input.color.a;
                
                // Final Premultiply alpha
                color.rgb *= color.a;
                
                return color;
            }
            ENDHLSL
        }
    }
}
