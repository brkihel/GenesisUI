// GenesisUI/Metal: lights a thin-line frame as polished metal (D-033).
// The sprite is a relief map made by tools/art/metal.py: RG = surface normal (xy, 0.5 = flat),
// B = height, A = coverage. The frame keeps its shape from the sprite (9-slice works: a rail's
// profile is the same along its length); this shader only adds the material: a bronze-to-gold
// ramp lit from the top left, a specular highlight, and a slow glint that sweeps across the
// screen now and then. Based on the structure of Unity's UI/Default (stencil, RectMask2D clipping,
// CanvasGroup alpha through the vertex colour) so it behaves like any other UI graphic.
Shader "GenesisUI/Metal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Relief (RG normal, B height, A coverage)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Deep ("Ramp 0 deep bronze", Color) = (0.086, 0.055, 0.027, 1)
        _Bronze ("Ramp 1 bronze", Color) = (0.306, 0.204, 0.094, 1)
        _Gold ("Ramp 2 old gold", Color) = (0.588, 0.424, 0.204, 1)
        _Bright ("Ramp 3 gold", Color) = (0.808, 0.643, 0.369, 1)
        _Pale ("Ramp 4 highlight", Color) = (0.98, 0.902, 0.714, 1)
        _LightDir ("Light direction (x right, y up, z out)", Vector) = (-0.45, 0.65, 0.62, 0)
        _Ambient ("Ambient", Range(0, 1)) = 0.18
        _Diffuse ("Diffuse", Range(0, 1.5)) = 0.62
        _Specular ("Specular", Range(0, 2)) = 0.9
        _Shininess ("Shininess", Range(4, 128)) = 48
        _GlintStrength ("Glint strength", Range(0, 2)) = 0.8
        _GlintWidth ("Glint width (canvas units)", Float) = 18
        _GlintSpeed ("Glint speed (canvas units per second)", Float) = 420
        _GlintPeriod ("Glint period (seconds)", Float) = 9

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _Deep, _Bronze, _Gold, _Bright, _Pale;
            float4 _LightDir;
            float _Ambient, _Diffuse, _Specular, _Shininess;
            float _GlintStrength, _GlintWidth, _GlintSpeed, _GlintPeriod;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Five-stop ramp: 0 deep, .35 bronze, .62 old gold, .85 gold, 1 highlight (tools/art/metal.py RAMP).
            float3 Ramp(float v)
            {
                v = saturate(v);
                float3 c = lerp(_Deep.rgb, _Bronze.rgb, saturate(v / 0.35));
                c = lerp(c, _Gold.rgb, saturate((v - 0.35) / 0.27));
                c = lerp(c, _Bright.rgb, saturate((v - 0.62) / 0.23));
                c = lerp(c, _Pale.rgb, saturate((v - 0.85) / 0.15));
                return c;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float4 relief = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                float2 nxy = relief.rg * 2.0 - 1.0;
                float3 n = normalize(float3(nxy, sqrt(saturate(1.0 - dot(nxy, nxy)))));
                float3 l = normalize(_LightDir.xyz);
                float diffuse = saturate(dot(n, l));
                float3 h = normalize(l + float3(0, 0, 1));
                float spec = pow(saturate(dot(n, h)), _Shininess);

                // A slow diagonal sweep across the whole canvas, once per period.
                float along = IN.worldPosition.x + IN.worldPosition.y;
                float sweep = fmod(_Time.y, _GlintPeriod) * _GlintSpeed - 600.0;
                float band = exp(-pow(along - sweep, 2) / (2.0 * _GlintWidth * _GlintWidth));

                float value = _Ambient + _Diffuse * diffuse + 0.08 * n.z;
                float3 rgb = Ramp(value);
                rgb += (spec * _Specular + band * _GlintStrength * (0.35 + 0.65 * diffuse)) * _Pale.rgb * 0.8 * relief.b;

                fixed4 color = fixed4(rgb, relief.a) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
        ENDCG
        }
    }
}
