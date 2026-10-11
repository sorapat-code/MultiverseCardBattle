using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// คัตซีนเฉพาะตัวของ Vergil ("Judgment Cut") ใช้แทนไทม์ไลน์กลางของ SecretCinematic
    ///
    ///  0.0  จอดำ พระจันทร์ซีด ละอองน้ำเงินลอยขึ้น แถบจอกว้างเลื่อนเข้า
    ///  1.2  ช่องแคบกลางจอเปิดออก เห็นแค่ "ดวงตา" (ซูมใกล้จากภาพการ์ด) ภาพแยกสีแดง/ฟ้า พร้อมบทพูด
    ///  2.3  เสียงดาบเข้าฝัก "คลิก" ช่องปิดทันที เส้นขาวพาดกลางจอ แช่ภาพสั้นๆ
    ///  2.5  Judgment Cut: เส้นฟันถี่ขึ้นเรื่อยๆ เกิดทีละเส้นจนเป็นกรงรอบจุดกลาง
    ///  3.75 แช่ภาพ แล้ว 3.95 กรงแตกกระจาย แสงขาววาบ เส้นฟันไขว้เป็น X ใหญ่ คลื่นกระแทก
    ///  4.2  การ์ดพลิกลงมา (SecretCinematic เป็นคนจัดการ) ชื่อพิมพ์ทีละตัวอักษร
    ///
    /// ทุกอย่างสร้างจากโค้ดและขับด้วยเวลา (age) ที่ SecretCinematic ส่งมา
    /// </summary>
    public class VergilScene : MonoBehaviour
    {
        public const float HeroTime = 4.2f;
        public const float TitleTime = 4.95f;

        private static readonly Color Blue = new Color(0.27f, 0.62f, 1f, 1f);
        private static readonly Color Pale = new Color(0.9f, 0.97f, 1f, 1f);

        private struct Step
        {
            public float Time;
            public Action Action;
            public bool Done;
        }

        private RectTransform layer;
        private RectTransform root;
        private RectTransform shakeTarget;
        private CanvasGroup group;
        private CardData data;

        private Image black;
        private Image barTop;
        private Image barBottom;
        private Image moon;
        private Image moonHalo;

        private RectTransform slit;
        private RectTransform slitArt;
        private Image[] slitImages;
        private Image slitTint;
        private Image slitLine;

        private Text subtitle;

        private readonly List<Step> steps = new List<Step>();
        private readonly List<VergilCut> cuts = new List<VergilCut>();

        private float moteTimer;

        public static VergilScene Create(RectTransform layer, CardData data, RectTransform shakeTarget)
        {
            RectTransform root = UIFactory.CreateRect("VergilScene", layer);

            UIFactory.Stretch(root);

            root.SetAsFirstSibling();

            VergilScene scene = root.gameObject.AddComponent<VergilScene>();

            scene.layer = layer;
            scene.root = root;
            scene.data = data;
            scene.shakeTarget = shakeTarget != null ? shakeTarget : layer;
            scene.group = root.gameObject.AddComponent<CanvasGroup>();
            scene.group.blocksRaycasts = false;
            scene.group.interactable = false;

            scene.Build();
            scene.Plan();

            return scene;
        }

        // ---------- สร้างองค์ประกอบ ----------

        private void Build()
        {
            black = UIFactory.CreatePanel(root, "Black", Color.black);

            UIFactory.Stretch(black.rectTransform);

            black.rectTransform.offsetMin = new Vector2(-90f, -90f);
            black.rectTransform.offsetMax = new Vector2(90f, 90f);
            black.raycastTarget = false;

            // พระจันทร์ซีดมุมขวาบน + รัศมี
            moonHalo = MakeGlow("MoonHalo", new Vector2(430f, 210f), 620f, new Color(Blue.r, Blue.g, Blue.b, 0f));

            moon = UIFactory.CreatePanel(root, "Moon", new Color(Pale.r, Pale.g, Pale.b, 0f));

            moon.sprite = UIShapes.Circle;
            moon.type = Image.Type.Simple;
            moon.raycastTarget = false;

            CenterRect(moon.rectTransform, new Vector2(430f, 210f), new Vector2(120f, 120f));

            BuildSlit();

            barTop = MakeBar(true);
            barBottom = MakeBar(false);

            subtitle = ScreenKit.AddLabel(
                root, "", 40, TextAnchor.MiddleCenter, 0.1f, 0.15f, 0.9f, 0.23f, FontStyle.Italic);

            subtitle.color = new Color(Pale.r, Pale.g, Pale.b, 0f);
            subtitle.raycastTarget = false;
            subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = subtitle.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        // ช่องแคบกลางจอ: ครอบด้วย RectMask2D แล้วซูมภาพการ์ดให้เห็นแค่แถบดวงตา
        private void BuildSlit()
        {
            slit = UIFactory.CreateRect("Slit", root);

            slit.anchorMin = new Vector2(0.5f, 0.5f);
            slit.anchorMax = new Vector2(0.5f, 0.5f);
            slit.sizeDelta = new Vector2(2100f, 0f);
            slit.anchoredPosition = new Vector2(0f, 20f);

            slit.gameObject.AddComponent<RectMask2D>();

            Sprite art = data != null ? data.Artwork : null;

            slitArt = UIFactory.CreateRect("Art", slit);

            slitArt.anchorMin = new Vector2(0.5f, 0.5f);
            slitArt.anchorMax = new Vector2(0.5f, 0.5f);

            float aspect = art != null && art.rect.height > 0f ? art.rect.width / art.rect.height : 1f;
            float height = 1500f;

            slitArt.sizeDelta = new Vector2(height * aspect, height);

            // เลื่อนภาพให้ช่วงใบหน้า (ค่อนไปทางบน) มาอยู่กลางช่องแคบ
            slitArt.anchoredPosition = new Vector2(0f, -height * 0.2f);

            slitImages = new Image[3];

            Color[] colors =
            {
                new Color(1f, 0.15f, 0.25f, 0.55f),
                new Color(0.15f, 0.9f, 1f, 0.55f),
                Color.white
            };

            Vector2[] offsets = { new Vector2(10f, 0f), new Vector2(-10f, 0f), Vector2.zero };

            for (int i = 0; i < 3; i++)
            {
                Image img = UIFactory.CreatePanel(slitArt, "Layer" + i, colors[i]);

                img.raycastTarget = false;

                if (art != null)
                {
                    img.sprite = art;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }

                UIFactory.Stretch(img.rectTransform);

                img.rectTransform.anchoredPosition = offsets[i];

                slitImages[i] = img;
            }

            // ไม่มีภาพการ์ด: ใช้แสงน้ำเงินเต็มช่องแทน
            if (art == null)
            {
                for (int i = 0; i < 3; i++)
                {
                    slitImages[i].sprite = UIShapes.Glow;
                    slitImages[i].color = i == 2 ? new Color(Blue.r, Blue.g, Blue.b, 0.9f) : slitImages[i].color;
                }
            }

            slitTint = UIFactory.CreatePanel(slit, "SlitTint", new Color(0.05f, 0.15f, 0.4f, 0.35f));

            slitTint.raycastTarget = false;

            UIFactory.Stretch(slitTint.rectTransform);

            slitLine = UIFactory.CreatePanel(root, "SlitLine", new Color(Pale.r, Pale.g, Pale.b, 0f));

            slitLine.raycastTarget = false;

            CenterRect(slitLine.rectTransform, new Vector2(0f, 20f), new Vector2(2100f, 4f));

            slit.gameObject.SetActive(false);
        }

        private Image MakeGlow(string name, Vector2 pos, float size, Color color)
        {
            Image img = UIFactory.CreatePanel(root, name, color);

            img.sprite = UIShapes.Glow;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            CenterRect(img.rectTransform, pos, new Vector2(size, size));

            return img;
        }

        private static void CenterRect(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
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

        // ---------- ไทม์ไลน์ ----------

        private void At(float time, Action action)
        {
            steps.Add(new Step { Time = time, Action = action });
        }

        private void Plan()
        {
            At(1.15f, OpenSlit);
            At(1.3f, () => SetSubtitle("I need more power."));
            At(2.2f, () => SetSubtitle(""));
            At(2.3f, Sheathe);

            // Judgment Cut: ช่วงห่างระหว่างเส้นฟันแคบลงเรื่อยๆ
            float t = 2.55f;
            float gap = 0.15f;

            for (int i = 0; i < 16; i++)
            {
                int index = i;

                At(t, () => JudgmentCut(index));

                t += gap;
                gap = Mathf.Max(0.045f, gap * 0.86f);
            }

            At(3.75f, Freeze);
            At(3.95f, Finale);
            At(HeroTime, HeroBeam);
        }

        /// <summary>ส่งเวลาปัจจุบันของฉาก (SecretCinematic เรียกทุกเฟรม)</summary>
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

            // จอดำ: ทึบก่อนกรงแตก แล้วค่อยโปร่งให้เห็นสนามเลือนๆ ตอนการ์ดลง
            float fade = Mathf.Clamp01((age - 3.95f) / 0.6f);

            black.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0.86f, fade));

            float barHeight = 130f * open;

            barTop.rectTransform.sizeDelta = new Vector2(barTop.rectTransform.sizeDelta.x, barHeight);
            barBottom.rectTransform.sizeDelta = new Vector2(barBottom.rectTransform.sizeDelta.x, barHeight);

            // พระจันทร์
            float moonIn = Mathf.Clamp01((age - 0.2f) / 1.0f);
            float moonOut = 1f - Mathf.Clamp01((age - 2.05f) / 0.2f);
            float moonA = moonIn * moonOut;

            moon.color = new Color(Pale.r, Pale.g, Pale.b, 0.92f * moonA);
            moonHalo.color = new Color(Blue.r, Blue.g, Blue.b, (0.35f + 0.1f * Mathf.Sin(age * 3f)) * moonA);

            // ละอองน้ำเงินลอยขึ้น
            if (age > 0.3f && age < 2.3f)
            {
                moteTimer -= Time.unscaledDeltaTime;

                if (moteTimer <= 0f)
                {
                    moteTimer = 0.1f;

                    Rect rect = layer.rect;

                    Vector3 local = new Vector3(
                        UnityEngine.Random.Range(rect.xMin, rect.xMax),
                        UnityEngine.Random.Range(rect.yMin, rect.yMin + rect.height * 0.5f), 0f);

                    UiFx.Burst(root, layer.TransformPoint(local), Blue, 1, 30f, 14f, 2.2f, -40f);
                }
            }

            // ช่องดวงตาค่อยๆ ซูมเข้า
            if (slit.gameObject.activeSelf)
            {
                float zoom = 1f + 0.18f * Mathf.Clamp01((age - 1.15f) / 1.1f);

                slitArt.localScale = Vector3.one * zoom;
            }

            // ข้อความบทพูด
            if (subtitle != null)
            {
                Color c = subtitle.color;

                float target = string.IsNullOrEmpty(subtitle.text) ? 0f : 1f;

                c.a = Mathf.MoveTowards(c.a, target, Time.unscaledDeltaTime * 4f);

                subtitle.color = c;
            }
        }

        // ---------- จังหวะต่างๆ ----------

        private void SetSubtitle(string text)
        {
            if (subtitle != null && !string.IsNullOrEmpty(text))
                subtitle.text = text;

            if (subtitle != null && string.IsNullOrEmpty(text))
                subtitle.text = "";
        }

        private void OpenSlit()
        {
            slit.gameObject.SetActive(true);

            Sfx.Play(SoundId.Pack);

            SecretFx.Flash(layer, Pale, 0.35f, 0.25f);

            slit.gameObject.AddComponent<SlitOpener>().Init(slit, 200f, 0.16f);
        }

        // ดาบเข้าฝัก: ช่องปิด เส้นขาวพาดกลางจอ แช่ภาพ
        private void Sheathe()
        {
            slit.gameObject.SetActive(false);

            SecretFx.Shake(shakeTarget, 9f, 0.35f);
            SecretFx.Flash(layer, Color.white, 0.5f, 0.2f);
            SecretFx.HitStop(layer, 0.02f, 0.14f);

            Sfx.Play(SoundId.Hit);

            slitLine.color = new Color(Pale.r, Pale.g, Pale.b, 1f);
            slitLine.gameObject.AddComponent<LineFader>().Init(slitLine, 0.45f);

            UiFx.Ring(root, root.position, Blue, 20f, 700f, 0.6f);
        }

        private void JudgmentCut(int index)
        {
            Rect rect = layer.rect;

            float angle = UnityEngine.Random.Range(8f, 85f) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);

            // เส้นฟันวางสุ่มรอบจุดกลาง (เรียงเข้าหากึ่งกลางมากขึ้นเรื่อยๆ)
            float spread = Mathf.Lerp(0.30f, 0.12f, index / 15f);

            Vector2 pos = new Vector2(
                UnityEngine.Random.Range(-rect.width * spread, rect.width * spread),
                UnityEngine.Random.Range(-rect.height * spread, rect.height * spread));

            float remain = Mathf.Max(0.3f, 3.95f - (2.55f + index * 0.1f));

            cuts.Add(VergilCut.Make(root, pos, angle, 2000f, 5f, remain + 0.15f));

            if (index % 2 == 0)
                Sfx.Play(SoundId.Hit);

            SecretFx.Shake(shakeTarget, 5f + index * 0.5f, 0.12f);

            if (index % 3 == 0)
                SecretFx.Flash(layer, Pale, 0.22f, 0.09f);

            UiFx.Burst(root, root.TransformPoint(pos), Pale, 3, 260f, 14f, 0.4f);
        }

        // กรงเสร็จสมบูรณ์: แช่ภาพเงียบๆ ก่อนแตก
        private void Freeze()
        {
            SecretFx.HitStop(layer, 0.0f, 0.2f);

            UiFx.Ring(root, root.position, Pale, 40f, 500f, 0.5f);
        }

        // กรงแตกกระจาย: แสงขาวท่วม เส้นฟันไขว้เป็น X ใหญ่ คลื่นกระแทก
        private void Finale()
        {
            for (int i = 0; i < cuts.Count; i++)
            {
                if (cuts[i] != null)
                    cuts[i].Shatter();
            }

            cuts.Clear();

            SecretFx.Flash(layer, Color.white, 1f, 0.8f);
            SecretFx.Shake(shakeTarget, 34f, 0.9f);
            SecretFx.HitStop(layer, 0.05f, 0.18f);

            VergilCut.Make(root, Vector2.zero, 33f, 3000f, 16f, 0.9f);
            VergilCut.Make(root, Vector2.zero, -33f, 3000f, 16f, 0.9f);

            UiFx.Ring(root, root.position, Color.white, 60f, 1800f, 1.0f);
            UiFx.Ring(root, root.position, Blue, 40f, 1300f, 1.2f);
            UiFx.Burst(root, root.position, Pale, 90, 1100f, 30f, 1.3f, 120f);

            Sfx.Play(SoundId.Win);
            Sfx.Play(SoundId.Hit);
        }

        private void HeroBeam()
        {
            UiFx.Beam(root, root.TransformPoint(new Vector3(0f, layer.rect.height * 0.06f, 0f)),
                new Color(Blue.r, Blue.g, Blue.b, 0.8f), 360f, 1800f, 1.1f);
        }
    }

    /// <summary>ช่องดวงตาที่เปิดออกจากเส้นบางๆ</summary>
    public class SlitOpener : MonoBehaviour
    {
        private RectTransform rect;
        private float height;
        private float duration;
        private float age;

        public void Init(RectTransform target, float targetHeight, float seconds)
        {
            rect = target;
            height = targetHeight;
            duration = Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(age / duration);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);

            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Lerp(4f, height, ease));

            if (t >= 1f)
                Destroy(this);
        }
    }

    /// <summary>เส้นขาวที่จางหายเอง</summary>
    public class LineFader : MonoBehaviour
    {
        private Image image;
        private float life;
        private float age;

        public void Init(Image target, float seconds)
        {
            image = target;
            life = Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(age / life);
            Color c = image.color;

            c.a = 1f - t;

            image.color = c;

            if (t >= 1f)
                Destroy(this);
        }
    }

    /// <summary>
    /// เส้นฟัน 1 เส้น: แกนขาว + เงาแดง/ฟ้า (ภาพแยกสี) + แสงน้ำเงิน ลากออกจากกลางเส้นอย่างรวดเร็ว
    /// Shatter() ทำให้เส้นขยายหนาแล้วจางเร็ว
    /// </summary>
    public class VergilCut : MonoBehaviour
    {
        private Image[] images;
        private Color[] colors;
        private float life;
        private float age;
        private bool shattering;
        private float shatterAge;

        public static VergilCut Make(
            Transform parent, Vector2 anchoredPosition, float angle,
            float length, float thickness, float life)
        {
            RectTransform rect = UIFactory.CreateRect("Cut", parent);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            VergilCut cut = rect.gameObject.AddComponent<VergilCut>();

            cut.life = Mathf.Max(0.1f, life);

            Color blue = new Color(0.27f, 0.62f, 1f, 0.55f);
            Color red = new Color(1f, 0.2f, 0.25f, 0.7f);
            Color cyan = new Color(0.2f, 0.9f, 1f, 0.7f);
            Color core = new Color(0.95f, 0.98f, 1f, 1f);

            cut.images = new Image[4];
            cut.colors = new[] { blue, red, cyan, core };

            float[] heights = { thickness * 7f, thickness, thickness, thickness * 0.55f };
            float[] shifts = { 0f, 6f, -6f, 0f };

            for (int i = 0; i < 4; i++)
            {
                Image img = UIFactory.CreatePanel(rect, "Line" + i, cut.colors[i]);

                img.raycastTarget = false;

                if (i == 0)
                {
                    img.sprite = UIShapes.Glow;
                    img.type = Image.Type.Simple;
                }

                img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                img.rectTransform.sizeDelta = new Vector2(length, heights[i]);
                img.rectTransform.anchoredPosition = new Vector2(0f, shifts[i]);

                cut.images[i] = img;
            }

            rect.localScale = new Vector3(0f, 1f, 1f);

            return cut;
        }

        public void Shatter()
        {
            shattering = true;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            age += dt;

            float grow = Mathf.Clamp01(age / 0.07f);

            grow = 1f - (1f - grow) * (1f - grow) * (1f - grow);

            float t = age / life;
            float alpha = t > 0.65f ? 1f - Mathf.Clamp01((t - 0.65f) / 0.35f) : 1f;
            float scaleY = 1f;

            if (shattering)
            {
                shatterAge += dt;

                float s = Mathf.Clamp01(shatterAge / 0.3f);

                scaleY = 1f + s * 5f;
                alpha *= 1f - s;

                if (s >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            transform.localScale = new Vector3(grow, scaleY, 1f);

            for (int i = 0; i < images.Length; i++)
            {
                Color c = colors[i];

                c.a *= alpha;

                images[i].color = c;
            }

            if (age >= life)
                Destroy(gameObject);
        }
    }
}
