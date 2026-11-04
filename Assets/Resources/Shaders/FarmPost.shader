// FarmSim/PostFx — a single cheap full-screen pass: contrast, saturation,
// warm tint and vignette. This is what makes the flat low-poly scene read as
// "graded" without any post-processing package.
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
            float _Saturation;
            float _Contrast;
            float _Brightness;
            float _Warmth;
            float _Vignette;
            float _ShadowLift;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);

                // contrast around mid grey + slight brightness trim
                c.rgb = (c.rgb - 0.5) * _Contrast + 0.5 + _Brightness;

                // lift the darkest areas a touch so shadows keep colour
                c.rgb = lerp(c.rgb, c.rgb + float3(0.03, 0.035, 0.045), _ShadowLift);

                // saturation
                float luma = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(float3(luma, luma, luma), c.rgb, _Saturation);

                // warm highlights, cool shadows
                c.rgb *= float3(1.0 + _Warmth * 0.06, 1.0, 1.0 - _Warmth * 0.05);

                // vignette
                float2 d = i.uv - 0.5;
                float v = saturate(1.0 - dot(d, d) * _Vignette);
                c.rgb *= v;

                return saturate(c);
            }
            ENDCG
        }
    }

    FallBack Off
}
