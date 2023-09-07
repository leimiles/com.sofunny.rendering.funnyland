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

    half4 GaussianBlurH(Varyings input) : SV_Target
    {
        float texelSize = _BlitTexture_TexelSize.x * 2.0;
        float2 uv = input.texcoord;

        // 9-tap gaussian blur on the downsampled source
        half3 c0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 4.0, 0.0));
        half3 c1 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 3.0, 0.0));
        half3 c2 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 2.0, 0.0));
        half3 c3 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 1.0, 0.0));
        half3 c4 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        half3 c5 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 1.0, 0.0));
        half3 c6 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 2.0, 0.0));
        half3 c7 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 3.0, 0.0));
        half3 c8 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 4.0, 0.0));

        half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
            + c4 * 0.22702703
            + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

        return half4(color, 1);
    }

    half4 GaussianBlurV(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float texelSize = _BlitTexture_TexelSize.y;
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
        half3 c0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0.0, texelSize * 3.23076923));
        half3 c1 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0.0, texelSize * 1.38461538));
        half3 c2 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        half3 c3 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0, texelSize * 1.38461538));
        half3 c4 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0, texelSize * 3.23076923));

        half3 color = c0 * 0.07027027 + c1 * 0.31621622
            + c2 * 0.22702703
            + c3 * 0.31621622 + c4 * 0.07027027;

        return half4(color, 1);
    }

    half4 GaussianUpsample(Varyings input) : SV_Target
    {
        half2 uv = input.texcoord;
        half3 highMip = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
        half3 lowMip = SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv).rgb;
        half3 color = lerp(highMip, lowMip, 0.7);
        return half4(color, 1);
    }


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
            Name "GaussianBlurH 1"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlurH
            ENDHLSL
        }

        Pass
        {
            Name "GaussianBlurV 2"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianBlurV
            ENDHLSL
        }

        Pass
        {
            Name "GaussianUpsample 3"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GaussianUpsample
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseBlur DownSample 4"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseBlurDownSample
            ENDHLSL
        }

        Pass
        {
            Name "DualKawaseBlur UpSample 5"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DualKawaseBlurUpSample
            ENDHLSL
        }
    }
}