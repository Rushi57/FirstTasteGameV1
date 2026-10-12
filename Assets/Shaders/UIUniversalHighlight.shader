Shader "UI/UniversalHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _HighlightColor ("Highlight Color", Color) = (1,0.9,0.3,1)
        _HighlightAmount ("Highlight Amount", Range(0,1)) = 0
        _OutlineWidth ("Outline Width (texels)", Range(0,8)) = 2

        // Needed so Mask / RectMask2D still work
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True"
               "RenderType"="Transparent" "PreviewType"="Plane"
               "CanUseSpriteAtlas"="True" }

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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _HighlightColor;
            float _HighlightAmount;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;   // Image.color still works
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * i.color;

                // Outline: look at neighbouring pixels' alpha
                float2 d = _MainTex_TexelSize.xy * _OutlineWidth;
                float n = 0;
                n = max(n, tex2D(_MainTex, i.uv + float2( d.x, 0)).a);
                n = max(n, tex2D(_MainTex, i.uv + float2(-d.x, 0)).a);
                n = max(n, tex2D(_MainTex, i.uv + float2(0,  d.y)).a);
                n = max(n, tex2D(_MainTex, i.uv + float2(0, -d.y)).a);
                float outline = saturate(n - tex.a) * _HighlightAmount;

                // Soft color tint on the sprite itself
                col.rgb = lerp(col.rgb, _HighlightColor.rgb, _HighlightAmount * 0.3 * tex.a);

                // Add the outline on top
                col.rgb = lerp(col.rgb, _HighlightColor.rgb, outline);
                col.a = max(col.a, outline * _HighlightColor.a * i.color.a);

                return col;
            }
            ENDCG
        }
    }
}