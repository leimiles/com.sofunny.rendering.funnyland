Shader "Hidden/SoFunny/Funnyland/PostProcessing/Histogram"
{
    HLSLINCLUDE

        //#pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // #if SHADER_API_GLES3
        //     #define HISTOGRAM_BINS 128
        // #else
        //     #define HISTOGRAM_BINS 256
        // #endif
    
        #define HISTOGRAM_BINS 256
        #define RGBHISTOGRAM_BINS 256 * 3// Histogram.m_NumBins

        StructuredBuffer<uint> _HistogramBuffer;
        #define _Params float2(256, 512)
        
        #if SHADER_API_GLES
        struct Attributes
        {
            float4 positionOS       : POSITION;
            float2 uv               : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        #else
        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        #endif
        
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            float maxValue : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        
        float FindMaxHistogramValue()
        {
            uint maxValue = 0u;

            #ifdef _HISTOGRAM_RGB
            UNITY_UNROLL
            for (uint j = 0; j < RGBHISTOGRAM_BINS; j++)
            {
                uint h = _HistogramBuffer[j];
                maxValue = max(maxValue, h);
            }
        
            return float(maxValue);
            #endif

            UNITY_UNROLL
            for (uint w = 0; w < HISTOGRAM_BINS; w++)
            {
                uint h = _HistogramBuffer[w];
                maxValue = max(maxValue, h);
            }
        
            return float(maxValue);
        }
        
        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        
        #if SHADER_API_GLES
            float4 pos = input.positionOS;
            float2 uv  = input.uv;
        #else
            float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
        #endif
            // pos.xyw = pos.xyw * 0.5f ;

            #if UNITY_UV_STARTS_AT_TOP
            pos = pos * 0.5f + float4(1,1,1,1);
            #else
            pos.y = -pos.y;
            pos = pos * 0.5f + float4(1,-1,1,1);
            uv.y = 1.0 - uv.y;
            #endif
            
            
            
            output.positionCS = pos;

            output.texcoord   = uv;
            // output.texcoord.x = output.texcoord.x * 2 - 1.0f;
            // output.texcoord.y = output.texcoord.y * 2;
            #if SHADER_API_GLES3 // No texture loopup in VS on GLES3/Android
                output.maxValue = 0;
            #else
                output.maxValue = _Params.y / FindMaxHistogramValue();
            #endif
            return output;
        }
        
        float4 Frag(Varyings i) : SV_Target
        {
        #if SHADER_API_GLES3
            float maxValue = _Params.y / FindMaxHistogramValue();
        #else
            float maxValue = i.maxValue;
        #endif
        
            const float kBinsMinusOne = HISTOGRAM_BINS - 1.0;
            float remapI = i.texcoord.x * kBinsMinusOne;
            uint index = floor(remapI);
            float delta = frac(remapI);
            
            #ifdef _HISTOGRAM_RGB
                float3 v1_RGB = float3(_HistogramBuffer[index], _HistogramBuffer[index + 256], _HistogramBuffer[index + 256 * 2]) * maxValue;
                float3 v2_RGB = float3(_HistogramBuffer[min(index + 1, kBinsMinusOne)], _HistogramBuffer[min(index + 1, kBinsMinusOne) + 256], _HistogramBuffer[min(index + 1, kBinsMinusOne) + 256 * 2]) * maxValue;
                float3 h_RGB = v1_RGB * (1.0 - delta) + v2_RGB * delta;
                uint y_RGB = (uint)round(i.texcoord.y * _Params.y);
                float3 color_RGB = (0.0).xxx;
                float3 fill_RGB = step(y_RGB, h_RGB);
                color_RGB = lerp(color_RGB, (1.0).xxx, fill_RGB);
                float a = 1.0 - step(fill_RGB.x, 0.01) * step(fill_RGB.y, 0.01) * step(fill_RGB.z, 0.01);
                return float4(color_RGB, a + 0.3);
            #endif
            
            float v1 = float(_HistogramBuffer[index]) * maxValue;
            float v2 = float(_HistogramBuffer[min(index + 1, kBinsMinusOne)]) * maxValue;
            float h = v1 * (1.0 - delta) + v2 * delta;
            uint y = (uint)round(i.texcoord.y * _Params.y);
        
            float3 color = (0.0).xxx;
            float fill = step(y, h);
            color = lerp(color, (1.0).xxx, fill);
            return float4(color, fill + 0.3);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
                #pragma enable_d3d11_debug_symbols  
                #pragma multi_compile _ _HISTOGRAM_RGB

            ENDHLSL
        }
    }
}
