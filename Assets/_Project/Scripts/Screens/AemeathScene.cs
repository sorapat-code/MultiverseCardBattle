using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// คัตซีนเฉพาะตัวของ Aemeath ("Guiding Starlance") — ฉากเปิดตัวที่อลังการที่สุดในเกม
    ///
    ///  0.0  จอดำ ดาวพราวทีละดวง เส้นกลุ่มดาวเชื่อมกัน
    ///  1.2  ม่านแสงแยกออกจากกลางจอ เผยภาพ Aemeath ลางๆ ลูกแสงโคจรบนวงดาว วงแหวนหมุนซ้อนกัน
    ///  2.8  ชีพจรเพลงดาว 3 ครั้ง (จอวาบชมพู/ทอง คลื่นหดเข้า) ดาวตกพาดทั่วจอ
    ///  3.7  วงทั้งหมดยุบเข้าจุดเดียว แล้วเงียบ (hit-stop)
    ///  3.95 Starlance: เสาหอกแสงพุ่งลงจากฟ้า กระแทกกลางจอ แสงขาววาบ คลื่นกระแทก 4 ชั้น
    ///       รังสีพุ่งรอบทิศ จอสั่น ข้อความ STARLANCE กระแทกเข้ามา
    ///  4.4  การ์ดพลิกลงมา (SecretCinematic) ฝนประกายดาวตกลงมาจนจบฉาก
    /// </summary>
    public class AemeathScene : CharacterScene
    {
        private static readonly Color Pink = new Color(1f, 0.6f, 0.82f, 1f);
        private static readonly Color Gold = new Color(1f, 0.88f, 0.55f, 1f);
        private static readonly Color Ice = new Color(0.62f, 0.92f, 1f, 1f);
        private static readonly Color Violet = new Color(0.55f, 0.4f, 1f, 1f);

        private static readonly Vector2 Center = new Vector2(0f, 40f);

        private const int StarCount = 72;
        private const int OrbCount = 12;

        private Image[] stars;
        private float[] starPhase;
        private float[] starSpeed;
        private float[] starSize;

        private Image[] orbs;
        private Image core;
        private Image halo;

        private RectTransform curtain;
        private RectTransform curtainArt;
        private Image curtainImage;

        private float rainTimer;
        private float meteorTimer;
        private float moteTimer;
        private float kick;

        public override float HeroTime { get { return 4.4f; } }
        public override float TitleTime { get { return 5.35f; } }
        public override float ArrivalEnd { get { return 9.4f; } }
        public override float UnlockFinal { get { return 8.6f; } }
        public override float TransformEnd { get { return 6f; } }

        protected override Color SubtitleColor { get { return new Color(1f, 0.92f, 0.97f, 1f); } }

        protected override float BlackAlpha(float age)
        {
            // กระแทกแล้วจอดำจางลงให้เห็นแสงดาวเบื้องหลังการ์ด
            return Mathf.Lerp(1f, 0.78f, Mathf.Clamp01((age - 4.0f) / 0.8f));
        }

        // ---------- สร้าง ----------

        protected override void Build()
        {
            BuildStars();

            halo = MakeGlow("Halo", Center, new Vector2(1500f, 1500f), new Color(Pink.r, Pink.g, Pink.b, 0f));
            core = MakeGlow("Core", Center, new Vector2(700f, 700f), new Color(1f, 1f, 1f, 0f));

            BuildCurtain();

            orbs = new Image[OrbCount];

            for (int i = 0; i < OrbCount; i++)
            {
                Image orb = MakeGlow("Orb" + i, Center, new Vector2(46f, 46f), new Color(Gold.r, Gold.g, Gold.b, 0f));

                orbs[i] = orb;
            }
        }

        private void BuildStars()
        {
            stars = new Image[StarCount];
            starPhase = new float[StarCount];
            starSpeed = new float[StarCount];
            starSize = new float[StarCount];

            Rect rect = Stage;

            float halfW = Mathf.Max(600f, rect.width * 0.5f);
            float halfH = Mathf.Max(340f, rect.height * 0.5f);

            for (int i = 0; i < StarCount; i++)
            {
                Vector2 pos = new Vector2(
                    Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

                starSize[i] = Random.Range(8f, 34f);

                Color tint = i % 3 == 0 ? Gold : (i % 3 == 1 ? Pink : Ice);

                Image star = MakeGlow("Star" + i, pos, Vector2.one * starSize[i], new Color(tint.r, tint.g, tint.b, 0f));

                stars[i] = star;
                starPhase[i] = Random.Range(0f, 6.28f);
                starSpeed[i] = Random.Range(1.2f, 3.4f);
            }
        }

        // ม่านแสง: แถบกลางจอที่ค่อยๆ กว้างขึ้นเผยภาพ Aemeath (RectMask2D)
        private void BuildCurtain()
        {
            curtain = UIFactory.CreateRect("Curtain", root);

            CenterRect(curtain, Center, new Vector2(0f, 1500f));

            curtain.gameObject.AddComponent<RectMask2D>();

            Sprite art = data != null ? data.Artwork : null;

            curtainArt = UIFactory.CreateRect("Art", curtain);

            curtainArt.anchorMin = new Vector2(0.5f, 0.5f);
            curtainArt.anchorMax = new Vector2(0.5f, 0.5f);

            float aspect = art != null && art.rect.height > 0f ? art.rect.width / art.rect.height : 1.333f;
            float height = 1500f;

            curtainArt.sizeDelta = new Vector2(height * aspect, height);
            curtainArt.anchoredPosition = Vector2.zero;

            curtainImage = UIFactory.CreatePanel(curtainArt, "Image", new Color(1f, 1f, 1f, 0.5f));

            curtainImage.raycastTarget = false;

            if (art != null)
            {
                curtainImage.sprite = art;
                curtainImage.type = Image.Type.Simple;
                curtainImage.preserveAspect = true;
            }
            else
            {
                curtainImage.sprite = UIShapes.Glow;
                curtainImage.color = new Color(Pink.r, Pink.g, Pink.b, 0.6f);
            }

            UIFactory.Stretch(curtainImage.rectTransform);

            curtain.gameObject.SetActive(false);
        }

        // ---------- ไทม์ไลน์ ----------

        protected override void Plan()
        {
            At(0.2f, () => SetSubtitle("Even the farthest star..."));
            At(1.15f, OpenCurtain);
            At(1.3f, StarRings);
            At(2.0f, () => SetSubtitle("...still hears the song."));
            At(2.7f, () => SetSubtitle(""));

            At(2.85f, Pulse);
            At(3.2f, Pulse);
            At(3.5f, Pulse);

            At(3.72f, Collapse);
            At(3.95f, Starlance);
            At(HeroTime, HeroBeam);
        }

        protected override void OnTick(float age)
        {
            kick = Mathf.MoveTowards(kick, 0f, Time.unscaledDeltaTime * 3f);

            TickStars(age);
            TickCurtain(age);
            TickOrbs(age);
            TickLight(age);
            TickMeteors(age);
            TickRain(age);
        }

        // ดาวพราวทีละดวงแล้วระยิบ
        private void TickStars(float age)
        {
            float fadeIn = Mathf.Clamp01(age / 1.4f);
            float fadeOut = 1f - Mathf.Clamp01((age - 4.4f) / 2.0f) * 0.55f;

            for (int i = 0; i < stars.Length; i++)
            {
                float appear = Mathf.Clamp01((age - i * 0.018f) / 0.5f) * fadeIn;
                float twinkle = 0.55f + 0.45f * Mathf.Sin(age * starSpeed[i] + starPhase[i]);

                Color c = stars[i].color;

                c.a = appear * twinkle * fadeOut;

                stars[i].color = c;

                // ช่วงยุบ ดาวถูกดูดเข้าหากลางจอเล็กน้อย
                float pull = Mathf.Clamp01((age - 3.2f) / 0.55f) * (age < 3.95f ? 1f : 0f);

                stars[i].rectTransform.localScale = Vector3.one * (1f + 0.5f * pull * twinkle);
            }
        }

        private void TickCurtain(float age)
        {
            if (!curtain.gameObject.activeSelf)
                return;

            float open = Mathf.Clamp01((age - 1.15f) / 1.3f);

            open = 1f - (1f - open) * (1f - open) * (1f - open);

            curtain.sizeDelta = new Vector2(2300f * open, curtain.sizeDelta.y);

            // ภาพซูมช้าๆ แล้วจางหายตอนการ์ดพลิกลงมา
            curtainArt.localScale = Vector3.one * (1f + 0.14f * Mathf.Clamp01((age - 1.15f) / 3.2f));

            float fade = 1f - Mathf.Clamp01((age - 4.4f) / 1.2f) * 0.7f;
            float pulse = 0.5f + 0.1f * kick;

            curtainImage.color = new Color(1f, 1f, 1f, Mathf.Min(1f, pulse * fade));
        }

        // ลูกแสงโคจรบนวงดาว 2 วงสวนทางกัน
        private void TickOrbs(float age)
        {
            float grow = Mathf.Clamp01((age - 1.3f) / 1.0f) * (age < 3.72f ? 1f : 0f);

            for (int i = 0; i < orbs.Length; i++)
            {
                bool outer = i % 2 == 0;
                float radius = (outer ? 640f : 430f) * grow * (1f + 0.04f * kick);
                float dir = outer ? 1f : -1f;
                float angle = age * 0.95f * dir + i * (Mathf.PI * 2f / orbs.Length);

                orbs[i].rectTransform.anchoredPosition =
                    Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                Color tint = outer ? Gold : Pink;

                orbs[i].color = new Color(tint.r, tint.g, tint.b, 0.95f * grow);
            }
        }

        // ไฟกลางจอ: ค่อยๆ สว่าง เต้นตามชีพจร สว่างจ้าตอนกระแทก แล้วจางเป็นรัศมีหลังการ์ด
        private void TickLight(float age)
        {
            float build = Mathf.Clamp01((age - 1.2f) / 2.2f) * (age < 3.72f ? 1f : 0f);
            float after = age >= 3.95f ? Mathf.Clamp01(1f - (age - 3.95f) / 2.4f) : 0f;

            float coreA = Mathf.Clamp01(0.06f * build + 0.5f * kick + 0.9f * after * after);

            core.color = new Color(1f, 1f, 1f, coreA);

            float haloA = Mathf.Clamp01(0.1f + 0.25f * build + 0.35f * kick + 0.3f * after);

            halo.color = new Color(Pink.r, Pink.g, Pink.b, haloA);

            // หลังกระแทก รัศมีชมพูหายใจช้าๆ ค้างจนจบฉาก
            if (age > 5.2f)
            {
                float breathe = 0.16f + 0.05f * Mathf.Sin(age * 2.2f);

                halo.color = new Color(Pink.r, Pink.g, Pink.b, breathe);
            }
        }

        // ดาวตกพาดจอ (ช่วงชีพจร)
        private void TickMeteors(float age)
        {
            if (age < 2.7f || age > 3.7f)
                return;

            meteorTimer -= Time.unscaledDeltaTime;

            if (meteorTimer > 0f)
                return;

            meteorTimer = 0.09f;

            Rect r = Stage;

            Vector2 start = new Vector2(
                Random.Range(0f, r.xMax + 200f), Random.Range(r.yMax * 0.1f, r.yMax + 100f));

            VergilCut.Make(
                root, start, Random.Range(205f, 225f), Random.Range(380f, 640f), 6f, 0.5f,
                Gold, Color.white);
        }

        // ฝนประกายดาวตกจากบนจอจนจบฉาก + ละอองลอยขึ้นจากพื้น
        private void TickRain(float age)
        {
            if (age < 4.3f)
                return;

            rainTimer -= Time.unscaledDeltaTime;

            if (rainTimer <= 0f)
            {
                rainTimer = 0.035f;

                Rect r = Stage;

                Vector3 local = new Vector3(Random.Range(r.xMin, r.xMax), r.yMax, 0f);
                Color tint = Random.value < 0.4f ? Gold : (Random.value < 0.5f ? Pink : Ice);

                UiFx.Burst(root, layer.TransformPoint(local), tint, 1, 40f, Random.Range(14f, 30f), 3.2f, 260f);
            }

            moteTimer -= Time.unscaledDeltaTime;

            if (moteTimer <= 0f)
            {
                moteTimer = 0.08f;

                Rect r = Stage;

                Vector3 local = new Vector3(Random.Range(r.xMin, r.xMax), r.yMin, 0f);

                UiFx.Burst(root, layer.TransformPoint(local), Gold, 1, 30f, 12f, 2.8f, -70f);
            }
        }

        // ---------- จังหวะต่างๆ ----------

        private void OpenCurtain()
        {
            curtain.gameObject.SetActive(true);

            Sfx.Play(SoundId.Pack);

            SecretFx.Flash(layer, Pink, 0.3f, 0.4f);

            Flare(Center, 360f, 0.7f, Color.white);
        }

        // วงดาวหมุนซ้อนกันหลายชั้น
        private void StarRings()
        {
            RingAnim.Make(root, Center, Gold, 40f, 1320f, 1.0f, 2.45f, 0.3f, 8f);
            RingAnim.Make(root, Center, Pink, 40f, 960f, 1.0f, 2.45f, 0.3f, -18f);
            RingAnim.Make(root, Center, Ice, 40f, 640f, 1.0f, 2.45f, 0.3f, 30f);
            RingAnim.Make(root, Center, new Color(1f, 1f, 1f, 0.9f), 40f, 340f, 1.0f, 2.45f, 0.3f, -55f);

            // วงรีเอียงเหมือนวงโคจรของดาวเคราะห์
            RingAnim.Make(root, Center, new Color(Violet.r, Violet.g, Violet.b, 0.8f), 60f, 1500f, 1.2f, 2.4f, 0.3f, 6f, 0.32f);
            RingAnim.Make(root, Center, new Color(Gold.r, Gold.g, Gold.b, 0.8f), 60f, 1250f, 1.2f, 2.4f, 0.3f, -9f, 0.4f);

            Sfx.Play(SoundId.Skill);
        }

        // ชีพจรเพลงดาว: จอวาบ คลื่นหดเข้า ดาวกะพริบ
        private void Pulse()
        {
            kick = 1f;

            SecretFx.Flash(layer, Pink, 0.28f, 0.4f);
            SecretFx.Shake(shakeTarget, 8f, 0.3f);

            RingAnim.Make(root, Center, Gold, 1600f, 120f, 0.5f, 0.55f, 0.2f);
            RingAnim.Make(root, Center, Pink, 1100f, 80f, 0.4f, 0.45f, 0.2f);

            UiFx.Burst(root, World(Center), Gold, 14, 320f, 20f, 0.8f);

            Sfx.Play(SoundId.Buff);
        }

        // ทุกอย่างยุบเข้าหาจุดเดียว แล้วเงียบ
        private void Collapse()
        {
            RingAnim.Make(root, Center, Color.white, 1600f, 10f, 0.3f, 0.32f, 0.1f);
            RingAnim.Make(root, Center, Gold, 1200f, 10f, 0.3f, 0.32f, 0.1f);

            SecretFx.HitStop(layer, 0f, 0.3f);

            Sfx.Play(SoundId.Pack);
        }

        // เสาหอกแสงพุ่งลงกระแทก: แสงขาววาบ คลื่นกระแทก 4 ชั้น รังสีรอบทิศ ข้อความ
        private void Starlance()
        {
            kick = 1f;

            Vector3 impact = World(Center);

            // หอกแสงจากฟ้า (เสาสูง 2 ชั้น + เส้นแกนบางพาดลงมา)
            UiFx.Beam(root, impact, new Color(Pink.r, Pink.g, Pink.b, 0.9f), 420f, 2000f, 1.2f);
            UiFx.Beam(root, impact, new Color(1f, 1f, 1f, 1f), 150f, 1800f, 0.9f);
            UiFx.Beam(root, impact, new Color(Gold.r, Gold.g, Gold.b, 0.85f), 260f, 1900f, 1.0f);

            SecretFx.Flash(layer, Color.white, 1f, 0.9f);
            SecretFx.Flash(layer, Pink, 0.6f, 1.2f);
            SecretFx.Shake(shakeTarget, 42f, 1.0f);
            SecretFx.HitStop(layer, 0.05f, 0.18f);

            RingAnim.Make(root, Center, Color.white, 50f, 1300f, 0.7f, 0.85f, 0.5f);
            RingAnim.Make(root, Center, Pink, 60f, 1900f, 1.0f, 1.15f, 0.6f);
            RingAnim.Make(root, Center, Gold, 70f, 2400f, 1.2f, 1.4f, 0.7f);
            RingAnim.Make(root, Center, new Color(Ice.r, Ice.g, Ice.b, 0.9f), 80f, 2900f, 1.4f, 1.6f, 0.8f);

            // รังสีพุ่งรอบทิศ (ยาวมาก)
            int rays = 20;

            for (int i = 0; i < rays; i++)
            {
                float angle = i * (360f / rays) + Random.Range(-4f, 4f);
                float length = Random.Range(1000f, 1700f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                Color glow = i % 2 == 0 ? Pink : Gold;

                VergilCut.Make(root, Center + dir * (length * 0.5f), angle, length,
                    i % 4 == 0 ? 12f : 6f, 0.9f, glow, Color.white);
            }

            Flare(Center, 900f, 1.2f, Color.white);

            UiFx.Burst(root, impact, Pink, 90, 1100f, 34f, 1.6f, 90f);
            UiFx.Burst(root, impact, Gold, 70, 900f, 28f, 1.5f, 60f);
            UiFx.Burst(root, impact, Color.white, 40, 1300f, 20f, 1.0f);

            SlamText.Make(root, "STARLANCE", 120, Gold, new Vector2(0f, -250f), 1.0f, 0.7f);

            Sfx.Play(SoundId.Win);
            Sfx.Play(SoundId.Hit);
        }

        private void HeroBeam()
        {
            UiFx.Beam(root, World(new Vector2(0f, Stage.height * 0.06f)),
                new Color(Pink.r, Pink.g, Pink.b, 0.85f), 400f, 1900f, 1.2f);
        }

        // แสงวาบเป็นรูปดาว 4 แฉก (เส้นแนวนอน + แนวตั้ง + แนวทแยง)
        private void Flare(Vector2 pos, float size, float life, Color core)
        {
            float[] angles = { 0f, 90f, 45f, 135f };

            for (int i = 0; i < angles.Length; i++)
            {
                float length = i < 2 ? size * 2.4f : size * 1.3f;
                float thick = i < 2 ? 10f : 6f;

                VergilCut.Make(root, pos, angles[i], length, thick, life, Pink, core);
            }
        }
    }
}
