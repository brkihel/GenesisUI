// GenesisUI/Reveal: a window panel opening — two points of light leave the top centre of the frame,
// run down both sides along its gold lines and meet at the bottom, once. Additive. Drawn with the
// panel's own frame sprite (9-sliced like the frame), so the light lies exactly on the lines: the
// sprite's alpha is the mask. C# sets _Rect (the panel in its canvas's space: xMin, yMin, xMax, yMax)
// and _Progress (0..1, eased by C#; the light dims over the last part).
Shader "GenesisUI/Reveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Frame sprite (alpha = the lines)", 2D) = "white" {}
        _Color ("Light colour", Color) = (1.0, 0.86, 0.60, 1)
        _Rect ("Panel rect in canvas space", Vector) = (0, 0, 100, 100)
        _Progress ("Progress (0..1)", Range(0, 1)) = 0.5
        _Head ("Head length (share of the half perimeter)", Range(0.01, 0.5)) = 0.07
        _Trail ("Trail length (share of the half perimeter)", Range(0.01, 1)) = 0.45
        _Strength ("Strength", Range(0, 3)) = 1.4

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Rect, _ClipRect;
            float _Progress, _Head, _Trail, _Strength;

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
                float mask = tex2D(_MainTex, IN.uv).a;
                // Where this pixel lies along the frame, measured from the top centre down either side
                // (0 = top centre, 1 = bottom centre): both halves light up together.
                float2 c = (_Rect.xy + _Rect.zw) * 0.5;
                float2 hs = max(float2(1, 1), (_Rect.zw - _Rect.xy) * 0.5);
                float2 p = IN.worldPosition.xy - c;
                float ax = abs(p.x);
                float dTop = hs.y - p.y, dSide = hs.x - ax, dBottom = p.y + hs.y;
                float s;
                if (dTop <= dSide && dTop <= dBottom) s = ax;
                else if (dSide <= dBottom) s = hs.x + (hs.y - p.y);
                else s = hs.x + 2.0 * hs.y + (hs.x - ax);
                s /= 2.0 * (hs.x + hs.y);

                float head = _Progress * (1.0 + _Head);
                float d = head - s; // > 0 behind the head
                float spark = exp(-d * d / (2.0 * _Head * _Head * 0.25));
                float trail = d > 0.0 ? exp(-d / _Trail) * 0.45 : 0.0;
                float fade = 1.0 - smoothstep(0.7, 1.0, _Progress);
                float light = (spark + trail) * mask * fade * _Strength;

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
