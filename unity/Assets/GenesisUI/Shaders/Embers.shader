// GenesisUI/Embers: a few embers rising slowly from the bottom of an area, over a faint glow along
// that bottom edge. Additive, procedural. Used for the forge (sparse, behind the crafting details)
// and for a load near the carry limit (from the weight bar's filled part). C# sets _Size (canvas
// units), _Intensity (0 hides it) and _Fill (0..1: the share of the width the embers and the glow
// come from, left to right).
Shader "GenesisUI/Embers"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Hot ("Hot colour", Color) = (1.0, 0.78, 0.42, 1)
        _Cool ("Cooling colour", Color) = (0.95, 0.30, 0.08, 1)
        _Size ("Area size (width, height)", Vector) = (400, 120, 0, 0)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Fill ("Source width (0..1)", Range(0, 1)) = 1
        _Count ("Embers (1..12)", Range(1, 12)) = 6
        _Speed ("Rise speed (cycles per second)", Float) = 0.18
        _EmberSize ("Ember size (canvas units)", Float) = 1.3
        _Glow ("Bottom glow strength", Range(0, 2)) = 0.35
        _GlowHeight ("Bottom glow height (canvas units)", Float) = 10

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
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; };

            fixed4 _Hot, _Cool;
            float4 _Size, _ClipRect;
            float _Intensity, _Fill, _Count, _Speed, _EmberSize, _Glow, _GlowHeight;

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
                float w = _Size.x, h = max(1.0, _Size.y);
                float x = IN.uv.x * w, y = IN.uv.y * h; // y up from the bottom
                float t = _Time.y;
                float source = max(1.0, _Fill * w);

                // The glow along the source, flickering gently.
                float flicker = 0.85 + 0.15 * sin(t * 2.3 + x * 0.05) * sin(t * 1.7 - x * 0.031);
                float glow = exp(-y * y / (2.0 * _GlowHeight * _GlowHeight)) * smoothstep(source + 6.0, source - 6.0, x) * flicker * _Glow;

                float3 light = _Cool.rgb * glow;
                for (int i = 0; i < 12; i++)
                {
                    float on = step(i + 0.5, _Count);
                    float seed = i * 13.37 + 2.1;
                    float life = frac(t * _Speed * (0.7 + Hash(seed) * 0.6) + Hash(seed + 1.0));
                    float cycle = floor(t * _Speed * (0.7 + Hash(seed) * 0.6) + Hash(seed + 1.0));
                    float ex = Hash(seed + cycle * 3.1) * source + sin(t * 1.1 + seed) * 5.0 * life;
                    float ey = life * h * (0.6 + Hash(seed + 2.0) * 0.4);
                    float r = _EmberSize * (0.7 + Hash(seed + 4.0) * 0.6);
                    float2 q = float2(x - ex, y - ey);
                    float spark = exp(-dot(q, q) / (2.0 * r * r));
                    float fade = sin(life * 3.14159) * (0.75 + 0.25 * sin(t * 9.0 + seed * 5.0));
                    light += lerp(_Hot.rgb, _Cool.rgb, life) * spark * fade * on * 1.4;
                }
                // Soft at the sides and the top.
                light *= smoothstep(0.0, 8.0, x) * smoothstep(0.0, 8.0, w - x) * smoothstep(h, h * 0.6, y);

                float4 color = float4(light * _Intensity, 1.0);
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
