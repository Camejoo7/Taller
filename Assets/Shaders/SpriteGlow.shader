// Brillo aditivo para sprites: suma luz sobre lo que hay detrás en vez de
// taparlo. Es lo que hace que un farol, un cartel de neón o una pantalla se
// vean "prendidos" sin usar luces 2D (agregar un Light2D al Stage 2 manda la
// escena a negro, ver ROADMAP).
//
// El color del SpriteRenderer tiñe el brillo y su alfa lo atenúa. _Intensity
// pasa de 1 para que el bloom (si la cámara tiene post-proceso) lo agarre.
//
// Un solo pase, con LightMode Universal2D: el renderer 2D dibuja los pases
// Universal2D y también los que no tienen LightMode, así que con dos pases
// el brillo se sumaría dos veces.
Shader "CodeBreak/Sprite Glow (Aditivo)"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Intensity ("Intensidad", Float) = 1
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                // En Unity 6 el color del SpriteRenderer no viene en el color
                // del vértice: llega en unity_SpriteColor (como en los shaders
                // de sprite de URP). Sin esto todo brilla blanco y al máximo.
                o.color = v.color * unity_SpriteColor;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                c.rgb *= _Intensity;
                return c;
            }
            ENDHLSL
        }
    }
}
