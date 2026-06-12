Shader "VTT/FogOfWar"
{
    Properties
    {
        _MainTex ("Mask (Alpha)", 2D) = "white" {}
        _FogTex ("Fog Texture Overlay", 2D) = "white" {}
        _Color ("Fog Color", Color) = (0,0,0,1)
        _UseTexture ("Use Texture (0=No, 1=Yes)", Float) = 0
        _Tiling ("Texture Tiling", Float) = 1
        _ExploredOpacity ("Explored Opacity", Range(0,1)) = 0.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _FogTex;
            float4 _Color;
            float _UseTexture;
            float _Tiling;
            float _ExploredOpacity;

            uniform float4 _TokenPositions[64];
            uniform float4 _TokenDirections[64];
            uniform int _TokenCount;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float state = tex2D(_MainTex, i.uv).a; 
                
                float inLoS = 0;
                for(int j = 0; j < _TokenCount; j++) {
                    float radius = _TokenPositions[j].z;
                    float shape = _TokenPositions[j].w;
                    float dist = 0;

                    if (shape < 0.5) {
                        dist = distance(i.worldPos.xy, _TokenPositions[j].xy);
                    } 
                    else if (shape < 1.5) {
                        dist = max(abs(i.worldPos.x - _TokenPositions[j].x), abs(i.worldPos.y - _TokenPositions[j].y));
                    }
                    else { 
                        float2 dirToPixel = normalize(i.worldPos.xy - _TokenPositions[j].xy);
                        float2 tokenDir = _TokenDirections[j].xy;
                        float cosAngleThreshold = _TokenDirections[j].z;
        
                        float cosAngle = dot(dirToPixel, tokenDir);
                        dist = distance(i.worldPos.xy, _TokenPositions[j].xy);
        
                        if (cosAngle < cosAngleThreshold && dist > 0.01) {
                            dist = 9999.0;
                        }
                    }
    
                    float vision = smoothstep(radius, radius * 0.5, dist);
                    inLoS = max(inLoS, vision);
                }

                fixed4 finalColor = _Color;
                
                if (state > 0.75) { 
                    if (_UseTexture > 0.5) finalColor = tex2D(_FogTex, i.uv * _Tiling) * _Color;
                    finalColor.a = 1.0;
                } 
                else if (state > 0.25) { 
                    finalColor.a = _ExploredOpacity;
                } 
                else { 
                    finalColor.a = 0.0;
                }

                finalColor.a *= (1.0 - inLoS);
                finalColor.a *= i.color.a;

                return finalColor;
            }
            ENDCG
        }
    }
}