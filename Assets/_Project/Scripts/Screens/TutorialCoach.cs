using UnityEngine;
using UnityEngine.UI;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ผู้สอนในแมตช์ Tutorial: แสดงกล่องข้อความมุมซ้ายบนทีละขั้น ไฮไลต์ส่วนที่ต้องกด
    /// และเลื่อนขั้นเองเมื่อผู้เล่นทำตามจริง (ลงยูนิต จบเทิร์น โจมตี ใช้สเปล)
    /// สร้างและเรียก Update() จาก BattleScreen เฉพาะเมื่อ BattleSetup.Tutorial เป็น true
    /// </summary>
    public class TutorialCoach
    {
        private enum Step
        {
            Welcome,
            Goal,
            Energy,
            PlayUnit,
            EndTurn,
            EnemyTurn,
            Attack,
            Spell,
            Extras,
            Finish,
            Done
        }

        private RectTransform root;
        private RectTransform box;
        private Text titleText;
        private Text bodyText;
        private Button nextButton;
        private Text nextLabel;
        private Image highlight;

        private BattleManager battle;
        private BattlePlayer human;
        private BattlePlayer ai;

        private RectTransform handZone;
        private RectTransform playerField;
        private RectTransform endTurn;
        private RectTransform leader;
        private RectTransform leaderSkill;

        private RectTransform highlightTarget;
        private bool highlightCircle;

        private Step step = Step.Done;
        private int stepCount;
        private int attackBaseline;

        public static TutorialCoach Create(
            RectTransform root, BattleManager battle,
            BattlePlayer human, BattlePlayer ai,
            RectTransform handZone, RectTransform playerField,
            RectTransform endTurn, RectTransform leader,
            RectTransform leaderSkill, Transform drawBefore)
        {
            TutorialCoach coach = new TutorialCoach();

            coach.root = root;
            coach.battle = battle;
            coach.human = human;
            coach.ai = ai;
            coach.handZone = handZone;
            coach.playerField = playerField;
            coach.endTurn = endTurn;
            coach.leader = leader;
            coach.leaderSkill = leaderSkill;

            coach.Build(drawBefore);
            coach.Enter(Step.Welcome);

            return coach;
        }

        private void Build(Transform drawBefore)
        {
            // กรอบเรืองสีทองที่กะพริบรอบส่วนที่ต้องกด (เป็นเส้นกรอบ ไม่ทับการ์ดด้านใน)
            highlight = UIFactory.CreatePanel(root, "TutorialHighlight", new Color(1f, 0.85f, 0.3f, 0.9f));
            highlight.sprite = UIShapes.Ring;
            highlight.type = Image.Type.Sliced;
            highlight.pixelsPerUnitMultiplier = 1f;
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(false);

            box = UIFactory.CreateZone(root, "TutorialCoach", 0.012f, 0.70f, 0.385f, 0.835f);

            Image back = ScreenKit.AddPanel(
                box, "Back", new Color(0.04f, 0.06f, 0.1f, 0.94f), 0f, 0f, 1f, 1f);

            back.raycastTarget = false;

            titleText = ScreenKit.AddLabel(
                box, "", 20, TextAnchor.MiddleLeft,
                0.03f, 0.74f, 0.7f, 0.98f, FontStyle.Bold);

            titleText.color = UIFactory.Gold;
            titleText.raycastTarget = false;

            bodyText = ScreenKit.AddLabel(
                box, "", 22, TextAnchor.UpperLeft,
                0.03f, 0.05f, 0.76f, 0.74f);

            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 12;
            bodyText.resizeTextMaxSize = 22;
            bodyText.raycastTarget = false;

            nextButton = ScreenKit.AddButton(
                box, "Next", OnNext, UIFactory.Success, 0.79f, 0.08f, 0.97f, 0.42f, 22);

            nextLabel = nextButton.GetComponentInChildren<Text>();

            ScreenKit.AddButton(
                box, "Skip", Skip, UIFactory.Muted, 0.79f, 0.60f, 0.97f, 0.96f, 18);

            // ให้การ์ดพรีวิวขณะชี้การ์ดอยู่ด้านบนกล่องนี้
            if (drawBefore != null)
                box.SetSiblingIndex(drawBefore.GetSiblingIndex());
        }

        private void OnNext()
        {
            switch (step)
            {
                case Step.Welcome: Enter(Step.Goal); break;
                case Step.Goal: Enter(Step.Energy); break;
                case Step.Energy: Enter(Step.PlayUnit); break;
                case Step.Extras: Enter(Step.Finish); break;
                case Step.Finish: Enter(Step.Done); break;
            }
        }

        private void Skip()
        {
            Enter(Step.Done);
        }

        private bool NeedsNext
        {
            get
            {
                return step == Step.Welcome || step == Step.Goal ||
                    step == Step.Energy || step == Step.Extras || step == Step.Finish;
            }
        }

        private void Enter(Step next)
        {
            step = next;
            stepCount++;

            highlightTarget = null;
            highlightCircle = false;

            string body = "";

            switch (next)
            {
                case Step.Welcome:
                    body = "Welcome to Multiverse Card Battle! This short lesson teaches the " +
                        "basics against a Training Dummy. You can skip it at any time.";
                    break;

                case Step.Goal:
                    body = "Your goal: reduce the enemy Leader's HP to 0. Your own Leader " +
                        "(bottom left) works the same way - if its HP reaches 0, you lose.";
                    highlightTarget = leader;
                    break;

                case Step.Energy:
                    body = "Cards cost Energy (the blue gem on the left). You gain 1 max " +
                        "Energy every turn, up to 10. Your hand is at the bottom.";
                    highlightTarget = handZone;
                    break;

                case Step.PlayUnit:
                    body = "Drag Leaf Genin (cost 1) from your hand onto the board to play " +
                        "it. A unit can't attack on the turn it is played.";
                    highlightTarget = playerField;
                    break;

                case Step.EndTurn:
                    body = "Nice! Now press END TURN (or the Space bar) to pass the turn.";
                    highlightTarget = endTurn;
                    highlightCircle = true;
                    break;

                case Step.EnemyTurn:
                    body = "The Training Dummy plays units but never attacks. " +
                        "Wait for your turn...";
                    break;

                case Step.Attack:
                    attackBaseline = AttackScore();

                    body = "Your units are ready (green outline). Click one, then click " +
                        "an enemy unit or the enemy Leader to attack.";
                    highlightTarget = playerField;
                    break;

                case Step.Spell:
                    body = "Spells have a one-time effect. Play Lightning Bolt from your " +
                        "hand - it hits the enemy Leader for 4 damage.";
                    highlightTarget = handZone;
                    break;

                case Step.Extras:
                    body = "Tips: hover a card to read it larger - keywords like Taunt, " +
                        "Charge and Shield are explained under the preview. Your Leader's " +
                        "skill (bottom right) charges as you play; use it at the right time!";
                    highlightTarget = leaderSkill;
                    break;

                case Step.Finish:
                    body = "Now finish the job: reduce the Training Dummy's HP to 0 to " +
                        "win. Good luck!";
                    break;
            }

            bool visible = next != Step.Done;

            box.gameObject.SetActive(visible);

            if (!visible)
            {
                highlight.gameObject.SetActive(false);
                return;
            }

            titleText.text = "TUTORIAL";
            bodyText.text = body;

            nextButton.gameObject.SetActive(NeedsNext);
            nextLabel.text = next == Step.Finish ? "OK" : "Next";
        }

        // นับการกระทำโจมตีของผู้เล่น (ใช้ตรวจว่าโจมตีแล้ว แม้ยูนิตจะตายระหว่างต่อสู้)
        private int AttackScore()
        {
            BattleStats s = battle.Stats;

            int score = s.LeaderDamage[human.PlayerID] +
                s.UnitsKilled[human.PlayerID] +
                s.UnitDeaths[human.PlayerID];

            foreach (CardInstance unit in human.Field)
                score += unit.AttacksThisTurn;

            return score;
        }

        private bool HasSpellInHand()
        {
            foreach (CardInstance card in human.Hand.Cards)
            {
                if (card.Data != null && card.Data.Type == CardType.Spell)
                    return true;
            }

            return false;
        }

        public void Update()
        {
            if (step == Step.Done || battle == null)
                return;

            if (battle.IsOver)
            {
                Enter(Step.Done);
                return;
            }

            bool humanTurn = battle.CurrentPlayer == human;
            int turn = battle.TurnManager.TurnNumber;

            switch (step)
            {
                case Step.PlayUnit:
                    if (human.Field.Count > 0)
                        Enter(humanTurn ? Step.EndTurn : Step.EnemyTurn);
                    else if (!humanTurn)
                        Enter(Step.EnemyTurn);
                    break;

                case Step.EndTurn:
                    if (!humanTurn)
                        Enter(Step.EnemyTurn);
                    break;

                case Step.EnemyTurn:
                    if (humanTurn && turn >= 2)
                        Enter(Step.Attack);
                    break;

                case Step.Attack:
                    if (AttackScore() > attackBaseline)
                        Enter(HasSpellInHand() && battle.Stats.SpellsPlayed[human.PlayerID] == 0
                            ? Step.Spell
                            : Step.Extras);
                    else if (turn >= 5)
                        Enter(Step.Extras);
                    break;

                case Step.Spell:
                    if (battle.Stats.SpellsPlayed[human.PlayerID] > 0 || turn >= 5)
                        Enter(Step.Extras);
                    break;
            }

            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            bool show = step != Step.Done && highlightTarget != null &&
                highlightTarget.gameObject.activeInHierarchy;

            if (highlight.gameObject.activeSelf != show)
                highlight.gameObject.SetActive(show);

            if (!show)
                return;

            Vector3[] corners = new Vector3[4];

            highlightTarget.GetWorldCorners(corners);

            Vector3 a = root.InverseTransformPoint(corners[0]);
            Vector3 b = root.InverseTransformPoint(corners[2]);

            RectTransform rect = highlight.rectTransform;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f);

            float pad = highlightCircle ? 8f : 10f;

            rect.sizeDelta = new Vector2(
                Mathf.Abs(b.x - a.x) + pad * 2f, Mathf.Abs(b.y - a.y) + pad * 2f);

            // ปุ่มกลมใช้กรอบวงกลม ส่วนอื่นใช้กรอบสี่เหลี่ยมมุมมน
            Sprite wanted = highlightCircle ? UIShapes.RingCircle : UIShapes.Ring;

            if (highlight.sprite != wanted)
            {
                highlight.sprite = wanted;
                highlight.type = highlightCircle ? Image.Type.Simple : Image.Type.Sliced;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);

            Color c = highlight.color;

            c.a = 0.35f + 0.65f * pulse;

            highlight.color = c;
        }
    }
}
