Shader "Vehicle Showcase/Harbor Street Asphalt"
{
    Properties
    {
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = 0.12
        _GrainStrength("Asphalt grain", Range(0,0.3)) = 0.08
        _SeamStrength("Repair seams", Range(0,0.5)) = 0.18
        _WearStrength("Tyre wear", Range(0,0.3)) = 0.10
        _PatchStrength("Resurfacing patches", Range(0,0.4)) = 0.16
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
            half _GrainStrength, _SeamStrength, _WearStrength, _PatchStrength;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            half4 color : COLOR;
            float2 roadUV : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 color : TEXCOORD2;
            half fog : TEXCOORD3;
            float2 roadUV : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings GroundVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = position.positionCS;
            output.positionWS = position.positionWS;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.color = input.color;
            output.roadUV = input.roadUV;
            output.fog = ComputeFogFactor(position.positionCS.z);
            return output;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundVertex
            #pragma fragment GroundFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            float Hash(float2 p)
            {
                // Fractional mixing avoids a transcendental hash for every grain sample.
                p = frac(p * float2(.1031, .11369));
                float mixed = dot(p, p.yx + 19.19);
                return frac((p.x + mixed) * (p.y + mixed) * 17.17);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                float2 blend = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), blend.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1), blend.x), blend.y);
            }
            float Line(float distance, float width, float aa)
            {
                return 1 - smoothstep(max(0, width - aa), width + aa, distance);
            }
            half3 Asphalt(float2 p, float2 world, float corner)
            {
                float footprint = max(length(ddx(world)), length(ddy(world)));
                float aa = max(footprint * .7, .004);
                float grainFade = 1 - smoothstep(.025, .10, footprint);
                float aggregate = (Noise(world * 45) - .5) * grainFade;
                float mottling = (Noise(world * .65) - .5) * .08 + (Noise(world * 2.5) - .5) * .035;
                half3 tint = _BaseColor.rgb * (1 + aggregate * _GrainStrength * 2 + mottling);
                // Repairs follow the street, with varied widths, lengths and spacing.
                float section = floor(p.y / 34);
                float seed = Hash(float2(section, 3));
                float centerX = lerp(-2.9, 2.9, Hash(float2(section, 7)));
                float halfWidth = lerp(.45, 1.75, Hash(float2(section, 13)));
                float halfLength = lerp(3, 9, Hash(float2(section, 19)));
                float edgeX = abs(p.x - centerX) - halfWidth + (Noise(p * 2) - .5) * .035;
                float edgeY = abs(p.y - (section * 34 + 17)) - halfLength;
                float edge = max(edgeX, edgeY);
                float patch = (1 - smoothstep(-aa, aa, edge)) * step(.40, seed);
                tint *= 1 - patch * _PatchStrength;
                float seam = Line(abs(edge), .025, aa) * step(.40, seed);
                // A narrow irregular tar repair, broken into occasional short runs.
                float crackSection = floor(p.y / 75);
                float crackY = p.y - (crackSection * 75 + 33);
                float crackX = p.x - lerp(-2, 2, Hash(float2(crackSection, 31)));
                float crack = Line(abs(crackY + sin(p.x * 2) * .045), .018, aa)
                    * (1 - smoothstep(1.3, 2.2, abs(crackX))) * step(.65, Hash(float2(crackSection, 41)));
                tint *= 1 - max(seam, crack) * _SeamStrength;
                // Faint wheel paths become more apparent around the braking corners.
                float tracks = max(1 - smoothstep(.13, .43, abs(abs(p.x) - 1.8)),
                    1 - smoothstep(.13, .43, abs(abs(p.x) - 3.4)));
                tint *= 1 - tracks * _WearStrength * (.2 + corner * .8) * (.5 + Noise(float2(p.y * .15, 5)) * .5);
                float edgeGrime = smoothstep(4.3, 5.25, abs(p.x));
                tint *= 1 - edgeGrime * .1 * (.5 + Noise(p * .8) * .5);
                return tint;
            }
            half4 GroundFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = Asphalt(input.roadUV, input.positionWS.xz, input.color.r);
                surface.specular = half3(0.5,0.5,0.5);
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0,0,1);
                surface.occlusion = 1;
                surface.alpha = 1;
                InputData data = (InputData)0;
                data.positionWS = input.positionWS;
                data.normalWS = NormalizeNormalPerPixel(input.normalWS);
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    data.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                data.bakedGI = SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1,1,1,1);
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    data.vertexLighting = VertexLighting(input.positionWS, data.normalWS);
                #endif
                half4 color = UniversalFragmentPBR(data, surface);
                color.rgb = MixFog(color.rgb, input.fog);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            float4 ShadowVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            half4 ShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            half4 DepthFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GroundVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalsFragment(Varyings input) : SV_Target
            {
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = PackNormalOctQuadEncode(normalWS);
                    return half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
