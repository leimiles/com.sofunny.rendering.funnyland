Shader "Hidden/SoFunny/Funnyland/FunnyEffects"
{
    Properties
    {
        [HDR]_Color ("Color", Color) = (1, 0, 0, 0)
        [HideInInspector][PerRendererData]_AttackedColorIntensity ("Attacked Color Intensity", Range(0.0, 1.0)) = 1.0
        [HideInInspector][PerRendererData]_OccludeeColorIntensity ("Occludee Color Intensity", Range(0.0, 1.0)) = 1.0
        [HideInInspector][HDR][PerRendererData]_OccludeeColor ("Occludee Color", Color) = (1, 0, 0, 1)
        [HideInInspector][PerRendererData]_OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.008
        [HideInInspector][HDR][PerRendererData]_OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
    }

    // used for all passes
    HLSLINCLUDE

    //#pragma exclude_renderers gles gles3 glcore
    #pragma target 4.5
    #pragma multi_compile _ DOTS_INSTANCING_ON
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    CBUFFER_START(UnityPerMaterial)
        half4 _Color;
        half _AttackedColorIntensity;
        half _OccludeeColorIntensity;
        half4 _OccludeeColor;
        half _OutlineWidth;
        half4 _OutlineColor;
        float4 _SelectOutlineTex_TexelSize;
    CBUFFER_END
    TEXTURE2D(_SelectOutlineTex);                SAMPLER(sampler_SelectOutlineTex);
    
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Overlay" "Queue" = "Overlay" }

        Pass
        {
            Blend One OneMinusSrcAlpha
            Name "Attacked"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            struct attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };
            
            struct varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
            };

            void fresnelEffect(half3 Normal, half3 ViewDir, half Power, out half Out)
            {
                Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }

            varyings vert(attributes input)
            {
                varyings o = (varyings)0;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS);
                o.positionCS = vpi.positionCS;
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS);
                o.normalWS = vni.normalWS;
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(vpi.positionWS);
                return o;
            }

            half4 frag(varyings i) : SV_Target
            {
                _Color = _Color * _AttackedColorIntensity;
                return half4(_Color.rgb, _AttackedColorIntensity);
            }
            ENDHLSL
        }
        Pass
        {
            Blend One Zero
            Name "Occludee"

            Stencil
            {
                Ref 3
                Comp Equal
            }

            ZTest Greater

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            struct attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };



            struct varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
            };

            varyings vert(attributes input)
            {
                varyings o = (varyings)0;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS);
                o.positionCS = vpi.positionCS;
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS);
                o.normalWS = vni.normalWS;
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(vpi.positionWS);
                return o;
            }

            half4 frag(varyings i) : SV_Target
            {
                _OccludeeColor = _OccludeeColor * _OccludeeColorIntensity;
                return half4(_OccludeeColor.rgb, _OccludeeColorIntensity);
            }
            ENDHLSL
        }

        Pass
        {
            //Blend One Zero
            Name "Outline"

            Cull Back
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            struct attributes
            {
                float3 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };


            struct varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
            };


            varyings vert(attributes input)
            {
                varyings o = (varyings)0;
                // input.positionOS += input.normalOS * _OutlineWidth;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS);
                o.positionCS = vpi.positionCS;
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS);
                o.normalWS = vni.normalWS;
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(vpi.positionWS);
                return o;
            }

            half4 frag(varyings i) : SV_Target
            {
                //_OutlineColor = _OutlineColor * _OccludeeColorIntensity;
                //return half4(_OutlineColor.rgb, _OccludeeColorIntensity);
                //return _OutlineColor;
                return 1;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ScreenOutline"

            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
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
                float2 texcoord[9]   : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
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

                output.positionCS = pos;
                output.texcoord[0] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(-1, -1);
                output.texcoord[1] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(0, -1);
                output.texcoord[2] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(1, -1);
                output.texcoord[3] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(-1, 0);
                output.texcoord[4] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(0, 0);
                output.texcoord[5] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(1, 0);
                output.texcoord[6] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(-1, 1);
                output.texcoord[7] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(0, 1);
                output.texcoord[8] = uv + _SelectOutlineTex_TexelSize.xy * _OutlineWidth * float2(1, 1);
                return output;
            }

            half Sobel(Varyings o)
            {
                const half GX[9] = {
                   -1, -2, -1,
                    0, 0, 0,
                    1, 2, 1
                };
                const half GY[9] = {
                   -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };
                half texColor;
                half gX;
                half gY;
                for(int i = 0; i < 9; i++)
                {
                    texColor = SAMPLE_TEXTURE2D(_SelectOutlineTex,sampler_SelectOutlineTex, o.texcoord[i]).r;
                    gX += texColor * GX[i];
                    gY += texColor * GY[i];
                }
                return 1 - abs(gX) - abs(gY);
            }
            
            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half diff = saturate(1.0 - Sobel(i));
                half3 color = _OutlineColor * diff;
                return half4(color, diff);
            }
            ENDHLSL
        }
    }
}
