// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Edge Padding/Simple"
{
	Properties
	{
		[HideInInspector] _MainTex ("Texture", 2D) = "white" {}
		[HideInInspector] _Delta ("Delta", Vector) = (0,0,0,0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float4 _Delta;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				
				if (col.a > 0)
					return col;
					
				fixed4 c0 = tex2D(_MainTex, i.uv + float2(0, _Delta.y));
				fixed4 c1 = tex2D(_MainTex, i.uv + float2(_Delta.x, 0));
				fixed4 c2 = tex2D(_MainTex, i.uv + float2(0, -_Delta.y));
				fixed4 c3 = tex2D(_MainTex, i.uv + float2(-_Delta.x, 0));
				
				float4 a = float4(	ceil(c0.a), 
									ceil(c1.a), 
									ceil(c2.a), 
									ceil(c3.a));
				
				float sum = a.x +
							a.y +
							a.z +
							a.w;
				
				if (sum <= 0)
					return col;
				
				col.rgb = (	a.x * c0.rgb +
							a.y * c1.rgb +
							a.z * c2.rgb +
							a.w * c3.rgb 
							) / sum;
				
				col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}
