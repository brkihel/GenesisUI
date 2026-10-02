// GenesisUI/Edge: a soft light hugging the border of a piece (a slot, a button, a status tile).
// Additive, procedural (no texture). The quad is larger than the piece (C# adds a margin) so the
// halo can leave the frame; C# sets _Rect (centered piece bounds in local design units) and _QuadSize.
// The distance field uses UVs: Canvas batching may transform vertex positions into another space.
// _Intensity, and optionally _Progress (< 1: only that share of the border is lit, clockwise from
// the top centre, with a bright head — a progress bar drawn in light) and _Pulse (breathing).
Shader "GenesisUI/Edge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Color ("Light colour", Color) = (1.0, 0.82, 0.52, 1)
        _Rect ("Centered piece rect", Vector) = (-30, -30, 30, 30)
        _QuadSize ("Halo quad size", Vector) = (84, 84, 0, 0)
        _Inset ("Where the light sits, inside the border (canvas units)", Float) = 3
        _Radius ("Corner radius (canvas units)", Float) = 6
        _Halo ("Halo width (canvas units)", Float) = 5
        _Line ("Thin line strength", Range(0, 2)) = 0.5
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Progress ("Progress (1 = whole border)", Range(0, 1)) = 1
        _Orbit ("Moving border segment", Float) = 0
        _Phase ("Orbit phase", Float) = 0
        _Pulse ("Breathing amount", Range(0, 1)) = 0
        _PulseSpeed ("Breathing speed", Float) = 3

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
            float4 _Rect, _ClipRect, _QuadSize;
            float _Inset, _Radius, _Halo, _Line, _Intensity, _Progress, _Pulse, _PulseSpeed, _Orbit, _Phase;

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
                float2 c = (_Rect.xy + _Rect.zw) * 0.5;
                float2 hs = max(float2(1, 1), (_Rect.zw - _Rect.xy) * 0.5) - _Inset;
                float2 p = (IN.uv - 0.5) * _QuadSize.xy - c;
                // Signed distance to the rounded border (0 on it, negative inside).
                float r = min(_Radius, min(hs.x, hs.y));
                float2 q = abs(p) - (hs - r);
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;

                float halo = exp(-d * d / (2.0 * _Halo * _Halo)) * (d > 0.0 ? 0.8 : 0.45);
                float line_ = exp(-d * d / 1.6) * _Line;
                float light = halo + line_;

                // Progress: clockwise from the top centre (0..1 around the border).
                if (_Orbit > 0.5)
                {
                    float u = frac(atan2(p.x, p.y) / 6.28318 + 1.0);
                    float distance_ = frac(u - frac(_Phase) + 0.5) - 0.5;
                    light *= 0.08 + 1.7 * exp(-distance_ * distance_ / (2.0 * 0.05 * 0.05));
                }
                else if (_Progress < 0.999)
                {
                    float u = frac(atan2(p.x, p.y) / 6.28318 + 1.0);
                    float lit = smoothstep(_Progress + 0.006, _Progress - 0.006, u);
                    float du = u - _Progress;
                    float head = exp(-du * du / (2.0 * 0.012 * 0.012)) * step(0.001, _Progress) * 1.6;
                    light *= lit * 0.8 + head;
                }

                float breath = 1.0 - _Pulse * 0.5 * (1.0 - cos(_Time.y * _PulseSpeed));
                float4 color = float4(_Color.rgb * light * _Intensity * breath, 1.0);
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
