// GenesisUI/Blur: the frosted background of the pause menu (Diego, after R-058). Two grab passes blur
// what is behind the quad horizontally then vertically (a 9-tap gaussian each), and darken it a little.
// Draws like any UI graphic (vertex colour alpha fades it with the menu). If a GPU or canvas mode does
// not support grabbing, GenesisUI falls back to a plain dark veil ([Theme] MenuBlur).
Shader "GenesisUI/Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unused", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Size ("Blur size (pixels)", Range(0, 8)) = 3
        _Darken ("Darken", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        CGINCLUDE
        #include "UnityCG.cginc"
        struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
        struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float4 grab : TEXCOORD0; };
        fixed4 _Color;
        float _Size, _Darken;

        v2f vert(appdata_t v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.grab = ComputeGrabScreenPos(o.vertex);
            o.color = v.color * _Color;
            return o;
        }

        static const float W[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };

        fixed4 Blur(sampler2D tex, float4 texel, float4 grab, float2 dir)
        {
            float2 uv = grab.xy / grab.w;
            float2 step = dir * texel.xy * _Size;
            fixed4 c = tex2D(tex, uv) * W[0];
            for (int i = 1; i < 5; i++)
            {
                c += tex2D(tex, uv + step * i) * W[i];
                c += tex2D(tex, uv - step * i) * W[i];
            }
            return c;
        }
        ENDCG

        GrabPass { "_GenesisBlurA" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            sampler2D _GenesisBlurA;
            float4 _GenesisBlurA_TexelSize;
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = Blur(_GenesisBlurA, _GenesisBlurA_TexelSize, i.grab, float2(1, 0));
                return fixed4(c.rgb, i.color.a);
            }
        ENDCG
        }

        GrabPass { "_GenesisBlurB" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            sampler2D _GenesisBlurB;
            float4 _GenesisBlurB_TexelSize;
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = Blur(_GenesisBlurB, _GenesisBlurB_TexelSize, i.grab, float2(0, 1));
                return fixed4(c.rgb * (1.0 - _Darken), i.color.a);
            }
        ENDCG
        }
    }
}
