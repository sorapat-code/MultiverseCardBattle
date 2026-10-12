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
    /// ฐานของคัตซีนเฉพาะตัวละคร: จอดำ แถบจอกว้าง บทพูด และตัวยิงเหตุการณ์ตามเวลา
    /// คลาสลูกกำหนดองค์ประกอบ (Build), จังหวะ (Plan) และจังหวะต่อเฟรม (OnTick)
    /// SecretCinematic เป็นคนขับเวลา (age) ส่วนการ์ดฮีโร่/ชื่อ/การบินลงช่องยังเป็นของ SecretCinematic
    /// </summary>
    public abstract class CharacterScene : MonoBehaviour
    {
        private struct Step
        {
            public float Time;
            public Action Action;
            public bool Done;
        }

        protected RectTransform layer;
        protected RectTransform root;
        protected RectTransform shakeTarget;
        protected CardData data;

        private CanvasGroup group;
        private Image black;
        private Image barTop;
        private Image barBottom;
        private Text subtitle;
        private string subtitleWanted = "";

        private readonly List<Step> steps = new List<Step>();

        // เวลาของฉาก (วินาที) ให้คลาสลูกกำหนด
        public virtual float HeroTime { get { return 4.2f; } }
        public virtual float TitleTime { get { return 4.95f; } }
        public virtual float ArrivalEnd { get { return 7.6f; } }
        public virtual float UnlockFinal { get { return 7.0f; } }
        public virtual float TransformEnd { get { return 5.2f; } }

        protected virtual Color SubtitleColor { get { return new Color(0.9f, 0.97f, 1f, 1f); } }
        protected virtual float BarHeight { get { return 130f; } }

        /// <summary>ความทึบของจอดำตามเวลา</summary>
        protected virtual float BlackAlpha(float age) { return 1f; }

        protected abstract void Build();
        protected abstract void Plan();
        protected virtual void OnTick(float age) { }

        // ---------- สร้าง ----------

        /// <summary>คืน null ถ้าการ์ดนี้ไม่มีคัตซีนเฉพาะตัว (ใช้ไทม์ไลน์กลางแทน)</summary>
        public static CharacterScene Create(
            RectTransform layer, CardData data, SecretCinematic.Mode mode, RectTransform shakeTarget)
        {
            if (data == null)
                return null;

            Type type = null;

            if (mode == SecretCinematic.Mode.Transform)
            {
                if (data.CardID == StarterCardLibrary.DevilTriggerId)
                    type = typeof(DevilTriggerScene);
            }
            else
            {
                switch (data.CardID)
                {
                    case StarterCardLibrary.VergilId: type = typeof(VergilScene); break;
                    case StarterCardLibrary.SecretId: type = typeof(JinWooScene); break;
                    case StarterCardLibrary.ArtoriasId: type = typeof(ArtoriasScene); break;
                    case StarterCardLibrary.AinzId: type = typeof(AinzScene); break;
                    case StarterCardLibrary.AemeathId: type = typeof(AemeathScene); break;
                }
            }

            if (type == null)
                return null;

            RectTransform root = UIFactory.CreateRect("CharacterScene", layer);

            UIFactory.Stretch(root);

            root.SetAsFirstSibling();

            CharacterScene scene = (CharacterScene)root.gameObject.AddComponent(type);

            scene.layer = layer;
            scene.root = root;
            scene.data = data;
            scene.shakeTarget = shakeTarget != null ? shakeTarget : layer;

            scene.group = root.gameObject.AddComponent<CanvasGroup>();
            scene.group.blocksRaycasts = false;
            scene.group.interactable = false;

            scene.black = UIFactory.CreatePanel(root, "Black", Color.black);

            UIFactory.Stretch(scene.black.rectTransform);

            scene.black.rectTransform.offsetMin = new Vector2(-90f, -90f);
            scene.black.rectTransform.offsetMax = new Vector2(90f, 90f);
            scene.black.raycastTarget = false;

            scene.Build();

            scene.barTop = scene.MakeBar(true);
            scene.barBottom = scene.MakeBar(false);

            scene.subtitle = ScreenKit.AddLabel(
                root, "", 40, TextAnchor.MiddleCenter, 0.1f, 0.15f, 0.9f, 0.23f, FontStyle.Italic);

            Color sc = scene.SubtitleColor;

            scene.subtitle.color = new Color(sc.r, sc.g, sc.b, 0f);
            scene.subtitle.raycastTarget = false;
            scene.subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            scene.subtitle.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = scene.subtitle.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            scene.Plan();

            return scene;
        }

        private Image MakeBar(bool top)
        {
            Image bar = UIFactory.CreatePanel(root, top ? "BarTop" : "BarBottom", Color.black);

            bar.raycastTarget = false;

            RectTransform rect = bar.rectTransform;

            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.offsetMin = new Vector2(-90f, 0f);
            rect.offsetMax = new Vector2(90f, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);

            return bar;
        }

        // ---------- ตัวช่วยสำหรับคลาสลูก ----------

        protected void At(float time, Action action)
        {
            steps.Add(new Step { Time = time, Action = action });
        }

        protected void SetSubtitle(string text)
        {
            subtitleWanted = text ?? "";

            if (subtitle != null && subtitleWanted.Length > 0)
                subtitle.text = subtitleWanted;
        }

        protected static void CenterRect(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        protected Image MakeGlow(string name, Vector2 pos, Vector2 size, Color color)
        {
            Image img = UIFactory.CreatePanel(root, name, color);

            img.sprite = UIShapes.Glow;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            CenterRect(img.rectTransform, pos, size);

            return img;
        }

        protected Image MakeSolid(string name, Vector2 pos, Vector2 size, Color color)
        {
            Image img = UIFactory.CreatePanel(root, name, color);

            img.raycastTarget = false;

            CenterRect(img.rectTransform, pos, size);

            return img;
        }

        /// <summary>จุดในจอ (anchored จากกลางจอ) -> พิกัดโลก</summary>
        protected Vector3 World(Vector2 anchored)
        {
            return root.TransformPoint(new Vector3(anchored.x, anchored.y, 0f));
        }

        protected Rect Stage
        {
            get { return layer.rect; }
        }

        /// <summary>สายฟ้า: เส้นหักหลายท่อนจากจุด a ไป b</summary>
        protected void Bolt(Vector2 a, Vector2 b, float life, float thick, Color glow, Color core)
        {
            int segments = 6;
            Vector2 previous = a;
            Vector2 direction = (b - a);
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized;

            for (int i = 1; i <= segments; i++)
            {
                Vector2 next = Vector2.Lerp(a, b, i / (float)segments);

                if (i < segments)
                    next += normal * UnityEngine.Random.Range(-60f, 60f);

                Vector2 delta = next - previous;
                float length = delta.magnitude;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

                VergilCut.Make(root, (previous + next) * 0.5f, angle, length + 6f, thick, life, glow, core);

                previous = next;
            }
        }

        // ---------- ต่อเฟรม ----------

        /// <summary>SecretCinematic เรียกทุกเฟรม</summary>
        public void Tick(float age, float endAt)
        {
            // ข้ามฉาก (เวลากระโดด): ไม่ยิงเอฟเฟกต์ที่ผ่านไปแล้วรวดเดียว
            for (int i = 0; i < steps.Count; i++)
            {
                Step step = steps[i];

                if (step.Done || age < step.Time)
                    continue;

                step.Done = true;
                steps[i] = step;

                if (age - step.Time < 0.4f)
                    step.Action();
            }

            float open = Mathf.Clamp01(age / 0.6f);
            float leave = Mathf.Clamp01((age - endAt) / 0.6f);

            group.alpha = 1f - leave;

            black.color = new Color(0f, 0f, 0f, BlackAlpha(age));

            float barHeight = BarHeight * open;

            barTop.rectTransform.sizeDelta = new Vector2(barTop.rectTransform.sizeDelta.x, barHeight);
            barBottom.rectTransform.sizeDelta = new Vector2(barBottom.rectTransform.sizeDelta.x, barHeight);

            if (subtitle != null)
            {
                Color c = subtitle.color;

                float target = subtitleWanted.Length == 0 ? 0f : 1f;

                c.a = Mathf.MoveTowards(c.a, target, Time.unscaledDeltaTime * 4f);

                subtitle.color = c;
            }

            OnTick(age);
        }
    }

    // =====================================================================
    //  ชิ้นส่วนที่ใช้ร่วมกัน
    // =====================================================================

    /// <summary>วงแหวนที่ขยาย/หดตามเวลา หมุนได้ แบนได้ (ใช้เป็นคลื่นพื้น/วงเวท)</summary>
    public class RingAnim : MonoBehaviour
    {
        private Image image;
        private RectTransform rect;
        private Color color;
        private float from;
        private float to;
        private float growTime;
        private float life;
        private float fadeOut;
        private float spin;
        private float age;

        public static RingAnim Make(
            Transform parent, Vector2 anchored, Color color,
            float from, float to, float growTime, float life, float fadeOut,
            float spin = 0f, float squash = 1f)
        {
            Image img = UIFactory.CreatePanel(parent, "RingAnim", color);

            img.sprite = UIShapes.RingCircle;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            RectTransform rect = img.rectTransform;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = Vector2.one * from;
            rect.localScale = new Vector3(1f, squash, 1f);

            RingAnim ring = img.gameObject.AddComponent<RingAnim>();

            ring.image = img;
            ring.rect = rect;
            ring.color = color;
            ring.from = from;
            ring.to = to;
            ring.growTime = Mathf.Max(0.05f, growTime);
            ring.life = Mathf.Max(0.1f, life);
            ring.fadeOut = Mathf.Max(0.05f, fadeOut);
            ring.spin = spin;

            return ring;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float g = Mathf.Clamp01(age / growTime);
            float ease = 1f - (1f - g) * (1f - g) * (1f - g);

            rect.sizeDelta = Vector2.one * Mathf.Lerp(from, to, ease);
            rect.Rotate(0f, 0f, spin * Time.unscaledDeltaTime);

            float a = Mathf.Clamp01(age / 0.12f);

            if (life - age < fadeOut)
                a *= Mathf.Clamp01((life - age) / fadeOut);

            Color c = color;

            c.a *= a;

            image.color = c;

            if (age >= life)
                Destroy(gameObject);
        }
    }

    /// <summary>ข้อความใหญ่พุ่งกระแทกเข้ามา (ซูมจากใหญ่ -> ปกติ) มีเงาแยกสีแดง/ฟ้า แล้วจางหาย</summary>
    public class SlamText : MonoBehaviour
    {
        private Text[] texts;
        private Color[] colors;
        private RectTransform rect;
        private float hold;
        private float fade;
        private float age;

        public static SlamText Make(
            Transform parent, string content, int size, Color color,
            Vector2 anchored, float hold, float fade)
        {
            RectTransform rect = UIFactory.CreateRect("SlamText", parent);

            CenterRect(rect, anchored, Vector2.zero);

            SlamText slam = rect.gameObject.AddComponent<SlamText>();

            slam.rect = rect;
            slam.hold = hold;
            slam.fade = Mathf.Max(0.05f, fade);

            Color red = new Color(1f, 0.2f, 0.25f, 0.6f);
            Color cyan = new Color(0.2f, 0.9f, 1f, 0.6f);

            slam.texts = new Text[3];
            slam.colors = new[] { red, cyan, color };

            Vector2[] offsets = { new Vector2(-7f, 0f), new Vector2(7f, 0f), Vector2.zero };

            for (int i = 0; i < 3; i++)
            {
                Text text = UIFactory.CreateText(
                    rect, content, size, slam.colors[i], TextAnchor.MiddleCenter, FontStyle.Bold);

                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                CenterRect(text.rectTransform, offsets[i], new Vector2(1800f, size * 1.6f));

                if (i == 2)
                {
                    Outline outline = text.gameObject.AddComponent<Outline>();

                    outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    outline.effectDistance = new Vector2(4f, -4f);
                }

                slam.texts[i] = text;
            }

            return slam;
        }

        private static void CenterRect(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float g = Mathf.Clamp01(age / 0.2f);
            float ease = 1f - (1f - g) * (1f - g) * (1f - g);

            rect.localScale = Vector3.one * Mathf.Lerp(3.2f, 1f, ease);

            float a = Mathf.Clamp01(age / 0.06f);

            if (age > hold)
                a *= 1f - Mathf.Clamp01((age - hold) / fade);

            for (int i = 0; i < texts.Length; i++)
            {
                Color c = colors[i];

                c.a *= a;

                texts[i].color = c;
            }

            if (age >= hold + fade)
                Destroy(gameObject);
        }
    }

    /// <summary>ดวงตาเรืองแสงคู่หนึ่ง (กะพริบเข้า ลุกวาบ ก้มลงแล้วจาง) ใช้กับกองทัพเงาของ Jin-Woo</summary>
    public class EyePair : MonoBehaviour
    {
        private Image[] images;
        private Color[] baseColors;
        private RectTransform rect;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private float age;
        private bool flared;
        private float flareAge;
        private bool bowing;
        private float bowAge;

        public static EyePair Make(Transform parent, Vector2 anchored, float scale, Color color)
        {
            RectTransform rect = UIFactory.CreateRect("EyePair", parent);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one * scale;

            EyePair pair = rect.gameObject.AddComponent<EyePair>();

            pair.rect = rect;
            pair.basePosition = anchored;
            pair.baseScale = Vector3.one * scale;

            pair.images = new Image[3];
            pair.baseColors = new Color[3];

            Color glow = new Color(color.r, color.g, color.b, 0.45f);
            Color eye = Color.Lerp(color, Color.white, 0.7f);

            pair.baseColors[0] = glow;
            pair.baseColors[1] = eye;
            pair.baseColors[2] = eye;

            for (int i = 0; i < 3; i++)
            {
                Image img = UIFactory.CreatePanel(rect, "E" + i, pair.baseColors[i]);

                img.raycastTarget = false;
                img.sprite = i == 0 ? UIShapes.Glow : UIShapes.Circle;
                img.type = Image.Type.Simple;

                RectTransform r = img.rectTransform;

                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);

                if (i == 0)
                {
                    r.sizeDelta = new Vector2(110f, 56f);
                    r.anchoredPosition = Vector2.zero;
                }
                else
                {
                    r.sizeDelta = new Vector2(20f, 9f);
                    r.anchoredPosition = new Vector2(i == 1 ? -18f : 18f, 0f);
                }

                pair.images[i] = img;
            }

            return pair;
        }

        public void Flare()
        {
            flared = true;
        }

        public void Bow()
        {
            bowing = true;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            age += dt;

            float a = Mathf.Clamp01(age / 0.25f);

            // กะพริบเล็กน้อยตอนลืมตา
            if (age > 0.35f && age < 0.43f)
                a *= 0.2f;

            Vector3 scale = baseScale;

            if (flared)
            {
                flareAge += dt;

                float f = Mathf.Clamp01(flareAge / 0.15f);

                scale *= 1f + 0.7f * f;
            }

            Vector2 position = basePosition;

            if (bowing)
            {
                bowAge += dt;

                float b = Mathf.Clamp01(bowAge / 0.6f);

                position.y -= 90f * b * b;
                a *= 1f - b;

                if (b >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            rect.anchoredPosition = position;
            rect.localScale = scale;

            for (int i = 0; i < images.Length; i++)
            {
                Color c = baseColors[i];

                if (flared && i > 0)
                    c = Color.Lerp(c, Color.white, 0.8f);

                c.a *= a * (flared ? 1.5f : 1f);

                images[i].color = c;
            }
        }
    }
}
