using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// สร้างรูปทรงพื้นฐาน (วงกลม / สี่เหลี่ยมมุมมน) ด้วยโค้ด ไม่ต้องใช้ไฟล์รูป
    /// ใช้ให้ UI ที่สร้างด้วยโค้ดดูนุ่มนวลขึ้น
    /// </summary>
    public static class UIShapes
    {
        private const int CircleSize = 128;
        private const int RoundedSize = 64;
        private const int RoundedRadius = 16;

        private static Sprite circle;
        private static Sprite rounded;
        private static Sprite bevel;
        private static Sprite ring;
        private static Sprite menuBackdrop;
        private static Sprite battleBackdrop;
        private static Sprite divider;

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                    circle = BuildCircle();

                return circle;
            }
        }

        // สี่เหลี่ยมมุมมน ใช้กับ Image แบบ Sliced
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null)
                    rounded = BuildRounded();

                return rounded;
            }
        }

        // ปุ่มนูน: สีเทาไล่เฉดจากสว่าง (บน) ไปเข้ม (ล่าง) ใช้ย้อมสีด้วย Image.color
        public static Sprite Bevel
        {
            get
            {
                if (bevel == null)
                    bevel = BuildRoundedVariant(false);

                return bevel;
            }
        }

        // กรอบเส้นมุมมน (ตรงกลางโปร่งใส) ใช้ย้อมเป็นสีทอง
        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                    ring = BuildRoundedVariant(true);

                return ring;
            }
        }

        // พื้นหลังเมนู: ไล่เฉดน้ำตาลอุ่นจากกลางจอไปขอบมืด
        public static Sprite MenuBackdrop
        {
            get
            {
                if (menuBackdrop == null)
                {
                    menuBackdrop = BuildBackdrop(
                        new Color(0.27f, 0.18f, 0.12f, 1f),
                        new Color(0.06f, 0.04f, 0.04f, 1f));
                }

                return menuBackdrop;
            }
        }

        // พื้นสนามต่อสู้: ผ้าสักหลาดเขียวเข้ม
        public static Sprite BattleBackdrop
        {
            get
            {
                if (battleBackdrop == null)
                {
                    battleBackdrop = BuildBackdrop(
                        new Color(0.13f, 0.22f, 0.19f, 1f),
                        new Color(0.03f, 0.06f, 0.06f, 1f));
                }

                return battleBackdrop;
            }
        }

        // เส้นคั่นแนวนอน จางหายที่ปลายทั้งสองข้าง (สีขาว ย้อมสีได้)
        public static Sprite Divider
        {
            get
            {
                if (divider == null)
                    divider = BuildDivider();

                return divider;
            }
        }

        /// <summary>
        /// ทำให้ Image เป็นสี่เหลี่ยมมุมมน cornerSize = รัศมีมุมเป็นหน่วย Canvas
        /// </summary>
        public static void MakeRounded(Image image, float cornerSize = 12f)
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier =
                RoundedRadius / Mathf.Max(1f, cornerSize);
        }

        public static void MakeCircle(Image image)
        {
            image.sprite = Circle;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static Sprite BuildCircle()
        {
            Texture2D texture = NewTexture(CircleSize);

            float radius = CircleSize / 2f;

            for (int y = 0; y < CircleSize; y++)
            {
                for (int x = 0; x < CircleSize; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;

                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - distance);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, CircleSize, CircleSize),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Sprite BuildRounded()
        {
            Texture2D texture = NewTexture(RoundedSize);

            float half = RoundedSize / 2f;
            float inner = half - RoundedRadius;

            for (int y = 0; y < RoundedSize; y++)
            {
                for (int x = 0; x < RoundedSize; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);

                    float distance =
                        Mathf.Sqrt(dx * dx + dy * dy) - RoundedRadius;

                    float alpha = Mathf.Clamp01(0.5f - distance);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, RoundedSize, RoundedSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(
                    RoundedRadius, RoundedRadius,
                    RoundedRadius, RoundedRadius));
        }

        // ระยะถึงขอบสี่เหลี่ยมมุมมน (ลบ = ข้างใน)
        private static float RoundedDistance(int x, int y)
        {
            float half = RoundedSize / 2f;
            float inner = half - RoundedRadius;

            float qx = Mathf.Abs(x + 0.5f - half) - inner;
            float qy = Mathf.Abs(y + 0.5f - half) - inner;

            float outside =
                Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                           Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));

            float insideDistance = Mathf.Min(Mathf.Max(qx, qy), 0f);

            return outside + insideDistance - RoundedRadius;
        }

        // ringOnly = true: กรอบเส้นหนา 4px / false: พื้นนูนไล่เฉด
        private static Sprite BuildRoundedVariant(bool ringOnly)
        {
            Texture2D texture = NewTexture(RoundedSize);

            const float thickness = 4f;

            for (int y = 0; y < RoundedSize; y++)
            {
                for (int x = 0; x < RoundedSize; x++)
                {
                    float distance = RoundedDistance(x, y);

                    float alpha = Mathf.Clamp01(0.5f - distance);

                    float gray = 1f;

                    if (ringOnly)
                    {
                        alpha *= Mathf.Clamp01(distance + thickness + 0.5f);
                    }
                    else
                    {
                        float v = (float)y / (RoundedSize - 1);

                        gray = Mathf.Lerp(0.6f, 0.95f, v);

                        // เส้นสะท้อนแสงบางๆ ใต้ขอบบน
                        if (distance > -4f && distance < -2f && y > RoundedSize / 2)
                            gray = Mathf.Min(1f, gray + 0.1f);
                    }

                    texture.SetPixel(x, y, new Color(gray, gray, gray, alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, RoundedSize, RoundedSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(
                    RoundedRadius, RoundedRadius,
                    RoundedRadius, RoundedRadius));
        }

        // พื้นหลังเต็มจอ: ไล่เฉดแบบวงรีจากกลางไปขอบ + ลายเส้นแนวตั้งจางๆ
        private static Sprite BuildBackdrop(Color center, Color edge)
        {
            const int width = 256;
            const int height = 144;

            Texture2D texture =
                new Texture2D(width, height, TextureFormat.RGB24, false);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f) / width * 2f - 1f;
                    float dy = (y + 0.5f) / height * 2f - 1f;

                    // วงรีกว้างกว่าสูง
                    float t = Mathf.Clamp01(
                        Mathf.Sqrt(dx * dx * 0.55f + dy * dy * 0.9f));

                    t = t * t * (3f - 2f * t);

                    Color color = Color.Lerp(center, edge, t);

                    float grain = 1f + 0.025f * Mathf.Sin(x * 1.7f + y * 0.05f);

                    texture.SetPixel(
                        x, y,
                        new Color(color.r * grain, color.g * grain, color.b * grain, 1f));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Sprite BuildDivider()
        {
            const int width = 256;
            const int height = 4;

            Texture2D texture = NewTexture(width, height);

            for (int x = 0; x < width; x++)
            {
                float t = Mathf.Abs((x + 0.5f) / width * 2f - 1f);

                float alpha = Mathf.Clamp01(1f - t * t * t);

                for (int y = 0; y < height; y++)
                {
                    float edgeFade = (y == 0 || y == height - 1) ? 0.5f : 1f;

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * edgeFade));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Texture2D NewTexture(int size)
        {
            return NewTexture(size, size);
        }

        private static Texture2D NewTexture(int width, int height)
        {
            Texture2D texture =
                new Texture2D(width, height, TextureFormat.RGBA32, false);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            // ไม่ให้ถูกลบตอนเปลี่ยน Scene
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }
    }
}
