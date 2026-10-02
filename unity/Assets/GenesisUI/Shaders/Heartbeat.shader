// GenesisUI/Heartbeat: low health felt at the screen's edges — an ember vignette (deep red to
// orange, never flat red) that swells with each heartbeat. Alpha blended, one full-screen quad,
// behind the HUD. C# sets _Intensity (0 hides it; how low the health is) and _Beat (0..1, the
// heartbeat's curve, computed in C# so the rhythm can speed up smoothly).
Shader "GenesisUI/Heartbeat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused (UI needs one)", 2D) = "white" {}
        _Deep ("Deep colour", Color) = (0.30, 0.03, 0.01, 1)
        _Hot ("Ember colour", Color) = (0.85, 0.25, 0.05, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _Beat ("Beat (0..1)", Range(0, 1)) = 0
        _Inner ("Vignette starts (0 centre .. 1 corner)", Range(0, 1)) = 0.5

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
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Deep, _Hot;
            float _Intensity, _Beat, _Inner;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            fixed4 frag(v2f IN) : SV_Target
            {
                float aspect = _ScreenParams.x / max(1.0, _ScreenParams.y);
                float2 q = (IN.uv - 0.5) * float2(aspect, 1.0);
                float r = length(q) / (0.5 * sqrt(aspect * aspect + 1.0));
                // The beat pushes the glow inwards a little and brightens it.
                float inner = _Inner - 0.08 * _Beat;
                float edge = smoothstep(inner, 1.05, r);
                // Slow, uneven embers along the rim, so it reads as heat, not a flat colour.
                float2 cell = floor(IN.uv * float2(48.0 * aspect, 48.0));
                float ember = 0.85 + 0.15 * sin(_Time.y * 2.0 + Hash(cell) * 6.28);
                float a = edge * edge * (0.45 + 0.55 * _Beat) * _Intensity * ember;
                float3 rgb = lerp(_Deep.rgb, _Hot.rgb, saturate(edge * 0.6 + _Beat * 0.4));
                return fixed4(rgb, saturate(a) * IN.color.a);
            }
        ENDCG
        }
    }
}
