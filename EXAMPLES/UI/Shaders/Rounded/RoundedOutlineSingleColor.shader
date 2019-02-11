﻿Shader "Playtime Painter/UI/Rounded/Outline"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
		_Edges("Sharpness", Range(0.05,1)) = 0.5
	}
	Category{
		Tags{
			"Queue" = "Transparent+10"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PixelPerfectUI" = "Simple"
			"SpriteRole" = "Hide"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 color: COLOR;
				};

			
				float _Edges;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;

					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = 0;
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.texcoord1.x, -v.texcoord1.x));

					return o;
				}


				float4 frag(v2f i) : COLOR{
					
					float4 _ProjTexPos = i.projPos;
					
					float _Courners = i.texcoord.w;
				
					float _Blur = (1 - i.color.a);
					float2 uv = abs(i.texcoord.xy - 0.5) * 2;
					uv = max(0, uv - _ProjTexPos.zw) / (1.0001 - _ProjTexPos.zw) - _Courners;
					float deCourners = 1.0001 - _Courners;
					uv = max(0, uv) / deCourners;
					uv *= uv;
					float clipp = max(0, (1 - uv.x - uv.y));

					float uvy = saturate(clipp * (8 - _Courners * 7)*(0.5*(1 + _Edges)));

					i.color.a *= saturate((1 - uvy) * 2) * min(clipp* _Edges * (1 - _Blur)*deCourners * 30, 1)*(2- _Edges);

					return i.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}