// FarmSim/Lit — the game's main material shader.
//
// v1.0.4 look: stock Lambert lighting (zero shader risk, fast on phones),
// a cool fill light in the scene for a soft terminator, and optional
// per-vertex tint used by the terrain for baked colour variation +
// contact darkening.
//
// It lives in Assets/Resources so Unity ALWAYS packs it into the APK.
// (Shader.Find("Standard") returns null in a player build when nothing
// references it, which is what caused the pink/magenta screen in v1.0.0.)
Shader "FarmSim/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Smoothness ("Smoothness", Range(0,1)) = 0.05
        _VertexTint ("Use Vertex Tint", Range(0,1)) = 0
        _VertexColor ("Use Vertex Colour (RGB)", Range(0,1)) = 0
        _Cull ("Cull Mode", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull [_Cull]

        CGPROGRAM
        #pragma surface surf Lambert addshadow fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _VertexTint;
        float _VertexColor;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float tint = lerp(1.0, IN.color.r, _VertexTint);
            fixed3 vertex = lerp(fixed3(1, 1, 1), IN.color.rgb, _VertexColor);
            o.Albedo = c.rgb * tint * vertex;
            o.Alpha = c.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
