Shader "Wobble Brawl/Toon Stage Water"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.09, 0.69, 0.94, 0.38)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half band = smoothstep(.25h, .29h, dot(normal, GetMainLight().direction));
                half edge = 1 - saturate(dot(normal, GetWorldSpaceNormalizeViewDir(input.positionWS)));
                half rim = smoothstep(.74h, .86h, edge) * .35h;
                half3 color = lerp(_BaseColor.rgb * lerp(.55h, 1.0h, band),
                    lerp(_BaseColor.rgb, half3(1, 1, 1), .38h), rim);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
