using System;
using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ฉากโยนเหรียญตอนเริ่มเกม: เหรียญลอยขึ้นหมุนพลิกแล้วตกกระดอน
    /// ด้านหน้า = เรา (YOU, ทอง) ด้านหลัง = ศัตรู (เงิน) หงายด้านไหนคนนั้นเริ่มก่อน
    /// จบด้วยป้ายชื่อสองฝั่งบอก 1ST / 2ND พร้อมสิ่งที่คนเริ่มหลังได้ (จั่วเพิ่ม + The Coin)
    /// ระหว่างฉาก BattleScreen หยุด AI/อินพุตทั้งหมด คลิกเพื่อข้ามหรือไปต่อ
    ///
    /// ผลลัพธ์ (ใครเริ่มก่อน) ถูกตัดสินไปแล้วโดย BattleManager ฉากนี้แค่แสดงผล
    /// </summary>
    public class CoinToss : MonoBehaviour
    {
        private const float FlipStart = 0.7f;
        private const float FlipTime = 2.3f;
        private const float LandAt = FlipStart + FlipTime;
        private const float BounceTime = 0.5f;
        private const float RevealAt = LandAt + 0.55f;
        private const float AutoEnd = 8.5f;
        private const float ExitSeconds = 0.4f;
        private const float CoinBaseY = 60f;
        private const float CoinHeight = 380f;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.35f, 1f);
        private static readonly Color Silver = new Color(0.72f, 0.82f, 1f, 1f);

        private RectTransform layer;
        private CanvasGroup group;
        private Action onFinish;

        private string humanName;
        private string aiName;
        private string frontLabel;
        private string backLabel;
        private bool humanFirst;
        private int halfTurns;

        private Image glow;
        private Text title;
        private Text subtitle;
        private Text hint;

        private RectTransform coin;
        private Image coinFace;
        private Image coinEdge;
        private Text coinText;
        private Image shadow;

        private Plate humanPlate;
        private Plate aiPlate;

        private float age;
        private int lastHalf = -1;
        private bool landed;
        private bool revealed;
        private bool exiting;
        private float exitAge;

        private class Plate
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Image Glow;
            public Text Name;
            public Text Order;
            public Text Detail;
        }

        // ---------- สร้าง ----------

        /// <summary>เริ่มฉากโยนเหรียญ (onFinish ถูกเรียกเมื่อฉากจบและถูกลบแล้ว)</summary>
        public static CoinToss Play(
            RectTransform parent, string humanName, string aiName,
            bool humanFirst, Action onFinish)
        {
            RectTransform layer = UIFactory.CreateRect("CoinToss", parent);

            UIFactory.Stretch(layer);

            layer.SetAsLastSibling();

            CoinToss toss = layer.gameObject.AddComponent<CoinToss>();

            toss.layer = layer;
            toss.humanName = string.IsNullOrEmpty(humanName) ? "YOU" : humanName.ToUpperInvariant();
            toss.aiName = string.IsNullOrEmpty(aiName) ? "FOE" : aiName.ToUpperInvariant();
            toss.humanFirst = humanFirst;
            toss.onFinish = onFinish;

            toss.frontLabel = "YOU";
            toss.backLabel = Abbreviate(toss.aiName);

            // หงายด้านหน้าเมื่อจำนวนครึ่งรอบเป็นเลขคู่
            toss.halfTurns = humanFirst ? 10 : 11;

            toss.Build();

            return toss;
        }

        private static string Abbreviate(string name)
        {
            string trimmed = (name ?? "").Trim();

            if (trimmed.Length == 0)
                return "FOE";

            return trimmed.Length <= 3 ? trimmed : trimmed.Substring(0, 3);
        }

        private void Build()
        {
            group = layer.gameObject.AddComponent<CanvasGroup>();

            // ตัวรับคลิก (ข้าม / ไปต่อ) และกันคลิกทะลุลงสนาม
            Image blocker = layer.gameObject.AddComponent<Image>();

            blocker.color = new Color(0.02f, 0.015f, 0.01f, 0.94f);

            Button click = layer.gameObject.AddComponent<Button>();

            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(OnClicked);

            // แสงทองด้านหลัง + ขอบมืด
            glow = UIFactory.CreatePanel(layer, "Glow", new Color(Gold.r, Gold.g, Gold.b, 0f));

            glow.sprite = UIShapes.Glow;
            glow.type = Image.Type.Simple;
            glow.raycastTarget = false;

            Center(glow.rectTransform, new Vector2(0f, 20f), new Vector2(1500f, 1100f));

            Image vignette = UIFactory.CreatePanel(layer, "Vignette", new Color(0f, 0f, 0f, 0.7f));

            vignette.sprite = UIShapes.Vignette;
            vignette.type = Image.Type.Simple;
            vignette.raycastTarget = false;

            UIFactory.Stretch(vignette.rectTransform);

            // หัวข้อ
            title = ScreenKit.AddLabel(
                layer, "COIN TOSS", 84, TextAnchor.MiddleCenter, 0.1f, 0.8f, 0.9f, 0.95f, FontStyle.Bold);

            title.color = new Color(Gold.r, Gold.g, Gold.b, 0f);
            title.raycastTarget = false;

            AddOutline(title);

            subtitle = ScreenKit.AddLabel(
                layer, "Who goes first?", 34, TextAnchor.MiddleCenter, 0.2f, 0.74f, 0.8f, 0.81f, FontStyle.Italic);

            subtitle.color = new Color(UIFactory.TextColor.r, UIFactory.TextColor.g, UIFactory.TextColor.b, 0f);
            subtitle.raycastTarget = false;

            AddOutline(subtitle);

            // ป้ายผู้เล่นสองฝั่ง
            humanPlate = BuildPlate(humanName, -640f, true);
            aiPlate = BuildPlate(aiName, 640f, false);

            // เงาใต้เหรียญ
            shadow = UIFactory.CreatePanel(layer, "CoinShadow", new Color(0f, 0f, 0f, 0.5f));

            shadow.sprite = UIShapes.Glow;
            shadow.type = Image.Type.Simple;
            shadow.raycastTarget = false;

            Center(shadow.rectTransform, new Vector2(0f, CoinBaseY - 150f), new Vector2(260f, 60f));

            BuildCoin();

            hint = ScreenKit.AddLabel(
                layer, "Click to continue", 26, TextAnchor.MiddleCenter, 0.3f, 0.03f, 0.7f, 0.09f);

            hint.color = new Color(UIFactory.TextColor.r, UIFactory.TextColor.g, UIFactory.TextColor.b, 0f);
            hint.raycastTarget = false;

            AddOutline(hint);

            Sfx.Play(SoundId.Pack);
        }

        private void BuildCoin()
        {
            coin = UIFactory.CreateRect("Coin", layer);

            Center(coin, new Vector2(0f, CoinBaseY), new Vector2(260f, 260f));

            // ขอบเหรียญ (ดูหนา) อยู่หลังหน้าเหรียญเยื้องลงเล็กน้อย
            coinEdge = UIFactory.CreatePanel(coin, "Edge", new Color(0.55f, 0.38f, 0.1f, 1f));

            coinEdge.sprite = UIShapes.Circle;
            coinEdge.type = Image.Type.Simple;
            coinEdge.raycastTarget = false;

            Center(coinEdge.rectTransform, new Vector2(0f, -12f), new Vector2(260f, 260f));

            coinFace = UIFactory.CreatePanel(coin, "Face", Gold);

            coinFace.sprite = UIShapes.Circle;
            coinFace.type = Image.Type.Simple;
            coinFace.raycastTarget = false;

            Center(coinFace.rectTransform, Vector2.zero, new Vector2(260f, 260f));

            Image rim = UIFactory.CreatePanel(coin, "Rim", new Color(0.6f, 0.4f, 0.08f, 0.9f));

            rim.sprite = UIShapes.RingCircle;
            rim.type = Image.Type.Simple;
            rim.raycastTarget = false;

            Center(rim.rectTransform, Vector2.zero, new Vector2(214f, 214f));

            coinText = UIFactory.CreateText(
                coin, frontLabel, 70, new Color(0.32f, 0.2f, 0.04f, 1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            coinText.raycastTarget = false;
            coinText.horizontalOverflow = HorizontalWrapMode.Overflow;
            coinText.verticalOverflow = VerticalWrapMode.Overflow;

            Center(coinText.rectTransform, Vector2.zero, new Vector2(220f, 100f));

            Image shine = UIFactory.CreatePanel(coin, "Shine", new Color(1f, 1f, 1f, 0.28f));

            shine.sprite = UIShapes.Glow;
            shine.type = Image.Type.Simple;
            shine.raycastTarget = false;

            Center(shine.rectTransform, new Vector2(-50f, 55f), new Vector2(120f, 70f));

            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        }

        private Plate BuildPlate(string name, float x, bool human)
        {
            Plate plate = new Plate();

            plate.Rect = UIFactory.CreateRect("Plate", layer);

            Center(plate.Rect, new Vector2(x, 40f), new Vector2(400f, 250f));

            plate.Group = plate.Rect.gameObject.AddComponent<CanvasGroup>();
            plate.Group.alpha = 0f;

            plate.Glow = UIFactory.CreatePanel(plate.Rect, "PlateGlow", new Color(Gold.r, Gold.g, Gold.b, 0f));

            plate.Glow.sprite = UIShapes.Glow;
            plate.Glow.type = Image.Type.Simple;
            plate.Glow.raycastTarget = false;

            Center(plate.Glow.rectTransform, Vector2.zero, new Vector2(560f, 400f));

            Image back = UIFactory.CreatePanel(plate.Rect, "Back", new Color(0.07f, 0.05f, 0.04f, 0.95f));

            back.raycastTarget = false;

            UIShapes.MakeRounded(back, 20f);
            UIFactory.Stretch(back.rectTransform);

            Color accent = human ? Gold : Silver;

            plate.Name = ScreenKit.AddLabel(
                plate.Rect, human ? humanName : aiName, 42, TextAnchor.MiddleCenter,
                0.04f, 0.68f, 0.96f, 0.96f, FontStyle.Bold);

            plate.Name.color = accent;
            plate.Name.raycastTarget = false;

            plate.Order = ScreenKit.AddLabel(
                plate.Rect, "", 84, TextAnchor.MiddleCenter,
                0.04f, 0.28f, 0.96f, 0.7f, FontStyle.Bold);

            plate.Order.color = new Color(accent.r, accent.g, accent.b, 0f);
            plate.Order.raycastTarget = false;

            AddOutline(plate.Order);

            plate.Detail = ScreenKit.AddLabel(
                plate.Rect, "", 24, TextAnchor.MiddleCenter,
                0.05f, 0.04f, 0.95f, 0.3f);

            plate.Detail.color = new Color(UIFactory.TextColor.r, UIFactory.TextColor.g, UIFactory.TextColor.b, 0f);
            plate.Detail.raycastTarget = false;

            return plate;
        }

        private static void Center(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void AddOutline(Text text)
        {
            Outline outline = text.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        // ---------- คลิก ----------

        private void OnClicked()
        {
            if (exiting)
                return;

            if (!revealed)
            {
                // ข้ามไปตอนเปิดผล
                if (age < RevealAt)
                    age = RevealAt;

                return;
            }

            StartExit();
        }

        private void StartExit()
        {
            if (exiting)
                return;

            exiting = true;
            exitAge = 0f;
        }

        // ---------- ต่อเฟรม ----------

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            age += dt;

            if (exiting)
                exitAge += dt;

            float open = Mathf.Clamp01(age / 0.4f);
            float close = exiting ? Mathf.Clamp01(exitAge / ExitSeconds) : 0f;

            group.alpha = open * (1f - close);

            UpdateTexts();
            UpdateCoin();
            UpdateEvents();

            if (!exiting && revealed && age >= AutoEnd)
                StartExit();

            if (exiting && exitAge >= ExitSeconds)
                Finish();
        }

        private void UpdateTexts()
        {
            float titleA = Mathf.Clamp01((age - 0.1f) / 0.4f);

            title.color = new Color(Gold.r, Gold.g, Gold.b, titleA);

            Color sub = UIFactory.TextColor;

            sub.a = Mathf.Clamp01((age - 0.3f) / 0.4f) * (revealed ? 0f : 1f);

            subtitle.color = sub;

            float glowPulse = 0.16f + 0.06f * Mathf.Sin(age * 2.4f);
            float glowBoost = landed ? 0.14f : 0f;

            glow.color = new Color(Gold.r, Gold.g, Gold.b, (glowPulse + glowBoost) * Mathf.Clamp01(age / 0.8f));

            float plateIn = Mathf.Clamp01((age - 0.25f) / 0.5f);

            humanPlate.Group.alpha = plateIn * (revealed && !humanFirst ? 0.6f : 1f);
            aiPlate.Group.alpha = plateIn * (revealed && humanFirst ? 0.6f : 1f);

            // ป้ายเลื่อนเข้าจากข้าง
            float slide = (1f - plateIn) * 120f;

            humanPlate.Rect.anchoredPosition = new Vector2(-640f - slide, 40f);
            aiPlate.Rect.anchoredPosition = new Vector2(640f + slide, 40f);

            Color hintColor = UIFactory.TextColor;

            hintColor.a = revealed && !exiting
                ? Mathf.Clamp01((age - RevealAt - 0.6f) / 0.4f) * (0.55f + 0.45f * Mathf.Sin(age * 3f))
                : 0f;

            hint.color = hintColor;
        }

        private void UpdateCoin()
        {
            float y = CoinBaseY;
            float scaleY = 1f;
            float scale = 1f;
            float arc = 0f;
            bool front = true;
            float tilt = 0f;

            if (age < FlipStart)
            {
                // รอก่อนโยน: ลอยเบาๆ
                y += 6f * Mathf.Sin(age * 5f);
            }
            else if (age < LandAt)
            {
                float p = (age - FlipStart) / FlipTime;

                arc = 4f * p * (1f - p);

                y += CoinHeight * arc;
                scale = 1f + 0.55f * arc;

                // หมุนเร็วตอนแรก ช้าลงตอนใกล้ตก
                float eased = 1f - (1f - p) * (1f - p);
                float theta = halfTurns * Mathf.PI * eased;

                float cos = Mathf.Cos(theta);

                front = cos >= 0f;
                scaleY = Mathf.Max(0.04f, Mathf.Abs(cos));

                int half = Mathf.FloorToInt(theta / Mathf.PI);

                if (half != lastHalf)
                {
                    if (lastHalf >= 0)
                        Sfx.Play(SoundId.Click);

                    lastHalf = half;
                }

                tilt = Mathf.Sin(age * 9f) * 8f * arc;
            }
            else
            {
                // ตกลงพื้น: กระดอนเล็กน้อยแล้วนิ่ง
                front = halfTurns % 2 == 0;

                float q = Mathf.Clamp01((age - LandAt) / BounceTime);

                y += 46f * 4f * q * (1f - q) * (1f - q);
            }

            coin.anchoredPosition = new Vector2(0f, y);
            coin.localScale = new Vector3(scale, scale * scaleY, 1f);
            coin.localRotation = Quaternion.Euler(0f, 0f, tilt);

            coinFace.color = front ? Gold : Silver;
            coinEdge.color = front ? new Color(0.55f, 0.38f, 0.1f, 1f) : new Color(0.4f, 0.48f, 0.65f, 1f);

            coinText.text = front ? frontLabel : backLabel;

            // เงาใต้เหรียญ: เล็กและจางลงเมื่อเหรียญสูง
            float shadowScale = 1f - 0.45f * arc;

            shadow.rectTransform.sizeDelta = new Vector2(260f * shadowScale, 60f * shadowScale);
            shadow.color = new Color(0f, 0f, 0f, 0.55f * (1f - 0.8f * arc) * Mathf.Clamp01(age / 0.5f));
        }

        private void UpdateEvents()
        {
            if (!landed && age >= LandAt)
            {
                landed = true;

                Vector3 pos = coin.position;

                UiFx.Ring(layer, pos, Gold, 100f, 700f, 0.7f);
                UiFx.Ring(layer, pos, Color.white, 60f, 420f, 0.5f);
                UiFx.Burst(layer, pos, Gold, 40, 520f, 26f, 1.0f, 200f);

                SecretFx.Shake(layer, 14f, 0.4f);

                Sfx.Play(SoundId.Hit);
            }

            if (!revealed && age >= RevealAt)
            {
                revealed = true;

                Reveal();
            }
        }

        // เปิดผล: ใครเริ่มก่อน / หลัง
        private void Reveal()
        {
            Plate first = humanFirst ? humanPlate : aiPlate;
            Plate second = humanFirst ? aiPlate : humanPlate;

            first.Order.text = "1ST";
            first.Order.color = new Color(Gold.r, Gold.g, Gold.b, 1f);

            first.Detail.text = "Goes first";
            first.Detail.color = new Color(1f, 1f, 1f, 1f);

            first.Glow.color = new Color(Gold.r, Gold.g, Gold.b, 0.55f);

            second.Order.text = "2ND";
            second.Order.color = new Color(0.75f, 0.78f, 0.85f, 1f);

            second.Detail.text = "Draws +1 card and gets The Coin";
            second.Detail.color = new Color(0.85f, 0.88f, 0.95f, 1f);

            string result = humanFirst
                ? "YOU GO FIRST"
                : aiName + " GOES FIRST";

            SlamText.Make(layer, result, 96, humanFirst ? Gold : Silver, new Vector2(0f, -300f), 60f, 0.5f);

            Vector3 firstPos = first.Rect.position;

            UiFx.Ring(layer, firstPos, Gold, 80f, 640f, 0.8f);
            UiFx.Burst(layer, firstPos, Gold, 36, 480f, 24f, 1.1f, 160f);

            Sfx.Play(SoundId.Win);
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
