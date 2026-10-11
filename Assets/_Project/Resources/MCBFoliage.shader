// ใบไม้/เปลือกไม้/หิน สำหรับโมเดลธรรมชาติ (Built-in): รับแสงและเงา ตัดใบด้วยช่องอัลฟา
Shader "MCB/FoliageCutout"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0.45
        _UseAlpha ("Use Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert addshadow fullforwardshadows

        sampler2D _MainTex;
        fixed4 _Color;
        half _Cutoff;
        half _UseAlpha;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

            clip(lerp(1.0, c.a, _UseAlpha) - _Cutoff);

            o.Albedo = c.rgb * _Color.rgb;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}
