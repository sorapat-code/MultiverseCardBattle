using UnityEngine;
using UnityEngine.UI;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// เอฟเฟกต์ UI สั้นๆ ที่สร้างจากโค้ด: อนุภาค (Burst) วงกระแทก (Ring) และรอยฟัน (Slash)
    /// ใช้เวลาแบบ unscaled
    /// </summary>
    public class UiFx : MonoBehaviour
    {
        private enum Kind
        {
            Particle,
            Ring,
            Slash,
            Beam
        }

        private Kind kind;
        private Image image;
        private RectTransform rect;
        private Color color;
        private Vector2 velocity;
        private float gravity;
        private float age;
        private float life;
        private float startSize;
        private float endSize;
        private float thickness;

        private static UiFx Make(
            Transform parent, Vector3 world, Color color, Sprite sprite,
            Kind kind, float life)
        {
            Image image = UIFactory.CreatePanel(parent, "Fx", color);

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;

            rect.position = world;

            UiFx fx = image.gameObject.AddComponent<UiFx>();

            fx.kind = kind;
            fx.image = image;
            fx.rect = rect;
            fx.color = color;
            fx.life = Mathf.Max(0.05f, life);

            return fx;
        }

        /// <summary>อนุภาคกระจายออกจากจุดเดียว (gravity ติดลบ = ลอยขึ้น)</summary>
        public static void Burst(
            Transform parent, Vector3 world, Color color,
            int count, float speed, float size, float life,
            float gravity = 0f)
        {
            if (parent == null)
                return;

            for (int i = 0; i < count; i++)
            {
                UiFx fx = Make(parent, world, color, UIShapes.Glow, Kind.Particle, life * Random.Range(0.7f, 1.2f));

                float angle = Random.value * Mathf.PI * 2f;
                float power = speed * Random.Range(0.35f, 1f);

                fx.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * power;
                fx.gravity = gravity;
                fx.startSize = size * Random.Range(0.6f, 1.2f);
                fx.endSize = fx.startSize * 0.2f;

                fx.rect.sizeDelta = Vector2.one * fx.startSize;
            }
        }

        /// <summary>วงแหวนขยายออกแล้วจาง</summary>
        public static void Ring(
            Transform parent, Vector3 world, Color color,
            float fromSize, float toSize, float life)
        {
            if (parent == null)
                return;

            UiFx fx = Make(parent, world, color, UIShapes.RingCircle, Kind.Ring, life);

            fx.startSize = fromSize;
            fx.endSize = toSize;

            fx.rect.sizeDelta = Vector2.one * fromSize;
        }

        /// <summary>รอยฟัน/แสงวาบเป็นเส้นยาวพุ่งออก</summary>
        public static void Slash(
            Transform parent, Vector3 world, Color color,
            float length = 260f, float thick = 30f, float life = 0.28f)
        {
            if (parent == null)
                return;

            UiFx fx = Make(parent, world, color, UIShapes.Glow, Kind.Slash, life);

            fx.startSize = length * 0.3f;
            fx.endSize = length;
            fx.thickness = thick;

            fx.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(25f, 65f) * (Random.value < 0.5f ? 1f : -1f));
            fx.rect.sizeDelta = new Vector2(fx.startSize, thick);
        }

        /// <summary>เสาแสงพุ่งขึ้นจากจุดที่กำหนดแล้วแคบลงและจาง (ฐานอยู่ที่จุดนั้น)</summary>
        public static void Beam(
            Transform parent, Vector3 world, Color color,
            float width, float height, float life)
        {
            if (parent == null)
                return;

            UiFx fx = Make(parent, world, color, UIShapes.Glow, Kind.Beam, life);

            fx.startSize = width;
            fx.endSize = height;

            // ฐานของเสาอยู่ที่จุดเกิดเหตุ
            fx.rect.pivot = new Vector2(0.5f, 0.35f);
            fx.rect.position = world;
            fx.rect.sizeDelta = new Vector2(width, 10f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            age += dt;

            float t = Mathf.Clamp01(age / life);

            Color c = color;

            switch (kind)
            {
                case Kind.Particle:
                    velocity.y -= gravity * dt;

                    rect.localPosition += (Vector3)(velocity * dt);

                    rect.sizeDelta = Vector2.one * Mathf.Lerp(startSize, endSize, t);

                    c.a = color.a * (1f - t);
                    break;

                case Kind.Ring:
                {
                    float ease = 1f - (1f - t) * (1f - t);

                    rect.sizeDelta = Vector2.one * Mathf.Lerp(startSize, endSize, ease);

                    c.a = color.a * (1f - t);
                    break;
                }

                case Kind.Beam:
                {
                    // พุ่งขึ้นเร็ว แล้วค่อยแคบลงพร้อมจาง
                    float rise = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.25f), 3f);

                    rect.sizeDelta = new Vector2(
                        startSize * Mathf.Lerp(1f, 0.15f, t),
                        Mathf.Lerp(10f, endSize, rise));

                    c.a = color.a * (1f - t * t);
                    break;
                }

                case Kind.Slash:
                {
                    float ease = 1f - (1f - t) * (1f - t) * (1f - t);

                    rect.sizeDelta = new Vector2(
                        Mathf.Lerp(startSize, endSize, ease),
                        thickness * (1f - t * 0.7f));

                    c.a = color.a * (1f - t * t);
                    break;
                }
            }

            image.color = c;

            if (t >= 1f)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// เวทวงกลมสีแดง-ดำหมุนช้าๆ พร้อมอนุภาคลอยขึ้น ใช้กับ Banner ปลดล็อกการ์ดลับ (Ainz)
    /// </summary>
    public class BannerMagic : MonoBehaviour
    {
        private RectTransform outer;
        private RectTransform inner;
        private Image glow;
        private Transform fxParent;
        private Color color;
        private float age;
        private float particleTimer;
        private float ringTimer;

        private bool slashes;
        private float slashTimer;

        public static void Create(RectTransform parent, Color color, bool slashes = false)
        {
            RectTransform root = UIFactory.CreateRect("BannerMagic", parent);

            UIFactory.SetAnchors(root, 0.5f, 0.5f, 0.5f, 0.5f);

            root.sizeDelta = new Vector2(1000f, 1000f);
            root.anchoredPosition = new Vector2(0f, 40f);

            Image glowImage = UIFactory.CreatePanel(
                root, "Glow", new Color(color.r, color.g, color.b, 0.35f));

            glowImage.sprite = UIShapes.Glow;
            glowImage.type = Image.Type.Simple;
            glowImage.raycastTarget = false;

            UIFactory.Stretch(glowImage.rectTransform);

            BannerMagic magic = root.gameObject.AddComponent<BannerMagic>();

            magic.color = color;
            magic.glow = glowImage;
            magic.fxParent = parent;
            magic.slashes = slashes;
            magic.outer = MakeRing(root, color, 1f);
            magic.inner = MakeRing(root, color, 0.62f);
        }

        private static RectTransform MakeRing(RectTransform root, Color color, float scale)
        {
            Image ring = UIFactory.CreatePanel(
                root, "Ring", new Color(color.r, color.g, color.b, 0.8f));

            ring.sprite = UIShapes.RingCircle;
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;

            RectTransform rect = ring.rectTransform;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * 900f * scale;

            return rect;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            age += dt;

            outer.localRotation = Quaternion.Euler(0f, 0f, age * 12f);
            inner.localRotation = Quaternion.Euler(0f, 0f, -age * 22f);

            Color g = glow.color;

            g.a = 0.28f + 0.12f * Mathf.Sin(age * 2.2f);

            glow.color = g;

            particleTimer -= dt;
            ringTimer -= dt;

            Vector3 center = transform.position;

            if (particleTimer <= 0f)
            {
                particleTimer = 0.06f;

                UiFx.Burst(fxParent, center, color, 2, 220f, 34f, 1.6f, -140f);
            }

            if (ringTimer <= 0f)
            {
                ringTimer = 1.1f;

                UiFx.Ring(fxParent, center, color, 200f, 1000f, 1.3f);
            }

            slashTimer -= dt;

            if (slashes && slashTimer <= 0f)
            {
                slashTimer = Random.Range(0.35f, 0.8f);

                Vector3 offset = new Vector3(
                    Random.Range(-260f, 260f), Random.Range(-200f, 200f), 0f);

                UiFx.Slash(fxParent, center + offset, color, 520f, 18f, 0.3f);
            }
        }
    }

    /// <summary>
    /// ไฟสีน้ำเงินลุกรอบกรอบที่กำหนด (ปุ่ม Devil Trigger / การ์ด Vergil ร่าง Devil Trigger)
    /// ปล่อยอนุภาคลอยขึ้นจากขอบเรื่อยๆ และทำให้แสงเรืองด้านหลังกะพริบ
    /// </summary>
    public class BlueFlame : MonoBehaviour
    {
        private RectTransform target;
        private Transform fxParent;
        private Image glow;
        private float interval;
        private float size;
        private float timer;
        private float age;

        private static readonly Vector3[] corners = new Vector3[4];

        public static void Attach(
            RectTransform target, Transform fxParent, Image glow,
            float interval = 0.035f, float size = 34f)
        {
            BlueFlame flame = target.gameObject.AddComponent<BlueFlame>();

            flame.target = target;
            flame.fxParent = fxParent;
            flame.glow = glow;
            flame.interval = interval;
            flame.size = size;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            age += dt;

            if (glow != null)
            {
                Color g = glow.color;

                g.a = 0.5f + 0.25f * Mathf.Sin(age * 5f);

                glow.color = g;
            }

            timer -= dt;

            if (timer > 0f || target == null || fxParent == null)
                return;

            timer = interval;

            target.GetWorldCorners(corners);

            // สุ่มจุดบนขอบกรอบ: ซ้ายล่าง -> ซ้ายบน -> ขวาบน -> ขวาล่าง
            Vector3 point;

            float t = Random.value;

            switch (Random.Range(0, 4))
            {
                case 0: point = Vector3.Lerp(corners[0], corners[1], t); break;
                case 1: point = Vector3.Lerp(corners[1], corners[2], t); break;
                case 2: point = Vector3.Lerp(corners[2], corners[3], t); break;
                default: point = Vector3.Lerp(corners[3], corners[0], t); break;
            }

            Color color = Random.value < 0.3f
                ? new Color(0.65f, 0.9f, 1f, 0.9f)
                : new Color(0.2f, 0.5f, 1f, 0.95f);

            UiFx.Burst(fxParent, point, color, 1, 60f, size, 0.9f, -200f);
        }
    }

    /// <summary>
    /// ฝุ่น/ประกายลอยช้าๆ เป็นฉากหลัง พร้อมขอบจอมืด ใส่ในทุกหน้าเพื่อให้ดูมีบรรยากาศ
    /// </summary>
    public class AmbientFx : MonoBehaviour
    {
        private const int MoteCount = 34;

        private RectTransform area;
        private RectTransform[] motes;
        private Image[] images;
        private float[] speed;
        private float[] sway;
        private float[] phase;
        private float[] baseX;
        private float[] alpha;
        private Color[] colors;

        public static void Attach(Transform canvas, bool battle)
        {
            RectTransform area = UIFactory.CreateRect("Ambient", canvas);

            UIFactory.Stretch(area);

            AmbientFx fx = area.gameObject.AddComponent<AmbientFx>();

            fx.Build(area, battle);

            // ขอบจอมืดทับฝุ่น แต่ยังอยู่ใต้ UI ทั้งหมด
            Image vignette = UIFactory.CreatePanel(
                canvas, "Vignette", new Color(0f, 0f, 0f, 1f));

            vignette.sprite = UIShapes.Vignette;
            vignette.type = Image.Type.Simple;
            vignette.raycastTarget = false;

            UIFactory.Stretch(vignette.rectTransform);
        }

        private void Build(RectTransform root, bool battle)
        {
            area = root;

            motes = new RectTransform[MoteCount];
            images = new Image[MoteCount];
            speed = new float[MoteCount];
            sway = new float[MoteCount];
            phase = new float[MoteCount];
            baseX = new float[MoteCount];
            alpha = new float[MoteCount];
            colors = new Color[MoteCount];

            for (int i = 0; i < MoteCount; i++)
            {
                Color tint = battle
                    ? (i % 4 == 0
                        ? new Color(1f, 0.6f, 0.25f, 1f)
                        : new Color(0.7f, 0.95f, 0.85f, 1f))
                    : (i % 3 == 0
                        ? new Color(1f, 0.55f, 0.25f, 1f)
                        : new Color(1f, 0.82f, 0.45f, 1f));

                Image image = UIFactory.CreatePanel(root, "Mote", tint);

                image.sprite = UIShapes.Glow;
                image.type = Image.Type.Simple;
                image.raycastTarget = false;

                float size = Random.Range(6f, 26f);

                image.rectTransform.sizeDelta = Vector2.one * size;

                motes[i] = image.rectTransform;
                images[i] = image;
                speed[i] = Random.Range(12f, 38f);
                sway[i] = Random.Range(10f, 40f);
                phase[i] = Random.value * 10f;
                alpha[i] = Random.Range(0.08f, 0.32f);
                colors[i] = tint;
                baseX[i] = Random.Range(-960f, 960f);

                motes[i].anchoredPosition = new Vector2(baseX[i], Random.Range(-540f, 540f));
            }
        }

        private void Update()
        {
            if (motes == null)
                return;

            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            Rect rect = area.rect;

            float halfH = rect.height * 0.5f + 30f;
            float halfW = rect.width * 0.5f;

            for (int i = 0; i < MoteCount; i++)
            {
                Vector2 position = motes[i].anchoredPosition;

                position.y += speed[i] * dt;

                position.x = Mathf.Clamp(
                    baseX[i] + Mathf.Sin(time * 0.35f + phase[i]) * sway[i],
                    -halfW, halfW);

                if (position.y > halfH)
                {
                    position.y = -halfH;
                    baseX[i] = Random.Range(-halfW, halfW);
                }

                motes[i].anchoredPosition = position;

                // กะพริบเบาๆ
                float twinkle = 0.65f + 0.35f * Mathf.Sin(time * 1.3f + phase[i] * 3f);

                Color c = colors[i];

                c.a = alpha[i] * twinkle;

                images[i].color = c;
            }
        }
    }
}
