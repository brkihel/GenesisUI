// GenesisUI/Beam: the window tabs' light. Additive, behind the tab's icon and label, inside the tab
// only. Two lights with two meanings (Diego, 2026-10-02: one light for both read as a duplicate):
// - the selected tab: a shaft falling from the top rail (_Intensity; 0 hides it). A narrow cone
//   widens downwards and fades towards the bottom; faint rays drift inside it, the rail catches a
//   thin line of light where the beam starts, and a few motes float down in it;
// - the hovered tab: a glint running once along the rail (_GlintPos 0..1+), leaving a faint line of
//   light on it while the pointer stays (_Hover).
// C# sets _Size (the tab in canvas units). The shaft sways like a hanging lantern (the source stays, the bottom swings by _Sway) and
// breathes a little; _Phase keeps neighbouring tabs out of step. Everything is procedural: no
// texture. Kept soft on purpose: it must read as light, not a box.
Shader "GenesisUI/Beam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Color ("Light colour", Color) = (1.0, 0.80, 0.50, 1)
        _Size ("Tab size (width, height)", Vector) = (200, 66, 0, 0)
        _Focus ("Focus (0..1 across)", Range(0, 1)) = 0.5
        _Intensity ("Intensity", Range(0, 1.5)) = 0.6
        _TopWidth ("Beam half-width at the rail", Float) = 14
        _BottomWidth ("Beam half-width at the bottom", Float) = 46
        _EdgeFade ("Fade near the tab's sides (canvas units)", Float) = 22
        _Sway ("Sway at the bottom (canvas units)", Float) = 5
        _Breath ("Breathing (0..1 of the light)", Range(0, 0.5)) = 0.1
        _Phase ("Phase (seconds)", Float) = 0
        _Hover ("Hover glint (0..1.5)", Range(0, 1.5)) = 0
        _GlintPos ("Glint position along the rail (0..1.2)", Range(0, 1.2)) = 1.2

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
            float4 _Size;
            float _Focus, _Intensity, _TopWidth, _BottomWidth, _EdgeFade, _Sway, _Breath, _Phase, _Hover, _GlintPos;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float Hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Canvas units: x across from the left, y down from the rail.
                float w = _Size.x, h = max(1.0, _Size.y);
                float x = IN.uv.x * w;
                float y = (1.0 - IN.uv.y) * h;
                float k = saturate(y / h);
                float t = _Time.y + _Phase;
                // The lantern sway: two slow, unrelated sines, so it never looks like a metronome.
                float sway = (sin(t * 0.53) * 0.7 + sin(t * 0.21 + 1.7) * 0.3) * _Sway;
                float dx = x - _Focus * w - sway * k * k;
                float breath = 1.0 + _Breath * (sin(t * 0.9) * 0.6 + sin(t * 0.37 + 0.8) * 0.4);

                // The cone: narrow at the rail, wider and dimmer towards the bottom.
                float spread = lerp(_TopWidth, _BottomWidth, k);
                float cone = exp(-dx * dx / (2.0 * spread * spread)) * lerp(1.0, 0.35, k);
                // Rays radiating from the source, drifting slowly.
                float a = dx / (y + 10.0);
                float rays = 0.82 + 0.18 * sin(a * 9.0 + t * 0.7) * sin(a * 5.3 - t * 0.45);
                // Where the light leaves the rail: a thin bright line and a small hotspot.
                float rim = exp(-y * y / 4.5) * exp(-dx * dx / (2.0 * pow(_TopWidth * 2.2, 2.0)));
                float hot = exp(-(dx * dx + y * y) / (2.0 * 49.0));

                // A few motes drifting down inside the beam.
                float motes = 0.0;
                for (int i = 0; i < 4; i++)
                {
                    float seed = i * 9.17 + 1.3;
                    float life = frac(t * (0.10 + Hash(seed) * 0.08) + Hash(seed + 1.0));
                    float my = life * h;
                    float mx = _Focus * w + (Hash(seed + 2.0) - 0.5) * lerp(_TopWidth, _BottomWidth, life) * 1.4 + sin(t * 0.8 + seed) * 3.0;
                    float2 q = float2(x - mx, y - my);
                    motes += exp(-dot(q, q) / 1.6) * sin(life * 3.14159);
                }

                float light = (cone * rays * 0.55 + rim * 0.9 + hot * 0.35 + motes * cone * 0.9) * _Intensity * breath;

                // The hover glint: a line of light on the rail, and a brighter spot running along it once.
                float rail = exp(-y * y / 2.4) + 0.25 * exp(-y * y / 40.0);
                float gx = x - _GlintPos * w;
                float glint = exp(-gx * gx / (2.0 * 16.0 * 16.0));
                light += rail * (0.3 + glint * 1.3) * _Hover;

                // Inside the tab only: soft at its sides and bottom.
                light *= smoothstep(0.0, _EdgeFade, x) * smoothstep(0.0, _EdgeFade, w - x) * smoothstep(h, h - 8.0, y);

                float4 color = float4(_Color.rgb * light, 1.0);
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
