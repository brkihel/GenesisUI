// GenesisUI/Burn: the loss of a resource bar drawn as light (D-033). Additive, over the bar.
// The quad covers the bar plus a margin (_Pad) so the halo can leak past the frame. C# sets
// _Edge (the current value, 0..1 along the bar), _Trail (the delayed value), _Size (the bar's
// length and thickness in canvas units) and _Heat (0..1, how fresh the loss is; 0 hides it).
// A white-hot core sits at the edge, an orange halo spreads around it, a cooling tail runs
// towards the trail, and a few embers rise and fade. Everything is procedural: no texture.
Shader "GenesisUI/Burn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Core ("Core colour", Color) = (1.0, 0.96, 0.86, 1)
        _Halo ("Halo colour", Color) = (1.0, 0.55, 0.18, 1)
        _Tail ("Tail colour", Color) = (1.0, 0.45, 0.10, 1)
        _Edge ("Edge (0..1)", Range(0, 1)) = 0.5
        _Trail ("Trail (0..1)", Range(0, 1)) = 0.7
        _Heat ("Heat (0..1)", Range(0, 1)) = 1
        _Size ("Bar size (length, thickness)", Vector) = (360, 14, 0, 0)
        _Pad ("Margin around the bar (canvas units)", Float) = 18
        _Vertical ("Vertical bar (fills upwards)", Float) = 0
        _CoreWidth ("Core width", Float) = 1.6
        _HaloWidth ("Halo width", Float) = 9
        _Embers ("Ember amount", Range(0, 1)) = 1

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

            fixed4 _Color, _Core, _Halo, _Tail;
            float _Edge, _Trail, _Heat, _Pad, _Vertical, _CoreWidth, _HaloWidth, _Embers;
            float4 _Size;
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

            float Hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Position in canvas units relative to the bar: a = along (0 at the empty end), c = across (0 at the centre).
                float2 full = float2(_Size.x + 2.0 * _Pad, _Size.y + 2.0 * _Pad);
                float2 p = _Vertical > 0.5 ? float2(IN.uv.y * full.x, (IN.uv.x - 0.5) * full.y)
                                           : float2(IN.uv.x * full.x, (IN.uv.y - 0.5) * full.y);
                float a = p.x - _Pad;
                float c = p.y;
                float edge = _Edge * _Size.x;
                float trail = max(_Trail, _Edge) * _Size.x;
                float half = _Size.y * 0.5;
                float t = _Time.y;

                float flicker = 0.85 + 0.15 * sin(t * 23.0 + Hash(floor(t * 12.0)) * 6.28);
                float d = a - edge;
                float inside = step(abs(c), half + 0.5);
                float core = exp(-d * d / (2.0 * _CoreWidth * _CoreWidth)) * inside;
                float halo = exp(-d * d / (2.0 * _HaloWidth * _HaloWidth) - c * c / (2.0 * pow(_Size.y * 0.9, 2)));
                float span = max(1.0, trail - edge);
                float tail = (d >= 0.0 && a <= trail) ? exp(-d / (span * 0.35 + 1.0)) * exp(-c * c / (2.0 * pow(half * 0.7, 2))) : 0.0;

                float3 light = _Core.rgb * core * 1.1 + _Halo.rgb * halo * 0.85 * flicker + _Tail.rgb * tail * 0.35;

                // Embers: a few sparks leave the edge and rise, fading.
                float embers = 0.0;
                for (int i = 0; i < 6; i++)
                {
                    float seed = i * 7.31;
                    float life = frac(t * (0.55 + Hash(seed) * 0.5) + Hash(seed + 1.0));
                    float2 e = float2(edge + (Hash(seed + 2.0) - 0.3) * 14.0 + sin(t * 3.0 + seed) * 2.0,
                                      life * (half + _Pad) * 1.6 * (Hash(seed + 3.0) > 0.5 ? 1.0 : -0.6));
                    float r = 0.6 + Hash(seed + 4.0) * 0.8;
                    float2 q = float2(a, c) - e;
                    embers += exp(-dot(q, q) / (2.0 * r * r)) * (1.0 - life);
                }
                light += _Core.rgb * embers * _Embers * 0.9;

                float4 color = float4(light * _Heat, 1.0) * IN.color;
                color.rgb *= IN.color.a; // additive: the CanvasGroup fade scales the light
                #ifdef UNITY_UI_CLIP_RECT
                color.rgb *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                return color;
            }
        ENDCG
        }
    }
}
