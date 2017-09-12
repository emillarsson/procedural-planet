// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PlanetShader" {
    Properties 
    {
      	_MainTex ("Base (RGB)", 2D) = "white" {}
      	_HeightMin ("Height Min", Float) = -1
      	_HeightMax ("Height Max", Float) = 1
      	_ColorMin ("Tint Color At Min", Color) = (0,0,0,1)
     	_ColorMax ("Tint Color At Max", Color) = (1,1,1,1)
   	}
   
  	SubShader {	

      	Pass {
 			Tags { "LightMode" = "ForwardBase"}
            CGPROGRAM
 
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // FOR SHADOWS
            #pragma multi_compile_fwdbase
            #include "AutoLight.cginc"

			uniform float4 _LightColor0;

           	struct appdata {
           		float4 vertex: POSITION;
           		float3 normal: NORMAL;
           	};
 
            struct v2f {
            	float4 pos : SV_POSITION;
                fixed3 color : COLOR0;
                float3 normal : TEXCOORD0;
                float4 scrPos : TEXCOORD1;
                float4 ambLight : TEXCOORD3;
                // FOR SHADOWS
                SHADOW_COORDS(2)
            };

 			float _HeightMax;
            float _HeightMin;
            fixed4 _ColorMin;
           	fixed4 _ColorMax;

            v2f vert (appdata v)
            {
             	v2f o;
             	o.normal = v.normal;
                float3 normalDirection = normalize(mul(float4(v.normal,0.0), unity_WorldToObject).xyz);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);

                float height = (_HeightMax - length(v.vertex.xyz)) / (_HeightMax - _HeightMin);
                float3 col = _LightColor0.xyz * lerp (_ColorMax.rgba, _ColorMin.rgba, height);
                o.ambLight = UNITY_LIGHTMODEL_AMBIENT;
               	float h = dot(normalDirection, v.vertex.xyz);
                
                o.color = float4(col,1.0);

                o.pos = UnityObjectToClipPos(v.vertex);

                o.scrPos = ComputeScreenPos(v.vertex);

                TRANSFER_SHADOW(o);
                return o;    
             }
 
             fixed4 frag (v2f i) : SV_Target
             {
             	float3 x = ddx(i.scrPos);  // IN.pos is the screen space vertex position
 				float3 y = ddy(i.scrPos);
 				float3 n = normalize(cross(x, y));
 				float diff = max(0.0, -dot(n, normalize(_WorldSpaceLightPos0.xyz)));
 				fixed shadow = SHADOW_ATTENUATION(i);

             	return float4(shadow * diff * i.color, 1) + i.ambLight;
             }
             ENDCG
 
         }
         UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

     }
 }