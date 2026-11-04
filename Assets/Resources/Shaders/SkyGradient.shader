// FarmSim/SkyGradient — cheap procedural sky: warm dawn/dusk at the
// horizon, clear blue overhead. Colours are driven from GameManager so the
// sky changes with the 90 second day cycle.
Shader "FarmSim/SkyGradient"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.33,0.63,0.95,1)
        _BottomColor ("Bottom", Color) = (0.83,0.92,0.98,1)
        _Exponent ("Curve", Float) = 1.4
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _Exponent;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float h = saturate(normalize(i.dir).y);
                float t = pow(h, 1.0 / max(_Exponent, 0.05));
                return lerp(_BottomColor, _TopColor, t);
            }
            ENDCG
        }
    }

    FallBack Off
}
