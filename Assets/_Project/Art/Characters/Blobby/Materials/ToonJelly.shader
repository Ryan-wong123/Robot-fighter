Shader "Wobble Brawl/Toon Jelly"
{
    Properties
    {
        _BaseMap ("Face Atlas", 2D) = "white" {}
        _Shade ("Shade", Color) = (0.46, 0.67, 0.34, 1)
        _Rim ("Rim", Color) = (0.83, 1, 0.46, 1)
        _Outline ("Outline", Color) = (0.045, 0.13, 0.025, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.04)) = 0.018
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _Shade, _Rim, _Outline;
            float _OutlineWidth;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            float2 uv : TEXCOORD2;
        };
        Varyings ForwardVertex(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            return output;
        }
        half4 ForwardFragment(Varyings input) : SV_Target
        {
            half3 normal = normalize(input.normalWS);
            Light light = GetMainLight();
            half illumination = dot(normal, light.direction);
            half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
            half3 color = albedo * lerp(_Shade.rgb, half3(1,1,1), smoothstep(0.25, 0.29, illumination));
            half rim = 1 - saturate(dot(normal, GetWorldSpaceNormalizeViewDir(input.positionWS)));
            return half4(lerp(color, _Rim.rgb, smoothstep(0.74, 0.86, rim) * 0.45), 1);
        }
        float4 OutlineVertex(Attributes input) : SV_POSITION
        {
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS += TransformObjectToWorldNormal(input.normalOS) * _OutlineWidth;
            return TransformWorldToHClip(positionWS);
        }
        half4 OutlineFragment() : SV_Target { return _Outline; }
        ENDHLSL
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            ENDHLSL
        }
        Pass
        {
            Name "Toon"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex ForwardVertex
            #pragma fragment ForwardFragment
            ENDHLSL
        }
    }
}
