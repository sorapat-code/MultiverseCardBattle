using System.Collections.Generic;
using System.Text;
using UnityEngine;
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

        private const float AIActionDelay = 0.6f;
        private const int MaxLogLinesShown = 16;

        private BattleManager battle;

        private RectTransform root;
        private RectTransform enemyField;
        private RectTransform playerField;
        private RectTransform handZone;
        private RectTransform overlay;

        private Text turnText;
        private Text hintText;
        private Text logText;

        private Button enemyLeader;
        private Button playerLeader;
        private Button endTurnButton;
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
        private Button enemyHandButton;
        private Button enemyGraveButton;
        private Button playerGraveButton;
        private Text playerDeckLabel;

        private class LeaderWidget
        {
            public Button Button;
            public Text Title;
            public Image HpFill;
            public Text HpText;
            public Image EnergyFill;
            public Text EnergyText;
        }

        // พรีวิวการ์ดใบใหญ่ตอนเอาเมาส์ชี้
        private CardView previewView;
        private RectTransform previewZone;

        // ใช้ตรวจว่าค่าอะไรเปลี่ยน เพื่อแสดงตัวเลขลอยและเล่นเสียง
        private readonly Dictionary<int, CardView> viewsByInstance =
            new Dictionary<int, CardView>();

        private Dictionary<int, int> lastHealth = new Dictionary<int, int>();
        private int lastHumanHealth = -1;
        private int lastAiHealth = -1;
        private int lastFieldCount = -1;
        private bool sawDamage;
        private bool sawHeal;

        // การเลือกของผู้เล่น
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

        private void HandleHotkeys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.spaceKey.wasPressedThisFrame)
                OnEndTurnClicked();

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (openPile != PileKind.None)
                    TogglePile(openPile);
                else
                    ClearSelectionAndRefresh();
            }
        }

        // ---------- AI ----------

        private void RunAI()
        {
            if (battle.IsOver || battle.HasPendingChoice)
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
            root = UIFactory.CreateScreenRoot("BattleCanvas");

            // บันทึกการต่อสู้ (ซ้าย)
            ScreenKit.AddPanel(
                root, "LogPanel", new Color(0f, 0f, 0f, 0.35f),
                0.005f, 0.01f, 0.17f, 0.99f);

            logText = ScreenKit.AddLabel(
                root, "", 17, TextAnchor.LowerLeft,
                0.01f, 0.02f, 0.165f, 0.58f);

            previewZone = UIFactory.CreateZone(
                root, "PreviewZone", 0.005f, 0.60f, 0.17f, 0.99f);

            previewView = CardView.Create(previewZone, PreviewCardSize);

            previewView.SetInteractable(false);
            previewView.gameObject.SetActive(false);

            // แถบบน
            turnText = ScreenKit.AddLabel(
                root, "", 30, TextAnchor.MiddleLeft,
                0.19f, 0.945f, 0.4f, 1f, FontStyle.Bold);

            hintText = ScreenKit.AddLabel(
                root, "", 24, TextAnchor.MiddleCenter,
                0.41f, 0.945f, 0.87f, 1f);

            ScreenKit.AddButton(root, "Concede", Concede,
                UIFactory.Danger, 0.88f, 0.95f, 0.99f, 0.995f, 22);

            // Leader ศัตรู / ผู้เล่น
            enemyWidget = CreateLeaderWidget(
                () => OnLeaderClicked(Ai),
                0.345f, 0.84f, 0.665f, 0.94f);

            playerWidget = CreateLeaderWidget(
                () => OnLeaderClicked(Human),
                0.345f, 0.265f, 0.665f, 0.365f);

            enemyLeader = enemyWidget.Button;
            playerLeader = playerWidget.Button;

            // ปุ่มดูกองทิ้ง / มือศัตรู
            enemyHandButton = ScreenKit.AddButton(
                root, "Opp. Hand", () => TogglePile(PileKind.EnemyHand),
                UIFactory.Muted, 0.675f, 0.89f, 0.785f, 0.94f, 20);

            enemyGraveButton = ScreenKit.AddButton(
                root, "Graveyard", () => TogglePile(PileKind.EnemyGraveyard),
                UIFactory.Muted, 0.675f, 0.84f, 0.785f, 0.885f, 20);

            playerGraveButton = ScreenKit.AddButton(
                root, "Graveyard", () => TogglePile(PileKind.PlayerGraveyard),
                UIFactory.Muted, 0.675f, 0.315f, 0.785f, 0.365f, 20);

            playerDeckLabel = ScreenKit.AddLabel(
                root, "", 20, TextAnchor.MiddleCenter,
                0.675f, 0.265f, 0.785f, 0.31f);

            // พื้นหลังโซนการ์ด
            ScreenKit.AddPanel(
                root, "EnemyLane", new Color(0.75f, 0.3f, 0.3f, 0.10f),
                0.18f, 0.60f, 0.99f, 0.84f);

            ScreenKit.AddPanel(
                root, "PlayerLane", new Color(0.3f, 0.6f, 0.9f, 0.10f),
                0.18f, 0.365f, 0.99f, 0.60f);

            ScreenKit.AddPanel(
                root, "HandLane", new Color(0f, 0f, 0f, 0.28f),
                0.18f, 0.0f, 0.99f, 0.265f);

            // โซนการ์ด
            enemyField = CreateRow(root, "EnemyField", 0.18f, 0.60f, 0.99f, 0.84f, 8f);
            playerField = CreateRow(root, "PlayerField", 0.18f, 0.365f, 0.99f, 0.60f, 8f);
            handZone = CreateRow(root, "Hand", 0.18f, 0.0f, 0.99f, 0.265f, 4f);

            endTurnButton = ScreenKit.AddButton(
                root, "END TURN", OnEndTurnClicked,
                UIFactory.Success, 0.80f, 0.275f, 0.98f, 0.355f, 28);

            cancelButton = ScreenKit.AddButton(
                root, "Cancel", ClearSelectionAndRefresh,
                UIFactory.Muted, 0.19f, 0.275f, 0.31f, 0.355f, 24);

            // หน้าต่างดูกองทิ้ง / มือศัตรู
            pileOverlay = UIFactory.CreateRect("PileOverlay", root);

            UIFactory.Stretch(pileOverlay);

            pileOverlay.gameObject.SetActive(false);

            // Overlay (Discover / ผลการต่อสู้) อยู่บนสุด
            overlay = UIFactory.CreateRect("Overlay", root);

            UIFactory.Stretch(overlay);

            overlay.gameObject.SetActive(false);
        }

        private LeaderWidget CreateLeaderWidget(
            UnityEngine.Events.UnityAction onClick,
            float xMin, float yMin, float xMax, float yMax)
        {
            LeaderWidget widget = new LeaderWidget();

            widget.Button = ScreenKit.AddButton(
                root, "", onClick, UIFactory.Panel,
                xMin, yMin, xMax, yMax, 22);

            widget.Title = widget.Button.GetComponentInChildren<Text>();
            widget.Title.alignment = TextAnchor.MiddleLeft;

            UIFactory.SetAnchors(
                widget.Title.rectTransform, 0.03f, 0.56f, 0.97f, 0.98f);

            widget.HpFill = AddBar(
                widget.Button.transform, "Hp", 0.30f, 0.54f,
                new Color(0.25f, 0.75f, 0.35f, 1f), out widget.HpText);

            widget.EnergyFill = AddBar(
                widget.Button.transform, "Energy", 0.05f, 0.27f,
                new Color(0.2f, 0.55f, 0.95f, 1f), out widget.EnergyText);

            return widget;
        }

        // แถบความคืบหน้า: พื้นหลัง + แถบสี + ข้อความกลางแถบ
        private static Image AddBar(
            Transform parent, string name, float yMin, float yMax,
            Color color, out Text label)
        {
            Image back = UIFactory.CreatePanel(
                parent, name + "Back", new Color(0f, 0f, 0f, 0.45f));

            UIFactory.SetAnchors(back.rectTransform, 0.03f, yMin, 0.97f, yMax);

            back.raycastTarget = false;

            Image fill = UIFactory.CreatePanel(back.transform, name + "Fill", color);

            UIFactory.Stretch(fill.rectTransform);

            fill.raycastTarget = false;

            label = UIFactory.CreateText(
                back.transform, "", 17, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.Stretch(label.rectTransform);

            return fill;
        }

        private static void SetBar(Image fill, float ratio)
        {
            fill.rectTransform.anchorMax =
                new Vector2(Mathf.Clamp01(ratio), 1f);
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

            RefreshLeader(enemyWidget, Ai);
            RefreshLeader(playerWidget, Human);
            RefreshPileButtons();

            viewsByInstance.Clear();

            RefreshField(enemyField, Ai, myTurn);
            RefreshField(playerField, Human, myTurn);
            RefreshHand(myTurn);

            // ต้องคำนวณ Layout ก่อน จึงจะรู้ตำแหน่งการ์ดสำหรับตัวเลขลอย
            LayoutRebuilder.ForceRebuildLayoutImmediate(enemyField);
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerField);
            LayoutRebuilder.ForceRebuildLayoutImmediate(handZone);

            DetectChanges();
            RefreshPreview();

            endTurnButton.interactable = myTurn;

            UIFactory.SetButtonLabel(
                endTurnButton, myTurn ? "END TURN" : "...");

            cancelButton.gameObject.SetActive(
                selectedHand != null || selectedAttacker != null);

            logText.text = BuildLog();

            RefreshOverlay();
            RefreshPile();
        }

        private string BuildLog()
        {
            StringBuilder text = new StringBuilder();

            int start = Mathf.Max(0, battle.LogLines.Count - MaxLogLinesShown);

            for (int i = start; i < battle.LogLines.Count; i++)
            {
                if (text.Length > 0)
                    text.Append('\n');

                text.Append(battle.LogLines[i]);
            }

            return text.ToString();
        }

        private void RefreshLeader(LeaderWidget widget, BattlePlayer player)
        {
            widget.Title.text =
                "<b>" + player.Name + "</b>     Hand " + player.Hand.Count +
                "   Deck " + player.Deck.Count;

            float hp = player.MaxLeaderHealth > 0
                ? (float)player.LeaderHealth / player.MaxLeaderHealth
                : 0f;

            SetBar(widget.HpFill, hp);

            widget.HpFill.color =
                hp > 0.5f
                    ? new Color(0.25f, 0.75f, 0.35f, 1f)
                    : (hp > 0.25f
                        ? new Color(0.95f, 0.75f, 0.2f, 1f)
                        : new Color(0.9f, 0.3f, 0.3f, 1f));

            widget.HpText.text =
                "HP " + player.LeaderHealth + " / " + player.MaxLeaderHealth;

            float energy = player.MaxEnergy > 0
                ? (float)player.CurrentEnergy / player.MaxEnergy
                : 0f;

            SetBar(widget.EnergyFill, energy);

            widget.EnergyText.text =
                "Energy " + player.CurrentEnergy + " / " + player.MaxEnergy;

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

                view.Clicked += OnFieldCardClicked;
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

            foreach (CardInstance card in Human.Hand.Cards)
            {
                CardView view = CardView.Create(handZone, HandCardSize);

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
        }

        private void HidePreview(CardView view)
        {
            if (previewView != null)
                previewView.gameObject.SetActive(false);
        }

        // วาดใหม่หลังสถานะเปลี่ยน: อัปเดตค่าบนพรีวิว หรือซ่อนถ้าการ์ดหายไปแล้ว
        private void RefreshPreview()
        {
            if (previewView == null || !previewView.gameObject.activeSelf)
                return;

            CardInstance shown = previewView.Instance;

            if (shown == null)
                return;

            if (shown.Zone == CardZone.Field ||
                shown.Zone == CardZone.Hand ||
                shown.Zone == CardZone.Graveyard)
                previewView.ShowInstance(shown);
            else
                previewView.gameObject.SetActive(false);
        }

        // ---------- ตัวเลขลอย / เสียง ----------

        private void DetectChanges()
        {
            sawDamage = false;
            sawHeal = false;

            Dictionary<int, int> now = new Dictionary<int, int>();

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
                        CardFx.Pop(entered);
                    }

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
                    sawDamage = true;
            }

            lastHealth = now;

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
        }

        private void CheckLeader(BattlePlayer player, ref int last, Button button)
        {
            if (last >= 0 && last != player.LeaderHealth)
            {
                int delta = player.LeaderHealth - last;

                SpawnDelta(button.transform.position, delta);

                NoteChange(delta);
            }

            last = player.LeaderHealth;
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

            // จำตำแหน่งเลื่อนไว้ ไม่ให้เด้งกลับขึ้นบนทุกครั้งที่สถานะเปลี่ยน
            float scrollPosition =
                pileScroll != null ? pileScroll.verticalNormalizedPosition : 1f;

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

                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

                pileScroll.verticalNormalizedPosition = scrollPosition;
            }

            ScreenKit.AddButton(
                pileOverlay, "Close", () => TogglePile(openPile),
                UIFactory.Accent, 0.4f, 0.03f, 0.6f, 0.11f, 28);
        }

        private void ShowResult()
        {
            bool won = battle.Winner == Human;

            if (!resultShown)
            {
                resultShown = true;

                resultLines = CampaignService.OnBattleFinished(won);

                if (battle.Winner != null)
                    Sfx.Play(won ? SoundId.Win : SoundId.Lose);
            }

            ScreenKit.ClearNow(overlay);

            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();

            Image dim = UIFactory.CreatePanel(
                overlay, "Dim", new Color(0f, 0f, 0f, 0.85f));

            UIFactory.Stretch(dim.rectTransform);

            string title = battle.Winner == null
                ? "DRAW"
                : (won ? "VICTORY" : "DEFEAT");

            ScreenKit.AddLabel(
                overlay, title, 90, TextAnchor.MiddleCenter,
                0.2f, 0.62f, 0.8f, 0.82f, FontStyle.Bold);

            ScreenKit.AddLabel(
                overlay, string.Join("\n", resultLines), 30,
                TextAnchor.UpperCenter, 0.25f, 0.34f, 0.75f, 0.6f);

            bool fromCampaign = BattleSetup.Stage != null;

            ScreenKit.AddButton(
                overlay,
                fromCampaign ? "Back to Campaign" : "Main Menu",
                () => SceneLoader.Go(
                    fromCampaign ? GameScene.Campaign : GameScene.MainMenu),
                UIFactory.Accent, 0.35f, 0.18f, 0.65f, 0.28f);
        }

        // ---------- ปฏิกิริยาของผู้เล่น ----------

        private bool CanAct()
        {
            return battle != null &&
                !battle.IsOver &&
                !battle.HasPendingChoice &&
                battle.CurrentPlayer == Human;
        }

        private void OnHandCardClicked(CardView view)
        {
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

        private void OnEndTurnClicked()
        {
            if (!CanAct())
                return;

            ClearSelection();

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
