// โพสต์เอฟเฟกต์ของหน้าต่อสู้ (Built-in): Bloom สองระดับ + ปรับสี + วิกเน็ต + tone shoulder นุ่มๆ
Shader "Hidden/MCB/PostFx"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _Bloom;
    sampler2D _Bloom2;

    float4 _Dir;        // xy = ทิศการเบลอ
    float4 _BloomP;     // x = threshold, y = knee, z = intensity ระดับ 1, w = intensity ระดับ 2
    float4 _Grade;      // x = วิกเน็ต, y = ความสด, z = คอนทราสต์, w = exposure
    float4 _TintLight;
    float4 _TintShadow;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    v2f vert (appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    float4 fragPrefilter (v2f i) : SV_Target
    {
        float2 t = _MainTex_TexelSize.xy;

        float3 c =
            tex2D(_MainTex, i.uv + t * float2(-1, -1)).rgb +
            tex2D(_MainTex, i.uv + t * float2( 1, -1)).rgb +
            tex2D(_MainTex, i.uv + t * float2(-1,  1)).rgb +
            tex2D(_MainTex, i.uv + t * float2( 1,  1)).rgb;

        c *= 0.25;

        float br = max(c.r, max(c.g, c.b));
        float soft = clamp(br - _BloomP.x + _BloomP.y, 0.0, 2.0 * _BloomP.y);
        soft = soft * soft / (4.0 * _BloomP.y + 0.0001);
        float contrib = max(soft, br - _BloomP.x) / max(br, 0.0001);

        return float4(c * contrib, 1.0);
    }

    float4 fragBlur (v2f i) : SV_Target
    {
        float2 step = _MainTex_TexelSize.xy * _Dir.xy;

        float3 c = tex2D(_MainTex, i.uv).rgb * 0.227027;

        c += (tex2D(_MainTex, i.uv + step * 1.384615).rgb + tex2D(_MainTex, i.uv - step * 1.384615).rgb) * 0.316216;
        c += (tex2D(_MainTex, i.uv + step * 3.230769).rgb + tex2D(_MainTex, i.uv - step * 3.230769).rgb) * 0.070270;

        return float4(c, 1.0);
    }

    float4 fragCopy (v2f i) : SV_Target
    {
        return float4(tex2D(_MainTex, i.uv).rgb, 1.0);
    }

    float4 fragComposite (v2f i) : SV_Target
    {
        float3 c = tex2D(_MainTex, i.uv).rgb * _Grade.w;

        c += tex2D(_Bloom, i.uv).rgb * _BloomP.z;
        c += tex2D(_Bloom2, i.uv).rgb * _BloomP.w;

        // โทนสี: เงาเย็น ไฮไลต์อุ่น
        float luma = dot(c, float3(0.299, 0.587, 0.114));
        c *= lerp(_TintShadow.rgb, _TintLight.rgb, saturate(luma * 1.4));

        // ความสดและคอนทราสต์
        luma = dot(c, float3(0.299, 0.587, 0.114));
        c = lerp(float3(luma, luma, luma), c, _Grade.y);
        c = (c - 0.5) * _Grade.z + 0.5;
        c = max(c, 0.0);

        // วิกเน็ต
        float2 d = (i.uv - 0.5) * float2(1.0, 0.9);
        float v = smoothstep(0.30, 0.78, length(d));
        c *= 1.0 - _Grade.x * v;

        // ไหล่ tone: ค่าเกิน 0.8 บีบเข้าหา 1 นุ่มๆ (สีการ์ดปกติไม่เปลี่ยน)
        float3 over = max(c - 0.8, 0.0);
        c = min(c, 0.8) + 0.2 * (1.0 - exp(-over / 0.2));

        return float4(c, 1.0);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPrefilter
        ENDCG }

        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlur
        ENDCG }

        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragComposite
        ENDCG }

        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragCopy
        ENDCG }
    }
}
