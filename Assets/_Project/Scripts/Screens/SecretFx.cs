using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ชุดเอฟเฟกต์ "อลังการ" สำหรับการ์ดลับ (Sung Jin-Woo / Artorias / Ainz / Vergil และร่าง Devil Trigger)
    /// ทั้งหมดสร้างจากโค้ด ใช้เวลาแบบ unscaled
    /// </summary>
    public static class SecretFx
    {
        public static bool IsSecret(CardData data)
        {
            return data != null &&
                   (data.Rarity == CardRarity.Secret ||
                    data.CardID == StarterCardLibrary.DevilTriggerId);
        }

        /// <summary>สีประจำการ์ดลับแต่ละใบ</summary>
        public static Color Tint(CardData data)
        {
            if (data == null)
                return new Color(1f, 0.4f, 0.5f, 1f);

            switch (data.CardID)
            {
                case StarterCardLibrary.SecretId:
                    return new Color(0.68f, 0.42f, 1f, 1f);       // ม่วงเงา

                case StarterCardLibrary.ArtoriasId:
                    return new Color(1f, 0.78f, 0.36f, 1f);       // ทองอำพัน

                case StarterCardLibrary.AinzId:
                    return new Color(0.95f, 0.12f, 0.18f, 1f);    // แดงเลือด

                case StarterCardLibrary.VergilId:
                case StarterCardLibrary.DevilTriggerId:
                    return new Color(0.27f, 0.62f, 1f, 1f);       // น้ำเงิน Yamato

                default:
                    return new Color(1f, 0.4f, 0.5f, 1f);
            }
        }

        // ---------- ส่วนประกอบพื้นฐาน ----------

        /// <summary>แสงวาบเต็มจอแล้วจางหาย</summary>
        public static void Flash(Transform parent, Color color, float peak, float life)
        {
            if (parent == null)
                return;

            Image image = UIFactory.CreatePanel(
                parent, "SecretFlash", new Color(color.r, color.g, color.b, 0f));

            UIFactory.Stretch(image.rectTransform);

            image.raycastTarget = false;

            image.gameObject.AddComponent<ScreenFlash>().Init(image, color, peak, life);
        }

        /// <summary>จอสั่น (สั่นรอบตำแหน่งเดิมแล้วคืนที่)</summary>
        public static void Shake(RectTransform target, float magnitude, float life)
        {
            if (target == null)
                return;

            ShakeRect shake = target.GetComponent<ShakeRect>();

            if (shake == null)
                shake = target.gameObject.AddComponent<ShakeRect>();

            shake.Begin(magnitude, life);
        }

        /// <summary>ข้อความใหญ่เด้งขึ้นแล้วจาง</summary>
        public static void Title(
            Transform parent, Vector3 world, string content, Color color, int size = 56)
        {
            if (parent == null)
                return;

            Text text = UIFactory.CreateText(
                parent, content, size, color,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            text.rectTransform.sizeDelta = new Vector2(1000f, 120f);
            text.rectTransform.position = world;

            Outline outline = text.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            text.gameObject.AddComponent<SecretTitle>().Init(text);
        }

        /// <summary>เรียกชุดคำสั่งตามเวลา (วินาทีนับจากตอนนี้)</summary>
        public static void Timeline(Transform parent, params KeyValuePair<float, Action>[] steps)
        {
            if (parent == null || steps == null || steps.Length == 0)
                return;

            GameObject host = new GameObject("SecretTimeline");

            host.transform.SetParent(parent, false);

            host.AddComponent<FxTimeline>().Init(steps);
        }

        private static KeyValuePair<float, Action> At(float time, Action action)
        {
            return new KeyValuePair<float, Action>(time, action);
        }

        // ---------- การ์ดลงสนาม ----------

        /// <summary>
        /// การ์ดลับเปิดตัวบนสนาม: แสงวาบ เสาแสง คลื่นกระแทก รอยฟัน อนุภาค จอสั่น และชื่อการ์ดขึ้นกลางจอ
        /// </summary>
        public static void Arrive(
            Transform fx, RectTransform shakeTarget, Vector3 pos, CardData data)
        {
            if (fx == null || data == null)
                return;

            Color c = Tint(data);

            Flash(fx, c, 0.5f, 0.6f);

            Shake(shakeTarget, 18f, 0.6f);

            UiFx.Beam(fx, pos, new Color(c.r, c.g, c.b, 0.8f), 240f, 1400f, 1.0f);
            UiFx.Beam(fx, pos, new Color(1f, 1f, 1f, 0.9f), 90f, 1200f, 0.7f);

            UiFx.Ring(fx, pos, c, 60f, 780f, 0.85f);
            UiFx.Burst(fx, pos, c, 56, 540f, 38f, 1.2f, -60f);

            Title(fx, pos + new Vector3(0f, 230f, 0f), data.CardName.ToUpperInvariant(), c);

            Sfx.Play(SoundId.Pack);

            Timeline(fx,
                At(0.12f, () =>
                {
                    UiFx.Ring(fx, pos, new Color(1f, 1f, 1f, 0.9f), 40f, 560f, 0.6f);

                    for (int i = 0; i < 3; i++)
                        UiFx.Slash(fx, pos, c, 520f, 24f, 0.35f);
                }),
                At(0.3f, () =>
                {
                    UiFx.Ring(fx, pos, c, 40f, 1100f, 1.0f);
                    UiFx.Burst(fx, pos, Color.Lerp(c, Color.white, 0.5f), 36, 380f, 30f, 1.0f, -160f);
                }));
        }

        // ---------- ใช้สกิล ----------

        /// <summary>สกิลของการ์ดลับ (โจมตี / ตาย) หน้าตาต่างกันตามตัวละคร</summary>
        public static void Skill(
            Transform fx, RectTransform shakeTarget,
            Vector3 from, Vector3 to, CardData data, TriggerType trigger)
        {
            if (fx == null || data == null)
                return;

            Color c = Tint(data);

            if (trigger == TriggerType.Death)
            {
                // หายไปอย่างสง่า: เสาแสงดึงขึ้นฟ้า อนุภาคลอยขึ้น
                Flash(fx, c, 0.3f, 0.5f);
                UiFx.Beam(fx, from, new Color(c.r, c.g, c.b, 0.8f), 200f, 1200f, 1.0f);
                UiFx.Ring(fx, from, c, 40f, 520f, 0.8f);
                UiFx.Burst(fx, from, c, 50, 260f, 34f, 1.5f, -260f);

                return;
            }

            switch (data.CardID)
            {
                case StarterCardLibrary.VergilId:
                case StarterCardLibrary.DevilTriggerId:
                    // Judgment Cut: รอยตัดบางๆ จำนวนมาก
                    Shake(shakeTarget, 8f, 0.3f);

                    for (int i = 0; i < 9; i++)
                    {
                        Vector3 offset = new Vector3(
                            UnityEngine.Random.Range(-130f, 130f),
                            UnityEngine.Random.Range(-110f, 110f), 0f);

                        UiFx.Slash(fx, to + offset, c, 620f, 9f, 0.4f);
                    }

                    UiFx.Ring(fx, to, c, 30f, 420f, 0.5f);
                    break;

                case StarterCardLibrary.ArtoriasId:
                    // ฟันดาบใหญ่สีทอง
                    Shake(shakeTarget, 12f, 0.35f);
                    Flash(fx, c, 0.25f, 0.3f);

                    UiFx.Slash(fx, to, c, 620f, 46f, 0.45f);
                    UiFx.Slash(fx, to, Color.white, 540f, 18f, 0.35f);
                    UiFx.Ring(fx, to, c, 40f, 460f, 0.5f);
                    UiFx.Burst(fx, to, c, 28, 380f, 28f, 0.8f, 220f);
                    break;

                case StarterCardLibrary.AinzId:
                    // พลังเวทสีเลือด: คลื่นซ้อนและเสาแสง
                    Shake(shakeTarget, 14f, 0.45f);
                    Flash(fx, c, 0.35f, 0.45f);

                    UiFx.Beam(fx, to, new Color(c.r, c.g, c.b, 0.85f), 200f, 1100f, 0.8f);
                    UiFx.Ring(fx, to, c, 40f, 620f, 0.7f);

                    Timeline(fx, At(0.15f, () => UiFx.Ring(fx, to, c, 40f, 860f, 0.8f)));

                    UiFx.Burst(fx, to, c, 36, 300f, 32f, 1.1f, -120f);
                    break;

                default:
                    // Sung Jin-Woo: เงาม่วงพุ่งออก
                    Shake(shakeTarget, 9f, 0.3f);

                    for (int i = 0; i < 3; i++)
                        UiFx.Slash(fx, to, c, 560f, 26f, 0.4f);

                    UiFx.Ring(fx, to, c, 40f, 420f, 0.5f);
                    UiFx.Burst(fx, to, c, 40, 340f, 32f, 1.1f, -200f);
                    break;
            }
        }

        // ---------- Devil Trigger ----------

        public static void DevilTrigger(
            Transform fx, RectTransform shakeTarget, Vector3 pos)
        {
            if (fx == null)
                return;

            Color blue = new Color(0.27f, 0.62f, 1f, 1f);

            Flash(fx, blue, 0.6f, 0.7f);

            Shake(shakeTarget, 22f, 0.7f);

            UiFx.Beam(fx, pos, new Color(blue.r, blue.g, blue.b, 0.85f), 260f, 1500f, 1.1f);
            UiFx.Beam(fx, pos, new Color(0.85f, 0.95f, 1f, 0.9f), 100f, 1300f, 0.8f);

            UiFx.Ring(fx, pos, blue, 60f, 820f, 0.85f);
            UiFx.Burst(fx, pos, blue, 64, 560f, 40f, 1.3f, -140f);

            Title(fx, pos + new Vector3(0f, 240f, 0f), "DEVIL TRIGGER", blue, 64);

            Timeline(fx,
                At(0.1f, () =>
                {
                    UiFx.Ring(fx, pos, new Color(0.85f, 0.95f, 1f, 0.9f), 40f, 600f, 0.6f);

                    for (int i = 0; i < 6; i++)
                        UiFx.Slash(fx, pos, blue, 640f, 12f, 0.4f);
                }),
                At(0.3f, () => UiFx.Ring(fx, pos, blue, 40f, 1200f, 1.0f)),
                At(0.5f, () => UiFx.Burst(fx, pos, new Color(0.7f, 0.9f, 1f, 1f), 40, 360f, 30f, 1.1f, -200f)));
        }

        // ---------- ปลดล็อกการ์ดลับ ----------

        /// <summary>จังหวะการ์ดกระแทกลงกลางจอตอนเปิดตัวใน Banner ปลดล็อก</summary>
        public static void Impact(Transform fx, Vector3 pos, Color c)
        {
            Flash(fx, Color.Lerp(c, Color.white, 0.6f), 0.7f, 0.55f);

            UiFx.Beam(fx, pos, new Color(c.r, c.g, c.b, 0.85f), 360f, 1700f, 1.1f);
            UiFx.Beam(fx, pos, new Color(1f, 1f, 1f, 0.9f), 120f, 1500f, 0.8f);

            UiFx.Ring(fx, pos, c, 80f, 1000f, 0.9f);
            UiFx.Ring(fx, pos, new Color(1f, 1f, 1f, 0.9f), 60f, 700f, 0.6f);

            UiFx.Burst(fx, pos, c, 90, 700f, 42f, 1.4f, -80f);

            for (int i = 0; i < 4; i++)
                UiFx.Slash(fx, pos, c, 700f, 22f, 0.4f);

            Sfx.Play(SoundId.Hit);
        }
    }

    // ---------- ตัวช่วย ----------

    public class ScreenFlash : MonoBehaviour
    {
        private Image image;
        private Color color;
        private float peak;
        private float life;
        private float age;

        public void Init(Image target, Color tint, float peakAlpha, float seconds)
        {
            image = target;
            color = tint;
            peak = peakAlpha;
            life = Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(age / life);

            // พุ่งขึ้นเร็ว จางลงช้า
            float alpha = t < 0.12f ? t / 0.12f : Mathf.Pow(1f - (t - 0.12f) / 0.88f, 2f);

            if (image != null)
                image.color = new Color(color.r, color.g, color.b, peak * alpha);

            if (t >= 1f)
                Destroy(gameObject);
        }
    }

    public class ShakeRect : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 basePosition;
        private bool active;
        private float magnitude;
        private float life;
        private float age;

        public void Begin(float strength, float seconds)
        {
            if (rect == null)
                rect = transform as RectTransform;

            if (rect == null)
                return;

            // ถ้ากำลังสั่นอยู่ ใช้ตำแหน่งเดิมเป็นฐาน ไม่ให้ค่อยๆ เลื่อนหนี
            if (!active)
                basePosition = rect.anchoredPosition;

            active = true;
            magnitude = Mathf.Max(magnitude * (1f - age / Mathf.Max(0.01f, life)), strength);
            life = Mathf.Max(0.05f, seconds);
            age = 0f;
        }

        private void Update()
        {
            if (!active || rect == null)
                return;

            age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(age / life);

            if (t >= 1f)
            {
                Stop();
                return;
            }

            Vector2 jitter = UnityEngine.Random.insideUnitCircle * magnitude * (1f - t);

            rect.anchoredPosition = basePosition + jitter;
        }

        private void Stop()
        {
            if (rect != null)
                rect.anchoredPosition = basePosition;

            active = false;
            magnitude = 0f;
        }

        private void OnDisable()
        {
            if (active)
                Stop();
        }
    }

    public class SecretTitle : MonoBehaviour
    {
        private const float Hold = 1.4f;
        private const float Fade = 0.6f;

        private Text text;
        private Color color;
        private float age;

        public void Init(Text target)
        {
            text = target;
            color = target.color;

            transform.localScale = Vector3.one * 0.5f;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float scale;

            if (age < 0.3f)
                scale = Mathf.Lerp(0.5f, 1.18f, age / 0.3f);
            else if (age < 0.5f)
                scale = Mathf.Lerp(1.18f, 1f, (age - 0.3f) / 0.2f);
            else
                scale = 1f + 0.02f * Mathf.Sin(age * 6f);

            transform.localScale = Vector3.one * scale;

            transform.position += Vector3.up * (14f * Time.unscaledDeltaTime);

            float alpha = age < Hold ? 1f : 1f - (age - Hold) / Fade;

            if (text != null)
                text.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));

            if (age >= Hold + Fade)
                Destroy(gameObject);
        }
    }

    public class FxTimeline : MonoBehaviour
    {
        private KeyValuePair<float, Action>[] steps;
        private bool[] done;
        private float age;

        public void Init(KeyValuePair<float, Action>[] sequence)
        {
            steps = sequence;
            done = new bool[sequence.Length];
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            bool all = true;

            for (int i = 0; i < steps.Length; i++)
            {
                if (done[i])
                    continue;

                if (age >= steps[i].Key)
                {
                    done[i] = true;

                    if (steps[i].Value != null)
                        steps[i].Value();
                }
                else
                {
                    all = false;
                }
            }

            if (all)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// การ์ดลับในหน้าจอปลดล็อก: พลิกหมุนเข้ามากระแทกกลางจอ แล้วลอยเรืองแสงเบาๆ
    /// </summary>
    public class CardReveal : MonoBehaviour
    {
        private const float Intro = 0.75f;

        private RectTransform rect;
        private CanvasGroup group;
        private Transform fx;
        private Color tint;
        private Vector2 basePosition;
        private float age;
        private bool landed;

        public static void Attach(CardView view, Transform fxParent, Color color)
        {
            CardReveal reveal = view.gameObject.AddComponent<CardReveal>();

            reveal.rect = (RectTransform)view.transform;
            reveal.group = view.GetComponent<CanvasGroup>();
            reveal.fx = fxParent;
            reveal.tint = color;
            reveal.basePosition = reveal.rect.anchoredPosition;

            if (reveal.group != null)
            {
                reveal.group.alpha = 0f;
                reveal.group.blocksRaycasts = false;
                reveal.group.interactable = false;
            }

            reveal.rect.localScale = new Vector3(0f, 0.01f, 1f);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            float p = t - 1f;

            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            if (age < Intro)
            {
                float t = age / Intro;

                float size = EaseOutBack(t);

                // พลิกการ์ดสองครั้ง (กว้างหดเป็นศูนย์แล้วเปิดออก)
                float flip = Mathf.Abs(Mathf.Cos((1f - t) * Mathf.PI * 1.5f));

                rect.localScale = new Vector3(size * flip, size, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * -20f);

                if (group != null)
                    group.alpha = Mathf.Clamp01(t * 3f);

                return;
            }

            if (!landed)
            {
                landed = true;

                if (group != null)
                    group.alpha = 1f;

                SecretFx.Impact(fx, rect.position, tint);
            }

            float idle = age - Intro;

            rect.anchoredPosition = basePosition + new Vector2(0f, Mathf.Sin(idle * 1.7f) * 9f);

            float breathe = 1f + 0.025f * Mathf.Sin(idle * 2.3f);

            rect.localScale = new Vector3(breathe, breathe, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(idle * 1.1f) * 1.2f);
        }
    }

    /// <summary>
    /// ออร่าของการ์ดลับ: กรอบเรืองแสงกะพริบ แสงสะท้อนพาดผ่านรูป และประกายลอยจากขอบการ์ด
    /// ติดกับ CardView อัตโนมัติ (คอลเลกชัน / เด็ค / การต่อสู้)
    /// </summary>
    public class SecretAura : MonoBehaviour
    {
        private static readonly Vector3[] corners = new Vector3[4];

        private Image ring;
        private Image shine;
        private RectTransform shineRect;
        private RectTransform rect;
        private Color tint;
        private float age;
        private float sparkTimer;
        private float phase;

        public static void Ensure(CardView view, CardData data)
        {
            if (view == null)
                return;

            SecretAura aura = view.GetComponent<SecretAura>();

            if (!SecretFx.IsSecret(data))
            {
                if (aura != null)
                    aura.SetVisible(false);

                return;
            }

            if (aura == null)
                aura = view.gameObject.AddComponent<SecretAura>();

            aura.Setup(SecretFx.Tint(data));
        }

        private void Setup(Color color)
        {
            tint = color;

            if (rect == null)
                rect = transform as RectTransform;

            if (ring == null)
            {
                ring = UIFactory.AddRing(transform, color, 10f);

                RectTransform ringRect = ring.rectTransform;

                // ใหญ่กว่าการ์ดเล็กน้อย (สัดส่วน จึงใช้ได้กับทุกขนาดการ์ด)
                ringRect.anchorMin = new Vector2(-0.045f, -0.032f);
                ringRect.anchorMax = new Vector2(1.045f, 1.032f);
                ringRect.offsetMin = Vector2.zero;
                ringRect.offsetMax = Vector2.zero;

                phase = UnityEngine.Random.value * 6.28f;
            }

            if (shine == null)
            {
                Transform art = transform.Find("ArtFrame");

                if (art != null)
                {
                    shine = UIFactory.CreatePanel(
                        art, "Shine", new Color(1f, 1f, 1f, 0f));

                    shine.sprite = UIShapes.Glow;
                    shine.type = Image.Type.Simple;
                    shine.raycastTarget = false;

                    shineRect = shine.rectTransform;

                    shineRect.anchorMin = new Vector2(0.5f, 0.5f);
                    shineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    shineRect.localRotation = Quaternion.Euler(0f, 0f, 22f);
                }
            }

            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            enabled = visible;

            if (ring != null)
                ring.gameObject.SetActive(visible);

            if (shine != null)
                shine.gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (ring == null)
                return;

            float dt = Time.unscaledDeltaTime;

            age += dt;

            float pulse = 0.5f + 0.5f * Mathf.Sin(age * 3.1f + phase);

            Color ringColor = Color.Lerp(tint, Color.white, 0.25f * pulse);

            ringColor.a = 0.35f + 0.55f * pulse;

            ring.color = ringColor;

            // แสงสะท้อนพาดผ่านรูปการ์ดทุก ~3.4 วินาที
            if (shineRect != null)
            {
                RectTransform parent = shineRect.parent as RectTransform;

                float width = parent != null ? parent.rect.width : 100f;
                float height = parent != null ? parent.rect.height : 140f;

                float cycle = (age + phase) % 3.4f;
                float t = Mathf.Clamp01(cycle / 1.1f);

                shineRect.sizeDelta = new Vector2(width * 0.5f, height * 1.7f);
                shineRect.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.9f, width * 0.9f, t), 0f);

                float alpha = cycle < 1.1f ? Mathf.Sin(t * Mathf.PI) * 0.45f : 0f;

                shine.color = new Color(
                    Mathf.Lerp(tint.r, 1f, 0.7f),
                    Mathf.Lerp(tint.g, 1f, 0.7f),
                    Mathf.Lerp(tint.b, 1f, 0.7f),
                    alpha);
            }

            // ประกายลอยขึ้นจากขอบการ์ด
            sparkTimer -= dt;

            if (sparkTimer > 0f || rect == null)
                return;

            sparkTimer = UnityEngine.Random.Range(0.12f, 0.26f);

            rect.GetWorldCorners(corners);

            float edge = UnityEngine.Random.value;

            Vector3 point;

            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0: point = Vector3.Lerp(corners[0], corners[1], edge); break;
                case 1: point = Vector3.Lerp(corners[1], corners[2], edge); break;
                case 2: point = Vector3.Lerp(corners[2], corners[3], edge); break;
                default: point = Vector3.Lerp(corners[3], corners[0], edge); break;
            }

            Color spark = UnityEngine.Random.value < 0.4f
                ? Color.Lerp(tint, Color.white, 0.6f)
                : tint;

            float size = Mathf.Max(14f, rect.rect.width * 0.14f);

            UiFx.Burst(transform, point, spark, 1, 50f, size, 1.0f, -110f);
        }
    }
}
