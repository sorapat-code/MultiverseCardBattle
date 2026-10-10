using System;
using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Battle
{
    public class BattleManager : MonoBehaviour
    {
        private const int MaxLogLines = 60;

        private const int Player1OpeningHand = 3;
        private const int Player2OpeningHand = 4;

        [SerializeField]
        private int startingHealth = 30;

        [SerializeField]
        private bool autoStart = true;

        [System.NonSerialized]
        public BattlePlayer Player1;

        [System.NonSerialized]
        public BattlePlayer Player2;

        public TurnManager TurnManager { get; private set; }

        public EventBus Events { get; private set; }

        public CardPool DiscoverPool { get; private set; }

        // ---------- สถานะการต่อสู้ ----------

        public bool HasStarted { get; private set; }
        public bool IsOver { get; private set; }

        // null ถ้ายังไม่จบ หรือเสมอ
        public BattlePlayer Winner { get; private set; }

        // เพิ่มทุกครั้งที่สถานะเปลี่ยน ให้ UI เทียบเพื่อรู้ว่าต้องวาดใหม่
        public int StateVersion { get; private set; }

        public List<string> LogLines { get; private set; }

        public DiscoverRequest PendingDiscover { get; private set; }

        public bool HasPendingDiscover
        {
            get { return PendingDiscover != null; }
        }

        public event Action<BattlePlayer> BattleEnded;

        private TriggerManager triggerManager;

        // ---------- Unity ----------

        private void Awake()
        {
            Player1 = new BattlePlayer(1, startingHealth);
            Player2 = new BattlePlayer(2, startingHealth);

            LogLines = new List<string>();

            Events = new EventBus();
            triggerManager = new TriggerManager(this, Events);

            DiscoverPool = CardRegistry.CreateDiscoverPool();

            TurnManager = GetComponent<TurnManager>();

            if (TurnManager == null)
                TurnManager = gameObject.AddComponent<TurnManager>();
        }

        private void Start()
        {
            if (autoStart)
                StartBattle();
        }

        // ---------- เริ่มการต่อสู้ ----------

        public void StartBattle()
        {
            if (HasStarted)
                return;

            HasStarted = true;

            ApplySetup();

            Debug.Log("Battle Started!");

            Debug.Log(
                $"Player 1 HP: {Player1.LeaderHealth} | " +
                $"Player 2 HP: {Player2.LeaderHealth}"
            );

            Log("Battle start: " + Player1.Name + " vs " + Player2.Name);

            for (int i = 0; i < Player1OpeningHand; i++)
                DrawCard(Player1);

            for (int i = 0; i < Player2OpeningHand; i++)
                DrawCard(Player2);

            Publish(TriggerType.BattleStart);

            TurnManager.StartBattle();
        }

        // เตรียมเด็ค/ชื่อ/HP จาก BattleSetup (ถ้าไม่มี ใช้เด็คตั้งต้น)
        private void ApplySetup()
        {
            Player1.Name = string.IsNullOrEmpty(BattleSetup.PlayerName)
                ? "You"
                : BattleSetup.PlayerName;

            Player2.Name = string.IsNullOrEmpty(BattleSetup.EnemyName)
                ? "Opponent"
                : BattleSetup.EnemyName;

            if (BattleSetup.PlayerHealth > 0)
            {
                Player1.MaxLeaderHealth = BattleSetup.PlayerHealth;
                Player1.LeaderHealth = BattleSetup.PlayerHealth;
            }

            if (BattleSetup.EnemyHealth > 0)
            {
                Player2.MaxLeaderHealth = BattleSetup.EnemyHealth;
                Player2.LeaderHealth = BattleSetup.EnemyHealth;
            }

            List<CardData> deck1 = BattleSetup.PlayerDeck;
            List<CardData> deck2 = BattleSetup.EnemyDeck;

            if (deck1 == null || deck1.Count == 0)
                deck1 = StarterDecks.CreatePlayerStarterDeck();

            if (deck2 == null || deck2.Count == 0)
                deck2 = StarterDecks.CreatePlayerStarterDeck();

            Player1.LoadDeck(deck1);
            Player2.LoadDeck(deck2);
        }

        // ---------- ผู้เล่น ----------

        public BattlePlayer GetPlayer(int playerID)
        {
            if (playerID == 1)
                return Player1;

            if (playerID == 2)
                return Player2;

            return null;
        }

        public BattlePlayer GetOpponent(BattlePlayer player)
        {
            if (player == Player1)
                return Player2;

            if (player == Player2)
                return Player1;

            return null;
        }

        public BattlePlayer CurrentPlayer
        {
            get
            {
                if (TurnManager == null)
                    return null;

                return GetPlayer(TurnManager.CurrentPlayerID);
            }
        }

        // ผู้เล่น 1 เป็นมนุษย์ ผู้เล่น 2 เป็น AI
        public bool IsAI(BattlePlayer player)
        {
            return player == Player2;
        }

        // ---------- Command ----------

        /// <summary>
        /// ทุกการกระทำของผู้เล่น/AI ต้องผ่านที่นี่
        /// </summary>
        public ActionResult Submit(IBattleCommand command)
        {
            if (command == null)
                return ActionResult.Fail("No command");

            if (!HasStarted)
                return ActionResult.Fail("The battle has not started");

            if (IsOver)
                return ActionResult.Fail("The battle is over");

            ActionResult result = command.Execute(this);

            if (result.Success)
                Resolve();

            Touch();

            return result;
        }

        // ---------- Event / Log ----------

        public void Publish(
            TriggerType trigger,
            CardInstance card = null,
            BattlePlayer player = null,
            CardInstance target = null,
            BattlePlayer targetPlayer = null,
            int amount = 0,
            TargetCandidate chosen = null)
        {
            GameEvent gameEvent = new GameEvent();

            gameEvent.Trigger = trigger;
            gameEvent.Card = card;
            gameEvent.Player = player;
            gameEvent.Target = target;
            gameEvent.TargetPlayer = targetPlayer;
            gameEvent.Amount = amount;
            gameEvent.ChosenTarget = chosen;

            Events.Publish(gameEvent);

            Touch();
        }

        public void Log(string message)
        {
            LogLines.Add(message);

            if (LogLines.Count > MaxLogLines)
                LogLines.RemoveAt(0);

            Touch();
        }

        public void Touch()
        {
            StateVersion++;
        }

        // ---------- จั่ว / ทิ้ง ----------

        public CardInstance DrawCard(BattlePlayer player)
        {
            if (player == null)
                return null;

            if (player.Deck.Count == 0)
            {
                player.FatigueCounter++;

                Log(
                    player.Name + " has no cards left and takes " +
                    player.FatigueCounter + " fatigue damage");

                DamageLeader(null, player, player.FatigueCounter, true);

                return null;
            }

            CardInstance card = player.Deck.Draw();

            if (!player.Hand.Add(card))
            {
                // มือเต็ม การ์ดที่จั่วถูกเผาทิ้ง
                card.Zone = CardZone.Graveyard;
                player.Graveyard.Add(card);

                Log(player.Name + "'s hand is full, " +
                    card.Data.CardName + " is burned");

                return null;
            }

            Publish(TriggerType.Draw, card: card, player: player);

            return card;
        }

        public bool DiscardCard(BattlePlayer player, CardInstance card)
        {
            if (player == null || card == null)
                return false;

            if (!player.Hand.Remove(card))
                return false;

            player.AddToZone(card, CardZone.Graveyard);

            Log(player.Name + " discards " + card.Data.CardName);

            Publish(TriggerType.Discard, card: card, player: player);

            return true;
        }

        // ---------- ลงสนาม / ตาย ----------

        /// <summary>
        /// ย้ายการ์ดลงสนามให้เจ้าของ (ถ้าสนามเต็มจะไม่ทำอะไรและคืน false)
        /// </summary>
        public bool SummonUnit(CardInstance card, BattlePlayer owner)
        {
            if (card == null || owner == null)
                return false;

            if (owner.Field.Count >= BattlePlayer.MaxFieldSize)
            {
                Log(owner.Name + "'s field is full");
                return false;
            }

            owner.RemoveCard(card);
            owner.AddToZone(card, CardZone.Field);

            card.SummonedOnTurn = TurnManager.TotalTurns;
            card.AttacksThisTurn = 0;

            Publish(TriggerType.Summon, card: card, player: owner);

            return true;
        }

        /// <summary>
        /// ฆ่า/เนรเทศยูนิตบนสนาม (ยิง Death เมื่อเป็นการตายปกติ)
        /// </summary>
        public bool KillUnit(CardInstance card, bool banish = false)
        {
            if (card == null)
                return false;

            BattlePlayer owner = GetPlayer(card.OwnerID);

            if (owner == null)
                return false;

            bool wasOnField = card.Zone == CardZone.Field;

            if (!owner.RemoveCard(card))
                return false;

            if (banish)
            {
                owner.AddToZone(card, CardZone.Removed);

                Log(card.Data.CardName + " is banished");
            }
            else
            {
                owner.AddToZone(card, CardZone.Graveyard);

                Log(card.Data.CardName + " is destroyed");

                if (wasOnField)
                    Publish(TriggerType.Death, card: card, player: owner);
            }

            return true;
        }

        // ---------- ความเสียหาย / การรักษา ----------

        /// <summary>
        /// ทำความเสียหายกับยูนิต จัดการ Shield / Poison / Lifesteal และ Trigger
        /// source เป็น null ได้ (เช่น Burn) คืนค่าความเสียหายที่ทำได้จริง
        /// </summary>
        public int DamageUnit(
            CardInstance source,
            CardInstance target,
            int amount,
            bool trueDamage = false)
        {
            if (target == null || amount <= 0)
                return 0;

            if (target.Zone != CardZone.Field)
                return 0;

            if (!trueDamage && target.HasKeyword(Keyword.Shield))
            {
                target.RemoveKeyword(Keyword.Shield);

                Log(target.Data.CardName + "'s shield absorbs the damage");

                return 0;
            }

            target.CurrentHealth -= amount;

            if (target.CurrentHealth < 0)
                target.CurrentHealth = 0;

            Log(target.Data.CardName + " takes " + amount + " damage");

            // Poison: ยูนิตที่โดนความเสียหายจริงจะตาย
            if (source != null && source.HasKeyword(Keyword.Poison))
                target.CurrentHealth = 0;

            // Lifesteal: ฟื้น HP ให้ Leader เจ้าของผู้ทำความเสียหาย
            if (source != null && source.HasKeyword(Keyword.Lifesteal))
                HealLeader(source, GetPlayer(source.OwnerID), amount);

            Publish(
                TriggerType.DamageTaken,
                card: target,
                player: GetPlayer(target.OwnerID),
                target: source,
                amount: amount);

            if (source != null)
            {
                Publish(
                    TriggerType.Hit,
                    card: source,
                    player: GetPlayer(source.OwnerID),
                    target: target,
                    targetPlayer: GetPlayer(target.OwnerID),
                    amount: amount);
            }

            return amount;
        }

        public int DamageLeader(
            CardInstance source,
            BattlePlayer target,
            int amount,
            bool trueDamage = false)
        {
            if (target == null || amount <= 0)
                return 0;

            target.LeaderHealth -= amount;

            if (target.LeaderHealth < 0)
                target.LeaderHealth = 0;

            Log(target.Name + " takes " + amount + " damage");

            if (source != null && source.HasKeyword(Keyword.Lifesteal))
                HealLeader(source, GetPlayer(source.OwnerID), amount);

            Publish(
                TriggerType.DamageTaken,
                player: target,
                target: source,
                amount: amount);

            if (source != null)
            {
                Publish(
                    TriggerType.Hit,
                    card: source,
                    player: GetPlayer(source.OwnerID),
                    targetPlayer: target,
                    amount: amount);
            }

            return amount;
        }

        public int HealUnit(CardInstance source, CardInstance target, int amount)
        {
            if (target == null || amount <= 0)
                return 0;

            int before = target.CurrentHealth;

            target.CurrentHealth = Mathf.Min(
                target.CurrentHealth + amount,
                target.MaxHealth);

            int healed = target.CurrentHealth - before;

            if (healed <= 0)
                return 0;

            Log(target.Data.CardName + " heals " + healed);

            Publish(
                TriggerType.Heal,
                card: target,
                player: GetPlayer(target.OwnerID),
                target: source,
                amount: healed);

            return healed;
        }

        public int HealLeader(CardInstance source, BattlePlayer target, int amount)
        {
            if (target == null || amount <= 0)
                return 0;

            int before = target.LeaderHealth;

            target.LeaderHealth = Mathf.Min(
                target.LeaderHealth + amount,
                target.MaxLeaderHealth);

            int healed = target.LeaderHealth - before;

            if (healed > 0)
                Log(target.Name + " heals " + healed);

            return healed;
        }

        // ---------- Discover ----------

        /// <summary>
        /// ผู้เล่นมนุษย์: รอ UI ให้เลือก / AI: สุ่มเลือกทันที
        /// </summary>
        public void RequestDiscover(BattlePlayer player, DiscoverResult result)
        {
            if (player == null || result == null || result.Options.Count == 0)
                return;

            if (IsAI(player))
            {
                CardData pick =
                    result.Options[UnityEngine.Random.Range(0, result.Options.Count)];

                result.Select(pick);

                DiscoverExecutor.Execute(result, player);

                Log(player.Name + " discovers a card");

                Touch();

                return;
            }

            PendingDiscover = new DiscoverRequest
            {
                Player = player,
                Result = result
            };

            Touch();
        }

        public bool ResolveDiscover(CardData chosen)
        {
            if (PendingDiscover == null || chosen == null)
                return false;

            PendingDiscover.Result.Select(chosen);

            bool added =
                DiscoverExecutor.Execute(
                    PendingDiscover.Result,
                    PendingDiscover.Player);

            Log(PendingDiscover.Player.Name + " discovers " + chosen.CardName);

            PendingDiscover = null;

            Resolve();

            return added;
        }

        // ---------- ตรวจผล ----------

        /// <summary>
        /// เก็บกวาดยูนิตที่ตาย + ตรวจแพ้ชนะ เรียกหลังทุก Command
        /// </summary>
        public void Resolve()
        {
            int guard = 0;

            while (SweepDeaths() && guard++ < 100)
            {
            }

            CheckVictory();

            Touch();
        }

        private bool SweepDeaths()
        {
            bool any = false;

            BattlePlayer[] players = { Player1, Player2 };

            foreach (BattlePlayer player in players)
            {
                foreach (CardInstance card in
                    new List<CardInstance>(player.Field))
                {
                    if (card.CurrentHealth > 0)
                        continue;

                    if (KillUnit(card))
                        any = true;
                }
            }

            return any;
        }

        private void CheckVictory()
        {
            if (IsOver || !HasStarted)
                return;

            bool p1Dead = Player1.LeaderHealth <= 0;
            bool p2Dead = Player2.LeaderHealth <= 0;

            if (!p1Dead && !p2Dead)
                return;

            BattlePlayer winner = null;

            if (p1Dead && !p2Dead)
                winner = Player2;
            else if (p2Dead && !p1Dead)
                winner = Player1;

            EndBattle(winner);
        }

        public void EndBattle(BattlePlayer winner)
        {
            if (IsOver)
                return;

            IsOver = true;
            Winner = winner;

            PendingDiscover = null;

            Log(winner == null
                ? "The battle ends in a draw"
                : winner.Name + " wins!");

            Publish(TriggerType.BattleEnd, player: winner);

            if (BattleEnded != null)
                BattleEnded(winner);

            Touch();
        }
    }
}
