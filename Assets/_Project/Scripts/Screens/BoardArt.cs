using UnityEngine;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ภาพของหน้าต่อสู้ที่วาดด้วยโค้ด (ไม่ต้องใช้ไฟล์รูป)
    ///  Grass  พื้นสนามหญ้าเขียวมีลายตัดหญ้าและแสงตรงกลาง
    ///  Frame  พื้นหลังด้านนอกสนาม เป็นป่ามืดสีเขียวอมฟ้า
    ///  FadeDown / FadeUp  ไล่ความโปร่งใสแนวตั้ง (ใช้ทำแสงเรืองที่ขอบสนาม)
    ///  Gem    อัญมณีหกเหลี่ยมมีหน้าตัด (ใช้แสดง Energy) ย้อมสีด้วย Image.color
    ///  Dome   ปุ่มกลมนูน (ใช้เป็นปุ่มจบเทิร์น) ย้อมสีด้วย Image.color
    /// ทุกภาพเป็นสีขาว/เทา ย้อมสีได้ สร้างครั้งเดียวแล้วเก็บไว้
    /// </summary>
    public static class BoardArt
    {
        private static Sprite grass;
        private static Sprite frame;
        private static Sprite fadeDown;
        private static Sprite fadeUp;
        private static Sprite gem;
        private static Sprite dome;
        private static Sprite stoneFrame;
        private static Texture2D stoneTexture;

        public static Sprite Grass
        {
            get
            {
                if (grass == null)
                    grass = BuildGrass();

                return grass;
            }
        }

        public static Sprite Frame
        {
            get
            {
                if (frame == null)
                    frame = BuildFrame();

                return frame;
            }
        }

        /// <summary>ทึบที่ขอบบน จางหายลงมาด้านล่าง</summary>
        public static Sprite FadeDown
        {
            get
            {
                if (fadeDown == null)
                    fadeDown = BuildFade(true);

                return fadeDown;
            }
        }

        /// <summary>ทึบที่ขอบล่าง จางหายขึ้นไปด้านบน</summary>
        public static Sprite FadeUp
        {
            get
            {
                if (fadeUp == null)
                    fadeUp = BuildFade(false);

                return fadeUp;
            }
        }

        public static Sprite Gem
        {
            get
            {
                if (gem == null)
                    gem = BuildGem();

                return gem;
            }
        }

        /// <summary>กรอบหินรอบสนาม (9-slice: ขอบหนา 20 หน่วย มีเส้นทองด้านใน)</summary>
        public static Sprite StoneFrame
        {
            get
            {
                if (stoneFrame == null)
                    stoneFrame = BuildStoneFrame();

                return stoneFrame;
            }
        }

        /// <summary>ลายหินเทาน้ำตาลแบบเรียงต่อกันได้ (ใช้กับโต๊ะและกรอบ 3D) ย้อมสีด้วย Material.color</summary>
        public static Texture2D StoneTexture
        {
            get
            {
                if (stoneTexture == null)
                    stoneTexture = BuildStoneTexture();

                return stoneTexture;
            }
        }

        public static Sprite Dome
        {
            get
            {
                if (dome == null)
                    dome = BuildDome();

                return dome;
            }
        }

        // ---------- สร้างภาพ ----------

        private static Texture2D NewTexture(int width, int height, TextureFormat format)
        {
            Texture2D texture = new Texture2D(width, height, format, false);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        private static Sprite ToSprite(Texture2D texture)
        {
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        // ผลรวมนอยส์หลายชั้น (0-1)
        private static float Fbm(float x, float y)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;

            for (int i = 0; i < 4; i++)
            {
                value += Mathf.PerlinNoise(x * frequency + 11.3f, y * frequency + 4.7f) * amplitude;

                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return value;
        }

        // สนามหญ้า: ไล่สีเข้ม-อ่อน + ลายใบหญ้า + แถบตัดหญ้าแนวตั้งจางๆ + แสงนวลตรงกลาง
        private static Sprite BuildGrass()
        {
            const int width = 384;
            const int height = 160;

            Texture2D texture = NewTexture(width, height, TextureFormat.RGB24);

            Color dark = new Color(0.07f, 0.24f, 0.10f, 1f);
            Color light = new Color(0.24f, 0.50f, 0.17f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;

                    float patch = Fbm(u * 5f, v * 3f);
                    float blades = Mathf.PerlinNoise(u * 140f, v * 18f);

                    float t = Mathf.Clamp01(0.25f + patch * 0.7f + (blades - 0.5f) * 0.28f);

                    // แถบตัดหญ้าสลับสี
                    float stripe = (Mathf.Floor(u * 12f) % 2f == 0f) ? 0.05f : -0.03f;

                    t = Mathf.Clamp01(t + stripe);

                    Color color = Color.Lerp(dark, light, t);

                    // แสงนวลตรงกลาง มืดลงที่ขอบ
                    float dx = u * 2f - 1f;
                    float dy = v * 2f - 1f;

                    float glow = 1.18f - (dx * dx * 0.45f + dy * dy * 0.55f);

                    color *= Mathf.Clamp(glow, 0.55f, 1.15f);

                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, 1f));
                }
            }

            return ToSprite(texture);
        }

        // พื้นหลังนอกสนาม: ป่ามืด ขอบจอมีพุ่มไม้สว่างขึ้นเล็กน้อย กลางจอมืด
        private static Sprite BuildFrame()
        {
            const int width = 256;
            const int height = 144;

            Texture2D texture = NewTexture(width, height, TextureFormat.RGB24);

            Color deep = new Color(0.015f, 0.04f, 0.045f, 1f);
            Color leaf = new Color(0.07f, 0.20f, 0.17f, 1f);
            Color lit = new Color(0.18f, 0.40f, 0.28f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;

                    float dx = u * 2f - 1f;
                    float dy = v * 2f - 1f;

                    // ใกล้ขอบจอ = ใบไม้หนา
                    float edge = Mathf.Clamp01(Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f) - 0.25f);

                    float foliage = Fbm(u * 9f, v * 6f);

                    float t = Mathf.Clamp01(edge * 1.5f * (0.45f + foliage));

                    Color color = Color.Lerp(deep, leaf, t);

                    // ไฮไลต์ใบไม้ที่โดนแสง
                    float spot = Mathf.Clamp01((foliage - 0.62f) * 3.2f) * edge;

                    color = Color.Lerp(color, lit, spot * 0.7f);

                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, 1f));
                }
            }

            return ToSprite(texture);
        }

        private static Sprite BuildFade(bool opaqueAtTop)
        {
            const int width = 4;
            const int height = 64;

            Texture2D texture = NewTexture(width, height, TextureFormat.RGBA32);

            for (int y = 0; y < height; y++)
            {
                // y = 0 คือขอบล่าง
                float t = (y + 0.5f) / height;

                float alpha = opaqueAtTop ? t : 1f - t;

                alpha = alpha * alpha * (3f - 2f * alpha);

                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            return ToSprite(texture);
        }

        // อัญมณีหกเหลี่ยม (ยอดแหลมขึ้น) มีหน้าตัดสว่างมืดสลับกัน ขอบบางมืด และจุดสะท้อนแสง
        private static Sprite BuildGem()
        {
            const int size = 128;

            Texture2D texture = NewTexture(size, size, TextureFormat.RGBA32);

            float center = size / 2f;
            float radius = size / 2f - 2f;
            float apothem = radius * 0.866f;

            float[] facets = { 1.0f, 0.82f, 0.62f, 0.5f, 0.66f, 0.88f };

            Vector2[] normals =
            {
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.866f),
                new Vector2(-0.5f, 0.866f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - center, y + 0.5f - center);

                    // ระยะถึงขอบหกเหลี่ยม (ติดลบ = ข้างใน)
                    float edge = -apothem;

                    for (int i = 0; i < normals.Length; i++)
                        edge = Mathf.Max(edge, Mathf.Abs(Vector2.Dot(p, normals[i])) - apothem);

                    float alpha = Mathf.Clamp01(-edge);

                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    // หน้าตัด: แบ่งตามมุม 6 ส่วน
                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;

                    if (angle < 0f)
                        angle += 360f;

                    int sector = Mathf.Clamp((int)(angle / 60f), 0, 5);

                    float shade = facets[sector];

                    // แกนกลางสว่างกว่า
                    float inner = p.magnitude / radius;

                    if (inner < 0.45f)
                        shade = Mathf.Min(1.1f, shade * 0.6f + 0.55f);

                    // ขอบมืด
                    float rim = Mathf.Clamp01((-edge) / 6f);

                    shade *= Mathf.Lerp(0.45f, 1f, rim);

                    // จุดสะท้อนแสงมุมบนซ้าย
                    float hx = p.x + radius * 0.3f;
                    float hy = p.y - radius * 0.38f;
                    float highlight = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / (radius * 0.28f));

                    float value = Mathf.Clamp01(shade + highlight * 0.6f);

                    texture.SetPixel(x, y, new Color(value, value, value, alpha));
                }
            }

            return ToSprite(texture);
        }

        // ปุ่มกลมนูน: แสงจากบนซ้าย ขอบมืด
        private static Sprite BuildDome()
        {
            const int size = 128;

            Texture2D texture = NewTexture(size, size, TextureFormat.RGBA32);

            float center = size / 2f;
            float radius = size / 2f - 1f;

            Vector3 light = new Vector3(-0.4f, 0.55f, 0.75f).normalized;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - center) / radius;
                    float dy = (y + 0.5f - center) / radius;

                    float r2 = dx * dx + dy * dy;
                    float distance = Mathf.Sqrt(r2) * radius;

                    float alpha = Mathf.Clamp01(radius - distance);

                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                        continue;
                    }

                    float z = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Min(1f, r2)));

                    Vector3 normal = new Vector3(dx, dy, z).normalized;

                    float diffuse = Mathf.Clamp01(Vector3.Dot(normal, light));

                    // ค่อยๆ นูน: ส่วนกลางแบนกว่าเล็กน้อย ขอบมืดลง
                    float value = 0.55f + diffuse * 0.5f;

                    value *= Mathf.Lerp(0.6f, 1f, Mathf.Clamp01((1f - Mathf.Sqrt(r2)) * 6f));

                    texture.SetPixel(x, y, new Color(value, value, value, alpha));
                }
            }

            return ToSprite(texture);
        }

        // กรอบหิน: หินสีน้ำตาลเทามีลายนอยส์ ขอบนอกมืด ขอบเอียงสว่าง และเส้นทองบางๆ ด้านใน
        private static Sprite BuildStoneFrame()
        {
            const int size = 64;
            const int border = 20;

            Texture2D texture = NewTexture(size, size, TextureFormat.RGBA32);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));

                    float grain = 0.8f + Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.4f;

                    Color color;

                    if (d >= border)
                    {
                        // ส่วนกลาง (ถูกสนามบังอยู่)
                        color = new Color(0.08f, 0.06f, 0.05f, 1f);
                    }
                    else if (d < 2)
                    {
                        color = new Color(0.04f, 0.03f, 0.025f, 1f);
                    }
                    else if (d < 5)
                    {
                        // ขอบเอียงสว่าง
                        float k = 1f - (d - 2) / 3f;

                        color = Color.Lerp(
                            new Color(0.26f, 0.21f, 0.17f, 1f),
                            new Color(0.42f, 0.35f, 0.27f, 1f), k) * grain;
                    }
                    else if (d >= border - 3 && d < border - 1)
                    {
                        // เส้นทองด้านใน
                        color = new Color(0.78f, 0.6f, 0.26f, 1f);
                    }
                    else if (d >= border - 1)
                    {
                        color = new Color(0.05f, 0.04f, 0.03f, 1f);
                    }
                    else
                    {
                        // เนื้อหิน ลึกลงด้านในมืดลงเล็กน้อย
                        float depth = (d - 5) / (float)(border - 8);

                        color = Color.Lerp(
                            new Color(0.24f, 0.19f, 0.15f, 1f),
                            new Color(0.15f, 0.12f, 0.09f, 1f), depth) * grain;
                    }

                    color.a = 1f;

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private static Texture2D BuildStoneTexture()
        {
            const int size = 128;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.hideFlags = HideFlags.HideAndDontSave;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // นอยส์ที่วนต่อกันได้: ผสมสองมุมมองเพื่อให้ขอบซ้ายขวาบนล่างเนียน
                    float a = Fbm(u * 6f, v * 6f);
                    float b = Fbm((1f - u) * 6f, v * 6f);
                    float c = Fbm(u * 6f, (1f - v) * 6f);
                    float d = Fbm((1f - u) * 6f, (1f - v) * 6f);

                    float n = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);

                    float value = 0.55f + n * 0.5f;

                    // รอยแตกบางๆ
                    float crack = Mathf.PerlinNoise(u * 18f, v * 18f);

                    if (crack > 0.47f && crack < 0.5f)
                        value *= 0.7f;

                    texture.SetPixel(x, y, new Color(value, value * 0.96f, value * 0.9f, 1f));
                }
            }

            texture.Apply();

            return texture;
        }
    }
}
