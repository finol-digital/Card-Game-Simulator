// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Edge Padding/Diagonal"
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
					
				fixed4 c0 = tex2D(_MainTex, i.uv + float2(-_Delta.x, _Delta.y));
				fixed4 c1 = tex2D(_MainTex, i.uv + float2(0, _Delta.y));
				fixed4 c2 = tex2D(_MainTex, i.uv + float2(_Delta.x, _Delta.y));
				fixed4 c3 = tex2D(_MainTex, i.uv + float2(_Delta.x, 0));
				fixed4 c4 = tex2D(_MainTex, i.uv + float2(_Delta.x, -_Delta.y));
				fixed4 c5 = tex2D(_MainTex, i.uv + float2(0, -_Delta.y));
				fixed4 c6 = tex2D(_MainTex, i.uv + float2(-_Delta.x, -_Delta.y));
				fixed4 c7 = tex2D(_MainTex, i.uv + float2(-_Delta.x, 0));
				
				float a0 = ceil(c0.a);
				float a1 = ceil(c1.a);
				float a2 = ceil(c2.a);
				float a3 = ceil(c3.a);
				float a4 = ceil(c4.a);
				float a5 = ceil(c5.a);
				float a6 = ceil(c6.a);
				float a7 = ceil(c7.a);
				
				float sum = a0 +
							a1 +
							a2 +
							a3 +
							a4 +
							a5 +
							a6 +
							a7;
				
				if (sum <= 0)
					return col;
				
				col.rgb = (	a0 * c0.rgb +
							a1 * c1.rgb +
							a2 * c2.rgb +
							a3 * c3.rgb +
							a4 * c4.rgb +
							a5 * c5.rgb +
							a6 * c6.rgb +
							a7 * c7.rgb 
							) / sum;
				
				col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}
