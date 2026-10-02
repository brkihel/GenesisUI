// GenesisUI/Backdrop: what dims the world behind an open window. Instead of a flat black veil, a
// warm, dark vignette — lighter in the middle, where the window is, deeper at the corners — with a
// faint film grain so the dark does not band. Alpha blended, one full-screen quad, no texture.
// The CanvasGroup fade reaches it through the vertex alpha.
Shader "GenesisUI/Backdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Tint ("Shadow colour", Color) = (0.055, 0.034, 0.016, 1)
        _Centre ("Darkness in the middle", Range(0, 1)) = 0.30
        _Corner ("Darkness at the corners", Range(0, 1)) = 0.66
        _Inner ("Vignette starts (0 centre .. 1 corner)", Range(0, 1)) = 0.3
        _Grain ("Grain", Range(0, 0.1)) = 0.025
        _GrainSize ("Grain size (pixels)", Float) = 1.5

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
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 screen : TEXCOORD1; };

            fixed4 _Tint;
            float _Centre, _Corner, _Inner, _Grain, _GrainSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screen = ComputeScreenPos(o.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Distance from the centre, corrected for the aspect ratio: 0 in the middle, 1 at a corner.
                float aspect = _ScreenParams.x / max(1.0, _ScreenParams.y);
                float2 q = (IN.uv - 0.5) * float2(aspect, 1.0);
                float r = length(q) / (0.5 * sqrt(aspect * aspect + 1.0));
                float a = lerp(_Centre, _Corner, smoothstep(_Inner, 1.0, r));
                // Film grain, re-rolled about twelve times a second.
                float2 px = floor(IN.screen.xy / max(0.0001, IN.screen.w) * _ScreenParams.xy / _GrainSize);
                float grain = (Hash(px + floor(_Time.y * 12.0) * 17.0) - 0.5) * _Grain;
                return fixed4(_Tint.rgb, saturate(a + grain) * IN.color.a);
            }
        ENDCG
        }
    }
}
