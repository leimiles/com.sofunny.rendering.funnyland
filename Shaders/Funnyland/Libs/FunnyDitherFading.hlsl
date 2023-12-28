#ifndef FUNNYDITHERFADING_HLSL_INCLUDED
#define FUNNYDITHERFADING_HLSL_INCLUDED

float DitherMatrix(float4 input, float4 positionSS)
{
    float2 uv = positionSS.xy / positionSS.w * _ScreenParams.xy;
    float DITHER_THRESHOLDS[16] = {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    float output = input - DITHER_THRESHOLDS[index];
    return output;
}

float Remap(float value, float inputMin, float inputMax, float outputMin, float outputMax)
{
    return outputMin + (outputMax - outputMin) * ((value - inputMin) / (inputMax - inputMin));
}

#endif