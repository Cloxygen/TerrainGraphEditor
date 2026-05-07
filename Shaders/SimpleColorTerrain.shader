Shader "Custom/SimpleColorTerrain"
{
    Properties
    {
        [Header(Colors)]
        _SandColor ("Sand Color", Color) = (0.76, 0.70, 0.50, 1.0)
        _GrassColor ("Grass Color", Color) = (0.23, 0.38, 0.17, 1.0)
        _RockColor ("Rock Color", Color) = (0.35, 0.33, 0.30, 1.0)
        _SnowColor ("Snow Color", Color) = (0.90, 0.95, 1.0, 1.0)

        [Header(Levels)]
        _BeachLevel ("Beach Level", Float) = 2.0
        _MountainLevel ("Mountain Level", Float) = 25.0
        _SnowLevel ("Snow Level", Float) = 40.0
        _BlendRange ("Blend Range", Float) = 2.0

        [Header(Slope)]
        _SlopeThreshold ("Slope Threshold", Range(0, 1)) = 0.6
        _SlopeBlend ("Slope Blend", Range(0, 1)) = 0.2

        [Header(Standard PBR)]
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2

        // Compatibility properties for standard URP passes
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _BaseMap("BaseMap", 2D) = "white" {}
        [HideInInspector] _BaseColor("BaseColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Surface("__surface", Float) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry" 
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Standard multi-compiles to support all URP lighting features
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS               : SV_POSITION;
                float3 positionWS               : TEXCOORD0;
                float3 normalWS                 : TEXCOORD1;
                float4 tangentWS                : TEXCOORD2; 
                float4 shadowCoord              : TEXCOORD3;
                float2 uv                       : TEXCOORD4;
                float2 staticLightmapUV         : TEXCOORD5;
                float  fogFactor                : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SandColor;
                float4 _GrassColor;
                float4 _RockColor;
                float4 _SnowColor;
                float _BeachLevel;
                float _MountainLevel;
                float _SnowLevel;
                float _BlendRange;
                float _SlopeThreshold;
                float _SlopeBlend;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                output.staticLightmapUV = input.lightmapUV;
                
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // --- BIOME BLENDING ---
                float height = input.positionWS.y;
                float3 normalWS = normalize(input.normalWS);
                
                float sandAmount = 1.0 - smoothstep(_BeachLevel - _BlendRange, _BeachLevel + _BlendRange, height);
                float4 color = lerp(_GrassColor, _SandColor, sandAmount);
                color = lerp(color, _RockColor, smoothstep(_MountainLevel - _BlendRange, _MountainLevel + _BlendRange, height));
                color = lerp(color, _SnowColor, smoothstep(_SnowLevel - _BlendRange, _SnowLevel + _BlendRange, height)); 
                float slopeRock = smoothstep(_SlopeThreshold - _SlopeBlend, _SlopeThreshold + _SlopeBlend, 1.0 - normalWS.y);
                slopeRock *= (1.0 - sandAmount); // Prevent slope rock below sea level
                color = lerp(color, _RockColor, slopeRock);

                // --- OFFICIAL URP INITIALIZATION ---
                // We use Unity's built-in initialization functions to handle all the complex lighting logic
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = color.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = 1.0;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = float3(0, 0, 1);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS; // Use our blended normal
                inputData.viewDirectionWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                
                // Explicitly use Unity's world-to-shadow functions for high-precision fragment-side resolution
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    inputData.shadowCoord = ComputeScreenPos(input.positionCS);
                #else
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif

                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = 0;

                // Let the official PBR engine handle all lighting, multi-lights, shadows, and reflections
                float4 combinedColor = UniversalFragmentPBR(inputData, surfaceData);
                
                combinedColor.rgb = MixFog(combinedColor.rgb, inputData.fogCoord);
                return combinedColor;
            }
            ENDHLSL
        }

        // Official Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Official Depth Only Pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Official Depth Normals Pass
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
