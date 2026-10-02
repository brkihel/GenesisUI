// GenesisUI/Ring: a ring of light spreading once from a point (a pin just placed on the map), with
// a second, fainter ring just behind it and a small flash at the start. Additive, on a square quad
// centred on the point; C# sets _Progress (0..1).
Shader "GenesisUI/Ring"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Color ("Light colour", Color) = (1.0, 0.84, 0.55, 1)
        _Progress ("Progress (0..1)", Range(0, 1)) = 0.5
        _Strength ("Strength", Range(0, 3)) = 1.2

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
            float4 _ClipRect;
            float _Progress, _Strength;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float RingAt(float dist, float p)
            {
                if (p <= 0.0) return 0.0;
                float r = lerp(0.04, 0.46, p);
                float w = 0.012 + 0.03 * p;
                float x = dist - r;
                return exp(-x * x / (2.0 * w * w)) * pow(1.0 - p, 1.6);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float dist = length(IN.uv - 0.5);
                float p = _Progress;
                float light = RingAt(dist, p) + 0.45 * RingAt(dist, (p - 0.18) / 0.82);
                light += exp(-dist * dist / (2.0 * 0.035 * 0.035)) * pow(saturate(1.0 - p * 2.5), 2.0) * 1.2;
                light *= smoothstep(0.5, 0.44, dist); // nothing reaches the quad's edge

                float4 color = float4(_Color.rgb * light * _Strength, 1.0);
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
