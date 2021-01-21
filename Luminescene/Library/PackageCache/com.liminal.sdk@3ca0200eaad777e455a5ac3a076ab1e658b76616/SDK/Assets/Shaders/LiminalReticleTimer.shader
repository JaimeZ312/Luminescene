Shader "Liminal/UI/Reticle Timer"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
	_Color ("Color", Color) = (1,1,1,1)
	_OutlineColor ("Outline Color", Color) = (0,0,0,1)
    _Progress ("Progress", Range(0, 1)) = 0.5
  }

  SubShader
  {
    Tags
  {
      "Queue"="Overlay"
      "IgnoreProjector"="True"
      "RenderType"="Transparent"
    }

    LOD 100

    Cull Back
    Lighting Off
	ZWrite Off
	ZTest Always
    Blend One OneMinusSrcAlpha
    Offset -150, -150

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 2.0

      #include "UnityCG.cginc"

      struct appdata_t {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        half2 texcoord : TEXCOORD0;
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;
	  fixed _Progress;
	  fixed4 _Color;
	  fixed4 _OutlineColor;

      v2f vert (appdata_t v) {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
      }

      fixed4 frag (v2f i) : SV_Target {
        fixed4 tex = tex2D(_MainTex, i.texcoord);

	    clip(tex.a - (1 - _Progress));

		fixed a = tex.r * pow(_Progress, 0.25);
		fixed4 col1 = fixed4(tex.rrrr) * _Color;
		fixed4 col2 = fixed4(tex.gggg) * _OutlineColor;
        return saturate(col2 + col1);
      }
      ENDCG
    }
  }
}
