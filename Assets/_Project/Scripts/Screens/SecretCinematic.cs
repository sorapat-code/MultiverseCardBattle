using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ฉากไซเนมาติกเต็มจอของการ์ดลับ (ยาว ~5-8 วินาที กดที่ไหนก็ได้เพื่อข้าม)
    ///
    ///  Unlock    : ปลดล็อกการ์ดลับ จบแล้วค้างที่การ์ด รอคลิกเพื่อไปต่อ
    ///  Arrival   : การ์ดลับลงสนาม ฉากจบแล้วการ์ดบินไปยังช่องของมันบนสนาม
    ///  Transform : Devil Trigger แปลงร่าง (ไม่มีตัวการ์ด)
    ///
    /// ทุกอย่างขับด้วยเวลา (age) จึงข้ามไปท้ายฉากได้ทันที: แถบจอกว้าง ไฟสาดส่อง รัศมีหมุน
    /// การ์ดพลิกกระแทก ชื่อพิมพ์ทีละตัวอักษร ฝนประกาย และจังหวะกระแทกพร้อมจอสั่น
    /// </summary>
    public class SecretCinematic : MonoBehaviour
    {
        public enum Mode
        {
            Unlock,
            Arrival,
            Transform
        }

        private const float ExitSeconds = 0.75f;
        private const float HeroAt = 0.55f;
        private const float TitleAt = 1.7f;
        private const float LetterSeconds = 0.07f;

        private Mode mode;
        private RectTransform layer;
        private RectTransform shakeTarget;
        private CardData data;
        private Color tint;

        private Image dim;
        private Image spot;
        private Image barTop;
        private Image barBottom;
        private RectTransform rays;
        private Image[] rayImages;

        private CardView hero;
        private RectTransform heroRect;

        private Text topText;
        private Text titleText;
        private Text tagText;
        private Text hintText;

        private string topLine;
        private string title;
        private string tagline;

        private Action onFinish;
        private Action onContinue;
        private bool hasDestination;
        private Vector3 destination;

        private float age;
        private float endAt;
        private float finalAt;
        private int shownLetters;
        private float rainTimer;

        private bool exiting;
        private bool finalState;
        private bool heroLanded;
        private bool beatA;
        private bool beatB;
        private bool beatC;
        private bool opened;
        private Vector3 exitFrom;

        public RectTransform Layer
        {
            get { return layer; }
        }

        // ---------- สร้าง ----------

        /// <summary>เริ่มฉาก ส่วนที่ไม่ใช้ใส่ null</summary>
        public static SecretCinematic Play(
            RectTransform parent,
            Mode mode,
            CardData data,
            string topLine,
            string title,
            string tagline,
            RectTransform shakeTarget,
            Vector3? destination,
            Action onFinish,
            Action onContinue)
        {
            RectTransform layer = UIFactory.CreateRect("SecretCinematic", parent);

            UIFactory.Stretch(layer);

            layer.SetAsLastSibling();

            SecretCinematic cinematic = layer.gameObject.AddComponent<SecretCinematic>();

            cinematic.layer = layer;
            cinematic.mode = mode;
            cinematic.data = data;
            cinematic.tint = SecretFx.Tint(data);
            cinematic.shakeTarget = shakeTarget;
            cinematic.topLine = topLine ?? "";
            cinematic.title = (title ?? "").ToUpperInvariant();
            cinematic.tagline = tagline ?? "";
            cinematic.onFinish = onFinish;
            cinematic.onContinue = onContinue;

            if (destination.HasValue)
            {
                cinematic.hasDestination = true;
                cinematic.destination = destination.Value;
            }

            // Unlock จบแล้วค้างรอคลิก / Arrival, Transform จบเองแล้วออกจากฉาก
            cinematic.finalAt = mode == Mode.Unlock ? 5.2f : 0f;
            cinematic.endAt = mode == Mode.Arrival ? 5.2f : (mode == Mode.Transform ? 4.4f : float.MaxValue);

            cinematic.Build();

            return cinematic;
        }

        private void Build()
        {
            // ตัวรับคลิก (ข้ามฉาก / ไปต่อ)
            Image blocker = layer.gameObject.AddComponent<Image>();

            blocker.color = new Color(0f, 0f, 0f, 0f);

            Button click = layer.gameObject.AddComponent<Button>();

            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(OnClicked);

            // ฉากมืด (เกินขอบจอเล็กน้อยกันช่องว่างตอนจอสั่น)
            dim = UIFactory.CreatePanel(
                layer, "Dim", new Color(tint.r * 0.12f, tint.g * 0.12f, tint.b * 0.12f, 0f));

            UIFactory.Stretch(dim.rectTransform);

            dim.rectTransform.offsetMin = new Vector2(-90f, -90f);
            dim.rectTransform.offsetMax = new Vector2(90f, 90f);
            dim.raycastTarget = false;

            // ไฟสาดส่องหลังการ์ด
            spot = UIFactory.CreatePanel(layer, "Spot", new Color(tint.r, tint.g, tint.b, 0f));

            spot.sprite = UIShapes.Glow;
            spot.type = Image.Type.Simple;
            spot.raycastTarget = false;

            RectTransform spotRect = spot.rectTransform;

            spotRect.anchorMin = new Vector2(0.5f, 0.55f);
            spotRect.anchorMax = new Vector2(0.5f, 0.55f);
            spotRect.sizeDelta = new Vector2(1700f, 1300f);

            // รัศมีแสงหมุนรอบการ์ด
            rays = UIFactory.CreateRect("Rays", layer);

            rays.anchorMin = new Vector2(0.5f, 0.55f);
            rays.anchorMax = new Vector2(0.5f, 0.55f);
            rays.sizeDelta = Vector2.zero;

            const int rayCount = 12;

            rayImages = new Image[rayCount];

            for (int i = 0; i < rayCount; i++)
            {
                Image ray = UIFactory.CreatePanel(rays, "Ray", new Color(tint.r, tint.g, tint.b, 0f));

                ray.sprite = UIShapes.Glow;
                ray.type = Image.Type.Simple;
                ray.raycastTarget = false;

                ray.rectTransform.sizeDelta = new Vector2(2200f, i % 2 == 0 ? 70f : 36f);
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * (180f / rayCount * 2f));

                rayImages[i] = ray;
            }

            // แถบจอกว้างบน-ล่าง
            barTop = MakeBar("BarTop", true);
            barBottom = MakeBar("BarBottom", false);

            // ข้อความ
            topText = MakeText(topLine, 54, FontStyle.Bold, 0.03f, 0.84f, 0.97f, 0.95f, tint);
            titleText = MakeText("", 92, FontStyle.Bold, 0.03f, 0.17f, 0.97f, 0.3f, tint);
            tagText = MakeText(tagline, 34, FontStyle.Italic, 0.1f, 0.1f, 0.9f, 0.17f, UIFactory.TextColor);
            hintText = MakeText("Click to continue", 24, FontStyle.Normal, 0.3f, 0.025f, 0.7f, 0.09f, UIFactory.TextColor);

            if (mode == Mode.Transform)
            {
                // ไม่มีตัวการ์ด: ชื่อขึ้นกลางจอ
                UIFactory.SetAnchors(titleText.rectTransform, 0.03f, 0.44f, 0.97f, 0.62f);
                UIFactory.SetAnchors(tagText.rectTransform, 0.1f, 0.36f, 0.9f, 0.44f);
            }

            Sfx.Play(SoundId.Pack);
        }

        private Image MakeBar(string name, bool top)
        {
            Image bar = UIFactory.CreatePanel(layer, name, new Color(0f, 0f, 0f, 0.96f));

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

        private Text MakeText(
            string content, int size, FontStyle style,
            float x0, float y0, float x1, float y1, Color color)
        {
            Text text = ScreenKit.AddLabel(
                layer, content, size, TextAnchor.MiddleCenter, x0, y0, x1, y1, style);

            text.color = new Color(color.r, color.g, color.b, 0f);
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = text.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);

            return text;
        }

        // ---------- คลิก ----------

        private void OnClicked()
        {
            if (exiting)
                return;

            if (mode == Mode.Unlock)
            {
                if (finalState)
                {
                    if (onContinue != null)
                        onContinue();

                    return;
                }

                Skip(finalAt);
                return;
            }

            Skip(endAt);
        }

        private void Skip(float toAge)
        {
            if (age < toAge)
                age = toAge;

            beatA = true;
            beatB = true;
            beatC = true;
        }

        // ---------- วนทุกเฟรม ----------

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            UpdateBackdrop();
            UpdateHero();
            UpdateTexts();
            UpdateBeats();
            UpdateRain();

            if (mode == Mode.Unlock && !finalState && age >= finalAt)
                finalState = true;

            if (!exiting && age >= endAt)
            {
                exiting = true;
                exitFrom = heroRect != null ? heroRect.position : Vector3.zero;

                CardReveal reveal = hero != null ? hero.GetComponent<CardReveal>() : null;

                if (reveal != null)
                    Destroy(reveal);
            }

            if (exiting)
                UpdateExit();
        }

        private void UpdateBackdrop()
        {
            float open = Mathf.Clamp01(age / 0.6f);
            float close = exiting ? Mathf.Clamp01((age - endAt) / ExitSeconds) : 0f;
            float visible = open * (1f - close);

            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0.9f * visible);

            float barHeight = 140f * visible;

            barTop.rectTransform.sizeDelta = new Vector2(barTop.rectTransform.sizeDelta.x, barHeight);
            barBottom.rectTransform.sizeDelta = new Vector2(barBottom.rectTransform.sizeDelta.x, barHeight);

            float pulse = 0.5f + 0.5f * Mathf.Sin(age * 2.6f);

            spot.color = new Color(tint.r, tint.g, tint.b, (0.28f + 0.14f * pulse) * visible * Mathf.Clamp01((age - 0.2f) / 0.8f));

            rays.localRotation = Quaternion.Euler(0f, 0f, age * 7f);

            float rayAlpha = 0.2f * visible * Mathf.Clamp01((age - 0.4f) / 1.0f);

            for (int i = 0; i < rayImages.Length; i++)
            {
                float flicker = 0.7f + 0.3f * Mathf.Sin(age * 3f + i);

                rayImages[i].color = new Color(tint.r, tint.g, tint.b, rayAlpha * flicker);
            }
        }

        private void UpdateHero()
        {
            if (mode == Mode.Transform || data == null)
                return;

            if (hero == null && age >= HeroAt && !exiting)
            {
                hero = CardView.Create(layer, new Vector2(340f, 476f));

                hero.ShowData(data);

                heroRect = (RectTransform)hero.transform;

                heroRect.anchorMin = new Vector2(0.5f, 0.56f);
                heroRect.anchorMax = new Vector2(0.5f, 0.56f);
                heroRect.anchoredPosition = Vector2.zero;

                CardReveal.Attach(hero, layer, tint);
            }

            // จังหวะการ์ดลงกระแทก (CardReveal ยิงเอฟเฟกต์เอง) เพิ่มจอสั่นหนักๆ
            if (hero != null && !heroLanded && age >= HeroAt + 0.75f)
            {
                heroLanded = true;

                SecretFx.Shake(shakeTarget != null ? shakeTarget : layer, 26f, 0.7f);
            }
        }

        private void UpdateTexts()
        {
            float close = exiting ? Mathf.Clamp01((age - endAt) / (ExitSeconds * 0.6f)) : 0f;
            float fade = 1f - close;

            // บรรทัดบน (Unlock)
            SetAlpha(topText, Mathf.Clamp01((age - 0.9f) / 0.6f) * fade);

            // ชื่อพิมพ์ทีละตัวอักษร
            if (title.Length > 0)
            {
                int target = Mathf.Clamp(Mathf.FloorToInt((age - TitleAt) / LetterSeconds) + 1, 0, title.Length);

                if (age < TitleAt)
                    target = 0;

                if (target != shownLetters)
                {
                    bool grew = target > shownLetters;

                    shownLetters = target;

                    titleText.text = title.Substring(0, shownLetters);

                    if (grew && !exiting)
                    {
                        Vector3 tip = titleText.rectTransform.position +
                            Vector3.right * (titleText.preferredWidth * 0.5f * layer.lossyScale.x);

                        UiFx.Burst(layer, tip, Color.Lerp(tint, Color.white, 0.5f), 4, 260f, 22f, 0.6f, 200f);
                    }
                }

                SetAlpha(titleText, (shownLetters > 0 ? 1f : 0f) * fade);
            }

            float tagStart = TitleAt + title.Length * LetterSeconds + 0.2f;

            SetAlpha(tagText, Mathf.Clamp01((age - tagStart) / 0.7f) * fade);

            SetAlpha(hintText,
                mode == Mode.Unlock && finalState
                    ? 0.55f + 0.45f * Mathf.Sin(age * 3f)
                    : 0f);
        }

        private static void SetAlpha(Text text, float alpha)
        {
            if (text == null)
                return;

            Color c = text.color;

            c.a = Mathf.Clamp01(alpha);

            text.color = c;
        }

        private void UpdateBeats()
        {
            if (exiting)
                return;

            Vector3 center = layer.TransformPoint(new Vector3(0f, layer.rect.height * 0.06f, 0f));

            if (!opened && age >= 0.15f)
            {
                opened = true;

                SecretFx.Flash(layer, tint, 0.55f, 0.6f);
                SecretFx.Shake(shakeTarget != null ? shakeTarget : layer, 12f, 0.4f);

                UiFx.Ring(layer, center, tint, 60f, 1100f, 0.9f);

                if (mode == Mode.Transform)
                {
                    UiFx.Beam(layer, center, new Color(tint.r, tint.g, tint.b, 0.85f), 300f, 1500f, 1.0f);
                    Sfx.Play(SoundId.Hit);
                }
            }

            if (!beatA && age >= 3.0f)
            {
                beatA = true;

                SecretFx.Flash(layer, Color.Lerp(tint, Color.white, 0.4f), 0.4f, 0.5f);
                SecretFx.Shake(shakeTarget != null ? shakeTarget : layer, 14f, 0.5f);

                UiFx.Ring(layer, center, tint, 80f, 1300f, 1.0f);
                UiFx.Ring(layer, center, Color.white, 40f, 800f, 0.7f);
                UiFx.Burst(layer, center, tint, 70, 760f, 38f, 1.3f, -40f);

                Sfx.Play(SoundId.Win);
            }

            if (!beatB && age >= 3.8f)
            {
                beatB = true;

                for (int i = 0; i < 6; i++)
                {
                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-420f, 420f),
                        UnityEngine.Random.Range(-260f, 260f), 0f);

                    UiFx.Slash(layer, center + offset, tint, 780f, 18f, 0.45f);
                }

                SecretFx.Shake(shakeTarget != null ? shakeTarget : layer, 10f, 0.4f);
            }

            if (!beatC && age >= 4.5f)
            {
                beatC = true;

                UiFx.Beam(layer, center, new Color(tint.r, tint.g, tint.b, 0.8f), 420f, 1800f, 1.1f);
                UiFx.Ring(layer, center, tint, 60f, 1500f, 1.1f);
            }
        }

        // ฝนประกายตกจากด้านบนของจอ
        private void UpdateRain()
        {
            if (age < 1.0f || exiting)
                return;

            rainTimer -= Time.unscaledDeltaTime;

            if (rainTimer > 0f)
                return;

            rainTimer = 0.035f;

            Rect rect = layer.rect;

            for (int i = 0; i < 2; i++)
            {
                Vector3 local = new Vector3(
                    UnityEngine.Random.Range(rect.xMin, rect.xMax), rect.yMax, 0f);

                Color c = UnityEngine.Random.value < 0.35f
                    ? Color.Lerp(tint, Color.white, 0.7f)
                    : tint;

                UiFx.Burst(layer, layer.TransformPoint(local), c, 1, 50f, 24f, 2.2f, 420f);
            }
        }

        private void UpdateExit()
        {
            float t = Mathf.Clamp01((age - endAt) / ExitSeconds);
            float ease = t * t * (3f - 2f * t);

            if (heroRect != null)
            {
                if (hasDestination)
                {
                    heroRect.position = Vector3.Lerp(exitFrom, destination, ease);
                    heroRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, ease);
                }
                else
                {
                    heroRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, ease);
                }

                heroRect.localRotation = Quaternion.identity;

                CanvasGroup group = hero.GetComponent<CanvasGroup>();

                if (group != null && !hasDestination)
                    group.alpha = 1f - t;
            }

            if (t >= 1f)
                Finish();
        }

        private void Finish()
        {
            Action done = onFinish;

            onFinish = null;

            Destroy(layer.gameObject);

            if (done != null)
                done();
        }
    }
}
