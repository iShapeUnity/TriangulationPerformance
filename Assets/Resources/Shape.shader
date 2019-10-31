Shader "Custom/Shape"
{
    SubShader {
    
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
            "PreviewType" = "Plane"
        }

        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZTest Off
        
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }

}
