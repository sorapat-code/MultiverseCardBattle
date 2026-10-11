using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ของตกแต่งและบรรยากาศของสนามต่อสู้ (วาด/ขยับด้วยโค้ดทั้งหมด ใช้เวลา unscaled)
    ///  BuildFrame       กรอบหินรอบสนาม (วางก่อนสนามหญ้า)
    ///  BuildOrnaments   อัญมณีทองตามมุมกรอบและกลางขอบซ้ายขวา
    ///  BuildFoliage     พุ่มไม้ / ก้อนหินรอบขอบสนาม
    ///  BuildAtmosphere  ลำแสงทะลุใบไม้, ลมพัดผ่านสนาม, หิ่งห้อย, ใบไม้ปลิว, หมอกบางๆ (ถูกตัดไว้ในกรอบสนาม)
    /// </summary>
    public class BoardDecor : MonoBehaviour
    {
        // ---------- ตำแหน่งสนาม (สัดส่วนหน้าจอ) ----------

        public const float BoardXMin = 0.10f;
        public const float BoardYMin = 0.22f;
        public const float BoardXMax = 0.90f;
        public const float BoardYMax = 0.78f;

        private const float FrameThickness = 18f;

        // ---------- กรอบ ----------

        public static void BuildFrame(RectTransform root)
        {
            Image frame = UIFactory.CreatePanel(root, "StoneFrame", Color.white);

            frame.sprite = BoardArt.StoneFrame;
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 20f / FrameThickness;
            frame.raycastTarget = false;

            UIFactory.SetAnchors(frame.rectTransform, BoardXMin, BoardYMin, BoardXMax, BoardYMax);

            frame.rectTransform.offsetMin = new Vector2(-FrameThickness, -FrameThickness);
            frame.rectTransform.offsetMax = new Vector2(FrameThickness, FrameThickness);
        }

        // ---------- เครื่องประดับ ----------

        public static void BuildOrnaments(RectTransform root)
        {
            Color gold = new Color(0.95f, 0.75f, 0.3f, 1f);

            // มุมทั้งสี่
            Gem(root, "OrnamentTL", gold, BoardXMin, BoardYMax, 44f);
            Gem(root, "OrnamentTR", gold, BoardXMax, BoardYMax, 44f);
            Gem(root, "OrnamentBL", gold, BoardXMin, BoardYMin, 44f);
            Gem(root, "OrnamentBR", gold, BoardXMax, BoardYMin, 44f);

            // กลางขอบซ้ายขวา ตรงเส้นแบ่งฝั่ง
            Gem(root, "OrnamentL", gold, BoardXMin, 0.5f, 36f);
            Gem(root, "OrnamentR", gold, BoardXMax, 0.5f, 36f);
        }

        private static void Gem(
            RectTransform root, string name, Color color, float x, float y, float size)
        {
            Image gem = UIFactory.CreatePanel(root, name, color);

            gem.sprite = BoardArt.Gem;
            gem.type = Image.Type.Simple;
            gem.preserveAspect = true;
            gem.raycastTarget = false;

            RectTransform rect = gem.rectTransform;

            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
        }

        // ---------- พุ่มไม้ ----------

        public static void BuildFoliage(RectTransform root)
        {
            // สุ่มแบบคงที่ ให้หน้าตาเหมือนเดิมทุกครั้ง
            Random.State saved = Random.state;

            Random.InitState(20261011);

            // ตามขอบบน/ล่างของกรอบ
            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(BoardXMin - 0.02f, BoardXMax + 0.02f, (i + Random.value * 0.6f) / 16f);

                Bush(root, x, BoardYMax + Random.Range(-0.004f, 0.012f));
                Bush(root, x + Random.Range(-0.01f, 0.01f), BoardYMin + Random.Range(-0.012f, 0.004f));
            }

            // ตามขอบซ้าย/ขวา
            for (int i = 0; i < 7; i++)
            {
                float y = Mathf.Lerp(BoardYMin, BoardYMax, (i + Random.value * 0.6f) / 7f);

                Bush(root, BoardXMin + Random.Range(-0.012f, 0.004f), y);
                Bush(root, BoardXMax + Random.Range(-0.004f, 0.012f), y);
            }

            // ก้อนหินกระจายตามมุม
            Rock(root, BoardXMin - 0.008f, BoardYMax + 0.01f, 70f);
            Rock(root, BoardXMax + 0.008f, BoardYMax + 0.01f, 64f);
            Rock(root, BoardXMin - 0.008f, BoardYMin - 0.01f, 60f);
            Rock(root, BoardXMax + 0.008f, BoardYMin - 0.01f, 68f);

            Random.state = saved;
        }

        private static void Bush(RectTransform root, float x, float y)
        {
            float size = Random.Range(70f, 130f);

            Color dark = new Color(
                Random.Range(0.03f, 0.07f), Random.Range(0.14f, 0.22f), Random.Range(0.07f, 0.12f), 0.95f);

            Blob(root, "Bush", dark, x, y, size, size * 0.75f);

            // ปลายใบสว่างขึ้นด้านบน
            Color tip = new Color(
                Random.Range(0.12f, 0.2f), Random.Range(0.32f, 0.45f), Random.Range(0.14f, 0.2f), 0.7f);

            Blob(root, "BushLight", tip, x, y + 0.008f, size * 0.55f, size * 0.4f);
        }

        private static void Rock(RectTransform root, float x, float y, float size)
        {
            Blob(root, "Rock", new Color(0.12f, 0.11f, 0.1f, 1f), x, y, size, size * 0.62f);
            Blob(root, "RockLight", new Color(0.3f, 0.28f, 0.25f, 0.9f), x - 0.003f, y + 0.007f, size * 0.55f, size * 0.3f);
        }

        private static void Blob(
            RectTransform root, string name, Color color,
            float x, float y, float width, float height)
        {
            Image blob = UIFactory.CreatePanel(root, name, color);

            blob.sprite = UIShapes.Glow;
            blob.type = Image.Type.Simple;
            blob.raycastTarget = false;

            RectTransform rect = blob.rectTransform;

            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        // ---------- บรรยากาศที่ขยับได้ ----------

        private const int RayCount = 3;
        private const int FireflyCount = 22;
        private const int LeafCount = 9;
        private const int FogCount = 3;

        private RectTransform area;

        private RectTransform[] rays;
        private Image[] rayImages;
        private float[] rayPhase;

        private RectTransform gust;
        private Image gustImage;

        private RectTransform[] flies;
        private Image[] flyImages;
        private float[] flyPhase;
        private float[] flySpeed;
        private Vector2[] flyHome;

        private RectTransform[] leaves;
        private float[] leafSpeed;
        private float[] leafSpin;
        private float[] leafPhase;

        private RectTransform[] fogs;
        private float[] fogSpeed;

        public static void BuildAtmosphere(RectTransform root)
        {
            // ภาชนะตัดขอบ: อนุภาคทุกอย่างอยู่ภายในกรอบสนามเท่านั้น
            RectTransform clip = UIFactory.CreateRect("Atmosphere", root);

            UIFactory.SetAnchors(clip, BoardXMin, BoardYMin, BoardXMax, BoardYMax);

            clip.gameObject.AddComponent<RectMask2D>();

            BoardDecor decor = clip.gameObject.AddComponent<BoardDecor>();

            decor.Build(clip);
        }

        private void Build(RectTransform clip)
        {
            area = clip;

            // ลำแสง: แท่งเอียงไล่จางลงด้านล่าง
            rays = new RectTransform[RayCount];
            rayImages = new Image[RayCount];
            rayPhase = new float[RayCount];

            for (int i = 0; i < RayCount; i++)
            {
                Image ray = UIFactory.CreatePanel(clip, "Ray", new Color(1f, 0.95f, 0.7f, 0.07f));

                ray.sprite = BoardArt.FadeDown;
                ray.raycastTarget = false;

                RectTransform rect = ray.rectTransform;

                rect.anchorMin = new Vector2(0.2f + i * 0.28f, 1f);
                rect.anchorMax = new Vector2(0.2f + i * 0.28f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(Random.Range(150f, 260f), 900f);
                rect.localRotation = Quaternion.Euler(0f, 0f, 18f);

                rays[i] = rect;
                rayImages[i] = ray;
                rayPhase[i] = Random.Range(0f, 6.28f);
            }

            // ลมพัดผ่าน: แถบสว่างนุ่มๆ วิ่งจากซ้ายไปขวา
            Image gustImg = UIFactory.CreatePanel(clip, "Gust", new Color(1f, 1f, 0.85f, 0.07f));

            gustImg.sprite = UIShapes.Glow;
            gustImg.raycastTarget = false;

            gust = gustImg.rectTransform;
            gustImage = gustImg;

            gust.anchorMin = new Vector2(0f, 0.5f);
            gust.anchorMax = new Vector2(0f, 0.5f);
            gust.sizeDelta = new Vector2(520f, 700f);

            // หมอก
            fogs = new RectTransform[FogCount];
            fogSpeed = new float[FogCount];

            for (int i = 0; i < FogCount; i++)
            {
                Image fog = UIFactory.CreatePanel(clip, "Fog", new Color(0.85f, 1f, 0.9f, 0.06f));

                fog.sprite = UIShapes.Glow;
                fog.raycastTarget = false;

                RectTransform rect = fog.rectTransform;

                rect.anchorMin = new Vector2(0f, i % 2 == 0 ? 0.12f : 0.88f);
                rect.anchorMax = rect.anchorMin;
                rect.sizeDelta = new Vector2(Random.Range(700f, 1000f), Random.Range(220f, 320f));
                rect.anchoredPosition = new Vector2(Random.Range(0f, 1400f), 0f);

                fogs[i] = rect;
                fogSpeed[i] = Random.Range(8f, 18f) * (i % 2 == 0 ? 1f : -1f);
            }

            // หิ่งห้อย
            flies = new RectTransform[FireflyCount];
            flyImages = new Image[FireflyCount];
            flyPhase = new float[FireflyCount];
            flySpeed = new float[FireflyCount];
            flyHome = new Vector2[FireflyCount];

            for (int i = 0; i < FireflyCount; i++)
            {
                Color tint = i % 3 == 0
                    ? new Color(1f, 0.85f, 0.4f, 1f)
                    : new Color(0.75f, 1f, 0.5f, 1f);

                Image fly = UIFactory.CreatePanel(clip, "Firefly", tint);

                fly.sprite = UIShapes.Glow;
                fly.raycastTarget = false;

                RectTransform rect = fly.rectTransform;

                float size = Random.Range(8f, 18f);

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(size, size);

                flies[i] = rect;
                flyImages[i] = fly;
                flyPhase[i] = Random.Range(0f, 6.28f);
                flySpeed[i] = Random.Range(0.3f, 0.9f);
                flyHome[i] = new Vector2(Random.value, Random.value);
            }

            // ใบไม้ปลิว: แผ่นรีเล็กๆ หมุนช้าๆ ตกเฉียง
            leaves = new RectTransform[LeafCount];
            leafSpeed = new float[LeafCount];
            leafSpin = new float[LeafCount];
            leafPhase = new float[LeafCount];

            for (int i = 0; i < LeafCount; i++)
            {
                Color tint = i % 3 == 0
                    ? new Color(0.85f, 0.55f, 0.2f, 0.85f)
                    : new Color(0.45f, 0.75f, 0.3f, 0.85f);

                Image leaf = UIFactory.CreatePanel(clip, "Leaf", tint);

                leaf.sprite = UIShapes.Circle;
                leaf.raycastTarget = false;

                RectTransform rect = leaf.rectTransform;

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(Random.Range(14f, 22f), Random.Range(6f, 9f));

                leaves[i] = rect;
                leafSpeed[i] = Random.Range(0.025f, 0.06f);
                leafSpin[i] = Random.Range(-90f, 90f);
                leafPhase[i] = Random.value;
            }
        }

        private void Update()
        {
            if (area == null)
                return;

            float time = Time.unscaledTime;
            float width = area.rect.width;
            float height = area.rect.height;

            // ลำแสงกะพริบช้าๆ และแกว่งเล็กน้อย
            for (int i = 0; i < RayCount; i++)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 0.35f + rayPhase[i]);

                Color color = rayImages[i].color;

                color.a = 0.03f + pulse * 0.07f;

                rayImages[i].color = color;

                rays[i].localRotation = Quaternion.Euler(0f, 0f, 18f + Mathf.Sin(time * 0.2f + rayPhase[i]) * 3f);
            }

            // ลมวิ่งผ่านทุก ~11 วินาที
            float gustT = Mathf.Repeat(time / 11f, 1f);

            gust.anchoredPosition = new Vector2(
                Mathf.Lerp(-300f, width + 300f, gustT), 0f);

            Color gustColor = gustImage.color;

            gustColor.a = Mathf.Sin(gustT * Mathf.PI) * 0.09f;

            gustImage.color = gustColor;

            // หมอกลอยช้าๆ วนกลับเมื่อพ้นขอบ
            for (int i = 0; i < FogCount; i++)
            {
                Vector2 position = fogs[i].anchoredPosition;

                position.x += fogSpeed[i] * Time.unscaledDeltaTime;

                float span = width + fogs[i].sizeDelta.x;

                if (position.x > width + fogs[i].sizeDelta.x * 0.5f)
                    position.x -= span;
                else if (position.x < -fogs[i].sizeDelta.x * 0.5f)
                    position.x += span;

                fogs[i].anchoredPosition = position;
            }

            // หิ่งห้อยล่องลอยรอบจุดของตัวเอง กะพริบสว่างมืด
            for (int i = 0; i < FireflyCount; i++)
            {
                float phase = time * flySpeed[i] + flyPhase[i];

                float x = flyHome[i].x * width + Mathf.Sin(phase * 1.3f) * 60f;
                float y = flyHome[i].y * height + Mathf.Cos(phase) * 40f;

                flies[i].anchoredPosition = new Vector2(x, y);

                Color color = flyImages[i].color;

                color.a = Mathf.Clamp01(0.15f + 0.85f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(phase * 2.1f), 2f));

                flyImages[i].color = color;
            }

            // ใบไม้ตกเฉียงจากบนขวาไปล่างซ้าย แล้ววนใหม่
            for (int i = 0; i < LeafCount; i++)
            {
                leafPhase[i] += leafSpeed[i] * Time.unscaledDeltaTime;

                float t = Mathf.Repeat(leafPhase[i], 1f);

                float x = Mathf.Lerp(width * 1.05f, -width * 0.05f, t) + Mathf.Sin(t * 14f + i) * 26f;
                float y = Mathf.Lerp(height * 1.05f, -height * 0.05f, t) + Mathf.Sin(t * 9f + i * 2f) * 14f;

                leaves[i].anchoredPosition = new Vector2(x, y);
                leaves[i].localRotation = Quaternion.Euler(0f, 0f, time * leafSpin[i] + i * 40f);
            }
        }
    }
}
