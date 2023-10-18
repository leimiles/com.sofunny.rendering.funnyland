Shader "SoFunny/Funnyland/FunnyDecal"
{
    Properties
    {
        _BaseMap ("Main Texture", 2D) = "white" { }
        [HDR]_Color ("Color", color) = (1, 1, 1, 1)

        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("SrcBlend", Float) = 5 // 5 = SrcAlpha
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)]_DstBlend ("DstBlend", Float) = 10 // 10 = OneMinusSrcAlpha

        [HideInInspector]_AlphaRemap ("AlphaRemap", vector) = (1, 0, 0, 0)

        [HideInInspector]_StencilRef ("StencilRef", Float) = 0
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp ("StencilComp", Float) = 0 //0 = disable

        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)]_ZTest ("ZTest", Float) = 0 //0 = disable

        [HideInInspector][Enum(UnityEngine.Rendering.CullMode)]_Cull ("Cull", Float) = 1 //1 = Front

        [Toggle(_UnityFogEnable)] _UnityFogEnable ("UnityFogEnable", Float) = 1

        [HideInInspector][Toggle(_SupportOrthographicCamera)] _SupportOrthographicCamera ("SupportOrthographicCamera", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Overlay" "Queue" = "Transparent-499" "DisableBatching" = "True" }

        Pass
        {
            Name "UnlitDecal"
            Tags { "LightMode" = "FunnylandMobileForward" }
            Stencil
            {
                Ref[_StencilRef]
                Comp[_StencilComp]
            }

            Cull[_Cull]
            ZTest[_ZTest]

            ZWrite off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog

            #pragma target 2.0
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #pragma shader_feature_local _UnityFogEnable
            #pragma shader_feature_local_fragment _SupportOrthographicCamera
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct a2v
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 viewRayOS : TEXCOORD1; // xyz: viewRayOS, w: extra copy of positionVS.z
                float4 cameraPosOSAndFogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _BaseMap;
            sampler2D _CameraDepthTexture;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                half2 _AlphaRemap;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _Color)
                    UNITY_DOTS_INSTANCED_PROP(float2, _AlphaRemap)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _Color              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _Color)
                #define _AlphaRemap          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float2 , _AlphaRemap)
            #endif
            

            float4 ComputeScreenPosition(float4 positionCS)
            {
                float4 o = positionCS * 0.5f;
                o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
                o.zw = positionCS.zw;
                return o;
            }

            v2f vert(a2v input)
            {
                v2f o = (v2f)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(input.positionOS);

                o.positionCS = vertexPositionInput.positionCS;


                #if _UnityFogEnable
                    o.cameraPosOSAndFogFactor.a = ComputeFogFactor(o.positionCS.z);
                #else
                    o.cameraPosOSAndFogFactor.a = 0;
                #endif


                //o.screenPos = ComputeScreenPos(o.positionCS);
                o.screenPos = vertexPositionInput.positionNDC;

                float3 viewRay = vertexPositionInput.positionVS;

                o.viewRayOS.w = viewRay.z;
                viewRay *= -1;

                float4x4 ViewToObjectMatrix = mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V);
                o.viewRayOS.xyz = mul((float3x3)ViewToObjectMatrix, viewRay);
                o.cameraPosOSAndFogFactor.xyz = mul(ViewToObjectMatrix, float4(0, 0, 0, 1)).xyz;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                i.viewRayOS.xyz /= i.viewRayOS.w;

                float2 screenSpaceUV = i.screenPos.xy / i.screenPos.w;
                float sceneRawDepth = tex2D(_CameraDepthTexture, screenSpaceUV).r;

                float3 decalSpaceScenePos;

                #if _SupportOrthographicCamera
                    if (unity_OrthoParams.w)
                    {

                        #if defined(UNITY_REVERSED_Z)
                            sceneRawDepth = 1 - sceneRawDepth;
                        #endif

                        float sceneDepthVS = lerp(_ProjectionParams.y, _ProjectionParams.z, sceneRawDepth);

                        float2 viewRayEndPosVS_xy = float2(unity_OrthoParams.xy * (i.screenPos.xy - 0.5) * 2);
                        float4 vposOrtho = float4(viewRayEndPosVS_xy, -sceneDepthVS, 1);
                        float3 wposOrtho = mul(UNITY_MATRIX_I_V, vposOrtho).xyz;

                        decalSpaceScenePos = mul(GetWorldToObjectMatrix(), float4(wposOrtho, 1)).xyz;
                    }
                    else
                    {
                #endif
                float sceneDepthVS = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                decalSpaceScenePos = i.cameraPosOSAndFogFactor.xyz + i.viewRayOS.xyz * sceneDepthVS;

                #if _SupportOrthographicCamera
                }
                #endif

                float2 decalSpaceUV = decalSpaceScenePos.xy + 0.5;

                float shouldClip = 0;
                clip(0.5 - abs(decalSpaceScenePos) - shouldClip);

                float2 uv = decalSpaceUV.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

                half4 col = tex2D(_BaseMap, uv);
                col *= _Color;
                col.a = saturate(col.a * _AlphaRemap.x + _AlphaRemap.y);
                //col.rgb *= lerp(1, col.a, _MulAlphaToRGB);

                #if _UnityFogEnable

                    col.rgb = MixFog(col.rgb, i.cameraPosOSAndFogFactor.a);
                #endif
                return col;
            }
            ENDHLSL
        }
    }
}
