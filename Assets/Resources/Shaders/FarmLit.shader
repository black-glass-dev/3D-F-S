// FarmSim/Lit — the game's main material shader.
//
// v1.0.3 look: soft "wrapped" diffuse (no harsh terminator), a cheap
// sky/ground ambient split, a faint rim light and optional per-vertex tint
// (used by the terrain for baked colour variation + contact darkening).
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
        _Wrap ("Diffuse Wrap", Range(0,1)) = 0.35
        _Rim ("Rim Light", Range(0,1)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Farm fullforwardshadows addshadow exclude_path:deferred
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _VertexTint;
        float _Wrap;
        float _Rim;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float tint = lerp(1.0, IN.color.r, _VertexTint);
            o.Albedo = c.rgb * tint;
            o.Alpha = c.a;
        }

        inline fixed4 LightingFarm (SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
        {
            float ndl = dot(s.Normal, lightDir);
            float wrapTerm = saturate((ndl + _Wrap) / (1.0 + _Wrap));

            // Sky above / bounce below: cheap stand-in for ambient occlusion.
            float up = saturate(s.Normal.y * 0.5 + 0.5);
            float3 ambient = s.Albedo * max(UNITY_LIGHTMODEL_AMBIENT.rgb, float3(0.16, 0.17, 0.20)) * lerp(0.78, 1.08, up);

            float3 diffuse = s.Albedo * _LightColor0.rgb * wrapTerm * atten;

            float rim = pow(1.0 - saturate(dot(s.Normal, viewDir)), 4.0) * _Rim;

            fixed4 c;
            c.rgb = diffuse + ambient + rim * _LightColor0.rgb;
            c.a = s.Alpha;
            return c;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
