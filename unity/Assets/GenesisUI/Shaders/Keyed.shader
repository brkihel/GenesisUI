// GenesisUI/Keyed: compose preview colour independently of the game shader's alpha (D-039).
// The source has no MSAA and uses point filtering: remove the key from each texel BEFORE
// bilinear interpolation, then interpolate premultiplied colour and coverage. Filtering the
// magenta background first leaves a pink silhouette. Alpha blended like UI/Default otherwise.
Shader "GenesisUI/Keyed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Preview", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Key ("Key colour", Color) = (1, 0, 1, 1)
        _Tolerance ("Key tolerance", Range(0, 1)) = 0.12
        _Softness ("Edge softness", Range(0.001, 1)) = 0.18

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _Key;
            float _Tolerance, _Softness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float4 Decode(float2 uv)
            {
                float4 c = tex2D(_MainTex, uv);
                float d = distance(c.rgb, _Key.rgb);
                float a = smoothstep(_Tolerance, _Tolerance + _Softness, d);
                return float4(saturate(c.rgb - _Key.rgb * (1.0 - a)), a);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 texel = abs(_MainTex_TexelSize.xy);
                float2 pixel = IN.uv * _MainTex_TexelSize.zw - 0.5;
                float2 uv = (floor(pixel) + 0.5) * texel;
                float2 weight = frac(pixel);
                float4 top = lerp(Decode(uv), Decode(uv + float2(texel.x, 0)), weight.x);
                float4 bottom = lerp(Decode(uv + float2(0, texel.y)), Decode(uv + texel), weight.x);
                float4 mixed = lerp(top, bottom, weight.y);
                // UI/Default's blend expects straight colour; divide only after filtering.
                float3 rgb = saturate(mixed.rgb / max(mixed.a, 0.0001));
                fixed4 color = fixed4(rgb, mixed.a) * IN.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                return color;
            }
        ENDCG
        }
    }
}
