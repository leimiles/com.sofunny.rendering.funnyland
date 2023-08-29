Shader "Hidden/Funny Render Pipeline/Effects"
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

    #pragma exclude_renderers gles gles3 glcore
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
    CBUFFER_END

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

            Stencil
            {
                Ref 4
                Comp NotEqual
            }

            //ZTest LEqual


            Cull Front
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
                input.positionOS += input.normalOS * _OutlineWidth;
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
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
