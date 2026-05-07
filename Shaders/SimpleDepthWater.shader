Shader "Custom/SimpleDepthWater"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.4, 0.9, 1.0, 0.0)
        _DeepColor("Deep Color", Color) = (0.01, 0.1, 0.2, 1.0)
        _WaterDensity("Water Density", Float) = 0.005
        
        [Header(Foam Settings)]
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDepth("Foam Depth", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { 
                float4 positionCS : SV_POSITION; 
                float4 screenPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _WaterDensity;
                half4 _FoamColor;
                half _FoamDepth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 1. DEPTH SAMPLE
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float groundZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterZ = input.screenPos.w;
                
                half depthThickness = (half)max(0, groundZ - waterZ);
                
                // 2. DENSITY LOGIC (Water Fog)
                half fade = (half)saturate(1.0 - exp(-depthThickness * _WaterDensity));
                half4 waterColor = lerp(_ShallowColor, _DeepColor, fade);

                // 3. CLEAN FOAM LOGIC (No Noise)
                half finalFoam = saturate(1.0 - (depthThickness / _FoamDepth));
                
                // Final Color Mix
                return lerp(waterColor, _FoamColor, finalFoam);
            }
            ENDHLSL
        }
    }
}
