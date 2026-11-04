// FarmSim/PostFx — the single full-screen pass that gives the game its
// "cinematic" look instead of the flat, candy-coloured one:
//   exposure -> filmic (ACES-ish) tonemap -> contrast -> desaturate ->
//   split tone (cool shadows / warm highlights) -> black lift -> vignette -> grain
// One pass, no render-texture ping-pong, so it stays cheap on phones.
Shader "FarmSim/PostFx"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure;
            float _ToneStrength;
            float _Contrast;
            float _Saturation;
            float _Lift;
            float _Vignette;
            float _Warmth;
            float _Grain;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float3 col = c.rgb * _Exposure;

                // filmic shoulder so bright grass/roofs never clip to white
                float3 aces = (col * (2.51 * col + 0.03)) / (col * (2.43 * col + 0.59) + 0.14);
                col = lerp(col, saturate(aces), _ToneStrength);

                // contrast around a slightly dark pivot
                col = (col - 0.45) * _Contrast + 0.45;

                // pull the palette back toward neutral: this is what removes
                // the "everything is neon" feeling
                float luma = dot(col, float3(0.2126, 0.7152, 0.0722));
                col = lerp(float3(luma, luma, luma), col, _Saturation);

                // split toning: cool shadows, warm highlights
                float3 shadowTint = float3(-0.008, 0.002, 0.030) * _Warmth;
                float3 highTint = float3(0.034, 0.014, -0.022) * _Warmth;
                col += lerp(shadowTint, highTint, saturate(luma));

                // filmic black lift (nothing is pure black)
                col = col * (1.0 - _Lift) + _Lift * 0.62;

                // vignette
                float2 d = i.uv - 0.5;
                col *= saturate(1.0 - dot(d, d) * _Vignette);

                // very fine static grain
                float noise = frac(sin(dot(i.uv, float2(12.9898, 78.233))) * 43758.5453);
                col += (noise - 0.5) * _Grain;

                return fixed4(saturate(col), c.a);
            }
            ENDCG
        }
    }

    FallBack Off
}
