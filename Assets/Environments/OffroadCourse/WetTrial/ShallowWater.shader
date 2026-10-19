Shader "Vehicle Showcase/Shallow Trial Water"
{
    Properties
    {
        _BaseColor ("Water tint", Color) = (.12,.32,.32,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float waves = sin(i.world.x * 3.5 + i.world.z * 4.1 + _Time.y * 1.3)
                    + .4 * sin(i.world.x * 7.1 - i.world.z * 2.3 - _Time.y * 1.8);
                half glint = pow(saturate(waves * .5), 12) * .24;
                return half4(_BaseColor.rgb * (.94 + waves * .045) + glint, 1);
            }
            ENDHLSL
        }
    }
}
