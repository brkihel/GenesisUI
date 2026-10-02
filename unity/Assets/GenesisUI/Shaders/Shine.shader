// GenesisUI/Shine: one slanted band of light crossing a piece from left to right, once (a recipe
// that has just become craftable). Additive, behind the piece's text. C# sets _Size (the piece in
// canvas units) and _Progress (0..1); the band fades in and out on its own over the crossing.
Shader "GenesisUI/Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Color ("Light colour", Color) = (1.0, 0.84, 0.55, 1)
        _Size ("Piece size (width, height)", Vector) = (300, 54, 0, 0)
        _Progress ("Progress (0..1)", Range(0, 1)) = 0.5
        _Width ("Band half-width (canvas units)", Float) = 16
        _Slant ("Slant (x per y)", Float) = 0.55
        _Strength ("Strength", Range(0, 2)) = 0.5
        _EdgeFade ("Fade at the edges (canvas units)", Float) = 6

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
        Blend One One
        ColorMask [_ColorMask]

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; };

            fixed4 _Color;
            float4 _Size, _ClipRect;
            float _Progress, _Width, _Slant, _Strength, _EdgeFade;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float w = _Size.x, h = max(1.0, _Size.y);
                float x = IN.uv.x * w;
                float y = IN.uv.y * h;
                // The band runs along a slanted line; its centre travels from before the left edge to past the right.
                float u = x - (y - h * 0.5) * _Slant;
                float reach = h * abs(_Slant) * 0.5 + _Width * 3.0;
                float centre = lerp(-reach, w + reach, _Progress);
                float d = u - centre;
                float band = exp(-d * d / (2.0 * _Width * _Width));
                float core = exp(-d * d / (2.0 * 2.0 * 2.0)) * 0.6;
                float life = sin(saturate(_Progress) * 3.14159);
                float edge = smoothstep(0.0, _EdgeFade, x) * smoothstep(0.0, _EdgeFade, w - x) *
                             smoothstep(0.0, _EdgeFade, y) * smoothstep(0.0, _EdgeFade, h - y);
                float light = (band + core) * life * edge * _Strength;

                float4 color = float4(_Color.rgb * light, 1.0);
                color.rgb *= IN.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                color.rgb *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                return color;
            }
        ENDCG
        }
    }
}
