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
                ClearSelectionAndRefresh();
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
            enemyLeader = ScreenKit.AddButton(
                root, "", () => OnLeaderClicked(Ai),
                UIFactory.Panel, 0.36f, 0.84f, 0.74f, 0.94f, 24);

            playerLeader = ScreenKit.AddButton(
                root, "", () => OnLeaderClicked(Human),
                UIFactory.Panel, 0.36f, 0.265f, 0.74f, 0.365f, 24);

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

            // Overlay (Discover / ผลการต่อสู้) อยู่บนสุด
            overlay = UIFactory.CreateRect("Overlay", root);

            UIFactory.Stretch(overlay);

            overlay.gameObject.SetActive(false);
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
                        ? "Your turn"
                        : "Opponent's turn"));

            hintText.text = string.IsNullOrEmpty(hint)
                ? "<color=#8A93A6>Space: end turn  -  Esc: cancel</color>"
                : hint;

            RefreshLeader(enemyLeader, Ai, true);
            RefreshLeader(playerLeader, Human, false);

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

        private void RefreshLeader(Button button, BattlePlayer player, bool isEnemy)
        {
            string text =
                "<b>" + player.Name + "</b>   HP " +
                player.LeaderHealth + "/" + player.MaxLeaderHealth +
                "\nEnergy " + player.CurrentEnergy + "/" + player.MaxEnergy +
                "   Hand " + player.Hand.Count +
                "   Deck " + player.Deck.Count;

            UIFactory.SetButtonLabel(button, text);

            bool isTarget = IsActiveTarget(player);

            button.GetComponent<Image>().color =
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
                    BattleRules.ValidateBasicPlay(battle, Human, card).Success)
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

            if (shown.Zone == CardZone.Field || shown.Zone == CardZone.Hand)
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
                BattleRules.ValidateBasicPlay(battle, Human, card);

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
