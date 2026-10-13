Shader "UI/SpriteOutlineExpand"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.85,0.2,1)
        _PadLocal ("Pad Local", Vector) = (0,0,0,0)
        _PadUV ("Pad UV", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
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
            fixed4 _Color, _OutlineColor;
            float4 _PadLocal, _PadUV;

            v2f vert(appdata v)
            {
                v2f o;
                float2 dir = v.uv * 2 - 1;              // -1 or +1 at the quad corners
                v.vertex.xy += dir * _PadLocal.xy;      // grow the quad
                o.uv = v.uv + dir * _PadUV.xy;          // grow the UVs to match
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }

            float SampleA(float2 uv)
            {
                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
                return tex2D(_MainTex, uv).a * inside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float ca = SampleA(i.uv);
                fixed3 rgb = tex2D(_MainTex, i.uv).rgb;

                float a = 0;
                for (int k = 0; k < 16; k++)
                {
                    float ang = k * 0.3926991;
                    a = max(a, SampleA(i.uv + float2(cos(ang), sin(ang)) * _PadUV.xy));
                }

                float oa = _OutlineColor.a * a * i.color.a;
                float outA = ca * i.color.a + oa * (1 - ca * i.color.a);
                fixed3 col = (rgb * i.color.rgb * ca * i.color.a + _OutlineColor.rgb * oa * (1 - ca * i.color.a)) / max(outA, 0.0001);
                return fixed4(col, outA);
            }
            ENDCG
        }
    }
}