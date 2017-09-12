// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Water"
{
	Properties
	{
		_ColorTop ("Top Color", Color) = (1.0,1.0,1.0,1.0)
		_ColorBottom ("Bottom Color", Color) = (1.0,1.0,1.0,1.0)
		_BumpMap ("Normal map", 2D) = "bump" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		ZWrite Off
     	Blend SrcAlpha OneMinusSrcAlpha

     	LOD 100

		Pass
		{
			//Tags { "LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			uniform sampler2D _CameraDepthTexture;

            #pragma multi_compile_fwdbase
            #include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				float4 ambLight : TEXCOORD1;
				float3 normalWorld: TEXCOORD2;
				float3 tangentWorld : TEXCOORD3;
				float3 binormalWorld : TEXCOORD4;
				float4 projPos : TEXCOORD5; //Screen position of pos

				SHADOW_COORDS(6)
			};

			float4 _ColorTop;
			float4 _ColorBottom;
			uniform sampler2D _BumpMap;
			uniform float4 _BumpMap_ST;


			v2f vert (appdata v)
			{
				v2f o;
				float3 normalDirection = normalize(mul(float4(v.normal,0.0), unity_WorldToObject).xyz);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				o.pos = v.vertex;
				float sinValue = sin(2*_Time.y*o.pos.x);
				//o.pos.xyz += 0.01*sinValue*v.normal;
				o.col = _ColorTop*max(0.3, dot(normalDirection, lightDirection));
				o.pos = UnityObjectToClipPos(o.pos);
				o.ambLight = UNITY_LIGHTMODEL_AMBIENT;
	            TRANSFER_SHADOW(o);

	            o.normalWorld = normalize(mul(float4(v.normal,0.0),unity_WorldToObject).xyz);
				o.tangentWorld = normalize(mul(unity_ObjectToWorld, v.tangent).xyz);
				o.binormalWorld = normalize( cross(o.normalWorld, o.tangentWorld)*v.tangent.w);

				o.projPos = ComputeScreenPos(o.pos);

				return o;
			}
			
			float4 frag (v2f i) : COLOR
			{
				fixed shadow = SHADOW_ATTENUATION(i);

				float4 texN = tex2D(_BumpMap, _BumpMap_ST.xy + _BumpMap_ST.zw);
				float3 localCoords = float3(2.0 * texN.ag - float2(1.0, 1.0), 0.0);
				localCoords.z = 1;
				float3x3 local2WorldTranspose = float3x3(i.tangentWorld, i.binormalWorld, i.normalWorld);

				float3 normalDirection = normalize(mul(localCoords, local2WorldTranspose));

				float objectZ = LinearEyeDepth (tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD(i.projPos)).r);
				float partZ = i.projPos.z;
			    float diff = saturate(abs(objectZ - partZ));
			    float4 col = i.col;
			    //if (diff <= 1) {
			    //	col = lerp(_ColorTop,_ColorBottom,8*diff) * shadow + i.ambLight;
			    //} else {
			    //	col = i.col * shadow + i.ambLight;
			    //}
				col.w = 1;
				return col;
			}
			ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

	}
}
