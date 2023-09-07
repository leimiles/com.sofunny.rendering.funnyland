Shader "Hidden/SoFunny/Funnyland/UIBackgroundBlur"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float4 _BlitTexture_TexelSize;

    TEXTURE2D_X(_SourceTexLowMip);
    float4 _SourceTexLowMip_TexelSize;

    half _BlurRadius = 0;
    
    half4 DualKawaseBlurDownSample(Varyings input): SV_Target
    {
        float2 texelSize = _BlitTexture_TexelSize.xy;
        texelSize *= 0.5;
        half2 uv = input.texcoord;

        half blurOffset = _BlurRadius;
        half4 sum = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * 4;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - texelSize * float2(1 + blurOffset, 1 + blurOffset));
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(1 + blurOffset, 1 + blurOffset));
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize.x, -texelSize.y) * float2(1 + blurOffset, 1 + blurOffset));
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize.x, -texelSize.y) * float2(1 + blurOffset, 1 + blurOffset));

        return sum * 0.125;
    }

    half4 DualKawaseBlurUpSample(Varyings input): SV_Target
    {
        float2 texelSize = _BlitTexture_TexelSize.xy;
        texelSize *= 0.5;
        half2 blurOffset = float2(1 + _BlurRadius, 1 + _BlurRadius);
        half2 uv = input.texcoord;

        half4 sum = 0;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x * 2, 0) * blurOffset);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(-texelSize.x, texelSize.y) * blurOffset) * 2;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0, texelSize.y * 2) * blurOffset);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + texelSize * blurOffset) * 2;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize.x * 2, 0) * blurOffset);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize.x, -texelSize.y) * blurOffset) * 2;
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0, -texelSize.y * 2) * blurOffset);
        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - texelSize * blurOffset) * 2;

        return sum * 0.0833;
    }
    ENDHLSL

    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Blit 0"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBilinear
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseBlur DownSample 1"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseBlurDownSample
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseBlur UpSample 2"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseBlurUpSample
            ENDHLSL
        }
    }
}