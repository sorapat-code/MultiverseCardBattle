using UnityEngine;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// โพสต์เอฟเฟกต์ของกล้องหน้าต่อสู้ (Built-in Render Pipeline ผ่าน OnRenderImage):
    /// Bloom สองระดับ (จับเฉพาะของที่สว่างเกิน threshold เช่นคริสตัล/หิ่งห้อย/รูน สีการ์ดปกติไม่ฟุ้ง),
    /// ปรับสีแบบหนัง (เงาเย็น ไฮไลต์อุ่น), วิกเน็ต และ tone shoulder นุ่ม
    /// ใช้เชดเดอร์ Resources/MCBPostFx — ถ้าไม่พบหรือไม่รองรับ จะปิดตัวเองเงียบๆ
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class StagePostFx : MonoBehaviour
    {
        public float Threshold = 0.95f;
        public float Knee = 0.5f;
        public float Bloom = 0.7f;
        public float WideBloom = 0.9f;
        public float Vignette = 0.5f;
        public float Saturation = 1.1f;
        public float Contrast = 1.05f;
        public float Exposure = 1f;

        public Color LightTint = new Color(1.04f, 1.0f, 0.94f, 1f);
        public Color ShadowTint = new Color(0.9f, 0.97f, 1.03f, 1f);

        private Material material;

        private void OnEnable()
        {
            Shader shader = Resources.Load<Shader>("MCBPostFx");

            if (shader == null || !shader.isSupported)
            {
                enabled = false;
                return;
            }

            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDisable()
        {
            if (material != null)
            {
                Destroy(material);
                material = null;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int width = source.width;
            int height = source.height;

            RenderTextureFormat format = RenderTextureFormat.DefaultHDR;

            RenderTexture a = RenderTexture.GetTemporary(Mathf.Max(2, width / 4), Mathf.Max(2, height / 4), 0, format);
            RenderTexture b = RenderTexture.GetTemporary(a.width, a.height, 0, format);
            RenderTexture c = RenderTexture.GetTemporary(Mathf.Max(2, width / 8), Mathf.Max(2, height / 8), 0, format);
            RenderTexture d = RenderTexture.GetTemporary(c.width, c.height, 0, format);

            a.filterMode = FilterMode.Bilinear;
            b.filterMode = FilterMode.Bilinear;
            c.filterMode = FilterMode.Bilinear;
            d.filterMode = FilterMode.Bilinear;

            material.SetVector("_BloomP", new Vector4(Threshold, Knee, Bloom, WideBloom));

            Graphics.Blit(source, a, material, 0);

            BlurInPlace(a, b);
            BlurInPlace(a, b);

            Graphics.Blit(a, c, material, 3);

            BlurInPlace(c, d);
            BlurInPlace(c, d);

            material.SetTexture("_Bloom", a);
            material.SetTexture("_Bloom2", c);
            material.SetVector("_Grade", new Vector4(Vignette, Saturation, Contrast, Exposure));
            material.SetColor("_TintLight", LightTint);
            material.SetColor("_TintShadow", ShadowTint);

            Graphics.Blit(source, destination, material, 2);

            RenderTexture.ReleaseTemporary(a);
            RenderTexture.ReleaseTemporary(b);
            RenderTexture.ReleaseTemporary(c);
            RenderTexture.ReleaseTemporary(d);
        }

        // เบลอแนวนอนแล้วแนวตั้ง ผลลัพธ์อยู่ที่ target (scratch ใช้พักกลางทาง)
        private void BlurInPlace(RenderTexture target, RenderTexture scratch)
        {
            material.SetVector("_Dir", new Vector4(1f, 0f, 0f, 0f));
            Graphics.Blit(target, scratch, material, 1);

            material.SetVector("_Dir", new Vector4(0f, 1f, 0f, 0f));
            Graphics.Blit(scratch, target, material, 1);
        }
    }
}
