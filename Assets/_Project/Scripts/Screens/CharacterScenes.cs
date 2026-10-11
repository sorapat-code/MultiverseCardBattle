using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    // =====================================================================
    //  Sung Jin-Woo : "ARISE"
    //  ความมืด -> คู่ดวงตาของกองทัพเงาลืมตาทีละคู่ถี่ขึ้นเรื่อยๆ -> ตัวหนังสือ ARISE กระแทก
    //  -> ตาทุกคู่ลุกวาบ คลื่นพื้น ควันม่วงพวยพุ่ง -> ทหารเงาก้มคำนับ -> เสาแสงม่วงพุ่งจากพื้น -> การ์ดลง
    // =====================================================================
    public class JinWooScene : CharacterScene
    {
        private static readonly Color Violet = new Color(0.68f, 0.42f, 1f, 1f);

        private readonly List<EyePair> eyes = new List<EyePair>();
        private Image ground;

        public override float HeroTime { get { return 4.0f; } }
        public override float TitleTime { get { return 4.75f; } }
        public override float ArrivalEnd { get { return 7.4f; } }
        public override float UnlockFinal { get { return 6.8f; } }

        protected override Color SubtitleColor { get { return new Color(0.85f, 0.75f, 1f, 1f); } }

        protected override float BlackAlpha(float age)
        {
            return Mathf.Lerp(1f, 0.85f, Mathf.Clamp01((age - 2.5f) / 0.8f));
        }

        protected override void Build()
        {
            ground = MakeGlow("Ground", new Vector2(0f, -260f), new Vector2(2200f, 700f), new Color(Violet.r, Violet.g, Violet.b, 0f));
        }

        protected override void Plan()
        {
            // ตาลืมทีละคู่ ช่วงห่างแคบลงเรื่อยๆ
            float t = 0.5f;
            float gap = 0.24f;

            while (t < 2.3f)
            {
                At(t, SpawnEyes);

                t += gap;
                gap = Mathf.Max(0.05f, gap * 0.9f);
            }

            At(1.2f, () => SetSubtitle("Shadows... answer my call."));
            At(2.3f, () => SetSubtitle(""));
            At(2.45f, Arise);
            At(2.9f, Smoke);
            At(3.15f, Smoke);
            At(3.3f, Bow);
            At(3.55f, Pillar);
        }

        protected override void OnTick(float age)
        {
            float glow = 0.08f * Mathf.Clamp01((age - 0.8f) / 0.8f)
                         + 0.32f * Mathf.Clamp01((age - 2.4f) / 1.0f);

            float pulse = 0.85f + 0.15f * Mathf.Sin(age * 3.5f);

            ground.color = new Color(Violet.r, Violet.g, Violet.b, glow * pulse);
        }

        private void SpawnEyes()
        {
            Rect r = Stage;

            Vector2 pos = new Vector2(
                Random.Range(-r.width * 0.44f, r.width * 0.44f),
                Random.Range(-r.height * 0.25f, r.height * 0.3f));

            // อยู่สูง = ไกล = เล็ก
            float depth = Mathf.InverseLerp(r.height * 0.3f, -r.height * 0.25f, pos.y);
            float scale = Mathf.Lerp(0.6f, 1.5f, depth);

            eyes.Add(EyePair.Make(root, pos, scale, Violet));
        }

        private void Arise()
        {
            SecretFx.Flash(layer, Violet, 0.6f, 0.5f);
            SecretFx.Shake(shakeTarget, 32f, 0.9f);
            SecretFx.HitStop(layer, 0.05f, 0.15f);

            SlamText.Make(root, "ARISE", 170, Color.Lerp(Violet, Color.white, 0.6f), new Vector2(0f, 40f), 1.0f, 0.5f);

            RingAnim.Make(root, new Vector2(0f, -200f), Violet, 80f, 1700f, 0.9f, 1.0f, 0.5f, 0f, 0.28f);
            RingAnim.Make(root, new Vector2(0f, -200f), Violet, 60f, 1200f, 0.7f, 0.8f, 0.4f, 0f, 0.28f);
            RingAnim.Make(root, new Vector2(0f, -200f), Color.white, 40f, 800f, 0.5f, 0.6f, 0.3f, 0f, 0.28f);

            for (int i = 0; i < eyes.Count; i++)
            {
                if (eyes[i] != null)
                    eyes[i].Flare();
            }

            Sfx.Play(SoundId.Win);
            Sfx.Play(SoundId.Hit);
        }

        private void Smoke()
        {
            float y = -Stage.height * 0.3f;

            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * Stage.width * 0.2f;

                UiFx.Burst(root, World(new Vector2(x, y)), Violet, 14, 300f, 70f, 1.6f, -120f);
            }

            SecretFx.Shake(shakeTarget, 8f, 0.3f);
        }

        // ทหารเงาก้มคำนับ
        private void Bow()
        {
            for (int i = 0; i < eyes.Count; i++)
            {
                if (eyes[i] != null)
                    eyes[i].Bow();
            }

            eyes.Clear();
        }

        private void Pillar()
        {
            UiFx.Beam(root, World(new Vector2(0f, -Stage.height * 0.3f)),
                new Color(Violet.r, Violet.g, Violet.b, 0.85f), 520f, 2000f, 1.2f);

            UiFx.Ring(root, World(new Vector2(0f, -Stage.height * 0.3f)), Violet, 60f, 1400f, 1.0f);

            Sfx.Play(SoundId.Pack);
        }
    }

    // =====================================================================
    //  Artorias : "A Knight's Duty"
    //  หมอกและถ่านไฟลอย -> ดาบใหญ่ตกลงปักพื้นกลางจอ (กระแทกหนัก) -> เสาแสงทอง บทพูดสั้นๆ
    //  -> ฟันกวาดกว้างครั้งเดียว -> ดาบสลายเป็นประกายทอง -> การ์ดลง
    // =====================================================================
    public class ArtoriasScene : CharacterScene
    {
        private static readonly Color Gold = new Color(1f, 0.78f, 0.36f, 1f);

        private Image[] fog;
        private Image shaft;
        private RectTransform sword;
        private SwordDrop swordDrop;
        private float emberTimer;

        public override float HeroTime { get { return 4.0f; } }
        public override float TitleTime { get { return 4.75f; } }
        public override float ArrivalEnd { get { return 7.4f; } }
        public override float UnlockFinal { get { return 6.8f; } }

        protected override Color SubtitleColor { get { return new Color(1f, 0.92f, 0.7f, 1f); } }

        protected override float BlackAlpha(float age)
        {
            return Mathf.Lerp(1f, 0.88f, Mathf.Clamp01((age - 3.2f) / 0.8f));
        }

        protected override void Build()
        {
            fog = new Image[3];

            for (int i = 0; i < 3; i++)
            {
                fog[i] = MakeGlow("Fog" + i, new Vector2(0f, -120f + i * 140f), new Vector2(2400f, 520f),
                    new Color(0.7f, 0.62f, 0.45f, 0f));
            }

            shaft = MakeGlow("Shaft", new Vector2(0f, 60f), new Vector2(560f, 1700f), new Color(Gold.r, Gold.g, Gold.b, 0f));

            // ดาบใหญ่ (ปลายดาบอยู่ที่จุดกำเนิดของ sword ด้านล่าง)
            sword = UIFactory.CreateRect("Sword", root);

            CenterRect(sword, new Vector2(0f, 1500f), Vector2.zero);

            AddPart("SwordGlow", new Vector2(0f, 420f), new Vector2(170f, 900f), new Color(Gold.r, Gold.g, Gold.b, 0.5f), true);
            AddPart("Blade", new Vector2(0f, 390f), new Vector2(26f, 780f), new Color(0.95f, 0.9f, 0.75f, 1f), false);
            AddPart("Guard", new Vector2(0f, 780f), new Vector2(230f, 24f), Gold, false);
            AddPart("Grip", new Vector2(0f, 870f), new Vector2(14f, 140f), new Color(0.35f, 0.25f, 0.12f, 1f), false);

            swordDrop = sword.gameObject.AddComponent<SwordDrop>();
        }

        private void AddPart(string name, Vector2 pos, Vector2 size, Color color, bool soft)
        {
            Image img = UIFactory.CreatePanel(sword, name, color);

            img.raycastTarget = false;

            if (soft)
            {
                img.sprite = UIShapes.Glow;
                img.type = Image.Type.Simple;
            }

            img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = size;
        }

        protected override void Plan()
        {
            At(1.4f, Drop);
            At(1.72f, Impact);
            At(2.1f, () => SetSubtitle("A knight does not kneel."));
            At(3.0f, () => SetSubtitle(""));
            At(3.15f, Sweep);
            At(3.4f, Dissolve);
            At(HeroTime, HeroBeam);
        }

        protected override void OnTick(float age)
        {
            // หมอกลอยช้าๆ
            float fogIn = Mathf.Clamp01(age / 1.0f);
            float fogOut = 1f - 0.5f * Mathf.Clamp01((age - 3.4f) / 0.6f);

            for (int i = 0; i < fog.Length; i++)
            {
                fog[i].rectTransform.anchoredPosition = new Vector2(
                    Mathf.Sin(age * 0.3f + i * 1.7f) * 90f, -120f + i * 140f);

                Color c = fog[i].color;

                c.a = (0.10f + 0.05f * Mathf.Sin(age * 0.8f + i)) * fogIn * fogOut;

                fog[i].color = c;
            }

            // เสาแสงทองหลังดาบ
            float shaftA = Mathf.Clamp01((age - 1.72f) / 0.8f) * 0.45f * (1f - Mathf.Clamp01((age - 3.4f) / 0.5f));

            shaft.color = new Color(Gold.r, Gold.g, Gold.b, shaftA);

            // ถ่านไฟลอยขึ้นตลอดฉาก
            if (age > 0.3f && age < 3.8f)
            {
                emberTimer -= Time.unscaledDeltaTime;

                if (emberTimer <= 0f)
                {
                    emberTimer = 0.07f;

                    Rect r = Stage;

                    Vector3 local = new Vector3(
                        Random.Range(r.xMin, r.xMax),
                        Random.Range(r.yMin, r.yMin + r.height * 0.4f), 0f);

                    UiFx.Burst(root, layer.TransformPoint(local), Gold, 1, 40f, 12f, 2.6f, -70f);
                }
            }
        }

        private void Drop()
        {
            swordDrop.Begin(1500f, -60f, 0.32f);
        }

        private void Impact()
        {
            Vector3 tip = World(new Vector2(0f, -60f));

            RingAnim.Make(root, new Vector2(0f, -60f), Gold, 40f, 1500f, 0.9f, 1.0f, 0.5f, 0f, 0.25f);
            RingAnim.Make(root, new Vector2(0f, -60f), Color.white, 30f, 800f, 0.6f, 0.7f, 0.4f, 0f, 0.25f);

            UiFx.Burst(root, tip, Gold, 50, 700f, 30f, 1.0f, 300f);
            UiFx.Burst(root, tip, new Color(0.7f, 0.62f, 0.45f, 0.8f), 30, 400f, 60f, 1.4f, 80f);

            SecretFx.Flash(layer, Gold, 0.45f, 0.3f);
            SecretFx.Shake(shakeTarget, 26f, 0.7f);
            SecretFx.HitStop(layer, 0.04f, 0.12f);

            Sfx.Play(SoundId.Hit);
        }

        // ฟันกวาดกว้างครั้งเดียว
        private void Sweep()
        {
            SecretFx.Flash(layer, Color.white, 0.7f, 0.5f);
            SecretFx.Shake(shakeTarget, 30f, 0.8f);

            Color white = new Color(1f, 0.97f, 0.88f, 1f);

            VergilCut.Make(root, new Vector2(0f, -40f), 0f, 2800f, 34f, 0.9f, Gold, white);
            VergilCut.Make(root, new Vector2(0f, -20f), 8f, 2600f, 14f, 0.8f, Gold, white);
            VergilCut.Make(root, new Vector2(0f, -60f), -8f, 2600f, 14f, 0.8f, Gold, white);

            RingAnim.Make(root, new Vector2(0f, -40f), Gold, 60f, 1900f, 0.9f, 1.0f, 0.5f);

            UiFx.Burst(root, World(new Vector2(0f, -40f)), Gold, 90, 1000f, 30f, 1.2f, 100f);

            Sfx.Play(SoundId.Win);
            Sfx.Play(SoundId.Hit);
        }

        private void Dissolve()
        {
            swordDrop.Dissolve();

            UiFx.Burst(root, World(new Vector2(0f, 300f)), Gold, 70, 500f, 26f, 1.6f, -80f);
        }

        private void HeroBeam()
        {
            UiFx.Beam(root, World(new Vector2(0f, Stage.height * 0.06f)),
                new Color(Gold.r, Gold.g, Gold.b, 0.8f), 360f, 1800f, 1.1f);
        }
    }

    /// <summary>ดาบใหญ่ตกลงมาเร็วขึ้นเรื่อยๆ แล้วปักพื้น / สลายหายเมื่อสั่ง</summary>
    public class SwordDrop : MonoBehaviour
    {
        private RectTransform rect;
        private Image[] images;
        private Color[] colors;
        private float from;
        private float to;
        private float duration;
        private float age;
        private bool dropping;
        private bool dissolving;
        private float dissolveAge;

        private void Awake()
        {
            rect = (RectTransform)transform;
        }

        private void EnsureImages()
        {
            if (images != null)
                return;

            images = GetComponentsInChildren<Image>();
            colors = new Color[images.Length];

            for (int i = 0; i < images.Length; i++)
                colors[i] = images[i].color;
        }

        public void Begin(float fromY, float toY, float seconds)
        {
            EnsureImages();

            from = fromY;
            to = toY;
            duration = Mathf.Max(0.05f, seconds);
            age = 0f;
            dropping = true;
        }

        public void Dissolve()
        {
            EnsureImages();

            dissolving = true;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (dropping)
            {
                age += dt;

                float t = Mathf.Clamp01(age / duration);

                // ตกเร่งความเร็ว (ease-in ยกกำลังสาม)
                rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, to, t * t * t));

                if (t >= 1f)
                    dropping = false;
            }

            if (dissolving && images != null)
            {
                dissolveAge += dt;

                float s = Mathf.Clamp01(dissolveAge / 0.5f);

                for (int i = 0; i < images.Length; i++)
                {
                    Color c = colors[i];

                    c.a *= 1f - s;

                    images[i].color = c;
                }

                if (s >= 1f)
                    dissolving = false;
            }
        }
    }

    // =====================================================================
    //  Ainz : "The Throne of Nazarick"
    //  วงเวทแดงหลายชั้นหมุนสวนทางกันและขยายออก มีลูกแสงโคจร -> ตาแดงลืม -> ชีพจรครอบงำสามครั้ง
    //  วงหดยุบเข้าหาจุดกลาง -> ระเบิดออกพร้อมเสาแสงแดงเรียงแถว กลีบสีเลือดร่วง -> การ์ดลง
    // =====================================================================
    public class AinzScene : CharacterScene
    {
        private static readonly Color Red = new Color(0.95f, 0.12f, 0.18f, 1f);
        private static readonly Vector2 Center = new Vector2(0f, 40f);

        private Image[] orbs;
        private Image eyeLeft;
        private Image eyeRight;
        private Image core;
        private float petalTimer;

        public override float HeroTime { get { return 4.5f; } }
        public override float TitleTime { get { return 5.25f; } }
        public override float ArrivalEnd { get { return 8.0f; } }
        public override float UnlockFinal { get { return 7.4f; } }

        protected override Color SubtitleColor { get { return new Color(1f, 0.75f, 0.75f, 1f); } }

        protected override float BlackAlpha(float age)
        {
            return Mathf.Lerp(1f, 0.86f, Mathf.Clamp01((age - 3.85f) / 0.6f));
        }

        protected override void Build()
        {
            core = MakeGlow("Core", Center, new Vector2(900f, 900f), new Color(Red.r, Red.g, Red.b, 0f));

            orbs = new Image[8];

            for (int i = 0; i < orbs.Length; i++)
            {
                Image orb = MakeSolid("Orb" + i, Center, new Vector2(26f, 26f), new Color(Red.r, Red.g, Red.b, 0f));

                orb.sprite = UIShapes.Circle;
                orb.type = Image.Type.Simple;

                orbs[i] = orb;
            }

            eyeLeft = MakeGlow("EyeL", new Vector2(-80f, 110f), new Vector2(130f, 60f), new Color(1f, 0.2f, 0.2f, 0f));
            eyeRight = MakeGlow("EyeR", new Vector2(80f, 110f), new Vector2(130f, 60f), new Color(1f, 0.2f, 0.2f, 0f));
        }

        protected override void Plan()
        {
            At(0.4f, Circles);
            At(1.2f, () => SetSubtitle("Bow before the Supreme One."));
            At(2.2f, () => SetSubtitle(""));

            At(2.35f, Pulse);
            At(2.75f, Pulse);
            At(3.15f, Pulse);

            At(3.5f, Collapse);
            At(3.85f, Explode);
            At(HeroTime, HeroBeam);
        }

        protected override void OnTick(float age)
        {
            float grow = Mathf.Clamp01((age - 0.9f) / 0.8f) * (age < 3.5f ? 1f : 0f);

            // ลูกแสงโคจรบนวงเวท
            for (int i = 0; i < orbs.Length; i++)
            {
                float radius = (i % 2 == 0 ? 625f : 450f) * grow;
                float dir = i % 2 == 0 ? 1f : -1f;
                float angle = (age * 0.9f * dir + i * (Mathf.PI * 2f / orbs.Length)) ;

                orbs[i].rectTransform.anchoredPosition = Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                orbs[i].color = new Color(Red.r, Red.g, Red.b, 0.9f * grow);
            }

            // ตาแดง
            float eyeOn = Mathf.Clamp01((age - 1.9f) / 0.5f) * (age < 3.5f ? 1f : 0f);
            float eyePulse = 0.8f + 0.2f * Mathf.Sin(age * 6f);

            eyeLeft.color = new Color(1f, 0.2f, 0.2f, eyeOn * eyePulse);
            eyeRight.color = new Color(1f, 0.2f, 0.2f, eyeOn * eyePulse);

            // แสงกลางวง สว่างขึ้นก่อนยุบ
            float coreA = 0f;

            if (age > 0.6f && age < 3.5f)
                coreA = Mathf.Clamp01((age - 0.6f) / 1f) * (0.12f + 0.2f * Mathf.Clamp01((age - 2.3f) / 1.2f));

            core.color = new Color(Red.r, Red.g, Red.b, coreA);

            // กลีบสีเลือดร่วงจากด้านบน
            if (age > 2.0f && age < 6.0f)
            {
                petalTimer -= Time.unscaledDeltaTime;

                if (petalTimer <= 0f)
                {
                    petalTimer = 0.06f;

                    Rect r = Stage;

                    Vector3 local = new Vector3(Random.Range(r.xMin, r.xMax), r.yMax, 0f);

                    UiFx.Burst(root, layer.TransformPoint(local), Red, 1, 40f, 22f, 3f, 220f);
                }
            }
        }

        private void Circles()
        {
            RingAnim.Make(root, Center, Red, 40f, 1250f, 0.9f, 3.05f, 0.25f, 10f);
            RingAnim.Make(root, Center, new Color(0.7f, 0.05f, 0.1f, 1f), 40f, 900f, 0.9f, 3.05f, 0.25f, -22f);
            RingAnim.Make(root, Center, Red, 40f, 520f, 0.9f, 3.05f, 0.25f, 40f);
            RingAnim.Make(root, Center, new Color(1f, 0.4f, 0.4f, 1f), 40f, 260f, 0.9f, 3.05f, 0.25f, -60f);

            Sfx.Play(SoundId.Pack);
        }

        // ชีพจรครอบงำ: จอแดงวาบ จอสั่น คลื่นหดเข้าหากลาง
        private void Pulse()
        {
            SecretFx.Flash(layer, Red, 0.35f, 0.45f);
            SecretFx.Shake(shakeTarget, 9f, 0.35f);

            RingAnim.Make(root, Center, Red, 1500f, 100f, 0.45f, 0.5f, 0.2f);

            Sfx.Play(SoundId.Hit);
        }

        // วงเวททั้งหมดยุบเข้าหาจุดกลาง แล้วเงียบ
        private void Collapse()
        {
            RingAnim.Make(root, Center, Red, 1400f, 10f, 0.3f, 0.32f, 0.1f);

            SecretFx.HitStop(layer, 0.0f, 0.28f);

            Sfx.Play(SoundId.Pack);
        }

        private void Explode()
        {
            SecretFx.Flash(layer, Color.white, 1f, 0.8f);
            SecretFx.Flash(layer, Red, 0.6f, 1.0f);
            SecretFx.Shake(shakeTarget, 36f, 0.9f);
            SecretFx.HitStop(layer, 0.05f, 0.15f);

            RingAnim.Make(root, Center, Red, 60f, 1900f, 1.0f, 1.1f, 0.6f);
            RingAnim.Make(root, Center, Color.white, 40f, 1300f, 0.8f, 0.9f, 0.5f);
            RingAnim.Make(root, Center, new Color(0.5f, 0f, 0.08f, 1f), 80f, 2300f, 1.2f, 1.3f, 0.7f);

            UiFx.Burst(root, World(Center), Red, 100, 1000f, 34f, 1.5f, 100f);

            // เสาแสงแดงเรียงแถว
            float[] columns = { -820f, -520f, -260f, 260f, 520f, 820f };

            for (int i = 0; i < columns.Length; i++)
            {
                float x = columns[i];

                Vector3 basePoint = World(new Vector2(x, -Stage.height * 0.4f));

                UiFx.Beam(root, basePoint, new Color(Red.r, Red.g, Red.b, 0.8f), 140f, 1800f, 1.1f);
            }

            Sfx.Play(SoundId.Win);
            Sfx.Play(SoundId.Hit);
        }

        private void HeroBeam()
        {
            UiFx.Beam(root, World(new Vector2(0f, Stage.height * 0.06f)),
                new Color(Red.r, Red.g, Red.b, 0.8f), 360f, 1800f, 1.1f);
        }
    }

    // =====================================================================
    //  Devil Trigger : แปลงร่าง (ไม่มีตัวการ์ด)
    //  จอดำ -> เสียงหัวใจเต้นถี่ขึ้น คลื่นน้ำเงินกระแทกพร้อมสายฟ้า -> แช่ภาพ
    //  -> ระเบิดเส้นความเร็วพุ่งออกรอบทิศ แสงขาววาบ -> ชื่อ DEVIL TRIGGER พิมพ์ขึ้นกลางจอ
    // =====================================================================
    public class DevilTriggerScene : CharacterScene
    {
        private static readonly Color Blue = new Color(0.27f, 0.62f, 1f, 1f);
        private static readonly Color Pale = new Color(0.9f, 0.97f, 1f, 1f);

        private Image core;
        private float kick;

        public override float TitleTime { get { return 3.0f; } }
        public override float TransformEnd { get { return 5.4f; } }

        protected override float BlackAlpha(float age)
        {
            return Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((age - 2.7f) / 0.8f));
        }

        protected override void Build()
        {
            core = MakeGlow("Core", Vector2.zero, new Vector2(1100f, 1100f), new Color(Blue.r, Blue.g, Blue.b, 0f));
        }

        protected override void Plan()
        {
            float[] beats = { 0.4f, 1.0f, 1.5f, 1.9f, 2.2f, 2.45f };

            for (int i = 0; i < beats.Length; i++)
            {
                int index = i;

                At(beats[i], () => Heartbeat(index));
            }

            At(0.3f, () => SetSubtitle("Let the devil out."));
            At(2.3f, () => SetSubtitle(""));
            At(2.62f, Freeze);
            At(2.85f, Explode);
        }

        protected override void OnTick(float age)
        {
            kick = Mathf.MoveTowards(kick, 0f, Time.unscaledDeltaTime * 4f);

            float after = age > 2.85f ? Mathf.Clamp01(1f - (age - 2.85f) / 1.5f) * 0.5f : 0f;

            core.color = new Color(Blue.r, Blue.g, Blue.b, Mathf.Clamp01(0.05f + 0.5f * kick + after));
        }

        private void Heartbeat(int index)
        {
            float k = index / 5f;

            kick = 1f;

            RingAnim.Make(root, Vector2.zero, Blue, 120f, 800f + k * 500f, 0.5f, 0.55f, 0.3f);

            SecretFx.Shake(shakeTarget, 4f + k * 10f, 0.25f);
            SecretFx.Flash(layer, Blue, 0.12f + 0.18f * k, 0.22f);

            Sfx.Play(SoundId.Hit);

            // สายฟ้าจากขอบจอเข้าหากลาง (มากขึ้นตามจังหวะ)
            Rect r = Stage;
            int bolts = 1 + index / 2;

            for (int i = 0; i < bolts; i++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;

                Vector2 from = new Vector2(side * r.width * 0.5f, Random.Range(-r.height * 0.4f, r.height * 0.4f));
                Vector2 to = new Vector2(Random.Range(-120f, 120f), Random.Range(-120f, 120f));

                Bolt(from, to, 0.3f, 6f, Blue, Pale);
            }
        }

        private void Freeze()
        {
            SecretFx.HitStop(layer, 0.0f, 0.22f);

            RingAnim.Make(root, Vector2.zero, Pale, 900f, 60f, 0.25f, 0.28f, 0.1f);
        }

        private void Explode()
        {
            SecretFx.Flash(layer, Color.white, 1f, 0.8f);
            SecretFx.Shake(shakeTarget, 36f, 0.9f);
            SecretFx.HitStop(layer, 0.05f, 0.15f);

            // เส้นความเร็วพุ่งออกรอบทิศ
            int lines = 28;

            for (int i = 0; i < lines; i++)
            {
                float angle = i * 360f / lines + Random.Range(-4f, 4f);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                VergilCut.Make(root, dir * 900f, angle, 1200f, Random.Range(5f, 11f), 0.65f, Blue, Pale);
            }

            RingAnim.Make(root, Vector2.zero, Color.white, 60f, 1800f, 1.0f, 1.1f, 0.6f);
            RingAnim.Make(root, Vector2.zero, Blue, 40f, 1300f, 0.8f, 0.9f, 0.5f);
            RingAnim.Make(root, Vector2.zero, Blue, 80f, 2300f, 1.2f, 1.3f, 0.7f);

            UiFx.Burst(root, root.position, Blue, 120, 900f, 30f, 1.4f, -80f);
            UiFx.Beam(root, root.position, new Color(Blue.r, Blue.g, Blue.b, 0.85f), 420f, 1800f, 1.1f);

            Sfx.Play(SoundId.Win);
            Sfx.Play(SoundId.Hit);
        }
    }
}
