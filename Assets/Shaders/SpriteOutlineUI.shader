Shader "UI/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.85,0.2,1)
        _OutlineSize ("Outline Size (px)", Range(0,12)) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float2 px = _MainTex_TexelSize.xy * _OutlineSize;

                float a = 0;
                for (int k = 0; k < 16; k++)
                {
                    float ang = k * 0.3926991; // 2*PI/16
                    a = max(a, tex2D(_MainTex, i.uv + float2(cos(ang), sin(ang)) * px).a);
                }

                fixed4 o = _OutlineColor;
                o.a *= a * i.color.a;

                fixed4 res;
                res.a = c.a + o.a * (1 - c.a);
                res.rgb = (c.rgb * c.a + o.rgb * o.a * (1 - c.a)) / max(res.a, 0.0001);
                return res;
            }
            ENDCG
        }
    }
}