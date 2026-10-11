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

        private const int FirstOpeningHand = 3;
        private const int SecondOpeningHand = 4;

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

        public AdaptRequest PendingAdapt { get; private set; }

        public bool HasPendingAdapt
        {
            get { return PendingAdapt != null; }
        }

        public TargetRequest PendingTarget { get; private set; }

        public bool HasPendingTarget
        {
            get { return PendingTarget != null; }
        }

        /// <summary>มีตัวเลือกที่ผู้เล่นต้องเลือกก่อน (Discover, Adapt หรือเลือกเป้าหมาย)</summary>
        public bool HasPendingChoice
        {
            get
            {
                return PendingDiscover != null ||
                       PendingAdapt != null ||
                       PendingTarget != null;
            }
        }

        // เอฟเฟกต์ที่กำลังทำงานอยู่ (ตั้งโดย CardActionResolver) null = ไม่ใช่เอฟเฟกต์ เช่น การต่อสู้
        public CardInstance ActiveEffectSource;
        public BattlePlayer ActiveEffectPlayer;

        private readonly System.Random adaptRng = new System.Random();

        public event Action<BattlePlayer> BattleEnded;

        private TriggerManager triggerManager;

        // ---------- Unity ----------

        private void Awake()
        {
            // ตัวสำรอง: ถ้าซีนนี้ยังไม่มีหน้าจอต่อสู้ ให้สร้าง
            MultiverseCardBattle.Screens.ScreenBootstrap.AttachToActiveScene();

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

            // โยนเหรียญ: ผู้เล่นที่เริ่มหลังได้จั่วเพิ่ม 1 ใบ และได้การ์ด Coin (+1 Energy)
            int firstId = 1;

            if (BattleSetup.UseCoinFlip)
            {
                firstId = BattleSetup.FirstPlayerOverride == 1 ||
                          BattleSetup.FirstPlayerOverride == 2
                    ? BattleSetup.FirstPlayerOverride
                    : (UnityEngine.Random.value < 0.5f ? 1 : 2);
            }

            FirstPlayerID = firstId;
            CoinFlipped = BattleSetup.UseCoinFlip;

            BattlePlayer first = GetPlayer(firstId);
            BattlePlayer second = GetOpponent(first);

            if (CoinFlipped)
                Log("Coin flip: " + first.Name + " goes first");

            for (int i = 0; i < FirstOpeningHand; i++)
                DrawCard(first);

            for (int i = 0; i < SecondOpeningHand; i++)
                DrawCard(second);

            if (CoinFlipped)
                GiveCoin(second);

            Publish(TriggerType.BattleStart);

            TurnManager.StartBattle(firstId);
        }

        // ผู้เล่นที่เริ่มก่อน และว่ามีการโยนเหรียญหรือไม่ (ให้ UI แสดงผล)
        public int FirstPlayerID { get; private set; } = 1;
        public bool CoinFlipped { get; private set; }

        // เหตุการณ์สุ่ม: ผู้เล่นได้การ์ดลับจากการ Summon ในการต่อสู้นี้
        public bool SecretCardSummoned { get; set; }

        // สถิติระหว่างแมตช์ (ใช้ปลดล็อกการ์ดลับ)
        public readonly BattleStats Stats = new BattleStats();

        // ID ของการ์ดลับทั้งหมดที่ผู้เล่นได้ในการต่อสู้นี้
        public readonly List<string> SecretsSummoned = new List<string>();

        private void GiveCoin(BattlePlayer player)
        {
            CardData coin = StarterCardLibrary.Get(StarterCardLibrary.CoinId);

            if (coin == null)
                return;

            player.AddToZone(new CardInstance(coin), CardZone.Hand);
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

            Stats.Init(Player1, Player2);
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

        /// <summary>จบเทิร์นของเจ้าของ: อุปกรณ์ Leader เหลือเทิร์นน้อยลง หมดแล้วลงสุสาน</summary>
        public void TickLeaderGear(BattlePlayer player)
        {
            if (player == null || player.LeaderGear == null)
                return;

            player.LeaderGearTurns--;

            if (player.LeaderGearTurns > 0)
                return;

            Log(player.LeaderGear.Data.CardName + " is used up");

            player.AddToZone(player.LeaderGear, CardZone.Graveyard);

            player.LeaderGear = null;
            player.LeaderGearTurns = 0;

            Touch();
        }

        // ---------- จั่ว / ทิ้ง ----------

        public CardInstance DrawCard(BattlePlayer player)
        {
            if (player == null)
                return null;

            // เด็คหมด = แพ้ทันที (ตัดสินใน CheckVictory) จึงไม่มีอะไรให้จั่ว
            if (player.Deck.Count == 0)
                return null;

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
            card.TurnsOnField = 0;

            // สถานะ Cost ใช้กับการ์ดในมือเท่านั้น
            card.RemoveStatus(StatusType.CostUp);
            card.RemoveStatus(StatusType.CostDown);

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

            // Last Guardian: ครั้งแรกในแมตช์ที่กำลังจะตาย เหลือ HP 1 แต่เสีย Taunt ชั่วคราว
            if (!banish && wasOnField &&
                card.HasPassive(CardPassive.LastGuardian) &&
                !card.LastGuardianUsed)
            {
                card.LastGuardianUsed = true;
                card.CurrentHealth = 1;

                if (card.HasKeyword(Keyword.Taunt))
                {
                    card.RemoveKeyword(Keyword.Taunt);
                    card.TauntRestoreTurn = TurnManager.TotalTurns + 1;
                }

                Log(card.Data.CardName + " refuses to fall (1 HP)");

                return true;
            }

            // Reborn: ตายครั้งแรกแล้วกลับมาบนสนามด้วย 1 HP (ความสามารถ Death ยังทำงาน)
            if (!banish && wasOnField && card.HasKeyword(Keyword.Reborn))
            {
                card.RemoveKeyword(Keyword.Reborn);
                card.Statuses.Clear();

                card.CurrentHealth = 1;

                if (card.MaxHealth < 1)
                    card.MaxHealth = 1;

                card.AttacksThisTurn = 0;
                card.SummonedOnTurn = TurnManager.TotalTurns;

                Log(card.Data.CardName + " is reborn with 1 HP");

                Publish(TriggerType.Death, card: card, player: owner);

                return true;
            }

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
                {
                    Stats.AddUnitDeath(owner.PlayerID);
                    Stats.AddUnitKilled(owner.PlayerID == 1 ? 2 : 1);

                    RegisterEffectDeath(card, owner);

                    Publish(TriggerType.Death, card: card, player: owner);
                }
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

            // Overwhelming Power: ความเสียหายจากการโจมตีของยูนิตนี้ไม่ถูกลดด้วย Immune / Defense Up / Weakness
            bool overwhelming =
                source != null &&
                ActiveEffectSource == null &&
                source.HasPassive(CardPassive.OverwhelmingPower);

            // Immune: ลดความเสียหายทุกครั้งที่โดน (เช่น Immune 3 โดน 5 เหลือ 2)
            int immune = overwhelming ? 0 : target.ImmuneAmount;

            if (immune > 0)
            {
                amount -= immune;

                if (amount <= 0)
                {
                    Log(target.Data.CardName + " is immune");

                    return 0;
                }

                Log(target.Data.CardName + " blocks " + immune + " damage");
            }

            bool exposed = target.HasStatus(StatusType.Exposed);

            if (!trueDamage && !exposed && target.HasKeyword(Keyword.Shield))
            {
                target.RemoveKeyword(Keyword.Shield);

                Log(target.Data.CardName + "'s shield absorbs the damage");

                return 0;
            }

            if (!trueDamage)
            {
                // ผู้โจมตีอ่อนแรง: สร้างความเสียหายลดลงครึ่งหนึ่ง
                if (source != null && !overwhelming &&
                    source.HasStatus(StatusType.Weakness))
                    amount = Mathf.Max(1, amount / 2);

                // เป้าหมายเปราะบาง: ได้รับเพิ่ม 50% (ปัดขึ้น)
                if (target.HasStatus(StatusType.Vulnerable))
                    amount += (amount + 1) / 2;

                amount += target.StatusValue(StatusType.DefenseDown);

                if (!exposed)
                {
                    int defense = overwhelming
                        ? 0
                        : target.StatusValue(StatusType.DefenseUp);

                    if (defense > 0)
                        amount = Mathf.Max(1, amount - defense);

                    amount = AbsorbWithBarrier(target, amount);

                    if (amount <= 0)
                        return 0;
                }
            }

            target.CurrentHealth -= amount;

            if (target.CurrentHealth < 0)
                target.CurrentHealth = 0;

            target.DamagedSinceTurnStart = true;

            // จดว่าดาเมจนี้มาจากเอฟเฟกต์ของใคร (ไม่ใช่การต่อสู้) ไว้ตัดสินสาเหตุการตาย
            target.LastEffectPlayerID =
                ActiveEffectPlayer != null ? ActiveEffectPlayer.PlayerID : 0;
            target.LastEffectSource = ActiveEffectSource;

            Log(target.Data.CardName + " takes " + amount + " damage");

            // Backstab: ยูนิตที่โดนความเสียหายจริงจะตาย
            if (source != null && source.HasKeyword(Keyword.Backstab))
                target.CurrentHealth = 0;

            // The Undead Overlord: ครั้งแรกที่จะตายจากความเสียหาย เหลือ HP 1 + Unstoppable ชั่วคราว
            // (ไม่กันการถูกทำลายโดยตรง เช่น Doom / Destroy ซึ่งไม่ผ่านที่นี่)
            if (target.CurrentHealth <= 0 &&
                target.HasPassive(CardPassive.UndeadOverlord) &&
                !target.UndeadOverlordUsed)
            {
                target.UndeadOverlordUsed = true;
                target.CurrentHealth = 1;

                if (!target.HasKeyword(Keyword.Unstoppable))
                {
                    target.AddKeyword(Keyword.Unstoppable);
                    target.TempUnstoppableGranted = true;
                }

                // จนจบ "เทิร์นของเรา" ถัดไป
                bool ownTurn = TurnManager.CurrentPlayerID == target.OwnerID;

                target.TempUnstoppableUntil =
                    TurnManager.TotalTurns + (ownTurn ? 2 : 1);

                Log(target.Data.CardName + " rises again (1 HP)");
            }

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

        // Barrier ดูดซับความเสียหายตามค่าที่เหลือ คืนความเสียหายที่ทะลุเข้าไป
        private int AbsorbWithBarrier(CardInstance target, int amount)
        {
            StatusEffect barrier = target.GetStatus(StatusType.Barrier);

            if (barrier == null)
                return amount;

            int absorbed = Mathf.Min(barrier.Value, amount);

            barrier.Value -= absorbed;
            amount -= absorbed;

            Log(target.Data.CardName + "'s barrier absorbs " + absorbed);

            if (barrier.Value <= 0)
                target.Statuses.Remove(barrier);

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

            if (!trueDamage && source != null &&
                ActiveEffectSource == null &&
                !source.HasPassive(CardPassive.OverwhelmingPower) &&
                source.HasStatus(StatusType.Weakness))
                amount = Mathf.Max(1, amount / 2);

            target.LeaderHealth -= amount;

            if (target.LeaderHealth < 0)
                target.LeaderHealth = 0;

            Stats.NoteLeaderHealth(target);
            Stats.AddLeaderDamage(target.PlayerID == 1 ? 2 : 1, amount);

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

            if (target.HasStatus(StatusType.Curse))
            {
                Log(target.Data.CardName + " is cursed and cannot be healed");

                return 0;
            }

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

        // ---------- Adapt ----------

        /// <summary>
        /// ผู้เล่นมนุษย์: รอ UI ให้เลือก / AI: สุ่มเลือกทันที
        /// </summary>
        public void RequestAdapt(
            BattlePlayer player,
            CardInstance card,
            int optionCount)
        {
            if (player == null || card == null)
                return;

            List<AdaptOption> options = AdaptOption.Roll(
                optionCount <= 0 ? 3 : optionCount, adaptRng);

            if (options.Count == 0)
                return;

            if (IsAI(player))
            {
                ApplyAdapt(card, options[adaptRng.Next(options.Count)]);

                Touch();

                return;
            }

            PendingAdapt = new AdaptRequest
            {
                Player = player,
                Card = card,
                Options = options
            };

            Touch();
        }

        public bool ResolveAdapt(int index)
        {
            if (PendingAdapt == null)
                return false;

            if (index < 0 || index >= PendingAdapt.Options.Count)
                return false;

            ApplyAdapt(PendingAdapt.Card, PendingAdapt.Options[index]);

            PendingAdapt = null;

            Resolve();

            return true;
        }

        private void ApplyAdapt(CardInstance card, AdaptOption option)
        {
            if (card == null || option == null)
                return;

            if (option.Attack != 0)
                card.CurrentAttack += option.Attack;

            if (option.Health != 0)
            {
                card.MaxHealth += option.Health;
                card.CurrentHealth += option.Health;
            }

            if (option.Keyword != Keyword.None)
                card.AddKeyword(option.Keyword);

            Log(card.Data.CardName + " adapts: " + option.Label);
        }

        // ---------- สาเหตุการตาย / Dark Wisdom ----------

        /// <summary>จดว่ายูนิตนี้ถูกเอฟเฟกต์ของใครทำลาย (ก่อนเรียก KillUnit)</summary>
        public void NoteEffectKill(
            CardInstance target,
            CardInstance source,
            BattlePlayer sourcePlayer)
        {
            if (target == null)
                return;

            target.LastEffectSource = source;
            target.LastEffectPlayerID =
                sourcePlayer != null ? sourcePlayer.PlayerID : 0;
        }

        // ยูนิตตายบนสนาม: ถ้าเอฟเฟกต์ของศัตรูเป็นต้นเหตุ นับสถิติและให้ Dark Wisdom
        private void RegisterEffectDeath(CardInstance dead, BattlePlayer owner)
        {
            int killerId = dead.LastEffectPlayerID;
            CardInstance source = dead.LastEffectSource;

            dead.LastEffectPlayerID = 0;
            dead.LastEffectSource = null;

            if (killerId == 0 || killerId == owner.PlayerID)
                return;

            Stats.AddEffectKill(killerId);

            BattlePlayer killer = GetPlayer(killerId);

            if (killer == null || source == null || source.Data == null)
                return;

            bool bySpell = source.Data.Type == CardType.Spell;
            bool byAinz = (source.Data.Passives & CardPassive.DarkWisdom) != 0;

            if (!bySpell && !byAinz)
                return;

            foreach (CardInstance unit in killer.Field)
            {
                if (!unit.HasPassive(CardPassive.DarkWisdom))
                    continue;

                if (killer.DarkWisdomEnergyThisTurn >= DarkWisdomCap ||
                    killer.CurrentEnergy >= MaxEnergyCap)
                    return;

                killer.DarkWisdomEnergyThisTurn++;
                killer.CurrentEnergy++;

                Log(unit.Data.CardName + " drinks in the death (+1 Energy)");

                return;
            }
        }

        private const int ConcentrationBonus = 2;
        private const int SinDevilHeal = 3;
        private const int DarkWisdomCap = 3;
        private const int MaxEnergyCap = 10;

        // ---------- ความสามารถต้นเทิร์น (Doom / Grasp Heart) ----------

        /// <summary>
        /// เรียกตอนเริ่มเทิร์นของ player: ทำลายยูนิตที่ติด Doom ของ player นี้
        /// แล้วให้ยูนิตที่มี Grasp Heart เลือกเป้าหมาย (มนุษย์คลิกเลือก, AI สุ่ม)
        /// </summary>
        public void OnTurnStartPassives(BattlePlayer player)
        {
            if (player == null || IsOver)
                return;

            ResolveDoom(player);

            foreach (CardInstance unit in new List<CardInstance>(player.Field))
            {
                if (unit.Zone != CardZone.Field)
                    continue;

                // Concentration: ไม่โดนดาเมจตั้งแต่ต้นเทิร์นก่อน ATK +2 จนจบเทิร์น
                if (unit.HasPassive(CardPassive.Concentration) &&
                    !unit.DamagedSinceTurnStart)
                {
                    unit.TurnAttackBonus += ConcentrationBonus;

                    Log(unit.Data.CardName + " concentrates (+" + ConcentrationBonus + " Attack)");
                }

                unit.DamagedSinceTurnStart = false;

                // Sin Devil's Regeneration
                if (unit.HasPassive(CardPassive.SinDevilRegeneration))
                    HealUnit(null, unit, SinDevilHeal);
            }

            foreach (CardInstance unit in new List<CardInstance>(player.Field))
            {
                if (unit.Zone != CardZone.Field ||
                    !unit.HasPassive(CardPassive.GraspHeart) ||
                    unit.IsStunned)
                    continue;

                RequestGraspHeart(player, unit);

                if (PendingTarget != null)
                    return;
            }
        }

        private void ResolveDoom(BattlePlayer caster)
        {
            BattlePlayer[] players = { Player1, Player2 };

            foreach (BattlePlayer holder in players)
            {
                foreach (CardInstance unit in new List<CardInstance>(holder.Field))
                {
                    StatusEffect doom = unit.GetStatus(StatusType.Doom);

                    if (doom == null || doom.Value != caster.PlayerID)
                        continue;

                    unit.Statuses.Remove(doom);

                    Log(unit.Data.CardName + " is claimed by Doom");

                    CardInstance ainz = null;

                    foreach (CardInstance candidate in caster.Field)
                    {
                        if (candidate.HasPassive(CardPassive.DarkWisdom))
                        {
                            ainz = candidate;
                            break;
                        }
                    }

                    NoteEffectKill(unit, ainz, caster);

                    // ทำลายโดยตรง ไม่ผ่านความเสียหาย / Shield
                    KillUnit(unit);
                }
            }
        }

        private void RequestGraspHeart(BattlePlayer player, CardInstance source)
        {
            BattlePlayer enemy = GetOpponent(player);

            List<CardInstance> candidates = new List<CardInstance>();

            foreach (CardInstance unit in enemy.Field)
            {
                if (unit.Zone == CardZone.Field && !unit.IsHidden)
                    candidates.Add(unit);
            }

            if (candidates.Count == 0)
                return;

            if (IsAI(player))
            {
                ApplyGraspHeart(
                    player, source,
                    candidates[UnityEngine.Random.Range(0, candidates.Count)]);

                return;
            }

            PendingTarget = new TargetRequest
            {
                Player = player,
                Source = source,
                Candidates = candidates,
                Prompt = source.Data.CardName + ": choose an enemy unit"
            };

            Touch();
        }

        public bool ResolvePendingTarget(CardInstance chosen)
        {
            if (PendingTarget == null || chosen == null)
                return false;

            if (!PendingTarget.Candidates.Contains(chosen) ||
                chosen.Zone != CardZone.Field)
                return false;

            TargetRequest request = PendingTarget;

            PendingTarget = null;

            ApplyGraspHeart(request.Player, request.Source, chosen);

            Resolve();

            return true;
        }

        private void ApplyGraspHeart(
            BattlePlayer player,
            CardInstance source,
            CardInstance target)
        {
            CardInstance previousSource = ActiveEffectSource;
            BattlePlayer previousPlayer = ActiveEffectPlayer;

            ActiveEffectSource = source;
            ActiveEffectPlayer = player;

            if (target.HasKeyword(Keyword.Ward))
            {
                target.RemoveKeyword(Keyword.Ward);

                Log(target.Data.CardName + "'s ward blocks the effect");
            }
            else
            {
                Log(source.Data.CardName + " crushes " + target.Data.CardName);

                DamageUnit(source, target, GraspHeartDamage);

                // ตายจากเอฟเฟกต์นี้: ดาเมจ 3 ให้ยูนิตศัตรูอื่นสุ่ม 2 ใบ
                if (target.CurrentHealth <= 0)
                {
                    List<CardInstance> others = new List<CardInstance>();

                    foreach (CardInstance unit in GetOpponent(player).Field)
                    {
                        if (unit != target && unit.CurrentHealth > 0 && !unit.IsHidden)
                            others.Add(unit);
                    }

                    for (int i = 0; i < GraspHeartSpread && others.Count > 0; i++)
                    {
                        int index = UnityEngine.Random.Range(0, others.Count);

                        DamageUnit(source, others[index], GraspHeartSpreadDamage);

                        others.RemoveAt(index);
                    }
                }
            }

            ActiveEffectSource = previousSource;
            ActiveEffectPlayer = previousPlayer;
        }

        private const int GraspHeartDamage = 4;
        private const int GraspHeartSpread = 2;
        private const int GraspHeartSpreadDamage = 3;

        // ---------- Aura / ตัวปรับค่าจากสถานะ ----------

        /// <summary>
        /// คำนวณ Aura และสถานะที่บวกลบค่า (AttackUp/Down, Fatigue, CostUp/Down) ใหม่
        /// ใช้วิธีเก็บ "ค่าที่บวกไว้แล้ว" ในการ์ดแล้วปรับเฉพาะส่วนต่าง
        /// จึงไม่ทับบัฟถาวรจาก Effect อื่น เรียกหลังทุก Command ใน Resolve()
        /// </summary>
        public void RefreshModifiers()
        {
            BattlePlayer[] players = { Player1, Player2 };

            foreach (BattlePlayer player in players)
            {
                int totalAttack = 0;
                int totalHealth = 0;

                foreach (CardInstance card in player.Field)
                {
                    if (!IsAuraSource(card))
                        continue;

                    totalAttack += card.Data.AuraAttack;
                    totalHealth += card.Data.AuraHealth;
                }

                foreach (CardInstance card in player.Field)
                {
                    int auraAttack = totalAttack;
                    int auraHealth = totalHealth;

                    // Aura ให้ "ยูนิตอื่น" ไม่รวมตัวเอง
                    if (IsAuraSource(card))
                    {
                        auraAttack -= card.Data.AuraAttack;
                        auraHealth -= card.Data.AuraHealth;
                    }

                    int attackMod =
                        auraAttack +
                        card.TurnAttackBonus +
                        card.StatusValue(StatusType.AttackUp) -
                        card.StatusValue(StatusType.AttackDown) -
                        card.StatusValue(StatusType.TransformationFatigue);

                    ApplyAttackMod(card, attackMod);
                    ApplyHealthMod(card, auraHealth);
                }

                RefreshShadowbound(player);

                int spellDiscountFirst = 0;

                foreach (CardInstance caster in player.Field)
                {
                    if (caster.HasPassive(CardPassive.SupremeMagicCaster))
                    {
                        spellDiscountFirst = 1;
                        break;
                    }
                }

                foreach (CardInstance card in player.Hand.Cards)
                {
                    int desired =
                        card.StatusValue(StatusType.CostUp) -
                        card.StatusValue(StatusType.CostDown);

                    // Supreme Magic Caster: Spell ใบแรกของเทิร์นลด 2 ใบที่สองลด 1
                    if (spellDiscountFirst > 0 &&
                        card.Data != null &&
                        card.Data.Type == CardType.Spell)
                    {
                        if (player.SpellsPlayedThisTurn == 0)
                            desired -= 2;
                        else if (player.SpellsPlayedThisTurn == 1)
                            desired -= 1;
                    }

                    int baseCost = card.CurrentCost - card.AppliedCostMod;
                    int newCost = Mathf.Max(0, baseCost + desired);

                    card.CurrentCost = newCost;
                    card.AppliedCostMod = newCost - baseCost;
                }
            }
        }

        // Shadowbound: มี Token ของตัวเองบนสนาม = ได้ Unstoppable + ถูกโจมตีไม่ได้
        private static void RefreshShadowbound(BattlePlayer player)
        {
            bool hasToken = false;

            foreach (CardInstance other in player.Field)
            {
                if (other.Data != null && (other.Data.Tags & CardTag.Token) != 0)
                {
                    hasToken = true;
                    break;
                }
            }

            foreach (CardInstance card in player.Field)
            {
                bool active = hasToken && card.HasKeyword(Keyword.Shadowbound);

                card.ShadowActive = active;

                if (active && !card.HasKeyword(Keyword.Unstoppable))
                {
                    card.AddKeyword(Keyword.Unstoppable);
                    card.GrantedUnstoppable = true;
                }
                else if (!active && card.GrantedUnstoppable)
                {
                    card.RemoveKeyword(Keyword.Unstoppable);
                    card.GrantedUnstoppable = false;
                }
            }
        }

        private static bool IsAuraSource(CardInstance card)
        {
            return card != null &&
                   card.Data != null &&
                   !card.IsSilenced &&
                   (card.Data.AuraAttack != 0 || card.Data.AuraHealth != 0);
        }

        private static void ApplyAttackMod(CardInstance card, int desired)
        {
            int baseAttack = Mathf.Max(0, card.CurrentAttack - card.AppliedAttackMod);

            if (desired < -baseAttack)
                desired = -baseAttack;

            card.CurrentAttack = baseAttack + desired;
            card.AppliedAttackMod = desired;
        }

        private static void ApplyHealthMod(CardInstance card, int desired)
        {
            int delta = desired - card.AppliedHealthMod;

            if (delta == 0)
                return;

            card.MaxHealth += delta;

            if (card.MaxHealth < 1)
                card.MaxHealth = 1;

            // ได้ HP เพิ่มจาก Aura ทันที (ยกเว้นยูนิตที่ตายแล้วรอเก็บกวาด)
            if (delta > 0 && card.CurrentHealth > 0)
                card.CurrentHealth += delta;

            if (card.CurrentHealth > card.MaxHealth)
                card.CurrentHealth = card.MaxHealth;

            card.AppliedHealthMod = desired;
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

            RefreshModifiers();

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

            // เด็คเหลือ 0 ใบเมื่อไหร่ก็แพ้ทันที
            if (Player1.Deck.Count == 0 && !p1Dead)
            {
                Log(Player1.Name + " has no cards left in the deck!");
                p1Dead = true;
            }

            if (Player2.Deck.Count == 0 && !p2Dead)
            {
                Log(Player2.Name + " has no cards left in the deck!");
                p2Dead = true;
            }

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
            PendingAdapt = null;
            PendingTarget = null;

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
