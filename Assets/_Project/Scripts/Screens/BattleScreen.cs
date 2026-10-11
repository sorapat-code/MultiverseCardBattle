using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// หน้าจอต่อสู้: วาดสถานะจาก BattleManager และส่งการกระทำผ่าน Command เท่านั้น
    /// ผู้เล่น 1 = มนุษย์ (ด้านล่าง) ผู้เล่น 2 = AI (ด้านบน)
    /// </summary>
    public class BattleScreen : MonoBehaviour
    {
        private static readonly Vector2 HandCardSize = new Vector2(140f, 200f);
        private static readonly Vector2 FieldCardSize = new Vector2(150f, 210f);
        private static readonly Vector2 DiscoverCardSize = new Vector2(220f, 310f);
        private static readonly Vector2 PreviewCardSize = new Vector2(250f, 350f);

        private static float AIActionDelay
        {
            get { return MultiverseCardBattle.Core.GameSettings.AiActionDelay; }
        }

        private BattleManager battle;

        private RectTransform root;
        private RectTransform enemyField;
        private RectTransform playerField;
        private RectTransform handZone;
        private RectTransform overlay;

        private Text turnText;
        private Text hintText;
        private ScrollRect logScroll;
        private int logBuilt;
        private int lastLogSide = 99;
        private const int MaxLogBubbles = 150;
        private Text termsText;

        // ป้ายประกาศผลโยนเหรียญตอนเริ่ม
        private RectTransform bannerRoot;
        private Text bannerText;
        private float bannerTimer;
        private bool bannerShown;

        private Button enemyLeader;
        private Button playerLeader;
        private Button endTurnButton;
        private RectTransform devilRoot;
        private Button devilButton;
        private int devilFxInstance;
        private Button cancelButton;

        // แผงข้อมูล Leader (แถบ HP / Energy)
        private LeaderWidget enemyWidget;
        private LeaderWidget playerWidget;

        // ดูกองทิ้งและมือฝั่งตรงข้าม
        private enum PileKind
        {
            None,
            EnemyHand,
            EnemyGraveyard,
            PlayerGraveyard
        }

        private PileKind openPile = PileKind.None;
        private RectTransform pileOverlay;
        private ScrollRect pileScroll;
        private string pileSignature = "";
        private PileKind builtPile = PileKind.None;
        private float pileScrollValue = 1f;
        private Button enemyHandButton;
        private Button enemyGraveButton;
        private Button playerGraveButton;
        private Text playerDeckLabel;
        private Text enemyDeckLabel;

        private class LeaderWidget
        {
            public Button Button;
            public Text Title;
            public Image HpFill;
            public Text HpText;
            public Text EnergyText;
            public Image PortraitArt;
            public Text PortraitInitial;
            public Image[] Crystals;
        }

        // พรีวิวการ์ดใบใหญ่ตอนเอาเมาส์ชี้
        private CardView previewView;
        private RectTransform previewZone;

        // ใช้ตรวจว่าค่าอะไรเปลี่ยน เพื่อแสดงตัวเลขลอยและเล่นเสียง
        private readonly Dictionary<int, CardView> viewsByInstance =
            new Dictionary<int, CardView>();

        private Dictionary<int, int> lastHealth = new Dictionary<int, int>();
        private Dictionary<int, int> lastAttacks = new Dictionary<int, int>();
        private Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Vector3> lastHandPositions = new Dictionary<int, Vector3>();
        private int lastAiHandCount = -1;
        private bool secretAnnounced;
        private int lastHumanGrave = -1;
        private int lastAiGrave = -1;
        private RectTransform fxLayer;
        private int lastHumanHealth = -1;
        private int lastAiHealth = -1;
        private int lastFieldCount = -1;
        private bool sawDamage;
        private bool sawHeal;

        // การเลือกของผู้เล่น
        // ลากการ์ด: ghost ตามเมาส์ แล้วตัดสินตอนปล่อย
        private CardView dragGhost;
        private CardInstance dragCard;
        private bool dragFromHand;
        private RectTransform dropZone;

        // Mulligan
        private RectTransform mulliganRoot;
        private RectTransform mulliganRow;
        private Button mulliganButton;
        private readonly HashSet<CardInstance> mulliganPicks = new HashSet<CardInstance>();
        private bool mulliganShown;
        private bool mulliganDirty;

        private CardInstance selectedHand;
        private CardInstance selectedAttacker;
        private List<TargetCandidate> activeTargets =
            new List<TargetCandidate>();

        private string hint = "";
        private int lastVersion = -1;
        private bool dirty = true;

        private float aiTimer;
        private int aiFailures;

        private bool resultShown;
        private List<string> resultLines = new List<string>();

        private BattlePlayer Human
        {
            get { return battle.Player1; }
        }

        private BattlePlayer Ai
        {
            get { return battle.Player2; }
        }

        // ---------- Unity ----------

        private void Start()
        {
            // CardActionTest ใช้ทดสอบ Effect ในซีนนี้ จะไปแก้สถานะการต่อสู้จริง
            // จึงปิดไว้ (ปิดก่อน Start ของมันทำงาน)
            foreach (CardActionTest test in
                FindObjectsByType<CardActionTest>())
            {
                test.enabled = false;
            }

            battle = FindAnyObjectByType<BattleManager>();

            if (battle == null)
            {
                GameObject go = new GameObject("BattleManager");

                battle = go.AddComponent<BattleManager>();
            }

            BuildUI();
        }

        private void Update()
        {
            if (battle == null)
                return;

            if (!battle.HasStarted)
            {
                battle.StartBattle();
                return;
            }

            // Mulligan: รอผู้เล่นเลือกเปลี่ยนการ์ดก่อน ยังไม่เริ่มเทิร์นแรก
            if (battle.MulliganPending)
            {
                if (!mulliganShown)
                    BeginMulligan();

                if (mulliganDirty)
                    RefreshMulligan();

                return;
            }

            UpdateDrag();

            UpdateBanner();

            RunAI();

            HandleHotkeys();

            // ไม่ใช่เทิร์นผู้เล่น ล้างการเลือกค้าง
            if (battle.CurrentPlayer != Human &&
                (selectedHand != null || selectedAttacker != null))
            {
                ClearSelection();
            }

            if (dirty || battle.StateVersion != lastVersion)
            {
                Refresh();
            }
        }

        private void UpdateBanner()
        {
            if (!bannerShown)
            {
                bannerShown = true;

                if (battle.CoinFlipped)
                {
                    bool youFirst = battle.FirstPlayerID == Human.PlayerID;

                    bannerText.text =
                        "<color=#FFD070>COIN FLIP</color>\n" +
                        (youFirst
                            ? "You go first"
                            : Ai.Name + " goes first\n" +
                              "<size=24>You get The Coin (+1 Energy)</size>");

                    bannerRoot.gameObject.SetActive(true);
                    bannerRoot.SetAsLastSibling();

                    bannerTimer = 3f;
                }
            }

            // เหตุการณ์สุ่ม: ได้การ์ดลับจากการ Summon
            if (battle.SecretCardSummoned && !secretAnnounced)
            {
                secretAnnounced = true;

                bannerText.text =
                    "<color=#B889FF>SHADOW MONARCH</color>\n" +
                    "<size=24>Sung Jin-Woo answers your summon!</size>";

                bannerRoot.gameObject.SetActive(true);
                bannerRoot.SetAsLastSibling();

                bannerTimer = 3.5f;

                UiFx.Ring(
                    fxLayer, bannerRoot.position, new Color(0.7f, 0.45f, 1f, 0.9f),
                    100f, 900f, 0.9f);

                UiFx.Burst(
                    fxLayer, bannerRoot.position, new Color(0.65f, 0.4f, 1f, 1f),
                    50, 520f, 36f, 1.2f, -40f);

                Sfx.Play(SoundId.Win);
            }

            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;

                if (bannerTimer <= 0f)
                    bannerRoot.gameObject.SetActive(false);
            }
        }

        private void HandleHotkeys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.spaceKey.wasPressedThisFrame)
                OnEndTurnClicked();

            HandleDevHotkeys(keyboard);

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (openPile != PileKind.None)
                    TogglePile(openPile);
                else
                    ClearSelectionAndRefresh();
            }
        }

        // ---------- Dev cheats (Numpad, เฉพาะ Editor / Development Build) ----------

        private List<CardData> devCards;
        private int devCardIndex;

        private void HandleDevHotkeys(Keyboard keyboard)
        {
            if (!DevCheats.Enabled || battle == null || battle.IsOver)
                return;

            if (devCards == null)
                devCards = DevCheats.AllCards();

            if (keyboard.numpad1Key.wasPressedThisFrame)
            {
                Human.MaxEnergy = Mathf.Max(Human.MaxEnergy, 10);
                Human.CurrentEnergy = 10;

                battle.Touch();
            }

            if (keyboard.numpad2Key.wasPressedThisFrame)
                battle.DrawCard(Human);

            if (keyboard.numpad3Key.wasPressedThisFrame)
            {
                Human.LeaderHealth = Human.MaxLeaderHealth;

                battle.Touch();
            }

            if (keyboard.numpad4Key.wasPressedThisFrame)
            {
                battle.DamageLeader(null, Ai, 10, true);

                battle.Resolve();
            }

            if (devCards.Count == 0)
                return;

            if (keyboard.numpad5Key.wasPressedThisFrame)
            {
                devCardIndex = (devCardIndex + 1) % devCards.Count;

                ShowDevCard();
            }

            if (keyboard.numpadMinusKey.wasPressedThisFrame)
            {
                devCardIndex = (devCardIndex - 1 + devCards.Count) % devCards.Count;

                ShowDevCard();
            }

            if (keyboard.numpad6Key.wasPressedThisFrame)
            {
                CardData data = devCards[devCardIndex];

                CardInstance card = new CardInstance(data);

                card.OwnerID = Human.PlayerID;

                Human.AddToZone(card, CardZone.Hand);

                battle.Touch();

                Toast.Show(root, "DEV: added to hand", data.CardName);
            }
        }

        private void ShowDevCard()
        {
            CardData data = devCards[devCardIndex];

            Toast.Show(
                root,
                "DEV card " + (devCardIndex + 1) + " / " + devCards.Count,
                data.CardID + "  " + data.CardName + "  (" + data.Rarity + ")");
        }

        // ---------- AI ----------

        private void RunAI()
        {
            if (battle.IsOver || battle.HasPendingChoice)
                return;

            // รอผู้เล่นตอบโต้ / เชนยังค้างอยู่
            if (battle.ChainBlocksActions)
                return;

            if (battle.CurrentPlayer != Ai)
            {
                aiTimer = 0f;
                return;
            }

            aiTimer += Time.deltaTime;

            if (aiTimer < AIActionDelay)
                return;

            aiTimer = 0f;

            IBattleCommand command = AIPlanner.NextCommand(battle, Ai);

            if (command == null)
            {
                battle.Submit(new EndTurnCommand(Ai));
                return;
            }

            ActionResult result = battle.Submit(command);

            if (result.Success)
            {
                aiFailures = 0;
                return;
            }

            // กัน AI ค้างถ้าคำสั่งที่เลือกใช้ไม่ได้
            aiFailures++;

            if (aiFailures >= 3)
            {
                aiFailures = 0;
                battle.Submit(new EndTurnCommand(Ai));
            }
        }

        // ---------- สร้าง UI ----------

        private void BuildUI()
        {
            root = UIFactory.CreateScreenRoot("BattleCanvas", true);

            // ----- แถบข้างซ้าย: พรีวิวการ์ด + คำอธิบายศัพท์ + ประวัติการต่อสู้ (เลื่อนดูย้อนหลังได้) -----
            ScreenKit.AddPanel(
                root, "SidePanel", new Color(0.04f, 0.03f, 0.03f, 0.72f),
                0.005f, 0.01f, 0.168f, 0.99f);

            AddSideHeader("KEYWORDS", 0.586f, 0.608f);

            Image termsPanel = ScreenKit.AddPanel(
                root, "TermsPanel", new Color(0f, 0f, 0f, 0.35f),
                0.011f, 0.385f, 0.162f, 0.584f);

            termsPanel.raycastTarget = false;

            termsText = ScreenKit.AddLabel(
                root, "", 15, TextAnchor.UpperLeft,
                0.016f, 0.39f, 0.158f, 0.58f);

            termsText.resizeTextForBestFit = true;
            termsText.resizeTextMinSize = 9;
            termsText.resizeTextMaxSize = 15;

            AddSideHeader("BATTLE LOG", 0.354f, 0.378f);

            RectTransform logContent;

            logScroll = ScreenKit.AddScroll(
                root, "LogScroll", 0.011f, 0.018f, 0.162f, 0.352f,
                out logContent);

            VerticalLayoutGroup logLayout =
                logContent.gameObject.AddComponent<VerticalLayoutGroup>();

            logLayout.spacing = 4f;
            logLayout.padding = new RectOffset(6, 6, 6, 6);
            logLayout.childControlWidth = true;
            logLayout.childControlHeight = true;
            logLayout.childForceExpandWidth = true;
            logLayout.childForceExpandHeight = false;

            previewZone = UIFactory.CreateZone(
                root, "PreviewZone", 0.005f, 0.60f, 0.168f, 0.99f);

            previewView = CardView.Create(previewZone, PreviewCardSize);

            previewView.SetInteractable(false);
            previewView.gameObject.SetActive(false);

            // ----- แถบบน -----
            turnText = ScreenKit.AddLabel(
                root, "", 30, TextAnchor.MiddleLeft,
                0.18f, 0.945f, 0.40f, 0.995f, FontStyle.Bold);

            UIFactory.AddTextShadow(turnText);

            hintText = ScreenKit.AddLabel(
                root, "", 24, TextAnchor.MiddleCenter,
                0.40f, 0.945f, 0.88f, 0.995f);

            UIFactory.AddTextShadow(hintText);

            ScreenKit.AddButton(root, "Concede", Concede,
                UIFactory.Danger, 0.90f, 0.95f, 0.99f, 0.993f, 22);

            // ----- สนาม: พื้นหลังโซน + เส้นกลาง -----
            ScreenKit.AddPanel(
                root, "EnemyLane", new Color(0.7f, 0.2f, 0.2f, 0.13f),
                0.175f, 0.525f, 0.865f, 0.765f);

            ScreenKit.AddPanel(
                root, "PlayerLane", new Color(0.2f, 0.45f, 0.75f, 0.13f),
                0.175f, 0.27f, 0.865f, 0.515f);

            UIFactory.CreateDivider(root, 0.19f, 0.5175f, 0.85f, 0.5235f);

            ScreenKit.AddPanel(
                root, "HandLane", new Color(0f, 0f, 0f, 0.35f),
                0.34f, 0.0f, 0.865f, 0.262f);

            // พื้นที่ปล่อยการ์ดที่ลากมาเล่น (ทั้งสนาม ไม่มีภาพ)
            dropZone = UIFactory.CreateZone(root, "DropZone", 0.175f, 0.27f, 0.865f, 0.765f);

            enemyField = CreateRow(root, "EnemyField", 0.175f, 0.525f, 0.865f, 0.765f, 8f);
            playerField = CreateRow(root, "PlayerField", 0.175f, 0.27f, 0.865f, 0.515f, 8f);
            handZone = CreateRow(root, "Hand", 0.345f, 0.0f, 0.86f, 0.262f, 4f);

            // ----- Leader ทั้งสองฝั่ง -----
            enemyWidget = CreateLeaderWidget(
                () => OnLeaderClicked(Ai),
                0.443f, 0.785f, 0.598f, 0.935f);

            playerWidget = CreateLeaderWidget(
                () => OnLeaderClicked(Human),
                0.178f, 0.05f, 0.333f, 0.20f);

            enemyLeader = enemyWidget.Button;
            playerLeader = playerWidget.Button;

            // ----- คอลัมน์ขวา: กองการ์ด / จบเทิร์น -----
            enemyHandButton = ScreenKit.AddButton(
                root, "Opp. Hand", () => TogglePile(PileKind.EnemyHand),
                UIFactory.Muted, 0.875f, 0.87f, 0.995f, 0.93f, 20);

            enemyGraveButton = ScreenKit.AddButton(
                root, "Graveyard", () => TogglePile(PileKind.EnemyGraveyard),
                UIFactory.Muted, 0.875f, 0.80f, 0.995f, 0.86f, 20);

            enemyDeckLabel = AddPileLabel(0.875f, 0.745f, 0.995f, 0.79f);

            // ปุ่มแปลงร่าง Devil Trigger (Vergil) มีไฟสีน้ำเงินลุกรอบ แสดงเมื่อใช้ได้เท่านั้น
            devilRoot = UIFactory.CreateRect("DevilTrigger", root);

            UIFactory.SetAnchors(devilRoot, 0.875f, 0.585f, 0.995f, 0.705f);

            Image devilGlow = UIFactory.CreatePanel(
                devilRoot, "Glow", new Color(0.2f, 0.5f, 1f, 0.6f));

            devilGlow.sprite = UIShapes.Glow;
            devilGlow.type = Image.Type.Simple;
            devilGlow.raycastTarget = false;

            devilGlow.rectTransform.anchorMin = Vector2.zero;
            devilGlow.rectTransform.anchorMax = Vector2.one;
            devilGlow.rectTransform.offsetMin = new Vector2(-50f, -40f);
            devilGlow.rectTransform.offsetMax = new Vector2(50f, 40f);

            devilButton = ScreenKit.AddButton(
                devilRoot, "DEVIL\nTRIGGER", OnDevilTriggerClicked,
                new Color(0.1f, 0.25f, 0.65f, 1f), 0f, 0f, 1f, 1f, 24);

            devilRoot.gameObject.SetActive(false);

            endTurnButton = ScreenKit.AddButton(
                root, "END TURN", OnEndTurnClicked,
                UIFactory.Success, 0.875f, 0.42f, 0.995f, 0.55f, 30);

            cancelButton = ScreenKit.AddButton(
                root, "Cancel", ClearSelectionAndRefresh,
                UIFactory.Muted, 0.875f, 0.34f, 0.995f, 0.40f, 22);

            playerGraveButton = ScreenKit.AddButton(
                root, "Graveyard", () => TogglePile(PileKind.PlayerGraveyard),
                UIFactory.Muted, 0.875f, 0.14f, 0.995f, 0.20f, 20);

            playerDeckLabel = AddPileLabel(0.875f, 0.085f, 0.995f, 0.13f);

            // ป้ายผลโยนเหรียญ (หายเองหลังไม่กี่วินาที)
            bannerRoot = UIFactory.CreateRect("CoinBanner", root);

            UIFactory.SetAnchors(bannerRoot, 0.3f, 0.43f, 0.7f, 0.57f);

            Image bannerBack = UIFactory.CreateFramedPanel(
                bannerRoot, "Back", new Color(0.05f, 0.03f, 0.02f, 0.9f), 16f);

            UIFactory.Stretch(bannerBack.rectTransform);

            bannerBack.raycastTarget = false;

            bannerText = UIFactory.CreateText(
                bannerRoot, "", 36, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(bannerText.rectTransform);

            UIFactory.AddTextShadow(bannerText);

            bannerRoot.gameObject.SetActive(false);

            // ชั้นเอฟเฟกต์ (อนุภาค/รอยฟัน) อยู่เหนือการ์ด ใต้หน้าต่างต่างๆ
            fxLayer = UIFactory.CreateRect("FxLayer", root);

            UIFactory.Stretch(fxLayer);

            BlueFlame.Attach(devilRoot, fxLayer, devilGlow);

            // หน้าต่างดูกองทิ้ง / มือศัตรู
            pileOverlay = UIFactory.CreateRect("PileOverlay", root);

            UIFactory.Stretch(pileOverlay);

            pileOverlay.gameObject.SetActive(false);

            // Overlay (Discover / ผลการต่อสู้) อยู่บนสุด
            overlay = UIFactory.CreateRect("Overlay", root);

            UIFactory.Stretch(overlay);

            overlay.gameObject.SetActive(false);
        }

        // ป้ายจำนวนการ์ดในกอง (กรอบเล็ก)
        private Text AddPileLabel(float xMin, float yMin, float xMax, float yMax)
        {
            ScreenKit.AddPanel(
                root, "PilePanel", new Color(0f, 0f, 0f, 0.4f),
                xMin, yMin, xMax, yMax);

            Text label = ScreenKit.AddLabel(
                root, "", 20, TextAnchor.MiddleCenter,
                xMin, yMin, xMax, yMax, FontStyle.Bold);

            label.color = UIFactory.Gold;

            return label;
        }

        // แผง Leader: รูป (วงกลมกรอบทอง) + ชื่อ + แถบ HP + คริสตัล Energy
        private LeaderWidget CreateLeaderWidget(
            UnityEngine.Events.UnityAction onClick,
            float xMin, float yMin, float xMax, float yMax)
        {
            LeaderWidget widget = new LeaderWidget();

            widget.Button = ScreenKit.AddButton(
                root, "", onClick, UIFactory.Panel,
                xMin, yMin, xMax, yMax, 22);

            Transform panel = widget.Button.transform;

            // ข้อความของปุ่มเดิมใช้เป็นชื่อ
            widget.Title = widget.Button.GetComponentInChildren<Text>();
            widget.Title.alignment = TextAnchor.MiddleLeft;
            widget.Title.resizeTextForBestFit = true;
            widget.Title.resizeTextMinSize = 12;
            widget.Title.resizeTextMaxSize = 24;

            UIFactory.SetAnchors(widget.Title.rectTransform, 0.34f, 0.66f, 0.97f, 0.95f);

            // รูป Leader: วงทองรอบนอก > วงมืดตัดมุม > รูป/ตัวอักษรย่อ
            Image goldRing = UIFactory.CreatePanel(panel, "PortraitRing", UIFactory.Gold);

            UIShapes.MakeCircle(goldRing);

            goldRing.raycastTarget = false;

            RectTransform ringRect = goldRing.rectTransform;

            ringRect.anchorMin = new Vector2(0f, 1f);
            ringRect.anchorMax = new Vector2(0f, 1f);
            ringRect.pivot = new Vector2(0f, 1f);
            ringRect.anchoredPosition = new Vector2(10f, -10f);
            ringRect.sizeDelta = new Vector2(88f, 88f);

            Image inner = UIFactory.CreatePanel(
                goldRing.transform, "Portrait", new Color(0.1f, 0.07f, 0.05f, 1f));

            UIShapes.MakeCircle(inner);

            inner.raycastTarget = false;

            UIFactory.Stretch(inner.rectTransform);

            inner.rectTransform.offsetMin = new Vector2(4f, 4f);
            inner.rectTransform.offsetMax = new Vector2(-4f, -4f);

            inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            widget.PortraitArt = UIFactory.CreatePanel(inner.transform, "Art", Color.white);

            widget.PortraitArt.raycastTarget = false;

            UIFactory.Stretch(widget.PortraitArt.rectTransform);

            widget.PortraitArt.enabled = false;

            widget.PortraitInitial = UIFactory.CreateText(
                inner.transform, "", 44, UIFactory.Gold,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(widget.PortraitInitial.rectTransform);

            // แถบ HP
            Image hpBack = UIFactory.CreatePanel(
                panel, "HpBack", new Color(0f, 0f, 0f, 0.55f));

            UIShapes.MakeRounded(hpBack, 6f);

            hpBack.raycastTarget = false;

            UIFactory.SetAnchors(hpBack.rectTransform, 0.34f, 0.40f, 0.97f, 0.62f);

            widget.HpFill = UIFactory.CreatePanel(
                hpBack.transform, "HpFill", new Color(0.25f, 0.75f, 0.35f, 1f));

            UIShapes.MakeRounded(widget.HpFill, 6f);

            widget.HpFill.raycastTarget = false;

            UIFactory.Stretch(widget.HpFill.rectTransform);

            widget.HpText = UIFactory.CreateText(
                hpBack.transform, "", 17, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(widget.HpText.rectTransform);

            UIFactory.AddTextShadow(widget.HpText);

            // คริสตัล Energy
            RectTransform crystalRow = UIFactory.CreateZone(
                panel, "Crystals", 0.04f, 0.05f, 0.76f, 0.32f);

            UIFactory.AddHorizontalRow(crystalRow, 4f, TextAnchor.MiddleLeft);

            widget.Crystals = new Image[MaxCrystals];

            for (int i = 0; i < MaxCrystals; i++)
            {
                Image crystal = UIFactory.CreatePanel(
                    crystalRow, "Crystal" + i, Color.white);

                UIShapes.MakeCircle(crystal);

                crystal.raycastTarget = false;
                crystal.rectTransform.sizeDelta = new Vector2(22f, 22f);

                UIFactory.AddRing(crystal.transform, new Color(0f, 0f, 0f, 0.6f), 11f);

                widget.Crystals[i] = crystal;
            }

            widget.EnergyText = UIFactory.CreateText(
                panel, "", 20, UIFactory.Gold, TextAnchor.MiddleRight, FontStyle.Bold);

            UIFactory.SetAnchors(widget.EnergyText.rectTransform, 0.74f, 0.05f, 0.97f, 0.32f);

            UIFactory.AddTextShadow(widget.EnergyText);

            return widget;
        }

        private const int MaxCrystals = 10;

        // ตั้งรูป Leader (ถ้ามีไฟล์ใน Resources/UI) ไม่งั้นใช้ตัวอักษรย่อของชื่อ
        private static void SetPortrait(LeaderWidget widget, string name, Sprite art)
        {
            if (art != null)
            {
                widget.PortraitArt.enabled = true;
                widget.PortraitArt.sprite = art;
                widget.PortraitInitial.text = "";

                return;
            }

            widget.PortraitArt.enabled = false;

            widget.PortraitInitial.text =
                string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
        }

        private static RectTransform CreateRow(
            Transform parent, string name,
            float xMin, float yMin, float xMax, float yMax,
            float spacing)
        {
            RectTransform row =
                UIFactory.CreateZone(parent, name, xMin, yMin, xMax, yMax);

            UIFactory.AddHorizontalRow(row, spacing);

            return row;
        }

        // ---------- วาดสถานะ ----------

        private void Refresh()
        {
            dirty = false;
            lastVersion = battle.StateVersion;

            bool myTurn =
                battle.CurrentPlayer == Human &&
                !battle.IsOver &&
                !battle.HasPendingChoice &&
                !battle.ChainBlocksActions;

            // หน้าต่างตอบโต้: เล่นได้เฉพาะ Fast spell หรือกด Pass
            bool responding =
                battle.ChainResponder == Human &&
                !battle.IsOver &&
                !battle.HasPendingChoice;

            turnText.text =
                "Turn " + battle.TurnManager.TurnNumber + " - " +
                (battle.IsOver
                    ? "Battle over"
                    : (battle.CurrentPlayer == Human
                        ? "<color=#6BE38A>Your turn</color>"
                        : "<color=#FFB060>Opponent's turn</color>"));

            // ปิดหน้าต่างกองทิ้งเมื่อมีหน้าต่างอื่นขึ้นมาทับ
            if (battle.IsOver || battle.HasPendingChoice)
                openPile = PileKind.None;

            hintText.text = string.IsNullOrEmpty(hint)
                ? "<color=#8A93A6>Space: end turn  -  Esc: cancel</color>"
                : hint;

            if (battle.HasPendingTarget && battle.PendingTarget.Player == Human)
                hintText.text = "<color=#FF6070>" + battle.PendingTarget.Prompt + "</color>";

            if (responding)
                hintText.text = "<color=#FFD060>" + BuildResponseHint() + "</color>";

            RefreshLeader(enemyWidget, Ai);
            RefreshLeader(playerWidget, Human);
            RefreshPileButtons();

            viewsByInstance.Clear();

            RefreshField(enemyField, Ai, myTurn);
            RefreshField(playerField, Human, myTurn);
            RefreshHand(myTurn || responding);

            // ต้องคำนวณ Layout ก่อน จึงจะรู้ตำแหน่งการ์ดสำหรับตัวเลขลอย
            LayoutRebuilder.ForceRebuildLayoutImmediate(enemyField);
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerField);
            LayoutRebuilder.ForceRebuildLayoutImmediate(handZone);

            DetectChanges();
            RefreshPreview();

            RefreshDevilTrigger(myTurn);

            endTurnButton.interactable = myTurn || responding;

            UIFactory.SetButtonLabel(
                endTurnButton,
                responding ? "PASS" : (myTurn ? "END TURN" : "..."));

            endTurnButton.GetComponent<Image>().color =
                responding
                    ? UIFactory.Accent
                    : (myTurn && !HasMovesLeft() ? UIFactory.Gold : UIFactory.Success);

            cancelButton.gameObject.SetActive(
                selectedHand != null || selectedAttacker != null);

            RefreshLog();

            RefreshOverlay();
            RefreshPile();
        }

        // ข้อความบอกว่าตอนนี้ต้องตอบโต้อะไร
        private string BuildResponseHint()
        {
            IReadOnlyList<ChainLink> stack = battle.ChainStack;

            if (stack.Count == 0)
                return "Respond with a Fast spell, or PASS";

            ChainLink top = stack[stack.Count - 1];

            string who = top.Player == Human ? "You" : top.Player.Name;

            string text = who + (top.IsAttack ? ": " : " plays: ") + top.Describe();

            return text + "  -  play a Fast spell or PASS" +
                (stack.Count > 1 ? "  (chain " + stack.Count + ")" : "");
        }

        private bool HasMovesLeft()
        {
            foreach (CardInstance card in Human.Hand.Cards)
            {
                if (BattleRules.ValidatePlayable(battle, Human, card).Success)
                    return true;
            }

            foreach (CardInstance unit in Human.Field)
            {
                if (BattleRules.CanAttack(battle, Human, unit))
                    return true;
            }

            return false;
        }

        private void AddSideHeader(string title, float yMin, float yMax)
        {
            Text header = ScreenKit.AddLabel(
                root, title, 15, TextAnchor.MiddleLeft,
                0.016f, yMin, 0.158f, yMax, FontStyle.Bold);

            header.color = UIFactory.Gold;
        }

        // แชทบับเบิล: ฝั่งผู้เล่นชิดขวา ศัตรูชิดซ้าย เหตุการณ์ทั่วไปอยู่กลาง เลื่อนดูย้อนหลังได้
        private void RefreshLog()
        {
            if (logScroll == null || battle == null)
                return;

            int add = battle.LogTotal - logBuilt;

            if (add <= 0)
                return;

            logBuilt = battle.LogTotal;

            bool atBottom =
                logScroll.verticalNormalizedPosition <= 0.02f ||
                logScroll.content.rect.height <= logScroll.viewport.rect.height + 1f;

            int count = battle.LogLines.Count;
            int start = Mathf.Max(0, count - add);

            for (int i = start; i < count; i++)
                AddLogBubble(battle.LogLines[i]);

            RectTransform content = logScroll.content;

            while (content.childCount > MaxLogBubbles)
            {
                Transform old = content.GetChild(0);

                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            if (!atBottom)
                return;

            Canvas.ForceUpdateCanvases();

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            logScroll.verticalNormalizedPosition = 0f;
        }

        private void AddLogBubble(string line)
        {
            RectTransform content = logScroll.content;

            int side = 0;
            string body = line;

            string mine = Human.Name;
            string theirs = Ai.Name;

            if (!string.IsNullOrEmpty(mine) && line.StartsWith(mine) &&
                line.Length > mine.Length && line[mine.Length] == ' ')
            {
                side = 1;
                body = line.Substring(mine.Length + 1);
            }
            else if (!string.IsNullOrEmpty(theirs) && line.StartsWith(theirs) &&
                line.Length > theirs.Length && line[theirs.Length] == ' ')
            {
                side = -1;
                body = line.Substring(theirs.Length + 1);
            }

            TextAnchor align = side > 0
                ? TextAnchor.UpperRight
                : (side < 0 ? TextAnchor.UpperLeft : TextAnchor.UpperCenter);

            // ชื่อผู้พูดเหนือบับเบิล เมื่อเปลี่ยนคน
            if (side != 0 && side != lastLogSide)
            {
                RectTransform nameRow = UIFactory.CreateRect("LogName", content);

                Text who = UIFactory.CreateText(
                    nameRow, side > 0 ? "You" : theirs, 13,
                    side > 0
                        ? new Color(0.95f, 0.8f, 0.45f, 1f)
                        : new Color(0.62f, 0.72f, 0.95f, 1f),
                    side > 0 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                    FontStyle.Bold);

                UIFactory.Stretch(who.rectTransform);

                LayoutElement nameLayout =
                    nameRow.gameObject.AddComponent<LayoutElement>();

                nameLayout.preferredHeight = 18f;
                nameLayout.minHeight = 18f;
            }

            lastLogSide = side;

            RectTransform row = UIFactory.CreateRect("LogRow", content);

            HorizontalLayoutGroup rowLayout =
                row.gameObject.AddComponent<HorizontalLayoutGroup>();

            rowLayout.childAlignment = align;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            Color bubbleColor = side > 0
                ? new Color(0.42f, 0.30f, 0.10f, 0.96f)
                : (side < 0
                    ? new Color(0.20f, 0.22f, 0.32f, 0.96f)
                    : new Color(0f, 0f, 0f, 0.38f));

            Image bubble = UIFactory.CreateRoundedPanel(
                row, "Bubble", bubbleColor, side == 0 ? 10f : 14f);

            bubble.raycastTarget = false;

            LayoutElement bubbleLayout =
                bubble.gameObject.AddComponent<LayoutElement>();

            bubbleLayout.preferredWidth = side == 0 ? 262f : 222f;

            VerticalLayoutGroup inner =
                bubble.gameObject.AddComponent<VerticalLayoutGroup>();

            inner.padding = new RectOffset(10, 10, 6, 6);
            inner.childControlWidth = true;
            inner.childControlHeight = true;
            inner.childForceExpandWidth = true;
            inner.childForceExpandHeight = false;

            Text message = UIFactory.CreateText(
                bubble.transform, body, side == 0 ? 13 : 15,
                side == 0
                    ? new Color(0.78f, 0.74f, 0.66f, 1f)
                    : UIFactory.TextColor,
                side == 0 ? TextAnchor.UpperCenter : TextAnchor.UpperLeft,
                side == 0 ? FontStyle.Italic : FontStyle.Normal);

            message.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void RefreshLeader(LeaderWidget widget, BattlePlayer player)
        {
            widget.Title.text = "<b>" + player.Name + "</b>";

            if (player.LeaderGear != null)
            {
                widget.Title.text +=
                    "  <color=#F2B33D>[" + player.LeaderGear.Data.CardName +
                    " " + player.LeaderGearTurns + "]</color>";
            }

            float hp = player.MaxLeaderHealth > 0
                ? Mathf.Clamp01((float)player.LeaderHealth / player.MaxLeaderHealth)
                : 0f;

            widget.HpFill.rectTransform.anchorMax = new Vector2(hp, 1f);

            widget.HpFill.color =
                hp > 0.5f
                    ? new Color(0.25f, 0.75f, 0.35f, 1f)
                    : (hp > 0.25f
                        ? new Color(0.95f, 0.75f, 0.2f, 1f)
                        : new Color(0.9f, 0.3f, 0.3f, 1f));

            widget.HpText.text =
                "HP " + player.LeaderHealth + " / " + player.MaxLeaderHealth;

            for (int i = 0; i < widget.Crystals.Length; i++)
            {
                Color color;

                if (i < player.CurrentEnergy)
                    color = new Color(0.35f, 0.85f, 1f, 1f);
                else if (i < player.MaxEnergy)
                    color = new Color(0.08f, 0.28f, 0.34f, 1f);
                else
                    color = new Color(1f, 1f, 1f, 0.12f);

                widget.Crystals[i].color = color;
            }

            widget.EnergyText.text =
                player.CurrentEnergy + "/" + player.MaxEnergy;

            SetPortrait(
                widget,
                player.Name,
                player == Human
                    ? UIArt.Get("portrait_player")
                    : (UIArt.Get(UIArt.PortraitName(player.Name)) ??
                       UIArt.Get("portrait_enemy")));

            bool isTarget = IsActiveTarget(player);

            widget.Button.GetComponent<Image>().color =
                isTarget ? UIFactory.Danger : UIFactory.Panel;
        }

        private void RefreshField(RectTransform row, BattlePlayer owner, bool myTurn)
        {
            ScreenKit.ClearNow(row);

            foreach (CardInstance card in owner.Field)
            {
                CardView view = CardView.Create(row, FieldCardSize);

                view.ShowInstance(card);

                viewsByInstance[card.InstanceID] = view;
                view.HoverEntered += ShowPreview;
                view.HoverExited += HidePreview;

                CardHighlight highlight = CardHighlight.None;

                if (card == selectedAttacker)
                {
                    highlight = CardHighlight.Selected;
                }
                else if (IsActiveTarget(card))
                {
                    highlight = CardHighlight.Target;
                }
                else if (owner == Human && myTurn &&
                    selectedHand == null &&
                    BattleRules.CanAttack(battle, Human, card))
                {
                    highlight = CardHighlight.Ready;
                }

                view.SetHighlight(highlight);

                if (owner == Human)
                    view.SetStateLabel(UnitStateLabel(card));

                // Vergil ร่าง Devil Trigger: ไฟสีน้ำเงินลุกรอบการ์ด
                if (card.Data != null &&
                    (card.Data.Passives & CardPassive.DevilTriggerForm) != 0)
                {
                    BlueFlame.Attach(
                        (RectTransform)view.transform, fxLayer, null, 0.06f, 28f);
                }

                view.Clicked += OnFieldCardClicked;

                if (owner == Human)
                    view.DragStarted += OnCardDragStarted;

                if (card == dragCard)
                    view.SetDimmed(true);
            }
        }

        private string UnitStateLabel(CardInstance card)
        {
            ActionResult check =
                BattleRules.ValidateCanAttack(battle, Human, card);

            if (check.Success || battle.CurrentPlayer != Human)
                return "";

            if (check.Message.Contains("stunned"))
                return "STUNNED";

            if (check.Message.Contains("frozen"))
                return "FROZEN";

            if (check.Message.Contains("disarmed"))
                return "DISARMED";

            if (check.Message.Contains("just summoned"))
                return "SLEEPING";

            return "";
        }

        private void RefreshHand(bool myTurn)
        {
            ScreenKit.ClearNow(handZone);

            HorizontalLayoutGroup handLayout =
                handZone.GetComponent<HorizontalLayoutGroup>();

            if (handLayout != null)
            {
                int count = Human.Hand.Count;
                float width = handZone.rect.width > 10f ? handZone.rect.width : 1000f;

                handLayout.spacing = count > 1
                    ? Mathf.Clamp(
                        (width - count * HandCardSize.x) / (count - 1),
                        -HandCardSize.x * 0.45f, 6f)
                    : 6f;
            }

            foreach (CardInstance card in Human.Hand.Cards)
            {
                CardView view = CardView.Create(handZone, HandCardSize);

                view.ShowInstance(card);

                CardHover.Add(view);

                viewsByInstance[card.InstanceID] = view;
                view.HoverEntered += ShowPreview;
                view.HoverExited += HidePreview;

                CardHighlight highlight = CardHighlight.None;

                if (card == selectedHand)
                {
                    highlight = CardHighlight.Selected;
                }
                else if (myTurn &&
                    BattleRules.ValidatePlayable(battle, Human, card).Success)
                {
                    highlight = CardHighlight.Ready;
                }

                view.SetHighlight(highlight);

                view.Clicked += OnHandCardClicked;
                view.DragStarted += OnCardDragStarted;

                if (card == dragCard)
                    view.SetDimmed(true);
            }
        }

        // ---------- พรีวิวการ์ด ----------

        private void ShowPreview(CardView view)
        {
            if (view == null || previewView == null)
                return;

            if (view.Instance != null)
                previewView.ShowInstance(view.Instance);
            else if (view.Data != null)
                previewView.ShowData(view.Data);
            else
                return;

            previewView.SetHighlight(CardHighlight.None);

            termsText.text = GameGlossary.HintsFor(view.Data, view.Instance);
        }

        private void HidePreview(CardView view)
        {
            // มีการ์ดที่คลิกปักหมุดไว้ = กลับไปแสดงใบนั้นแทนการซ่อน
            if (ShowPinned())
                return;

            if (previewView != null)
                previewView.gameObject.SetActive(false);

            if (termsText != null)
                termsText.text = "";
        }

        // การ์ดที่คลิกซ้ายไว้ พรีวิวซ้ายบนจะค้างที่ใบนี้จนกว่าจะคลิกใบอื่น
        private CardInstance pinnedCard;

        private void PinCard(CardView view)
        {
            if (view == null || view.Instance == null)
                return;

            pinnedCard = view.Instance;

            ShowPreview(view);

            if (previewView != null)
                previewView.gameObject.SetActive(true);
        }

        private bool PinnedValid()
        {
            return pinnedCard != null && pinnedCard.Data != null &&
                (pinnedCard.Zone == CardZone.Field ||
                 pinnedCard.Zone == CardZone.Hand ||
                 pinnedCard.Zone == CardZone.Graveyard);
        }

        private bool ShowPinned()
        {
            if (!PinnedValid() || previewView == null || termsText == null)
            {
                pinnedCard = null;
                return false;
            }

            previewView.ShowInstance(pinnedCard);
            previewView.SetHighlight(CardHighlight.None);
            previewView.gameObject.SetActive(true);

            termsText.text = GameGlossary.HintsFor(pinnedCard.Data, pinnedCard);

            return true;
        }

        // วาดใหม่หลังสถานะเปลี่ยน: อัปเดตค่าบนพรีวิว หรือซ่อนถ้าการ์ดหายไปแล้ว
        private void RefreshPreview()
        {
            if (previewView == null)
                return;

            if (!previewView.gameObject.activeSelf)
            {
                if (pinnedCard != null)
                    ShowPinned();

                return;
            }

            CardInstance shown = previewView.Instance;

            if (shown == null)
                return;

            if (shown.Zone == CardZone.Field ||
                shown.Zone == CardZone.Hand ||
                shown.Zone == CardZone.Graveyard)
            {
                previewView.ShowInstance(shown);

                termsText.text = GameGlossary.HintsFor(shown.Data, shown);
            }
            else if (!ShowPinned())
            {
                previewView.gameObject.SetActive(false);
                termsText.text = "";
            }
        }

        // ---------- ตัวเลขลอย / เสียง ----------

        private void DetectChanges()
        {
            sawDamage = false;
            sawHeal = false;

            Dictionary<int, int> now = new Dictionary<int, int>();
            Dictionary<int, int> nowAttacks = new Dictionary<int, int>();
            Dictionary<int, Vector3> nowPositions = new Dictionary<int, Vector3>();

            // AI เล่นการ์ดจากมือ (มือลดลง) ใช้จับว่ายูนิตใหม่มาจากมือ
            bool aiPlayedFromHand =
                lastAiHandCount >= 0 && Ai.Hand.Count < lastAiHandCount;

            BattlePlayer[] players = { Human, Ai };

            foreach (BattlePlayer player in players)
            {
                foreach (CardInstance card in player.Field)
                {
                    int previous;

                    bool known =
                        lastHealth.TryGetValue(card.InstanceID, out previous);

                    // ยูนิตใหม่บนสนาม (ไม่นับการวาดครั้งแรก) ให้เด้งเข้า
                    if (!known && lastFieldCount >= 0 &&
                        viewsByInstance.TryGetValue(card.InstanceID, out CardView entered))
                    {
                        Vector3 origin = Vector3.zero;

                        bool fromHand = player == Human
                            ? lastHandPositions.TryGetValue(card.InstanceID, out origin)
                            : aiPlayedFromHand;

                        if (player != Human && fromHand && enemyHandButton != null)
                            origin = enemyHandButton.transform.position;

                        if (fromHand)
                        {
                            // การ์ดบินจากมือมาลงสนาม  ตัวจริงซ่อนไว้จนกว่าจะถึง
                            CanvasGroup hidden =
                                entered.GetComponent<CanvasGroup>();

                            if (hidden == null)
                                hidden = entered.gameObject.AddComponent<CanvasGroup>();

                            hidden.alpha = 0f;

                            CardView landing = entered;

                            CardFlight.Launch(
                                fxLayer, card, FieldCardSize, origin,
                                entered.transform.position, false,
                                () =>
                                {
                                    if (hidden != null)
                                        hidden.alpha = 1f;

                                    if (landing != null)
                                        CardFx.Pop(landing);
                                });

                            aiPlayedFromHand = false;
                        }
                        else
                        {
                            CardFx.Pop(entered);
                        }

                        UiFx.Ring(
                            fxLayer, entered.transform.position,
                            new Color(1f, 0.85f, 0.45f, 0.8f), 60f, 300f, 0.45f);

                        UiFx.Burst(
                            fxLayer, entered.transform.position,
                            new Color(1f, 0.85f, 0.45f, 0.9f), 10, 220f, 22f, 0.5f);
                    }

                    // ยูนิตโจมตี: พุ่งไปทางสนามฝั่งตรงข้าม
                    int previousAttacks;

                    if (lastAttacks.TryGetValue(card.InstanceID, out previousAttacks) &&
                        card.AttacksThisTurn > previousAttacks &&
                        viewsByInstance.TryGetValue(card.InstanceID, out CardView attacker))
                    {
                        CardFx.Lunge(
                            attacker,
                            player == Human ? Vector2.up : Vector2.down);
                    }

                    nowAttacks[card.InstanceID] = card.AttacksThisTurn;

                    CardView located;

                    if (viewsByInstance.TryGetValue(card.InstanceID, out located))
                        nowPositions[card.InstanceID] = located.transform.position;

                    if (known && previous != card.CurrentHealth)
                    {
                        CardView view;

                        if (viewsByInstance.TryGetValue(card.InstanceID, out view))
                        {
                            int delta = card.CurrentHealth - previous;

                            SpawnDelta(view.transform.position, delta);

                            CardFx.Flash(
                                view,
                                delta < 0
                                    ? new Color(1f, 0.15f, 0.15f, 0.65f)
                                    : new Color(0.3f, 1f, 0.4f, 0.55f));

                            PlayImpact(view.transform.position, delta);
                        }

                        NoteChange(card.CurrentHealth - previous);
                    }

                    now[card.InstanceID] = card.CurrentHealth;
                }
            }

            // ยูนิตที่หายไปจากสนาม (ตาย) นับเป็นความเสียหาย
            foreach (int id in lastHealth.Keys)
            {
                if (!now.ContainsKey(id))
                {
                    sawDamage = true;

                    Vector3 deathPos;

                    if (lastPositions.TryGetValue(id, out deathPos))
                    {
                        UiFx.Ring(
                            fxLayer, deathPos, new Color(1f, 0.4f, 0.25f, 0.8f),
                            50f, 280f, 0.4f);

                        UiFx.Burst(
                            fxLayer, deathPos, new Color(0.9f, 0.35f, 0.2f, 1f),
                            16, 300f, 26f, 0.7f, 380f);

                        UiFx.Burst(
                            fxLayer, deathPos, new Color(0.25f, 0.2f, 0.2f, 0.9f),
                            8, 160f, 40f, 0.8f, -60f);
                    }
                }
            }

            lastHealth = now;
            lastAttacks = nowAttacks;
            lastPositions = nowPositions;

            PlaySpellEffects();

            CheckLeader(Human, ref lastHumanHealth, playerLeader);
            CheckLeader(Ai, ref lastAiHealth, enemyLeader);

            int fieldCount = Human.Field.Count + Ai.Field.Count;

            if (sawDamage)
                Sfx.Play(SoundId.Hit);
            else if (sawHeal)
                Sfx.Play(SoundId.Heal);
            else if (lastFieldCount >= 0 && fieldCount > lastFieldCount)
                Sfx.Play(SoundId.Play);

            lastFieldCount = fieldCount;

            // จำตำแหน่งการ์ดในมือไว้ใช้ตอนการ์ดบินออกไปในครั้งถัดไป
            Dictionary<int, Vector3> nowHand = new Dictionary<int, Vector3>();

            foreach (CardInstance handCard in Human.Hand.Cards)
            {
                CardView handView;

                if (viewsByInstance.TryGetValue(handCard.InstanceID, out handView))
                    nowHand[handCard.InstanceID] = handView.transform.position;
            }

            lastHandPositions = nowHand;
            lastAiHandCount = Ai.Hand.Count;
        }

        private void CheckLeader(BattlePlayer player, ref int last, Button button)
        {
            if (last >= 0 && last != player.LeaderHealth)
            {
                int delta = player.LeaderHealth - last;

                SpawnDelta(button.transform.position, delta);

                PlayImpact(button.transform.position, delta);

                NoteChange(delta);
            }

            last = player.LeaderHealth;
        }

        // ดาเมจ = รอยฟันแดง + ประกาย / ฮีล = ประกายเขียวลอยขึ้น
        private void PlayImpact(Vector3 position, int delta)
        {
            if (delta < 0)
            {
                float power = Mathf.Clamp(-delta, 1, 8);

                UiFx.Slash(
                    fxLayer, position, new Color(1f, 0.35f, 0.3f, 0.95f),
                    200f + power * 18f, 26f + power * 2f);

                UiFx.Burst(
                    fxLayer, position, new Color(1f, 0.55f, 0.35f, 1f),
                    6 + (int)power * 2, 260f + power * 20f, 20f, 0.45f, 300f);
            }
            else if (delta > 0)
            {
                UiFx.Burst(
                    fxLayer, position, new Color(0.45f, 1f, 0.55f, 0.95f),
                    12, 90f, 22f, 0.9f, -140f);

                UiFx.Ring(
                    fxLayer, position, new Color(0.5f, 1f, 0.6f, 0.7f),
                    40f, 220f, 0.55f);
            }
        }

        // สเปลที่เพิ่งลงกองทิ้ง: ปล่อยพลังจากสนามฝั่งผู้ใช้ ตามธาตุของการ์ด
        private void PlaySpellEffects()
        {
            PlaySpellsFor(Human, ref lastHumanGrave, playerField);
            PlaySpellsFor(Ai, ref lastAiGrave, enemyField);
        }

        private void PlaySpellsFor(
            BattlePlayer player, ref int last, RectTransform lane)
        {
            int count = player.Graveyard.Count;

            if (last >= 0 && count > last)
            {
                for (int i = last; i < count; i++)
                {
                    CardInstance card = player.Graveyard[i];

                    if (card == null || card.Data == null ||
                        card.Data.Type != CardType.Spell)
                    {
                        continue;
                    }

                    Color color = SpellColor(card.Data);

                    // การ์ดสเปลบินจากมือไปกลางสนามแล้วสลาย
                    Vector3 spellOrigin = Vector3.zero;

                    bool hasOrigin = player == Human
                        ? lastHandPositions.TryGetValue(card.InstanceID, out spellOrigin)
                        : enemyHandButton != null;

                    if (player != Human && hasOrigin)
                        spellOrigin = enemyHandButton.transform.position;

                    if (hasOrigin)
                    {
                        CardFlight.Launch(
                            fxLayer, card, HandCardSize, spellOrigin,
                            lane.position, true, null);
                    }

                    UiFx.Ring(
                        fxLayer, lane.position, new Color(color.r, color.g, color.b, 0.85f),
                        80f, 520f, 0.6f);

                    UiFx.Burst(
                        fxLayer, lane.position, color, 26, 420f, 30f, 0.8f, 120f);

                }
            }

            last = count;
        }

        private static Color SpellColor(CardData data)
        {
            CardTag tags = data.Tags;

            if ((tags & CardTag.Fire) != 0) return new Color(1f, 0.55f, 0.2f, 1f);
            if ((tags & CardTag.Ice) != 0) return new Color(0.55f, 0.88f, 1f, 1f);
            if ((tags & CardTag.Lightning) != 0) return new Color(1f, 0.95f, 0.4f, 1f);
            if ((tags & CardTag.Dark) != 0) return new Color(0.65f, 0.3f, 0.95f, 1f);
            if ((tags & CardTag.Water) != 0) return new Color(0.3f, 0.6f, 1f, 1f);
            if ((tags & CardTag.Wind) != 0) return new Color(0.6f, 1f, 0.8f, 1f);
            if ((tags & CardTag.Earth) != 0) return new Color(0.75f, 0.58f, 0.3f, 1f);
            if ((tags & CardTag.Light) != 0) return new Color(1f, 0.96f, 0.75f, 1f);

            return new Color(1f, 0.85f, 0.45f, 1f);
        }

        private void NoteChange(int delta)
        {
            if (delta < 0)
                sawDamage = true;
            else if (delta > 0)
                sawHeal = true;
        }

        private void SpawnDelta(Vector3 position, int delta)
        {
            if (delta == 0)
                return;

            FloatingText.Spawn(
                root,
                position,
                delta > 0 ? "+" + delta : delta.ToString(),
                delta > 0
                    ? new Color(0.45f, 1f, 0.5f, 1f)
                    : new Color(1f, 0.4f, 0.4f, 1f));
        }

        // ---------- Overlay ----------

        private void RefreshOverlay()
        {
            if (battle.IsOver)
            {
                ShowResult();
                return;
            }

            if (battle.HasPendingDiscover &&
                battle.PendingDiscover.Player == Human)
            {
                ShowDiscover();
                return;
            }

            if (battle.HasPendingAdapt &&
                battle.PendingAdapt.Player == Human)
            {
                ShowAdapt();
                return;
            }

            overlay.gameObject.SetActive(false);
        }

        private void ShowDiscover()
        {
            ScreenKit.ClearNow(overlay);

            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();

            // พรีวิวต้องอยู่เหนือพื้นหลังมืดของ Discover
            previewZone.SetAsLastSibling();

            Image dim = UIFactory.CreatePanel(
                overlay, "Dim", new Color(0f, 0f, 0f, 0.8f));

            UIFactory.Stretch(dim.rectTransform);

            ScreenKit.AddLabel(
                overlay, "DISCOVER - choose a card", 44,
                TextAnchor.MiddleCenter, 0.2f, 0.75f, 0.8f, 0.9f,
                FontStyle.Bold);

            RectTransform row =
                CreateRow(overlay, "Options", 0.1f, 0.25f, 0.9f, 0.72f, 24f);

            foreach (CardData option in battle.PendingDiscover.Result.Options)
            {
                CardView view = CardView.Create(row, DiscoverCardSize);

                view.ShowData(option);
                view.HoverEntered += ShowPreview;
                view.HoverExited += HidePreview;
                view.SetHighlight(CardHighlight.Ready);
                view.Clicked += OnDiscoverClicked;
            }
        }

        private void ShowAdapt()
        {
            ScreenKit.ClearNow(overlay);

            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();

            Image dim = UIFactory.CreatePanel(
                overlay, "Dim", new Color(0f, 0f, 0f, 0.8f));

            UIFactory.Stretch(dim.rectTransform);

            AdaptRequest request = battle.PendingAdapt;

            ScreenKit.AddLabel(
                overlay,
                "ADAPT - choose an ability for " + request.Card.Data.CardName,
                40, TextAnchor.MiddleCenter, 0.1f, 0.68f, 0.9f, 0.85f,
                FontStyle.Bold);

            int count = request.Options.Count;
            float width = 0.2f;
            float gap = 0.03f;
            float total = count * width + (count - 1) * gap;
            float x = 0.5f - total / 2f;

            for (int i = 0; i < count; i++)
            {
                int index = i;

                ScreenKit.AddButton(
                    overlay,
                    request.Options[i].Label,
                    () => OnAdaptClicked(index),
                    UIFactory.Accent,
                    x, 0.4f, x + width, 0.55f, 32);

                x += width + gap;
            }
        }

        private void OnAdaptClicked(int index)
        {
            if (battle == null || !battle.HasPendingAdapt)
                return;

            Sfx.Play(SoundId.Play);

            battle.ResolveAdapt(index);
        }

        // ---------- ดูกองทิ้ง / มือฝั่งตรงข้าม ----------

        private void TogglePile(PileKind kind)
        {
            openPile = openPile == kind ? PileKind.None : kind;

            dirty = true;
        }

        private void RefreshPileButtons()
        {
            UIFactory.SetButtonLabel(
                enemyHandButton, "Opp. Hand " + Ai.Hand.Count);

            UIFactory.SetButtonLabel(
                enemyGraveButton, "Graveyard " + Ai.Graveyard.Count);

            UIFactory.SetButtonLabel(
                playerGraveButton, "Graveyard " + Human.Graveyard.Count);

            playerDeckLabel.text = "Deck " + Human.Deck.Count;
            enemyDeckLabel.text = "Deck " + Ai.Deck.Count;

            Tint(enemyHandButton, openPile == PileKind.EnemyHand);
            Tint(enemyGraveButton, openPile == PileKind.EnemyGraveyard);
            Tint(playerGraveButton, openPile == PileKind.PlayerGraveyard);
        }

        private static void Tint(Button button, bool active)
        {
            button.GetComponent<Image>().color =
                active ? UIFactory.Accent : UIFactory.Muted;
        }

        private void RefreshPile()
        {
            if (openPile == PileKind.None)
            {
                pileOverlay.gameObject.SetActive(false);
                pileScroll = null;
                pileSignature = "";
                builtPile = PileKind.None;
                return;
            }

            List<CardInstance> cards;
            string title;

            switch (openPile)
            {
                case PileKind.EnemyHand:
                    cards = new List<CardInstance>(Ai.Hand.Cards);
                    title = "OPPONENT'S HAND";
                    break;

                case PileKind.EnemyGraveyard:
                    cards = new List<CardInstance>(Ai.Graveyard);
                    cards.Reverse();
                    title = "OPPONENT'S GRAVEYARD";
                    break;

                default:
                    cards = new List<CardInstance>(Human.Graveyard);
                    cards.Reverse();
                    title = "YOUR GRAVEYARD";
                    break;
            }

            // สร้างหน้าต่างใหม่เฉพาะเมื่อรายการหรือค่าบนการ์ดเปลี่ยนจริง
            // (ไม่งั้นสร้างทิ้งสร้างใหม่ทุกครั้งที่สถานะเปลี่ยน ทำให้รายการกระพริบ/หาย)
            string signature = BuildPileSignature(cards);

            if (pileOverlay.gameObject.activeSelf &&
                builtPile == openPile &&
                signature == pileSignature)
            {
                return;
            }

            // เปิดกองเดิมซ้ำ = จำตำแหน่งเลื่อนไว้ กองใหม่ = เริ่มที่บนสุด
            float scrollPosition = builtPile == openPile ? pileScrollValue : 1f;

            pileSignature = signature;
            builtPile = openPile;
            pileScroll = null;

            ScreenKit.ClearNow(pileOverlay);

            pileOverlay.gameObject.SetActive(true);
            pileOverlay.SetAsLastSibling();

            // พรีวิวต้องอยู่เหนือพื้นหลังมืด
            previewZone.SetAsLastSibling();

            Image dim = UIFactory.CreatePanel(
                pileOverlay, "Dim", new Color(0f, 0f, 0f, 0.85f));

            UIFactory.Stretch(dim.rectTransform);

            ScreenKit.AddLabel(
                pileOverlay, title + " (" + cards.Count + ")", 40,
                TextAnchor.MiddleCenter, 0.2f, 0.88f, 0.8f, 0.98f,
                FontStyle.Bold);

            if (cards.Count == 0)
            {
                ScreenKit.AddLabel(
                    pileOverlay, "Empty", 36, TextAnchor.MiddleCenter,
                    0.3f, 0.45f, 0.7f, 0.55f);
            }
            else
            {
                RectTransform content;

                pileScroll = ScreenKit.AddScroll(
                    pileOverlay, "PileScroll",
                    0.19f, 0.14f, 0.97f, 0.87f, out content);

                UIFactory.AddGrid(content, new Vector2(150f, 210f), 14f, 14);

                foreach (CardInstance card in cards)
                {
                    CardView view = CardView.Create(content, new Vector2(150f, 210f));

                    view.ShowInstance(card);

                    view.HoverEntered += ShowPreview;
                    view.HoverExited += HidePreview;
                }

                pileScroll.onValueChanged.AddListener(
                    position => pileScrollValue = position.y);

                Canvas.ForceUpdateCanvases();

                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

                // เลื่อนกลับเฉพาะเมื่อเนื้อหายาวกว่าหน้าต่าง (ไม่งั้นการ์ดจะหลุดจอ)
                bool scrollable =
                    content.rect.height > pileScroll.viewport.rect.height + 1f;

                pileScroll.verticalNormalizedPosition =
                    scrollable ? Mathf.Clamp01(scrollPosition) : 1f;
            }

            ScreenKit.AddButton(
                pileOverlay, "Close", () => TogglePile(openPile),
                UIFactory.Accent, 0.4f, 0.03f, 0.6f, 0.11f, 28);
        }

        // ลายเซ็นของรายการ: ID + ค่าที่แสดงบนการ์ด ใช้เทียบว่าต้องวาดใหม่หรือไม่
        private static string BuildPileSignature(List<CardInstance> cards)
        {
            StringBuilder text = new StringBuilder();

            foreach (CardInstance card in cards)
            {
                text.Append(card.InstanceID).Append(':')
                    .Append(card.CurrentCost).Append(':')
                    .Append(card.CurrentAttack).Append(':')
                    .Append(card.CurrentHealth).Append(':')
                    .Append((int)card.Keywords).Append(';');
            }

            return text.ToString();
        }

        private List<SecretUnlock> unlockBanners;
        private int bannerIndex;

        private void ShowUnlockBanner(SecretUnlock unlock)
        {
            bool nazarick = unlock.Style == BannerStyle.Nazarick;
            bool yamato = unlock.Style == BannerStyle.Yamato;

            Image dim = UIFactory.CreatePanel(
                overlay, "BannerDim",
                nazarick
                    ? new Color(0.06f, 0f, 0.02f, 0.96f)
                    : (yamato
                        ? new Color(0f, 0.02f, 0.08f, 0.96f)
                        : new Color(0f, 0f, 0f, 0.92f)));

            UIFactory.Stretch(dim.rectTransform);

            if (nazarick)
                BannerMagic.Create(overlay, unlock.Tint);
            else if (yamato)
                BannerMagic.Create(overlay, unlock.Tint, true);

            Button next = dim.gameObject.AddComponent<Button>();

            next.transition = Selectable.Transition.None;
            next.onClick.AddListener(() =>
            {
                bannerIndex++;
                ShowResult();
            });

            Text title = ScreenKit.AddLabel(
                overlay, unlock.Title, 64, TextAnchor.MiddleCenter,
                0.05f, 0.52f, 0.95f, 0.72f, FontStyle.Bold);

            title.color = unlock.Tint;
            title.raycastTarget = false;
            UIFactory.AddTextShadow(title);

            Text sub = ScreenKit.AddLabel(
                overlay, unlock.Subtitle, 34, TextAnchor.MiddleCenter,
                0.2f, 0.42f, 0.8f, 0.52f);

            sub.raycastTarget = false;

            CardData card = CardRegistry.Get(unlock.CardId);

            string name = card != null ? card.CardName : unlock.CardId;

            Text unlocked = ScreenKit.AddLabel(
                overlay, "Secret card unlocked: " + name, 36,
                TextAnchor.MiddleCenter, 0.2f, 0.3f, 0.8f, 0.4f,
                FontStyle.Bold);

            unlocked.color = UIFactory.Gold;
            unlocked.raycastTarget = false;

            Text hint = ScreenKit.AddLabel(
                overlay, "Click to continue", 24, TextAnchor.MiddleCenter,
                0.3f, 0.12f, 0.7f, 0.2f);

            hint.raycastTarget = false;
        }

        private void ShowResult()
        {
            bool won = battle.Winner == Human;

            if (!resultShown)
            {
                resultShown = true;

                BattleSummary summary = new BattleSummary();

                summary.Turns = battle.TurnManager.TurnNumber;
                summary.PlayerHealth = Human.LeaderHealth;
                summary.PlayerMaxHealth = Human.MaxLeaderHealth;
                summary.OpponentDeckOut = Ai.Deck.Count == 0 && Human.Deck.Count > 0;
                summary.SecretSummoned = battle.SecretCardSummoned;
                summary.SecretIds.AddRange(battle.SecretsSummoned);

                summary.CardsPlayed = battle.Stats.CardsPlayed[Human.PlayerID];
                summary.SpellsPlayed = battle.Stats.SpellsPlayed[Human.PlayerID];
                summary.UnitsKilled = battle.Stats.UnitsKilled[Human.PlayerID];
                summary.LeaderDamage = battle.Stats.LeaderDamage[Human.PlayerID];
                summary.PlayedCardIds.AddRange(battle.Stats.PlayedCardIds[Human.PlayerID]);

                unlockBanners = SecretCardService.Evaluate(
                    battle, Human.PlayerID, won, SaveManager.Profile);

                foreach (SecretUnlock unlock in unlockBanners)
                    summary.SecretIds.Add(unlock.CardId);

                resultLines = CampaignService.OnBattleFinished(won, summary);

                if (battle.Winner != null)
                    Sfx.Play(won ? SoundId.Win : SoundId.Lose);
            }

            ScreenKit.ClearNow(overlay);

            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();

            // การ์ดลับที่ปลดล็อกได้ แสดง Banner ก่อนสรุปผล (คลิกเพื่อไปต่อ)
            if (unlockBanners != null && bannerIndex < unlockBanners.Count)
            {
                ShowUnlockBanner(unlockBanners[bannerIndex]);
                return;
            }

            Image dim = UIFactory.CreatePanel(
                overlay, "Dim", new Color(0f, 0f, 0f, 0.85f));

            UIFactory.Stretch(dim.rectTransform);

            string title = battle.Winner == null
                ? "DRAW"
                : (won ? "VICTORY" : "DEFEAT");

            Text titleText = ScreenKit.AddLabel(
                overlay, title, 90, TextAnchor.MiddleCenter,
                0.2f, 0.62f, 0.8f, 0.82f, FontStyle.Bold);

            titleText.color = battle.Winner != null && !won
                ? new Color(0.9f, 0.3f, 0.28f, 1f)
                : UIFactory.Gold;

            UIFactory.AddTextShadow(titleText);

            ScreenKit.AddLabel(
                overlay, string.Join("\n", resultLines), 30,
                TextAnchor.UpperCenter, 0.25f, 0.34f, 0.75f, 0.6f);

            bool fromCampaign = BattleSetup.Stage != null;

            ScreenKit.AddButton(
                overlay,
                fromCampaign ? "Back to Story" : "Main Menu",
                () => SceneLoader.Go(
                    fromCampaign ? GameScene.Campaign : GameScene.MainMenu),
                UIFactory.Accent, 0.35f, 0.18f, 0.65f, 0.28f);
        }

        // ---------- ปฏิกิริยาของผู้เล่น ----------

        private bool CanAct()
        {
            if (battle == null || battle.IsOver || battle.HasPendingChoice)
                return false;

            // หน้าต่างตอบโต้ของเรา: เล่น Fast spell ได้แม้ไม่ใช่เทิร์นเรา
            if (battle.ChainResponder == Human)
                return true;

            return battle.CurrentPlayer == Human && !battle.ChainBlocksActions;
        }

        // ---------- ลากการ์ด ----------

        private void OnCardDragStarted(CardView view)
        {
            if (mulliganRoot != null || dragCard != null ||
                !CanAct() || view == null || view.Instance == null)
                return;

            CardInstance card = view.Instance;

            if (card.OwnerID != Human.PlayerID)
                return;

            ClearSelection();

            if (card.Zone == CardZone.Hand)
            {
                ActionResult check = BattleRules.ValidatePlayable(battle, Human, card);

                if (!check.Success)
                {
                    SetHint(check.Message);
                    return;
                }

                dragFromHand = true;

                SetHint("Drop the card on the field to play it");
            }
            else if (card.Zone == CardZone.Field)
            {
                SelectAttacker(card);

                // เลือกไม่สำเร็จ (ยังตีไม่ได้) จะมีข้อความบอกไว้แล้ว
                if (selectedAttacker != card)
                    return;

                dragFromHand = false;
            }
            else
            {
                return;
            }

            dragCard = card;

            dragGhost = CardView.Create(
                root, card.Zone == CardZone.Hand ? HandCardSize : FieldCardSize);

            dragGhost.ShowInstance(card);
            dragGhost.SetInteractable(false);
            dragGhost.transform.SetAsLastSibling();
            dragGhost.transform.localScale = Vector3.one * 1.08f;

            MoveGhost();

            dirty = true;
        }

        private static Vector2 PointerPosition()
        {
            Pointer pointer = Pointer.current;

            return pointer != null ? pointer.position.ReadValue() : Vector2.zero;
        }

        private static bool PointerReleased()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                return true;

            return Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        }

        private void MoveGhost()
        {
            if (dragGhost == null)
                return;

            Vector2 pos = PointerPosition();

            dragGhost.transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        private void UpdateDrag()
        {
            if (dragCard == null)
                return;

            Keyboard keyboard = Keyboard.current;

            bool cancel =
                !CanAct() ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);

            if (cancel)
            {
                EndDrag();
                ClearSelection();
                return;
            }

            MoveGhost();

            if (PointerReleased())
                DropCard();
        }

        private void EndDrag()
        {
            if (dragGhost != null)
                Destroy(dragGhost.gameObject);

            dragGhost = null;
            dragCard = null;

            dirty = true;
        }

        private void DropCard()
        {
            Vector2 pos = PointerPosition();

            CardInstance card = dragCard;
            bool fromHand = dragFromHand;

            EndDrag();

            if (fromHand)
            {
                bool inField =
                    dropZone != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(dropZone, pos, null);

                if (inField)
                    PlayDroppedCard(card);
                else
                    ClearSelection();

                return;
            }

            object target = FindDropTarget(pos);

            if (target == null || ReferenceEquals(target, card))
            {
                ClearSelection();
                return;
            }

            TryAttack(target);

            // ปล่อยผิดเป้า: ยกเลิกการเลือก
            if (selectedAttacker == card)
                ClearSelection();
        }

        // ลงการ์ดที่ลากมาปล่อยบนสนาม: ถ้าต้องเลือกเป้าหมาย เข้าโหมดเลือกเป้าเหมือนการคลิก
        private void PlayDroppedCard(CardInstance card)
        {
            if (!CanAct() || card == null)
            {
                ClearSelection();
                return;
            }

            ActionResult check = BattleRules.ValidatePlayable(battle, Human, card);

            if (!check.Success)
            {
                SetHint(check.Message);
                return;
            }

            List<TargetCandidate> targets =
                BattleRules.GetPlayTargets(battle, Human, card);

            if (targets.Count == 0)
            {
                ClearSelection();

                Report(battle.Submit(new PlayCardCommand(Human, card)));
                return;
            }

            selectedHand = card;
            selectedAttacker = null;
            activeTargets = targets;

            SetHint("Choose a target for " + card.Data.CardName);
        }

        // ยูนิต / Leader ที่อยู่ใต้ตำแหน่งที่ปล่อย (null = ไม่มี)
        private object FindDropTarget(Vector2 screenPos)
        {
            if (EventSystem.current == null)
                return null;

            PointerEventData data = new PointerEventData(EventSystem.current);

            data.position = screenPos;

            List<RaycastResult> results = new List<RaycastResult>();

            EventSystem.current.RaycastAll(data, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject == null)
                    continue;

                CardView view = result.gameObject.GetComponentInParent<CardView>();

                if (view != null && view.Instance != null)
                    return view.Instance;

                Transform hit = result.gameObject.transform;

                if (enemyLeader != null && hit.IsChildOf(enemyLeader.transform))
                    return Ai;
            }

            return null;
        }

        // ---------- Mulligan (เปลี่ยนการ์ดตอนเริ่มเกม) ----------

        private void BeginMulligan()
        {
            mulliganShown = true;

            mulliganPicks.Clear();

            Image overlay = UIFactory.CreatePanel(
                root, "Mulligan", new Color(0f, 0f, 0f, 0.92f));

            UIFactory.Stretch(overlay.rectTransform);

            mulliganRoot = overlay.rectTransform;

            mulliganRoot.SetAsLastSibling();

            string order = battle.FirstPlayerID == Human.PlayerID
                ? "You go first"
                : Ai.Name + " goes first  -  you get The Coin (+1 Energy)";

            ScreenKit.AddLabel(
                mulliganRoot,
                "<b>MULLIGAN</b>\n<size=28>" + order +
                "\nClick the cards you want to replace (once only)</size>",
                48, TextAnchor.UpperCenter, 0.08f, 0.68f, 0.92f, 0.97f);

            mulliganRow = CreateRow(
                mulliganRoot, "MulliganHand", 0.04f, 0.28f, 0.96f, 0.68f, 18f);

            mulliganButton = ScreenKit.AddButton(
                mulliganRoot, "Keep all", ConfirmMulligan,
                UIFactory.Success, 0.38f, 0.1f, 0.62f, 0.2f, 32);

            RefreshMulligan();
        }

        private void RefreshMulligan()
        {
            mulliganDirty = false;

            ScreenKit.ClearNow(mulliganRow);

            Vector2 size = new Vector2(HandCardSize.x * 1.55f, HandCardSize.y * 1.55f);

            foreach (CardInstance card in Human.Hand.Cards)
            {
                CardView view = CardView.Create(mulliganRow, size);

                view.ShowInstance(card);

                bool replaceable = BattleManager.CanMulligan(card);
                bool picked = mulliganPicks.Contains(card);

                view.SetHighlight(picked ? CardHighlight.Selected : CardHighlight.None);

                if (picked)
                    view.SetStateLabel("REPLACE");
                else if (!replaceable)
                    view.SetStateLabel("KEPT");

                view.SetDimmed(picked);

                view.Clicked += OnMulliganCardClicked;
            }

            UIFactory.SetButtonLabel(
                mulliganButton,
                mulliganPicks.Count == 0
                    ? "Keep all"
                    : "Replace " + mulliganPicks.Count + " card(s)");
        }

        private void OnMulliganCardClicked(CardView view)
        {
            if (view == null || view.Instance == null ||
                !BattleManager.CanMulligan(view.Instance))
                return;

            if (!mulliganPicks.Add(view.Instance))
                mulliganPicks.Remove(view.Instance);

            mulliganDirty = true;
        }

        private void ConfirmMulligan()
        {
            if (!battle.MulliganPending)
                return;

            battle.FinishMulligan(new List<CardInstance>(mulliganPicks));

            if (mulliganRoot != null)
                Destroy(mulliganRoot.gameObject);

            mulliganRoot = null;

            dirty = true;
        }

        private void OnHandCardClicked(CardView view)
        {
            PinCard(view);

            if (!CanAct() || view.Instance == null)
                return;

            CardInstance card = view.Instance;

            if (card == selectedHand)
            {
                ClearSelectionAndRefresh();
                return;
            }

            ActionResult check =
                BattleRules.ValidatePlayable(battle, Human, card);

            if (!check.Success)
            {
                SetHint(check.Message);
                return;
            }

            List<TargetCandidate> targets =
                BattleRules.GetPlayTargets(battle, Human, card);

            if (targets.Count == 0)
            {
                ClearSelection();

                Report(battle.Submit(new PlayCardCommand(Human, card)));
                return;
            }

            selectedHand = card;
            selectedAttacker = null;
            activeTargets = targets;

            SetHint("Choose a target for " + card.Data.CardName);
        }

        private void OnFieldCardClicked(CardView view)
        {
            PinCard(view);

            // ความสามารถที่รอให้เลือกยูนิตศัตรู (Grasp Heart)
            if (battle != null && battle.HasPendingTarget &&
                battle.PendingTarget.Player == Human &&
                view.Instance != null)
            {
                if (battle.ResolvePendingTarget(view.Instance))
                    SetHint("");
                else
                    SetHint("Choose one of the highlighted enemy units");

                return;
            }

            if (!CanAct() || view.Instance == null)
                return;

            CardInstance card = view.Instance;

            // กำลังเลือกเป้าหมายให้การ์ดในมือ
            if (selectedHand != null)
            {
                TryPlayOnTarget(card);
                return;
            }

            if (card.OwnerID == Human.PlayerID)
            {
                SelectAttacker(card);
                return;
            }

            if (selectedAttacker != null)
                TryAttack(card);
        }

        private void OnLeaderClicked(BattlePlayer player)
        {
            if (!CanAct())
                return;

            if (selectedHand != null)
            {
                TryPlayOnTarget(player);
                return;
            }

            if (selectedAttacker != null && player == Ai)
                TryAttack(player);
        }

        private void SelectAttacker(CardInstance card)
        {
            if (card == selectedAttacker)
            {
                ClearSelectionAndRefresh();
                return;
            }

            ActionResult check =
                BattleRules.ValidateCanAttack(battle, Human, card);

            if (!check.Success)
            {
                SetHint(check.Message);
                return;
            }

            List<TargetCandidate> targets =
                BattleRules.GetAttackTargets(battle, Human, card);

            if (targets.Count == 0)
            {
                SetHint("No valid targets");
                return;
            }

            selectedAttacker = card;
            selectedHand = null;
            activeTargets = targets;

            SetHint("Choose a target for " + card.Data.CardName);
        }

        private void TryPlayOnTarget(object target)
        {
            TargetCandidate candidate = FindTarget(target);

            if (candidate == null)
            {
                SetHint("That is not a valid target");
                return;
            }

            CardInstance card = selectedHand;

            ClearSelection();

            Report(battle.Submit(new PlayCardCommand(Human, card, candidate)));
        }

        private void TryAttack(object target)
        {
            TargetCandidate candidate = FindTarget(target);

            if (candidate == null)
            {
                SetHint("That is not a valid target");
                return;
            }

            CardInstance attacker = selectedAttacker;

            ClearSelection();

            Report(battle.Submit(new AttackCommand(
                Human,
                attacker,
                candidate.TargetObject as CardInstance,
                candidate.TargetObject as BattlePlayer)));
        }

        // ยูนิตของเราที่แปลงร่าง Devil Trigger ได้ตอนนี้ (null = ไม่มี)
        private CardInstance FindDevilTriggerCard()
        {
            if (battle == null)
                return null;

            foreach (CardInstance card in Human.Field)
            {
                if (DevilTriggerCommand.Validate(battle, Human, card).Success)
                    return card;
            }

            return null;
        }

        // ยูนิต Devil Trigger ที่ยังไม่เคยแปลงร่าง (ยังใช้ไม่ได้ตอนนี้ก็ได้)
        private CardInstance FindDevilTriggerWaiting()
        {
            if (battle == null)
                return null;

            foreach (CardInstance card in Human.Field)
            {
                if (card.HasPassive(CardPassive.DevilTrigger) && !card.DevilTriggerUsed)
                    return card;
            }

            return null;
        }

        private void RefreshDevilTrigger(bool myTurn)
        {
            CardInstance ready = myTurn ? FindDevilTriggerCard() : null;
            CardInstance waiting = ready == null ? FindDevilTriggerWaiting() : null;

            // มี Vergil บนสนามก็แสดงปุ่มเสมอ ใช้ไม่ได้ตอนนี้ = ปุ่มจาง บอกเหตุผลใต้ชื่อ
            devilRoot.gameObject.SetActive(ready != null || waiting != null);

            if (devilButton != null)
            {
                devilButton.interactable = ready != null;

                string label = "DEVIL\nTRIGGER";

                if (ready == null && waiting != null)
                {
                    label += waiting.SummonedOnTurn == battle.TurnManager.TotalTurns
                        ? "\n(next turn)"
                        : "\n(your turn)";
                }

                UIFactory.SetButtonLabel(devilButton, label);
            }

            // เพิ่งแปลงร่าง: ไฟสีน้ำเงินลุกที่การ์ดทันที
            if (devilFxInstance != 0)
            {
                CardView view;

                if (viewsByInstance.TryGetValue(devilFxInstance, out view) &&
                    view != null)
                {
                    Vector3 position = view.transform.position;

                    UiFx.Ring(fxLayer, position,
                        new Color(0.3f, 0.65f, 1f, 0.9f), 80f, 520f, 0.7f);

                    UiFx.Burst(fxLayer, position,
                        new Color(0.25f, 0.55f, 1f, 1f), 46, 340f, 40f, 1.1f, -140f);

                    CardFx.Flash(view, new Color(0.4f, 0.7f, 1f, 1f));
                }

                devilFxInstance = 0;
            }
        }

        private void OnDevilTriggerClicked()
        {
            if (!CanAct())
                return;

            CardInstance card = FindDevilTriggerCard();

            if (card == null)
                return;

            ClearSelection();

            ActionResult result = battle.Submit(new DevilTriggerCommand(Human, card));

            if (result.Success)
                devilFxInstance = card.InstanceID;

            Report(result);
        }

        private void OnEndTurnClicked()
        {
            if (!CanAct())
                return;

            ClearSelection();

            // ระหว่างหน้าต่างตอบโต้ ปุ่มนี้คือ PASS
            if (battle.ChainResponder == Human)
            {
                Report(battle.Submit(new PassChainCommand(Human)));
                return;
            }

            Report(battle.Submit(new EndTurnCommand(Human)));
        }

        private void OnDiscoverClicked(CardView view)
        {
            if (view.Data == null)
                return;

            battle.ResolveDiscover(view.Data);

            dirty = true;
        }

        private void Concede()
        {
            if (battle == null || battle.IsOver)
                return;

            ClearSelection();

            battle.EndBattle(Ai);

            dirty = true;
        }

        // ---------- ตัวช่วยการเลือก ----------

        private TargetCandidate FindTarget(object target)
        {
            foreach (TargetCandidate candidate in activeTargets)
            {
                if (ReferenceEquals(candidate.TargetObject, target))
                    return candidate;
            }

            return null;
        }

        private bool IsActiveTarget(object target)
        {
            CardInstance pendingCard = target as CardInstance;

            if (pendingCard != null && battle.HasPendingTarget &&
                battle.PendingTarget.Player == Human &&
                battle.PendingTarget.Candidates.Contains(pendingCard))
                return true;

            if (selectedHand == null && selectedAttacker == null)
                return false;

            return FindTarget(target) != null;
        }

        private void Report(ActionResult result)
        {
            hint = result.Success ? "" : result.Message;
            dirty = true;
        }

        private void SetHint(string text)
        {
            hint = text;
            dirty = true;
        }

        private void ClearSelection()
        {
            selectedHand = null;
            selectedAttacker = null;
            activeTargets = new List<TargetCandidate>();
            hint = "";
            dirty = true;
        }

        private void ClearSelectionAndRefresh()
        {
            ClearSelection();
        }
    }
}
