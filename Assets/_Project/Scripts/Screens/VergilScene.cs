using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// คัตซีนเฉพาะตัวของ Vergil ("Judgment Cut")
    ///
    ///  0.0  จอดำ พระจันทร์ซีด ละอองน้ำเงินลอยขึ้น แถบจอกว้างเลื่อนเข้า
    ///  1.2  ช่องแคบกลางจอเปิดออก เห็นแค่ "ดวงตา" (ซูมใกล้จากภาพการ์ด) ภาพแยกสีแดง/ฟ้า พร้อมบทพูด
    ///  2.3  เสียงดาบเข้าฝัก "คลิก" ช่องปิดทันที เส้นขาวพาดกลางจอ แช่ภาพสั้นๆ
    ///  2.5  Judgment Cut: เส้นฟันถี่ขึ้นเรื่อยๆ เกิดทีละเส้นจนเป็นกรงรอบจุดกลาง
    ///  3.75 แช่ภาพ แล้ว 3.95 กรงแตกกระจาย แสงขาววาบ เส้นฟันไขว้เป็น X ใหญ่ คลื่นกระแทก
    ///  4.2  การ์ดพลิกลงมา (SecretCinematic เป็นคนจัดการ) ชื่อพิมพ์ทีละตัวอักษร
    /// </summary>
    public class VergilScene : CharacterScene
    {
        private static readonly Color Blue = new Color(0.27f, 0.62f, 1f, 1f);
        private static readonly Color Pale = new Color(0.9f, 0.97f, 1f, 1f);

        private Image moon;
        private Image moonHalo;

        private RectTransform slit;
        private RectTransform slitArt;
        private Image slitLine;

        private readonly List<VergilCut> cuts = new List<VergilCut>();

        private float moteTimer;

        protected override float BlackAlpha(float age)
        {
            return Mathf.Lerp(1f, 0.86f, Mathf.Clamp01((age - 3.95f) / 0.6f));
        }

        protected override void Build()
        {
            // พระจันทร์ซีดมุมขวาบน + รัศมี
            moonHalo = MakeGlow("MoonHalo", new Vector2(430f, 210f), Vector2.one * 620f, new Color(Blue.r, Blue.g, Blue.b, 0f));

            moon = MakeSolid("Moon", new Vector2(430f, 210f), Vector2.one * 120f, new Color(Pale.r, Pale.g, Pale.b, 0f));

            moon.sprite = UIShapes.Circle;
            moon.type = Image.Type.Simple;

            BuildSlit();
        }

        // ช่องแคบกลางจอ: ครอบด้วย RectMask2D แล้วซูมภาพการ์ดให้เห็นแค่แถบดวงตา
        private void BuildSlit()
        {
            slit = UIFactory.CreateRect("Slit", root);

            CenterRect(slit, new Vector2(0f, 20f), new Vector2(2100f, 0f));

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
                else
                {
                    // ไม่มีภาพการ์ด: ใช้แสงน้ำเงินแทน
                    img.sprite = UIShapes.Glow;

                    if (i == 2)
                        img.color = new Color(Blue.r, Blue.g, Blue.b, 0.9f);
                }

                UIFactory.Stretch(img.rectTransform);

                img.rectTransform.anchoredPosition = offsets[i];
            }

            Image tint = UIFactory.CreatePanel(slit, "SlitTint", new Color(0.05f, 0.15f, 0.4f, 0.35f));

            tint.raycastTarget = false;

            UIFactory.Stretch(tint.rectTransform);

            slitLine = MakeSolid("SlitLine", new Vector2(0f, 20f), new Vector2(2100f, 4f), new Color(Pale.r, Pale.g, Pale.b, 0f));

            slit.gameObject.SetActive(false);
        }

        // ---------- ไทม์ไลน์ ----------

        protected override void Plan()
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

        protected override void OnTick(float age)
        {
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

                    Rect rect = Stage;

                    Vector3 local = new Vector3(
                        UnityEngine.Random.Range(rect.xMin, rect.xMax),
                        UnityEngine.Random.Range(rect.yMin, rect.yMin + rect.height * 0.5f), 0f);

                    UiFx.Burst(root, layer.TransformPoint(local), Blue, 1, 30f, 14f, 2.2f, -40f);
                }
            }

            // ช่องดวงตาค่อยๆ ซูมเข้า
            if (slit.gameObject.activeSelf)
                slitArt.localScale = Vector3.one * (1f + 0.18f * Mathf.Clamp01((age - 1.15f) / 1.1f));
        }

        // ---------- จังหวะต่างๆ ----------

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
            Rect rect = Stage;

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

            UiFx.Burst(root, World(pos), Pale, 3, 260f, 14f, 0.4f);
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
            UiFx.Beam(root, World(new Vector2(0f, Stage.height * 0.06f)),
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
            return Make(parent, anchoredPosition, angle, length, thickness, life, null, null);
        }

        /// <summary>glow/core ที่ระบุ = จานสีของตัวละครอื่น (ไม่มีภาพแยกสีแดง/ฟ้า)</summary>
        public static VergilCut Make(
            Transform parent, Vector2 anchoredPosition, float angle,
            float length, float thickness, float life, Color? glowColor, Color? coreColor)
        {
            RectTransform rect = UIFactory.CreateRect("Cut", parent);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = anchoredPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            VergilCut cut = rect.gameObject.AddComponent<VergilCut>();

            cut.life = Mathf.Max(0.1f, life);

            bool custom = glowColor.HasValue;

            Color blue = custom ? new Color(glowColor.Value.r, glowColor.Value.g, glowColor.Value.b, 0.55f) : new Color(0.27f, 0.62f, 1f, 0.55f);
            Color red = custom ? new Color(0f, 0f, 0f, 0f) : new Color(1f, 0.2f, 0.25f, 0.7f);
            Color cyan = custom ? new Color(0f, 0f, 0f, 0f) : new Color(0.2f, 0.9f, 1f, 0.7f);
            Color core = coreColor.HasValue ? coreColor.Value : new Color(0.95f, 0.98f, 1f, 1f);

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
