Shader "Hidden/PostProcessing/Histogram"
{
    HLSLINCLUDE

        //#pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #if SHADER_API_GLES3
            #define HISTOGRAM_BINS 128
        #else
            #define HISTOGRAM_BINS 256
        #endif

        struct VaryingsHistogram
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            float maxValue : TEXCOORD1;
        };

        struct AttributesDefault
        {
            float3 vertex : POSITION;
        };
    
        StructuredBuffer<uint> _HistogramBuffer;
        #define _Params float2(256, 512)
        float FindMaxHistogramValue()
        {
            uint maxValue = 0u;

            UNITY_UNROLL
            for (uint i = 0; i < HISTOGRAM_BINS; i++)
            {
                uint h = _HistogramBuffer[i];
                maxValue = max(maxValue, h);
            }

            return float(maxValue);
        }

        VaryingsHistogram Vert(AttributesDefault v)
        {
            VaryingsHistogram o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.vertex = o.vertex * 0.5f + float4(1, 1,0.5,1);
            o.vertex.xy = float2(o.vertex.x, o.vertex.y * _ProjectionParams.x);
            o.texcoord = (v.vertex.xy + 1) * 0.5;

        #if UNITY_UV_STARTS_AT_TOP
            o.vertex.y = -o.vertex.y;
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

        #if SHADER_API_GLES3 // No texture loopup in VS on GLES3/Android
            o.maxValue = 0;
        #else
            o.maxValue = _Params.y / FindMaxHistogramValue();
        #endif

            return o;
        }

        float4 Frag(VaryingsHistogram i) : SV_Target
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
            float v1 = float(_HistogramBuffer[index]) * maxValue;
            float v2 = float(_HistogramBuffer[min(index + 1, kBinsMinusOne)]) * maxValue;
            float h = v1 * (1.0 - delta) + v2 * delta;
            uint y = (uint)round(i.texcoord.y * _Params.y);

            float3 color = (0.0).xxx;
            float fill = step(y, h);
            color = lerp(color, (1.0).xxx, fill);
            return float4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
