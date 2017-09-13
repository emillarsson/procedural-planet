// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Atmosphere"
{
	Properties
	{
		_Color ("Main color", Color) = (1,1,1,1)
		_Rim ("Fade Power", Range(0,8)) = 4
		_Depth ("Atmosphere depth", Range(0.001,0.1)) = 0.05
		_OuterRadius ("Outer radius", Float) = 1
		_InnerRadius ("Inner radius", Float) = 0.8

	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		ZWrite Off
     	Blend SrcAlpha OneMinusSrcAlpha

     	LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			uniform sampler2D _CameraDepthTexture;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
				float4 projPos : TEXCOORD0; //Screen position of pos
				float lightCam : TEXCOORD1;
			};

			float4 _Color;
			float _Rim;
			float _Depth;
			float _OuterRadius;
			float _InnerRadius;


			v2f vert (appdata v)
			{
				v2f o;
				//v.vertex.xyz -= v.vertex.xyz * 0.1;
             	//v.normal *= -1;
				o.vertex = UnityObjectToClipPos(v.vertex);

				float3 normalDir = normalize(mul(float4(v.normal,0.0), unity_WorldToObject).xyz);
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - v.vertex.xyz);
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

				float angle = atan2(_OuterRadius-_InnerRadius,distance(_WorldSpaceCameraPos.xyz,mul(unity_ObjectToWorld, float4(0.0,0.0,0.0,1.0))));
				float viewNormal = 	dot(v.normal.xyz, viewDir);
				float lightCamera = dot(viewDir, lightDir);
				float fade = viewNormal;// + asin(lightCamera);
		
				//float view = lerp(0,1,viewNormal);
				float atmosOffset = _InnerRadius*length(_WorldSpaceCameraPos.xyz);
				float view = exp(-(pow(fade-_InnerRadius,2)/_Depth));
				float diff = saturate(dot(normalDir, lightDir)+viewNormal);
				o.color = float4(_Color.xyz, pow(view,_Rim)*diff);
				o.projPos = ComputeScreenPos(o.vertex);
				o.lightCam = lightCamera;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			    float objectZ = LinearEyeDepth (tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD(i.projPos)).r);

			    float partZ = i.projPos.z;
			    float diff = saturate(abs(objectZ - partZ));
			    float4 col;
			    if (diff <= 1) {
			    	col = float4(i.color.rgb*diff, i.color.w*diff);
			    } else {
			    	col = i.color;
			    }
				return col;
			}
			ENDCG
		}
	}
}
