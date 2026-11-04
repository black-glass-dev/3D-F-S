// FarmSim/Foliage — alpha-tested, double sided surface shader used for
// grass tufts and leaf cards. Lighting is the same as the rest of the world
// so the cards sit naturally in the scene.
Shader "FarmSim/Foliage"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Transparent/Cutout/VertexLit"
}
