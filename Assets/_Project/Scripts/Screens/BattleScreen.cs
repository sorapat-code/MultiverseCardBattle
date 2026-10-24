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
        private static readonly Vector2 PreviewCardSize = new Vector2(220f, 308f);

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
        private SkillTextView skillView;
        private Image termsPanel;
        private Text termsHeader;
        private Image previewGlow;

        // สนามหญ้า: แสงเรืองขอบ / ลิ้นชักล็อก / หน้าต่างข้อมูล Leader
        private CanvasGroup logGroup;
        private LeaderInfoPanel leaderInfo;
        private bool bossIntroShown;
        private bool lastAiSkillUsed;
        private bool lastAiFinisher;
        private int lastTurnKey = -1;

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

            // อัญมณี Energy ข้างสนาม
            public Text GemText;
            public Text GemMax;

            // มิเตอร์ Power ของ Leader + ปุ่มอ่านความสามารถ
            public RectTransform PowerRoot;
            public Image PowerFill;
            public Text PowerText;
            public Button InfoButton;
        }

        // พรีวิวการ์ดใบใหญ่ตอนเอาเมาส์ชี้
        private CardView previewView;
        private RectTransform previewZone;

        // ใช้ตรวจว่าค่าอะไรเปลี่ยน เพื่อแสดงตัวเลขลอยและเล่นเสียง
        private readonly Dictionary<int, CardView> viewsByInstance =
            new Dictionary<int, CardView>();

        private Dictionary<int, int> lastHealth = new Dictionary<int, int>();
        private Dictionary<int, int> lastAttacks = new Dictionary<int, int>();

        // Geass ล่าสุดที่เล่นเอฟเฟกต์ไปแล้ว (เทียบกับ battle.GeassSeq)
        private int lastGeassSeq;

        // CardID ล่าสุดของยูนิตแต่ละใบ (ใช้จับการแปลงร่างเพื่อเล่นเอฟเฟกต์)
        private readonly Dictionary<int, string> lastForms = new Dictionary<int, string>();
        private Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Vector3> lastHandPositions = new Dictionary<int, Vector3>();
        private int lastAiHandCount = -1;
        private int lastHumanHandCount = -1;
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
        private BattleStage3D stage;
        private readonly List<CardHolder3D> fieldHolders = new List<CardHolder3D>();
        private readonly List<CardHolder3D> handHolders = new List<CardHolder3D>();

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

        // ผู้สอนในแมตช์ Tutorial (null ถ้าเป็นแมตช์ปกติ)
        private TutorialCoach coach;

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

            battle.Events.Raised += OnBattleEvent;

            BuildUI();
        }

        private void OnDestroy()
        {
            if (battle != null && battle.Events != null)
                battle.Events.Raised -= OnBattleEvent;

            if (stage != null)
                Destroy(stage.gameObject);
        }

        // การ์ดลับใช้สกิล: จดไว้ แล้วเล่นเอฟเฟกต์ตอนวาดหน้าจอครั้งถัดไป (ตอนนั้นการ์ดมี View แล้ว)
        private SecretCinematic cinematic;
        private CoinToss coinToss;

        // Leader: ปุ่มสกิล + มิเตอร์ Power
        private RectTransform leaderRoot;
        private Button leaderButton;
        private Text leaderPowerText;
        private Image leaderBarFill;
        private bool leaderFinisherShown;
        private BattleSettingsPanel settings;
        private bool coinTossStarted;
        private SecretCinematic unlockCinematic;
        private int unlockCinematicIndex = -1;

        private readonly List<GameEvent> pendingSecretEvents = new List<GameEvent>();

        // การ์ดลับลงสนาม: ฉากไซเนมาติกก่อน แล้วการ์ดบินลงช่องของมันพร้อมเอฟเฟกต์ลงสนาม
        private void PlayArrivalCinematic(CardData cardData, CardView view, CanvasGroup group)
        {
            if (group == null)
                group = view.GetComponent<CanvasGroup>();

            if (group != null)
                group.alpha = 0f;

            Vector3 destination = UiFx.ScreenOf(view.transform);

            cinematic = SecretCinematic.Play(
                root, SecretCinematic.Mode.Arrival, cardData,
                "", cardData.CardName, SecretFx.Epithet(cardData),
                root, destination,
                () =>
                {
                    if (group != null)
                        group.alpha = 1f;

                    if (view != null)
                        CardFx.Pop(view);

                    SecretFx.Arrive(fxLayer, root, destination, cardData, false);
                },
                null);
        }

        private void OnBattleEvent(GameEvent e)
        {
            if (e == null || e.Card == null || !SecretFx.IsSecret(e.Card.Data))
                return;

            if (e.Trigger != TriggerType.Attack &&
                e.Trigger != TriggerType.LeaderAttack &&
                e.Trigger != TriggerType.Death)
                return;

            pendingSecretEvents.Add(e);

            if (pendingSecretEvents.Count > 6)
                pendingSecretEvents.RemoveAt(0);
        }

        private void PlaySecretEvents()
        {
            if (pendingSecretEvents.Count == 0)
                return;

            foreach (GameEvent e in pendingSecretEvents)
            {
                Vector3 from = Vector3.zero;
                Vector3 to = Vector3.zero;

                CardView view;

                if (viewsByInstance.TryGetValue(e.Card.InstanceID, out view) && view != null)
                    from = UiFx.ScreenOf(view.transform);
                else if (!lastPositions.TryGetValue(e.Card.InstanceID, out from))
                    continue;

                to = from;

                if (e.Trigger != TriggerType.Death)
                {
                    CardView targetView;

                    if (e.Target != null &&
                        viewsByInstance.TryGetValue(e.Target.InstanceID, out targetView) &&
                        targetView != null)
                    {
                        to = UiFx.ScreenOf(targetView.transform);
                    }
                    else if (e.TargetPlayer != null)
                    {
                        Button leader = e.TargetPlayer == Human ? playerLeader : enemyLeader;

                        if (leader != null)
                            to = UiFx.ScreenOf(leader.transform);
                    }
                }

                SecretFx.Skill(fxLayer, root, from, to, e.Card.Data, e.Trigger);
            }

            pendingSecretEvents.Clear();
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

            // โยนเหรียญก่อนเริ่มเกม: ระหว่างฉากหยุด AI / อินพุต / Mulligan ทั้งหมด
            if (battle.CoinFlipped && !coinTossStarted)
            {
                coinTossStarted = true;
                bannerShown = true; // ฉากโยนเหรียญบอกผลแล้ว ไม่ต้องใช้ป้ายเดิมซ้ำ

                coinToss = CoinToss.Play(
                    root, "YOU", Ai.Name,
                    battle.FirstPlayerID == Human.PlayerID,
                    () => coinToss = null);
            }

            if (coinToss != null)
                return;

            // ด่านบอส: แนะนำบอสและความสามารถของมันก่อนเริ่ม (ครั้งเดียว)
            if (!bossIntroShown)
            {
                bossIntroShown = true;

                if (Ai.Leader != null && !Ai.Leader.PlayerSelectable)
                    OpenLeaderInfo(Ai, true);
            }

            // หน้าต่างข้อมูล Leader เปิดอยู่: หยุดเกมทั้งหมด
            if (leaderInfo != null && leaderInfo.IsOpen)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    leaderInfo.Close();

                return;
            }

            // หน้าตั้งค่าเปิดอยู่: หยุดเกมทั้งหมด (AI / อินพุต)
            if (settings != null && settings.IsOpen)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    settings.Close();

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

            UpdateTermsVisibility();

            UpdateTurnBanner();

            RunAI();

            if (coach != null)
                coach.Update();

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

        // กรอบคำอธิบายศัพท์โผล่เฉพาะตอนชี้การ์ด
        private void UpdateTermsVisibility()
        {
            if (skillView == null || termsPanel == null)
                return;

            bool show = skillView.HasContent;

            if (termsPanel.gameObject.activeSelf != show)
            {
                termsPanel.gameObject.SetActive(show);
                skillView.gameObject.SetActive(show);

                if (termsHeader != null)
                    termsHeader.gameObject.SetActive(show);
            }
        }

        // ป้ายสลับเทิร์น (YOUR TURN / OPPONENT'S TURN) ตอนเปลี่ยนฝั่ง
        private void UpdateTurnBanner()
        {
            if (battle.IsOver || battle.MulliganPending || battle.CurrentPlayer == null)
                return;

            int key = battle.TurnManager.TurnNumber * 10 + battle.CurrentPlayer.PlayerID;

            if (key == lastTurnKey)
                return;

            lastTurnKey = key;

            TurnBanner.Play(fxLayer, battle.CurrentPlayer == Human);

            Sfx.Play(battle.CurrentPlayer == Human ? SoundId.TurnStart : SoundId.EndTurn);
        }

        private void OpenLeaderInfo(BattlePlayer player, bool intro)
        {
            if (leaderInfo == null || player == null || player.Leader == null)
                return;

            leaderInfo.Open(
                player.Leader, player.LeaderPower, player.LeaderFinisherGiven,
                player == Ai, intro);
        }

        // บอสใช้สกิล / ไม้ตายพร้อมแล้ว: ป้ายประกาศให้ผู้เล่นอ่านออก (ผู้เล่นมองไม่เห็นมือศัตรู)
        private void AnnounceBossEvents()
        {
            LeaderData boss = Ai.Leader;

            if (boss == null)
            {
                lastAiSkillUsed = false;
                lastAiFinisher = false;

                return;
            }

            string hex = ColorUtility.ToHtmlStringRGB(boss.Theme);

            if (Ai.LeaderSkillUsed && !lastAiSkillUsed)
            {
                bannerText.text =
                    "<color=#" + hex + ">" + boss.Name.ToUpperInvariant() + "</color>\n" +
                    "<size=44>" + boss.SkillName + "</size>\n" +
                    "<size=22>" + boss.SkillText + "</size>";

                ShowBanner(3.4f, UiFx.ScreenOf(enemyLeader.transform), boss.Theme);
            }

            if (Ai.LeaderFinisherGiven && !lastAiFinisher)
            {
                CardData finisher = StarterCardLibrary.Get(boss.FinisherId);

                bannerText.text =
                    "<color=#FF6070>DANGER</color>\n" +
                    "<size=40>" + (finisher != null ? finisher.CardName : "Finisher") + "</size>\n" +
                    "<size=22>" + boss.Name + "'s " + boss.PowerName +
                    " is full. A finisher is now in the enemy hand!</size>";

                ShowBanner(4f, UiFx.ScreenOf(enemyLeader.transform), new Color(1f, 0.3f, 0.35f, 1f));
            }

            lastAiSkillUsed = Ai.LeaderSkillUsed;
            lastAiFinisher = Ai.LeaderFinisherGiven;
        }

        private void ShowBanner(float seconds, Vector3 fxOrigin, Color color)
        {
            bannerRoot.gameObject.SetActive(true);
            bannerRoot.SetAsLastSibling();

            bannerTimer = seconds;

            UiFx.Ring(fxLayer, fxOrigin, color, 60f, 520f, 0.7f);
            UiFx.Burst(fxLayer, fxOrigin, color, 30, 420f, 26f, 0.9f, -40f);

            Sfx.Play(SoundId.Hit);
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
                else if (selectedHand != null || selectedAttacker != null || dragCard != null)
                    ClearSelectionAndRefresh();
                else if (settings != null)
                    settings.Open();
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
            // ฉากการ์ดลับกำลังเล่น: รอให้จบก่อน
            if (cinematic != null)
                return;

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

            // พื้นหลังนอกสนาม: ป่ามืด (ถ้ามีรูป bg_battle ใน Resources/UI จะใช้รูปนั้นแทน)
            Transform backgroundTransform = root.Find("Background");

            if (backgroundTransform != null && UIArt.Get("bg_battle") == null)
            {
                Image background = backgroundTransform.GetComponent<Image>();

                if (background != null)
                    background.sprite = BoardArt.Frame;
            }

            BuildBoard();

            // ----- พรีวิวการ์ด + คำอธิบายศัพท์ (ลอยทางซ้าย โผล่เฉพาะตอนชี้การ์ด) -----
            previewZone = UIFactory.CreateZone(
                root, "PreviewZone", 0.0f, 0.49f, 0.16f, 0.80f);

            // แสงทองนุ่มๆ ด้านหลังการ์ดพรีวิว (เปิดพร้อมการ์ด)
            previewGlow = UIFactory.CreatePanel(previewZone, "PreviewGlow", new Color(1f, 0.78f, 0.35f, 0.5f));

            previewGlow.sprite = UIShapes.Glow;
            previewGlow.type = Image.Type.Simple;
            previewGlow.raycastTarget = false;

            previewGlow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            previewGlow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            previewGlow.rectTransform.anchoredPosition = Vector2.zero;
            previewGlow.rectTransform.sizeDelta = PreviewCardSize + new Vector2(90f, 90f);
            previewGlow.gameObject.SetActive(false);

            previewView = CardView.Create(previewZone, PreviewCardSize);

            previewView.SetInteractable(false);
            previewView.gameObject.SetActive(false);
            if (previewGlow != null)
                previewGlow.gameObject.SetActive(false);

            termsPanel = ScreenKit.AddPanel(
                root, "TermsPanel", new Color(0.035f, 0.05f, 0.06f, 0.93f),
                0.005f, 0.295f, 0.158f, 0.485f);

            termsPanel.raycastTarget = false;

            termsHeader = ScreenKit.AddLabel(
                root, "SKILL", 14, TextAnchor.MiddleLeft,
                0.014f, 0.452f, 0.15f, 0.48f, FontStyle.Bold);

            termsHeader.color = UIFactory.Gold;

            UIFactory.AddTextShadow(termsHeader);

            termsHeader.gameObject.SetActive(false);

            // สกิลของการ์ด: ศัพท์ถูกขีดเส้นใต้ ชี้แล้วมีคำอธิบายเด้ง (คลิกการ์ดเพื่อปักหมุดแล้วค่อยเอาเมาส์ไปชี้)
            skillView = SkillTextView.Create(
                root, root, 0.012f, 0.30f, 0.152f, 0.45f, 17, 9);

            // ----- มุมซ้ายบน: เทิร์น + คำใบ้ -----
            turnText = ScreenKit.AddLabel(
                root, "", 30, TextAnchor.MiddleLeft,
                0.012f, 0.925f, 0.39f, 0.99f, FontStyle.Bold);

            UIFactory.AddTextShadow(turnText);

            hintText = ScreenKit.AddLabel(
                root, "", 22, TextAnchor.UpperLeft,
                0.012f, 0.84f, 0.39f, 0.925f);

            hintText.resizeTextForBestFit = true;
            hintText.resizeTextMinSize = 12;
            hintText.resizeTextMaxSize = 22;

            UIFactory.AddTextShadow(hintText);

            // ปุ่มยอมแพ้ย้ายเข้าไปอยู่ในหน้าตั้งค่า (Settings -> Surrender)
            settings = BattleSettingsPanel.Create(root, Concede);

            // แผงพื้นเข้มรวมปุ่มคอลัมน์ขวาเป็นกลุ่มเดียว (สร้างก่อนปุ่มเพื่อให้อยู่ด้านหลัง)
            Image rightRail = ScreenKit.AddPanel(
                root, "RightRail", new Color(0.03f, 0.03f, 0.04f, 0.62f),
                0.903f, 0.15f, 1.002f, 0.997f);

            rightRail.raycastTarget = false;

            ScreenKit.AddButton(root, "Settings", () => settings.Open(),
                UIFactory.Muted, 0.91f, 0.945f, 0.995f, 0.99f, 20);

            ScreenKit.AddButton(root, "Log", ToggleLog,
                UIFactory.Muted, 0.91f, 0.89f, 0.995f, 0.935f, 20);

            // ----- Leader ทั้งสองฝั่ง -----
            enemyWidget = CreateLeaderWidget(
                () => OnLeaderClicked(Ai),
                () => OpenLeaderInfo(Ai, false),
                0.385f, 0.805f, 0.615f, 0.99f);

            playerWidget = CreateLeaderWidget(
                () => OnLeaderClicked(Human),
                () => OpenLeaderInfo(Human, false),
                0.008f, 0.02f, 0.165f, 0.185f);

            enemyLeader = enemyWidget.Button;
            playerLeader = playerWidget.Button;

            // ----- อัญมณี Energy ซ้ายสนาม (แดง = ศัตรู ฟ้า = เรา) -----
            enemyWidget.GemText = BuildGem(
                "EnemyGem", new Color(0.95f, 0.25f, 0.3f, 1f),
                0.624f, 0.895f, 0.662f, 0.985f, out enemyWidget.GemMax,
                0.664f, 0.915f, 0.725f, 0.965f);

            playerWidget.GemText = BuildGem(
                "PlayerGem", new Color(0.3f, 0.65f, 1f, 1f),
                0.012f, 0.195f, 0.052f, 0.285f, out playerWidget.GemMax,
                0.054f, 0.215f, 0.12f, 0.265f);

            // ----- คอลัมน์ขวา: กองการ์ด / จบเทิร์น -----
            // จำนวนการ์ดในมือคู่ต่อสู้ (แสดงเฉพาะตัวเลข ดูการ์ดในมือไม่ได้แล้ว คลิกไม่ได้)
            enemyHandButton = ScreenKit.AddButton(
                root, "Opp. Hand", () => { },
                UIFactory.Muted, 0.91f, 0.81f, 0.995f, 0.865f, 18);

            enemyHandButton.targetGraphic.raycastTarget = false;

            enemyGraveButton = ScreenKit.AddButton(
                root, "Graveyard", () => TogglePile(PileKind.EnemyGraveyard),
                UIFactory.Muted, 0.91f, 0.745f, 0.995f, 0.80f, 18);

            enemyDeckLabel = AddPileLabel(0.91f, 0.70f, 0.995f, 0.74f);

            // ปุ่มแปลงร่าง Devil Trigger (Vergil) มีไฟสีน้ำเงินลุกรอบ แสดงเมื่อใช้ได้เท่านั้น
            devilRoot = UIFactory.CreateRect("DevilTrigger", root);

            UIFactory.SetAnchors(devilRoot, 0.91f, 0.285f, 0.995f, 0.385f);

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
                new Color(0.1f, 0.25f, 0.65f, 1f), 0f, 0f, 1f, 1f, 20);

            devilRoot.gameObject.SetActive(false);

            // ปุ่มจบเทิร์นทรงกลมนูน กลางขอบขวาของสนาม
            endTurnButton = ScreenKit.AddButton(
                root, "END TURN", OnEndTurnClicked,
                UIFactory.Success, 0.912f, 0.44f, 0.992f, 0.58f, 26);

            Image dial = endTurnButton.GetComponent<Image>();

            dial.sprite = BoardArt.Dome;
            dial.type = Image.Type.Simple;
            dial.preserveAspect = true;

            Image dialRing = UIFactory.CreatePanel(
                endTurnButton.transform, "DialRing", UIFactory.Gold);

            dialRing.sprite = UIShapes.RingCircle;
            dialRing.type = Image.Type.Simple;
            dialRing.raycastTarget = false;

            UIFactory.Stretch(dialRing.rectTransform);

            cancelButton = ScreenKit.AddButton(
                root, "Cancel", ClearSelectionAndRefresh,
                UIFactory.Muted, 0.91f, 0.39f, 0.995f, 0.435f, 20);

            playerGraveButton = ScreenKit.AddButton(
                root, "Graveyard", () => TogglePile(PileKind.PlayerGraveyard),
                UIFactory.Muted, 0.91f, 0.205f, 0.995f, 0.26f, 18);

            playerDeckLabel = AddPileLabel(0.91f, 0.16f, 0.995f, 0.20f);

            BuildLeaderUI();

            BuildLogPanel();

            // ป้ายประกาศ (หายเองหลังไม่กี่วินาที) ใช้กับสกิลบอส / ไม้ตายพร้อม / การ์ดลับ
            bannerRoot = UIFactory.CreateRect("CoinBanner", root);

            UIFactory.SetAnchors(bannerRoot, 0.27f, 0.39f, 0.73f, 0.61f);

            Image bannerBack = UIFactory.CreateFramedPanel(
                bannerRoot, "Back", new Color(0.05f, 0.03f, 0.02f, 0.92f), 16f);

            UIFactory.Stretch(bannerBack.rectTransform);

            bannerBack.raycastTarget = false;

            bannerText = UIFactory.CreateText(
                bannerRoot, "", 36, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(bannerText.rectTransform);

            bannerText.supportRichText = true;
            bannerText.resizeTextForBestFit = true;
            bannerText.resizeTextMinSize = 14;
            bannerText.resizeTextMaxSize = 36;

            UIFactory.AddTextShadow(bannerText);

            bannerRoot.gameObject.SetActive(false);

            // ชั้นเอฟเฟกต์ (อนุภาค/รอยฟัน) อยู่เหนือการ์ด ใต้หน้าต่างต่างๆ
            fxLayer = UIFactory.CreateRect("FxLayer", root);

            UIFactory.Stretch(fxLayer);

            BlueFlame.Attach(devilRoot, fxLayer, devilGlow);

            // หน้าต่างอ่านความสามารถ Leader
            leaderInfo = LeaderInfoPanel.Create(root);

            // หน้าต่างดูกองทิ้ง / มือศัตรู
            pileOverlay = UIFactory.CreateRect("PileOverlay", root);

            UIFactory.Stretch(pileOverlay);

            pileOverlay.gameObject.SetActive(false);

            // แมตช์สอนเล่น: ผู้สอนขั้นตอนมุมซ้ายบน
            if (BattleSetup.Tutorial)
            {
                coach = TutorialCoach.Create(
                    root, battle, Human, Ai,
                    handZone, playerField,
                    (RectTransform)endTurnButton.transform,
                    (RectTransform)playerLeader.transform,
                    leaderRoot, previewZone);
            }

            // Overlay (Discover / ผลการต่อสู้) อยู่บนสุด
            overlay = UIFactory.CreateRect("Overlay", root);

            UIFactory.Stretch(overlay);

            overlay.gameObject.SetActive(false);
        }

        // โต๊ะ 3D: ตัวโต๊ะ/กรอบ/พุ่มไม้/แสง อยู่ใน BattleStage3D ส่วนนี้สร้างโซนล่องหนไว้ให้ตำแหน่ง FX อ้างอิง
        private void BuildBoard()
        {
            stage = BattleStage3D.Create();

            // พื้นหลัง 2D ปิดไว้ (กล้อง 3D วาดเอง)
            Transform backgroundTransform = root.Find("Background");

            if (backgroundTransform != null)
                backgroundTransform.gameObject.SetActive(false);

            enemyField = UIFactory.CreateZone(root, "EnemyField", 0.10f, 0.50f, 0.90f, 0.775f);
            playerField = UIFactory.CreateZone(root, "PlayerField", 0.10f, 0.225f, 0.90f, 0.495f);
            handZone = UIFactory.CreateZone(root, "Hand", 0.23f, 0.0f, 0.77f, 0.215f);

            LayoutZones();
        }

        // ตั้งโซนล่องหน (ใช้เป็นจุดอ้างอิง FX) ให้ตรงกับตำแหน่งบนจอของโต๊ะ 3D
        private void LayoutZones()
        {
            if (stage == null)
                return;

            stage.Fit();

            float w = Screen.width;
            float h = Screen.height;

            if (w < 1f || h < 1f)
                return;

            PlaceZone(enemyField, stage.ScreenBounds(
                -BattleStage3D.BoardHalfWidth, BattleStage3D.BoardHalfWidth, 0.2f, BattleStage3D.BoardHalfDepth), w, h);
            PlaceZone(playerField, stage.ScreenBounds(
                -BattleStage3D.BoardHalfWidth, BattleStage3D.BoardHalfWidth, -BattleStage3D.BoardHalfDepth, -0.2f), w, h);
            PlaceZone(handZone, stage.ScreenBounds(-5f, 5f, -6.4f, -4.6f), w, h);
        }

        private static void PlaceZone(RectTransform zone, Rect pixels, float w, float h)
        {
            if (zone == null)
                return;

            UIFactory.SetAnchors(
                zone,
                Mathf.Clamp01(pixels.xMin / w), Mathf.Clamp01(pixels.yMin / h),
                Mathf.Clamp01(pixels.xMax / w), Mathf.Clamp01(pixels.yMax / h));
        }

        // อัญมณี Energy: ตัวเลขปัจจุบันในอัญมณี ค่าสูงสุดอยู่ใต้ภาพ
        private Text BuildGem(
            string name, Color color,
            float xMin, float yMin, float xMax, float yMax,
            out Text maxText,
            float mxMin, float myMin, float mxMax, float myMax)
        {
            Image gem = UIFactory.CreatePanel(root, name, color);

            gem.sprite = BoardArt.Gem;
            gem.type = Image.Type.Simple;
            gem.preserveAspect = true;
            gem.raycastTarget = false;

            UIFactory.SetAnchors(gem.rectTransform, xMin, yMin, xMax, yMax);

            Text number = UIFactory.CreateText(
                gem.transform, "0", 44, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(number.rectTransform);

            number.resizeTextForBestFit = true;
            number.resizeTextMinSize = 14;
            number.resizeTextMaxSize = 48;

            UIFactory.AddTextShadow(number);

            maxText = ScreenKit.AddLabel(
                root, "", 18, TextAnchor.MiddleCenter, mxMin, myMin, mxMax, myMax, FontStyle.Bold);

            maxText.color = new Color(0.8f, 0.85f, 0.95f, 1f);

            UIFactory.AddTextShadow(maxText);

            return number;
        }

        // ล็อกการต่อสู้: ลิ้นชักลอยเหนือขวาของสนาม เปิด/ปิดด้วยปุ่ม Log
        private void BuildLogPanel()
        {
            Image back = ScreenKit.AddPanel(
                root, "LogPanel", new Color(0.03f, 0.025f, 0.025f, 0.93f),
                0.69f, 0.22f, 0.905f, 0.78f);

            logGroup = back.gameObject.AddComponent<CanvasGroup>();

            logGroup.alpha = 0f;
            logGroup.blocksRaycasts = false;
            logGroup.interactable = false;

            Text header = ScreenKit.AddLabel(
                back.transform, "BATTLE LOG", 18, TextAnchor.MiddleLeft,
                0.05f, 0.92f, 0.95f, 0.99f, FontStyle.Bold);

            header.color = UIFactory.Gold;

            RectTransform logContent;

            logScroll = ScreenKit.AddScroll(
                back.transform, "LogScroll", 0.03f, 0.02f, 0.97f, 0.91f,
                out logContent);

            VerticalLayoutGroup logLayout =
                logContent.gameObject.AddComponent<VerticalLayoutGroup>();

            logLayout.spacing = 4f;
            logLayout.padding = new RectOffset(6, 6, 6, 6);
            logLayout.childControlWidth = true;
            logLayout.childControlHeight = true;
            logLayout.childForceExpandWidth = true;
            logLayout.childForceExpandHeight = false;
        }

        private void ToggleLog()
        {
            if (logGroup == null)
                return;

            bool open = logGroup.alpha < 0.5f;

            logGroup.alpha = open ? 1f : 0f;
            logGroup.blocksRaycasts = open;
            logGroup.interactable = open;

            if (open)
            {
                logGroup.transform.SetAsLastSibling();

                Canvas.ForceUpdateCanvases();

                logScroll.verticalNormalizedPosition = 0f;
            }
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
            UnityEngine.Events.UnityAction onInfo,
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

            UIFactory.SetAnchors(widget.Title.rectTransform, 0.34f, 0.60f, 0.80f, 0.95f);

            // รูป Leader: วงทองรอบนอก > วงมืดตัดมุม > รูป/ตัวอักษรย่อ
            Image goldRing = UIFactory.CreatePanel(panel, "PortraitRing", UIFactory.Gold);

            UIShapes.MakeCircle(goldRing);

            goldRing.raycastTarget = false;

            RectTransform ringRect = goldRing.rectTransform;

            ringRect.anchorMin = new Vector2(0f, 1f);
            ringRect.anchorMax = new Vector2(0f, 1f);
            ringRect.pivot = new Vector2(0f, 1f);
            ringRect.anchoredPosition = new Vector2(12f, -12f);
            ringRect.sizeDelta = new Vector2(104f, 104f);

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

            UIFactory.SetAnchors(hpBack.rectTransform, 0.34f, 0.33f, 0.97f, 0.57f);

            widget.HpFill = UIFactory.CreatePanel(
                hpBack.transform, "HpFill", new Color(0.25f, 0.75f, 0.35f, 1f));

            UIShapes.MakeRounded(widget.HpFill, 6f);

            widget.HpFill.raycastTarget = false;

            UIFactory.Stretch(widget.HpFill.rectTransform);

            AddGloss(widget.HpFill.transform, 6f);
            UIFactory.AddRing(hpBack.transform, new Color(0.9f, 0.72f, 0.33f, 0.75f), 6f);

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

            // Energy แสดงเป็นอัญมณีข้างสนามแล้ว: ซ่อนคริสตัลและตัวเลขเดิมในแผง
            crystalRow.gameObject.SetActive(false);
            widget.EnergyText.gameObject.SetActive(false);

            // มิเตอร์ Power ของ Leader (โชว์เมื่อมี Leader)
            Image powerBack = UIFactory.CreatePanel(
                panel, "PowerBack", new Color(0f, 0f, 0f, 0.55f));

            UIShapes.MakeRounded(powerBack, 6f);

            powerBack.raycastTarget = false;

            widget.PowerRoot = powerBack.rectTransform;

            UIFactory.SetAnchors(widget.PowerRoot, 0.34f, 0.07f, 0.97f, 0.27f);

            widget.PowerFill = UIFactory.CreatePanel(
                powerBack.transform, "PowerFill", new Color(1f, 0.65f, 0.15f, 1f));

            UIShapes.MakeRounded(widget.PowerFill, 6f);

            widget.PowerFill.raycastTarget = false;

            UIFactory.Stretch(widget.PowerFill.rectTransform);

            widget.PowerFill.rectTransform.anchorMax = new Vector2(0f, 1f);

            AddGloss(widget.PowerFill.transform, 6f);
            UIFactory.AddRing(powerBack.transform, new Color(0.9f, 0.72f, 0.33f, 0.75f), 6f);

            widget.PowerText = UIFactory.CreateText(
                powerBack.transform, "", 15, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(widget.PowerText.rectTransform);

            widget.PowerText.resizeTextForBestFit = true;
            widget.PowerText.resizeTextMinSize = 9;
            widget.PowerText.resizeTextMaxSize = 15;

            UIFactory.AddTextShadow(widget.PowerText);

            widget.PowerRoot.gameObject.SetActive(false);

            // ปุ่ม "i" อ่านความสามารถของ Leader
            widget.InfoButton = ScreenKit.AddButton(
                panel, "i", onInfo, UIFactory.Accent, 0.83f, 0.62f, 0.97f, 0.95f, 22);

            widget.InfoButton.gameObject.SetActive(false);

            return widget;
        }

        private const int MaxCrystals = 10;

        // แถบสว่างครึ่งบนของแถบ HP/Power ให้ดูมันเงา
        private static void AddGloss(Transform fill, float corner)
        {
            Image gloss = UIFactory.CreateRoundedPanel(
                fill, "Gloss", new Color(1f, 1f, 1f, 0.2f), corner);

            gloss.raycastTarget = false;

            UIFactory.SetAnchors(gloss.rectTransform, 0.02f, 0.5f, 0.98f, 0.95f);
        }

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

            if (stage != null && stage.NeedsRefit)
                LayoutZones();

            CardHolder3D.Dispose(fieldHolders);
            CardHolder3D.Dispose(handHolders);

            RefreshField(enemyField, Ai, myTurn);
            RefreshField(playerField, Human, myTurn);
            // Fast spell เล่นได้ทุกจังหวะ (ถ้าเชนว่าง) จึงเปิดมือไว้ตลอด
            bool handActive =
                responding ||
                (!battle.IsOver && !battle.HasPendingChoice && !battle.ChainBlocksActions);

            RefreshHand(handActive);

            DetectChanges();
            RefreshPreview();

            RefreshDevilTrigger(myTurn);
            RefreshLeader();
            AnnounceBossEvents();
            RefreshBoardGlow();

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

        // แสงเรืองขอบโต๊ะ: ฝั่งที่เป็นเทิร์นสว่างกว่า
        private void RefreshBoardGlow()
        {
            if (stage == null)
                return;

            stage.SetTurnGlow(
                battle.CurrentPlayer == Ai && !battle.IsOver,
                battle.CurrentPlayer == Human && !battle.IsOver);
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

            if (widget.GemText != null)
                widget.GemText.text = player.CurrentEnergy.ToString();

            if (widget.GemMax != null)
                widget.GemMax.text = "/ " + player.MaxEnergy;

            // Leader: มิเตอร์ Power + ปุ่มอ่านความสามารถ
            LeaderData leaderData = player.Leader;

            widget.InfoButton.gameObject.SetActive(leaderData != null);
            widget.PowerRoot.gameObject.SetActive(leaderData != null);

            if (leaderData != null)
            {
                float fraction = leaderData.PowerMax > 0
                    ? Mathf.Clamp01(player.LeaderPower / (float)leaderData.PowerMax)
                    : 0f;

                widget.PowerFill.rectTransform.anchorMax = new Vector2(fraction, 1f);
                widget.PowerFill.color = leaderData.Theme;

                widget.PowerText.text = player.LeaderFinisherGiven
                    ? "FINISHER IN HAND"
                    : leaderData.PowerName + " " + player.LeaderPower + " / " + leaderData.PowerMax;
            }

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
            bool enemySide = owner == Ai;
            int count = owner.Field.Count;
            int slot = 0;

            foreach (CardInstance card in owner.Field)
            {
                CardHolder3D holder = stage.CreateFieldHolder(enemySide, slot++, count, FieldCardSize);

                fieldHolders.Add(holder);

                CardView view = CardView.Create(holder.Rect, FieldCardSize);

                holder.Bind(view);

                view.ShowInstance(card);

                viewsByInstance[card.InstanceID] = view;
                view.HoverEntered += ShowPreview;
                view.HoverExited += HidePreview;

                ApplyKaiokenAura(owner, card, view);

                // Bankai: ออร่าแดงเข้มลุกรอบการ์ดตลอดเวลาที่อยู่บนสนาม
                if (card.Data != null && card.Data.RevertsOnLeaveField)
                    CardFx.Aura(view, fxLayer, new Color(0.9f, 0.1f, 0.14f, 1f), 1000000f);

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
            int handCount = Human.Hand.Count;
            int handSlot = 0;

            foreach (CardInstance card in Human.Hand.Cards)
            {
                CardHolder3D holder = stage.CreateHandHolder(handSlot++, handCount, HandCardSize);

                handHolders.Add(holder);

                CardView view = CardView.Create(holder.Rect, HandCardSize);

                holder.Bind(view);

                view.ShowInstance(card);

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

            SetSkill(view.Data, view.Instance);
        }

        // แผงสกิลใต้การ์ดพรีวิว
        private void SetSkill(CardData data, CardInstance instance)
        {
            if (skillView == null)
                return;

            skillView.Show(
                GameGlossary.SkillTextFor(data, instance),
                GameGlossary.TermsFor(data, instance));
        }

        private void HidePreview(CardView view)
        {
            // มีการ์ดที่คลิกปักหมุดไว้ = กลับไปแสดงใบนั้นแทนการซ่อน
            if (ShowPinned())
                return;

            if (previewView != null)
                previewView.gameObject.SetActive(false);
                if (previewGlow != null)
                    previewGlow.gameObject.SetActive(false);

            if (skillView != null)
                skillView.Clear();
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
                if (previewGlow != null)
                    previewGlow.gameObject.SetActive(true);
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
            if (!PinnedValid() || previewView == null || skillView == null)
            {
                pinnedCard = null;
                return false;
            }

            previewView.ShowInstance(pinnedCard);
            previewView.SetHighlight(CardHighlight.None);
            previewView.gameObject.SetActive(true);
            if (previewGlow != null)
                previewGlow.gameObject.SetActive(true);

            SetSkill(pinnedCard.Data, pinnedCard);

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

                SetSkill(shown.Data, shown);
            }
            else if (!ShowPinned())
            {
                previewView.gameObject.SetActive(false);
                if (previewGlow != null)
                    previewGlow.gameObject.SetActive(false);
                if (skillView != null)
                    skillView.Clear();
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
                            origin = UiFx.ScreenOf(enemyHandButton.transform);

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
                                UiFx.ScreenOf(entered.transform), false,
                                () =>
                                {
                                    if (landing != null &&
                                        SecretFx.IsSecret(card.Data) &&
                                        cinematic == null)
                                    {
                                        PlayArrivalCinematic(card.Data, landing, hidden);
                                        return;
                                    }

                                    if (hidden != null)
                                        hidden.alpha = 1f;

                                    if (landing != null)
                                    {
                                        CardFx.Pop(landing);

                                        if (SecretFx.IsSecret(card.Data))
                                        {
                                            SecretFx.Arrive(
                                                fxLayer, root, UiFx.ScreenOf(landing.transform), card.Data);
                                        }
                                    }
                                });

                            aiPlayedFromHand = false;
                        }
                        else
                        {
                            if (SecretFx.IsSecret(card.Data) && cinematic == null)
                            {
                                PlayArrivalCinematic(card.Data, entered, null);
                            }
                            else
                            {
                                CardFx.Pop(entered);

                                if (SecretFx.IsSecret(card.Data))
                                {
                                    SecretFx.Arrive(
                                        fxLayer, root, UiFx.ScreenOf(entered.transform), card.Data);
                                }
                            }
                        }

                        if (card.HasKeyword(Keyword.Taunt) || card.HasKeyword(Keyword.Shield))
                            Sfx.Play(SoundId.Clang);

                        UiFx.Ring(
                            fxLayer, UiFx.ScreenOf(entered.transform),
                            new Color(1f, 0.85f, 0.45f, 0.8f), 60f, 300f, 0.45f);

                        UiFx.Burst(
                            fxLayer, UiFx.ScreenOf(entered.transform),
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

                        Sfx.Play(SoundId.Attack);
                    }

                    nowAttacks[card.InstanceID] = card.AttacksThisTurn;

                    // แปลงร่าง (เช่น Ichigo -> Bankai): เอฟเฟกต์ที่การ์ด
                    string form = card.Data != null ? card.Data.CardID : "";
                    string previousForm;

                    if (known &&
                        lastForms.TryGetValue(card.InstanceID, out previousForm) &&
                        previousForm != form &&
                        viewsByInstance.TryGetValue(card.InstanceID, out CardView transformed))
                    {
                        PlayTransformFx(card, transformed);
                    }

                    lastForms[card.InstanceID] = form;

                    CardView located;

                    if (viewsByInstance.TryGetValue(card.InstanceID, out located))
                        nowPositions[card.InstanceID] = UiFx.ScreenOf(located.transform);

                    if (known && previous != card.CurrentHealth)
                    {
                        CardView view;

                        if (viewsByInstance.TryGetValue(card.InstanceID, out view))
                        {
                            int delta = card.CurrentHealth - previous;

                            SpawnDelta(UiFx.ScreenOf(view.transform), delta);

                            CardFx.Flash(
                                view,
                                delta < 0
                                    ? new Color(1f, 0.15f, 0.15f, 0.65f)
                                    : new Color(0.3f, 1f, 0.4f, 0.55f));

                            PlayImpact(UiFx.ScreenOf(view.transform), delta);
                        }

                        NoteChange(card.CurrentHealth - previous);
                    }

                    now[card.InstanceID] = card.CurrentHealth;
                }
            }

            // ยูนิตที่หายไปจากสนาม (ตาย) นับเป็นความเสียหาย
            bool unitDied = false;

            foreach (int id in lastHealth.Keys)
            {
                if (!now.ContainsKey(id))
                {
                    sawDamage = true;
                    unitDied = true;

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

            PlayGeassEvent();

            PlaySecretEvents();

            lastHealth = now;
            lastAttacks = nowAttacks;
            lastPositions = nowPositions;

            PlaySpellEffects();

            CheckLeader(Human, ref lastHumanHealth, playerLeader);
            CheckLeader(Ai, ref lastAiHealth, enemyLeader);

            int fieldCount = Human.Field.Count + Ai.Field.Count;

            if (unitDied)
                Sfx.Play(SoundId.Death);
            else if (sawDamage)
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
                    nowHand[handCard.InstanceID] = UiFx.ScreenOf(handView.transform);
            }

            lastHandPositions = nowHand;
            lastAiHandCount = Ai.Hand.Count;

            // จั่วการ์ดใหม่เข้ามือ
            int humanHand = Human.Hand.Count;

            if (lastHumanHandCount >= 0 && humanHand > lastHumanHandCount)
                Sfx.Play(SoundId.Draw);

            lastHumanHandCount = humanHand;
        }

        private void CheckLeader(BattlePlayer player, ref int last, Button button)
        {
            if (last >= 0 && last != player.LeaderHealth)
            {
                int delta = player.LeaderHealth - last;

                SpawnDelta(UiFx.ScreenOf(button.transform), delta);

                PlayImpact(UiFx.ScreenOf(button.transform), delta);

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

                if (stage != null)
                    stage.Shake(0.05f + power * 0.014f, 0.22f + power * 0.02f);

                // แสงวาบสีขาวตรงจุดกระทบ ให้การโจมตีรู้สึกมีน้ำหนัก
                UiFx.Ring(
                    fxLayer, position, new Color(1f, 0.95f, 0.85f, 0.9f),
                    30f, 160f + power * 20f, 0.3f);

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

                    Sfx.Play(Sfx.SpellSoundFor(card.Data.Tags));

                    // การ์ดสเปลบินจากมือไปกลางสนามแล้วสลาย
                    Vector3 spellOrigin = Vector3.zero;

                    bool hasOrigin = player == Human
                        ? lastHandPositions.TryGetValue(card.InstanceID, out spellOrigin)
                        : enemyHandButton != null;

                    if (player != Human && hasOrigin)
                        spellOrigin = UiFx.ScreenOf(enemyHandButton.transform);

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
                overlay,
                string.IsNullOrEmpty(battle.PendingDiscover.Title)
                    ? "DISCOVER - choose a card"
                    : battle.PendingDiscover.Title,
                44,
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

            Sfx.Play(SoundId.Buff);

            battle.ResolveAdapt(index);
        }

        // ---------- ดูกองทิ้ง / มือฝั่งตรงข้าม ----------

        private void TogglePile(PileKind kind)
        {
            // ดูมือคู่ต่อสู้ไม่ได้
            if (kind == PileKind.EnemyHand)
                return;

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
            CardData card = CardRegistry.Get(unlock.CardId);

            if (card == null)
            {
                // ไม่พบข้อมูลการ์ด: ข้ามฉาก
                bannerIndex++;
                ShowResult();
                return;
            }

            unlockCinematicIndex = bannerIndex;

            unlockCinematic = SecretCinematic.Play(
                overlay, SecretCinematic.Mode.Unlock, card,
                unlock.Title, card.CardName, unlock.Subtitle,
                null, null, null,
                () =>
                {
                    bannerIndex++;
                    ShowResult();
                });

            // เวทวงกลมหมุนและประกายเดิม ซ้อนอยู่ในฉาก
            BannerMagic.Create(
                unlockCinematic.Layer, unlock.Tint, unlock.Style == BannerStyle.Yamato);

            // ให้อยู่หลังการ์ดและข้อความ (ต่อจากพื้นหลัง / ไฟสาด / รัศมี)
            Transform magic = unlockCinematic.Layer.GetChild(unlockCinematic.Layer.childCount - 1);

            magic.SetSiblingIndex(Mathf.Min(3, unlockCinematic.Layer.childCount - 1));
        }

        private void ShowResult()
        {
            bool won = battle.Winner == Human;

            if (!resultShown)
            {
                resultShown = true;

                BattleSummary summary = new BattleSummary();

                summary.Turns = battle.TurnManager.TurnNumber;
                summary.Draw = battle.Winner == null;
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

                // Tutorial ไม่ปลดล็อกการ์ดลับ
                unlockBanners = BattleSetup.Tutorial
                    ? new List<SecretUnlock>()
                    : SecretCardService.Evaluate(
                        battle, Human.PlayerID, won, SaveManager.Profile);

                foreach (SecretUnlock unlock in unlockBanners)
                    summary.SecretIds.Add(unlock.CardId);

                resultLines = CampaignService.OnBattleFinished(won, summary);

                if (battle.Winner != null)
                    Sfx.Play(won ? SoundId.Win : SoundId.Lose);
            }

            // ฉากปลดล็อกกำลังเล่นอยู่: อย่าสร้างซ้ำตอนมีการวาดหน้าจอใหม่
            if (unlockCinematic != null &&
                unlockCinematicIndex == bannerIndex &&
                unlockBanners != null &&
                bannerIndex < unlockBanners.Count)
                return;

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

            // สรุปแมตช์ (ไม่แสดงใน Tutorial)
            if (!BattleSetup.Tutorial)
            {
                SaveData data = SaveManager.Profile.Data;

                MatchRecord last = data.History != null && data.History.Count > 0
                    ? data.History[data.History.Count - 1]
                    : null;

                if (last != null)
                {
                    Text recap = ScreenKit.AddLabel(
                        overlay,
                        "Turns " + last.Turns +
                        "     Cards played " + last.CardsPlayed +
                        "     Units destroyed " + last.UnitsKilled +
                        "     Leader damage " + last.LeaderDamage +
                        "\nWin streak " + data.StatWinStreak +
                        "     Record " + data.StatWins + " W / " +
                        Mathf.Max(0, data.StatBattles - data.StatWins) + " L" +
                        "     Difficulty " +
                        AiDifficultyInfo.NameOf((AiDifficulty)last.Difficulty),
                        24, TextAnchor.MiddleCenter, 0.15f, 0.54f, 0.85f, 0.62f);

                    recap.color = UIFactory.GoldDim;
                }
            }

            ScreenKit.AddLabel(
                overlay, string.Join("\n", resultLines), 30,
                TextAnchor.UpperCenter, 0.25f, 0.30f, 0.75f, 0.53f);

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
            if (cinematic != null || (settings != null && settings.IsOpen))
                return false;

            if (battle == null || battle.IsOver || battle.HasPendingChoice)
                return false;

            // หน้าต่างตอบโต้ของเรา: เล่น Fast spell ได้แม้ไม่ใช่เทิร์นเรา
            if (battle.ChainResponder == Human)
                return true;

            // เชนว่างอยู่: เล่น Fast spell ได้ทุกเมื่อ (กติกาตรวจเทิร์นของการกระทำอื่นเอง)
            return !battle.ChainBlocksActions;
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
                bool inField = stage != null && stage.IsOverBoard(pos);

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
                    Vector3 position = UiFx.ScreenOf(view.transform);

                    CardData dtData = view.Data != null
                        ? view.Data
                        : CardRegistry.Get(StarterCardLibrary.DevilTriggerId);

                    CardView dtView = view;

                    if (cinematic == null && dtData != null)
                    {
                        cinematic = SecretCinematic.Play(
                            root, SecretCinematic.Mode.Transform, dtData,
                            "", "Devil Trigger", SecretFx.Epithet(CardRegistry.Get(StarterCardLibrary.DevilTriggerId)),
                            root, null,
                            () =>
                            {
                                SecretFx.DevilTrigger(fxLayer, root, position);

                                if (dtView != null)
                                    CardFx.Flash(dtView, new Color(0.4f, 0.7f, 1f, 1f));
                            },
                            null);
                    }
                    else
                    {
                        SecretFx.DevilTrigger(fxLayer, root, position);

                        CardFx.Flash(view, new Color(0.4f, 0.7f, 1f, 1f));
                    }
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

        // ---------- Leader (สกิลกด + มิเตอร์ Power) ----------

        private void BuildLeaderUI()
        {
            leaderRoot = UIFactory.CreateRect("LeaderSkill", root);

            UIFactory.SetAnchors(leaderRoot, 0.91f, 0.015f, 0.995f, 0.155f);

            leaderButton = ScreenKit.AddButton(
                leaderRoot, "SKILL", OnLeaderSkillClicked,
                new Color(0.75f, 0.4f, 0.08f, 1f), 0f, 0.38f, 1f, 1f, 20);

            leaderPowerText = ScreenKit.AddLabel(
                leaderRoot, "", 17, TextAnchor.MiddleCenter, 0f, 0.17f, 1f, 0.37f);

            leaderPowerText.resizeTextForBestFit = true;
            leaderPowerText.resizeTextMinSize = 10;
            leaderPowerText.resizeTextMaxSize = 17;

            Image barBack = ScreenKit.AddPanel(
                leaderRoot, "PowerBack", new Color(0f, 0f, 0f, 0.6f), 0f, 0.02f, 1f, 0.16f);

            barBack.raycastTarget = false;

            leaderBarFill = UIFactory.CreatePanel(
                barBack.transform, "Fill", new Color(1f, 0.65f, 0.15f, 1f));

            leaderBarFill.raycastTarget = false;

            RectTransform fill = leaderBarFill.rectTransform;

            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(3f, 3f);
            fill.offsetMax = new Vector2(-3f, -3f);

            leaderRoot.gameObject.SetActive(false);
        }

        private void RefreshLeader()
        {
            LeaderData leader = Human != null ? Human.Leader : null;

            if (leader == null)
            {
                leaderRoot.gameObject.SetActive(false);
                return;
            }

            leaderRoot.gameObject.SetActive(true);

            ActionResult check = LeaderSkillCommand.Validate(battle, Human);

            leaderButton.interactable = check.Success;

            UIFactory.SetButtonLabel(
                leaderButton,
                leader.SkillName.ToUpperInvariant() + "\n" + leader.SkillCostText);

            float fraction = leader.PowerMax > 0
                ? Mathf.Clamp01(Human.LeaderPower / (float)leader.PowerMax)
                : 0f;

            leaderBarFill.rectTransform.anchorMax = new Vector2(fraction, 1f);

            CardData finisher = StarterCardLibrary.Get(leader.FinisherId);
            string finisherName = finisher != null ? finisher.CardName : "Finisher";

            if (Human.LeaderFinisherGiven)
                leaderPowerText.text = finisherName + " ready";
            else if (Human.LeaderPower >= leader.PowerMax)
                leaderPowerText.text = leader.PowerName + " MAX (hand full)";
            else
                leaderPowerText.text = leader.PowerName + " " + Human.LeaderPower + "/" + leader.PowerMax;

            // ได้การ์ดไม้ตายแล้ว: ป้ายประกาศ + เอฟเฟกต์
            if (Human.LeaderFinisherGiven && !leaderFinisherShown)
            {
                leaderFinisherShown = true;

                Color theme = leader.Theme;

                bannerText.text =
                    "<color=#FFB040>" + finisherName.ToUpperInvariant() + "</color>\n" +
                    "<size=24>" + leader.Name + "'s finisher is in your hand!</size>";

                bannerRoot.gameObject.SetActive(true);
                bannerRoot.SetAsLastSibling();

                bannerTimer = 3.5f;

                UiFx.Ring(fxLayer, bannerRoot.position, theme, 100f, 900f, 0.9f);
                UiFx.Burst(fxLayer, bannerRoot.position, theme, 50, 520f, 36f, 1.2f, -40f);

                Sfx.Play(SoundId.Win);
            }
        }

        private void OnLeaderSkillClicked()
        {
            if (!CanAct() || Human == null || Human.Leader == null)
                return;

            ClearSelection();

            ActionResult result = battle.Submit(new LeaderSkillCommand(Human));

            if (result.Success)
                PlayLeaderSkillFx();

            Report(result);
        }

        // Kaioken ที่ยังมีผลอยู่ (ใช้สกิลในเทิร์นนี้ และยูนิตยังมี Attack Up): ออร่าแดงเต้นค้างจนจบเทิร์น
        private void ApplyKaiokenAura(BattlePlayer owner, CardInstance card, CardView view)
        {
            if (owner == null || owner.Leader == null || view == null ||
                owner.Leader.Id != LeaderLibrary.GokuId ||
                owner.Leader.SkillKind != LeaderSkillKind.BuffAttack ||
                !owner.LeaderSkillUsed ||
                card.StatusValue(StatusType.AttackUp) < owner.Leader.SkillValue)
                return;

            CardFx.Aura(view, fxLayer, KaiokenRed, 1000000f);
        }

        // Geass ของ Lelouch: ตราปีกนกที่ตาของ Lelouch เส้นพลังไปหาเป้าหมาย ตราล็อกเป้าหมาย
        private void PlayGeassEvent()
        {
            if (battle.GeassSeq == lastGeassSeq)
                return;

            lastGeassSeq = battle.GeassSeq;

            CardInstance source = battle.GeassSource;
            CardInstance target = battle.GeassTarget;

            if (source == null || target == null)
                return;

            Vector3 from;
            Vector3 to;

            CardView sourceView;
            CardView targetView;

            if (viewsByInstance.TryGetValue(source.InstanceID, out sourceView) && sourceView != null)
                from = UiFx.ScreenOf(sourceView.transform);
            else if (!lastPositions.TryGetValue(source.InstanceID, out from))
                return;

            if (viewsByInstance.TryGetValue(target.InstanceID, out targetView) && targetView != null)
                to = UiFx.ScreenOf(targetView.transform);
            else if (!lastPositions.TryGetValue(target.InstanceID, out to))
                return;

            GeassFx.Play(fxLayer, root, from, to, battle.GeassBlocked);

            if (targetView != null)
                CardFx.Flash(targetView, new Color(1f, 0.1f, 0.2f, 0.7f));
        }

        // แปลงร่าง: แสงวาบ วงแหวน เสาแสง และประกายจากการ์ด (Bankai ใช้สีแดงเข้ม + ข้อความ BANKAI!)
        private void PlayTransformFx(CardInstance card, CardView view)
        {
            if (view == null)
                return;

            bool bankai = card.Data != null && card.Data.RevertsOnLeaveField;

            Color theme = bankai
                ? new Color(0.95f, 0.12f, 0.14f, 1f)
                : new Color(1f, 0.85f, 0.45f, 1f);

            Vector3 origin = UiFx.ScreenOf(view.transform);

            CardFx.Pop(view);
            CardFx.Flash(view, new Color(1f, 1f, 1f, 0.9f));

            UiFx.Ring(fxLayer, origin, theme, 50f, 460f, 0.7f);
            UiFx.Ring(fxLayer, origin, Color.white, 30f, 300f, 0.5f);
            UiFx.Beam(fxLayer, origin, theme, 150f, 720f, 0.9f);
            UiFx.Burst(fxLayer, origin, theme, 36, 520f, 30f, 0.9f, -140f);

            if (bankai)
            {
                // ควันดำของพลังวิญญาณ
                UiFx.Burst(fxLayer, origin, new Color(0.08f, 0.02f, 0.06f, 0.9f), 18, 260f, 54f, 1.1f, -90f);

                FloatingText.Spawn(
                    fxLayer, origin + new Vector3(0f, 130f, 0f), "BANKAI!", theme);
            }

            Sfx.Play(SoundId.Skill);
        }

        // Kaioken: สีแดงเข้มของพลังเพิ่มร่าง (ใช้กับสกิลเพิ่ม ATK ของ Goku)
        private static readonly Color KaiokenRed = new Color(1f, 0.16f, 0.1f, 1f);

        private void PlayLeaderSkillFx()
        {
            bool kaioken = Human.Leader.Id == LeaderLibrary.GokuId &&
                           Human.Leader.SkillKind == LeaderSkillKind.BuffAttack;

            Color theme = kaioken ? KaiokenRed : Human.Leader.Theme;

            Vector3 origin = playerLeader != null
                ? UiFx.ScreenOf(playerLeader.transform)
                : leaderRoot.position;

            UiFx.Ring(fxLayer, origin, theme, 80f, 900f, 0.8f);
            UiFx.Burst(fxLayer, origin, theme, 40, 520f, 30f, 1.0f, -60f);

            SecretFx.Flash(root, theme, 0.3f, 0.35f);
            SecretFx.Shake(root, 10f, 0.3f);

            if (stage != null)
                stage.Shake(0.3f, 0.5f);

            Sfx.Play(SoundId.Skill);

            foreach (CardInstance unit in Human.Field)
            {
                CardView view;

                if (viewsByInstance.TryGetValue(unit.InstanceID, out view) && view != null)
                {
                    CardFx.Flash(view, theme);

                    UiFx.Burst(fxLayer, UiFx.ScreenOf(view.transform), theme, 12, 300f, 22f, 0.8f, -40f);

                    // Kaioken: วงแหวนกระแทก + ประกายไฟพุ่งขึ้น (ออร่าแดงค้างตลอดเทิร์นอยู่ใน RefreshField
                    // เพราะสนามถูกสร้างใหม่ทุกครั้งที่หน้าจอรีเฟรช)
                    if (kaioken)
                    {
                        UiFx.Ring(fxLayer, UiFx.ScreenOf(view.transform), theme, 60f, 320f, 0.6f);
                        UiFx.Burst(fxLayer, UiFx.ScreenOf(view.transform), theme, 18, 380f, 26f, 1.1f, -300f);
                    }
                }
            }
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

            if (!result.Success)
                Sfx.Play(SoundId.Error);
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
