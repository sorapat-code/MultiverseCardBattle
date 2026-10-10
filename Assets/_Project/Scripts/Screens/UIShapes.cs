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

        private static Texture2D NewTexture(int size)
        {
            Texture2D texture =
                new Texture2D(size, size, TextureFormat.RGBA32, false);

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            // ไม่ให้ถูกลบตอนเปลี่ยน Scene
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }
    }
}
