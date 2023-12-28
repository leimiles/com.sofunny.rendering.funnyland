#ifndef FUNNYDITHERFADING_HLSL_INCLUDED
#define FUNNYDITHERFADING_HLSL_INCLUDED

float DitherMatrix(float input, float4 positionSS)
{
    const float DITHER_THRESHOLDS[16] = {
        0.05882352, 0.52941176, 0.17647059, 0.64705882,
        0.76470588, 0.29411765, 0.88235294, 0.41176471,
        0.23529412, 0.70588235, 0.11764706, 0.58823529,
        0.94117647, 0.47058824, 0.82352941, 0.35294118
    };
    float2 uv = positionSS.xy / positionSS.w * _ScreenParams.xy;
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    float output = input - DITHER_THRESHOLDS[index];
    return output;
}

float Remap(float value, float inputMin, float inputMax, float outputMin, float outputMax)
{
    return outputMin + (outputMax - outputMin) * ((value - inputMin) / (inputMax - inputMin));
}

#endif