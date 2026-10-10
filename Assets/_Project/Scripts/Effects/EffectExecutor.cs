using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Effects
{
    /// <summary>
    /// ทำ Effect 1 รายการกับเป้าหมาย 1 อย่าง
    ///
    /// ถ้า context.Battle มีค่า จะทำผ่าน BattleManager เพื่อให้ Trigger / Keyword /
    /// การตาย / Log ทำงานครบ ถ้าไม่มี (เช่นในเทสต์เดี่ยว) จะทำแบบพื้นฐานเหมือนเดิม
    /// </summary>
    public static class EffectExecutor
    {
        private const int DefaultStatusDuration = 3;

        public static void Execute(
            EffectData effect,
            EffectContext context)
        {
            if (effect == null)
                return;

            if (context == null)
                return;

            switch (effect.Type)
            {
                // Damage
                case EffectType.DealDamage:
                case EffectType.DealTrueDamage:
                    DealDamage(effect, context);
                    break;

                // Heal
                case EffectType.Heal:
                    Heal(effect, context);
                    break;

                // Stats
                case EffectType.GainAttack:
                    ModifyAttack(effect, context);
                    break;

                case EffectType.GainHealth:
                    ModifyHealth(effect, context);
                    break;

                case EffectType.SetAttack:
                    SetAttack(effect, context);
                    break;

                case EffectType.SetHealth:
                    SetHealth(effect, context);
                    break;

                // Cost
                case EffectType.ReduceCost:
                    ModifyCost(effect, context, -effect.Value);
                    break;

                case EffectType.IncreaseCost:
                    ModifyCost(effect, context, effect.Value);
                    break;

                case EffectType.SetCost:
                    SetCost(effect, context);
                    break;

                // Card
                case EffectType.DrawCard:
                    DrawCards(effect, context);
                    break;

                case EffectType.DiscardCard:
                    DiscardCard(effect, context);
                    break;

                case EffectType.DestroyCard:
                    DestroyCard(effect, context);
                    break;

                case EffectType.ReturnToHand:
                    ReturnToHand(effect, context);
                    break;

                case EffectType.MoveToDeck:
                    MoveToDeck(effect, context);
                    break;

                case EffectType.Banish:
                    Banish(effect, context);
                    break;

                case EffectType.Summon:
                    Summon(effect, context);
                    break;

                case EffectType.CreateCard:
                    CreateCard(effect, context);
                    break;

                case EffectType.Discover:
                    Discover(effect, context);
                    break;

                case EffectType.Transform:
                    Transform(effect, context);
                    break;

                // Keywords
                case EffectType.AddKeyword:
                    AddKeyword(effect, context);
                    break;

                case EffectType.RemoveKeyword:
                    RemoveKeyword(effect, context);
                    break;

                // Status
                case EffectType.Silence:
                    ApplyStatus(context, StatusType.Silence, 0,
                        StatusEffect.Permanent);
                    break;

                case EffectType.Stun:
                    ApplyStatus(context, StatusType.Stun, 0,
                        PositiveOr(effect.Duration, PositiveOr(effect.Value, 1)));
                    break;

                case EffectType.Burn:
                    ApplyStatus(context, StatusType.Burn,
                        effect.Value,
                        PositiveOr(effect.Duration, DefaultStatusDuration));
                    break;

                case EffectType.Regen:
                    ApplyStatus(context, StatusType.Regen,
                        effect.Value,
                        PositiveOr(effect.Duration, DefaultStatusDuration));
                    break;

                case EffectType.ApplyStatus:
                    ApplyGenericStatus(effect, context);
                    break;

                case EffectType.RemoveStatus:
                    if (context.TargetCard != null)
                        context.TargetCard.RemoveStatus(effect.Status);
                    break;

                case EffectType.Adapt:
                    Adapt(effect, context);
                    break;

                case EffectType.GrantExtraAttack:
                    if (context.TargetCard != null)
                        context.TargetCard.ExtraAttacks += PositiveOr(effect.Value, 1);
                    break;

                // Resource
                case EffectType.GainEnergy:
                    GainEnergy(effect, context);
                    break;

                case EffectType.SpendEnergy:
                    SpendEnergy(effect, context);
                    break;
            }
        }

        private const int MaxEnergyCap = 10;

        private static int PositiveOr(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        // ---------- Damage / Heal ----------

        private static void DealDamage(
            EffectData effect,
            EffectContext context)
        {
            bool trueDamage = effect.Type == EffectType.DealTrueDamage;

            if (context.TargetCard != null)
            {
                if (context.Battle != null)
                {
                    int dealt = context.Battle.DamageUnit(
                        context.SourceCard,
                        context.TargetCard,
                        effect.Value,
                        trueDamage);

                    if (dealt > 0 && effect.HealOwnerPerHit > 0 &&
                        context.SourcePlayer != null)
                    {
                        context.Battle.HealLeader(
                            null, context.SourcePlayer, effect.HealOwnerPerHit);
                    }

                    return;
                }

                context.TargetCard.CurrentHealth -= effect.Value;

                if (context.TargetCard.CurrentHealth < 0)
                    context.TargetCard.CurrentHealth = 0;

                return;
            }

            if (context.TargetPlayer != null)
            {
                if (context.Battle != null)
                {
                    context.Battle.DamageLeader(
                        context.SourceCard,
                        context.TargetPlayer,
                        effect.Value,
                        trueDamage);

                    return;
                }

                context.TargetPlayer.LeaderHealth -= effect.Value;

                if (context.TargetPlayer.LeaderHealth < 0)
                    context.TargetPlayer.LeaderHealth = 0;
            }
        }

        private static void Heal(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard != null)
            {
                if (context.Battle != null)
                {
                    context.Battle.HealUnit(
                        context.SourceCard,
                        context.TargetCard,
                        effect.Value);

                    return;
                }

                context.TargetCard.CurrentHealth += effect.Value;

                if (context.TargetCard.CurrentHealth >
                    context.TargetCard.MaxHealth)
                {
                    context.TargetCard.CurrentHealth =
                        context.TargetCard.MaxHealth;
                }

                return;
            }

            if (context.TargetPlayer != null)
            {
                if (context.Battle != null)
                {
                    context.Battle.HealLeader(
                        context.SourceCard,
                        context.TargetPlayer,
                        effect.Value);

                    return;
                }

                context.TargetPlayer.LeaderHealth += effect.Value;

                if (context.TargetPlayer.LeaderHealth >
                    context.TargetPlayer.MaxLeaderHealth)
                {
                    context.TargetPlayer.LeaderHealth =
                        context.TargetPlayer.MaxLeaderHealth;
                }
            }
        }

        // ---------- Stats ----------

        private static void ModifyAttack(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentAttack += effect.Value;

            if (context.TargetCard.CurrentAttack < 0)
                context.TargetCard.CurrentAttack = 0;
        }

        private static void ModifyHealth(
            EffectData effect,
            EffectContext context)
        {
            CardInstance card = context.TargetCard;

            if (card == null)
                return;

            // เพิ่ม/ลดทั้ง HP ปัจจุบันและ HP สูงสุด
            card.MaxHealth += effect.Value;

            if (card.MaxHealth < 1)
                card.MaxHealth = 1;

            card.CurrentHealth += effect.Value;

            if (card.CurrentHealth > card.MaxHealth)
                card.CurrentHealth = card.MaxHealth;
        }

        private static void SetAttack(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentAttack = effect.Value;
        }

        private static void SetHealth(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentHealth = effect.Value;
            context.TargetCard.MaxHealth = effect.Value;
        }

        // ---------- Cost ----------

        private static void ModifyCost(
            EffectData effect,
            EffectContext context,
            int amount)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentCost += amount;

            if (context.TargetCard.CurrentCost < 0)
                context.TargetCard.CurrentCost = 0;
        }

        private static void SetCost(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.CurrentCost = effect.Value;

            if (context.TargetCard.CurrentCost < 0)
                context.TargetCard.CurrentCost = 0;
        }

        // ---------- Card ----------

        private static void DrawCards(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            for (int i = 0; i < effect.Value; i++)
            {
                if (context.Battle != null)
                {
                    // จัดการ Fatigue / มือเต็ม / Trigger Draw ให้เอง
                    context.Battle.DrawCard(context.TargetPlayer);
                    continue;
                }

                CardInstance card =
                    context.TargetPlayer.Deck.Draw();

                if (card == null)
                    return;

                context.TargetPlayer.Hand.Add(card);
            }
        }

        private static void DiscardCard(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            CardInstance card =
                context.TargetPlayer.Hand.GetCard(0);

            if (card == null)
                return;

            if (context.Battle != null)
            {
                context.Battle.DiscardCard(context.TargetPlayer, card);
                return;
            }

            if (context.TargetPlayer.Hand.Remove(card))
            {
                card.Zone = CardZone.Graveyard;
                context.TargetPlayer.Graveyard.Add(card);
            }
        }

        private static bool IsUnitOnField(CardInstance card)
        {
            return card != null &&
                   card.Zone == CardZone.Field &&
                   card.OwnerID > 0;
        }

        private static void DestroyCard(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            // ยูนิตบนสนาม: ผ่าน BattleManager เพื่อยิง Trigger Death
            if (context.Battle != null && IsUnitOnField(context.TargetCard))
            {
                // นับเอฟเฟกต์ที่ทำลายยูนิตศัตรูทันที (ใช้ตรวจเงื่อนไขปลดล็อกการ์ดลับ)
                if (context.SourcePlayer != null &&
                    context.TargetCard.OwnerID != context.SourcePlayer.PlayerID)
                {
                    context.Battle.Stats.AddInstantDestroy(
                        context.SourcePlayer.PlayerID);
                }

                // จดว่าใครเป็นคนทำลาย (ใช้นับสถิติและ Dark Wisdom)
                context.Battle.NoteEffectKill(
                    context.TargetCard, context.SourceCard, context.SourcePlayer);

                context.Battle.KillUnit(context.TargetCard);
                return;
            }

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Graveyard);
        }

        // Root: ย้ายโซนไม่ได้
        private static bool IsRooted(EffectContext context)
        {
            if (context.TargetCard == null ||
                !context.TargetCard.HasStatus(StatusType.Root))
                return false;

            if (context.Battle != null)
            {
                context.Battle.Log(
                    context.TargetCard.Data.CardName + " is rooted");
            }

            return true;
        }

        private static void ReturnToHand(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (IsRooted(context))
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            // การ์ดที่กลับเข้ามือจะกลับเป็นค่าเริ่มต้น (บัฟ/สถานะหาย) และกลับร่างเดิม
            context.TargetCard.RestoreOriginalForm();
            context.TargetCard.ApplyBaseStats(context.TargetCard.Data);

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Hand);
        }

        private static void MoveToDeck(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (IsRooted(context))
                return;

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetCard.RestoreOriginalForm();
            context.TargetCard.ApplyBaseStats(context.TargetCard.Data);

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Deck);
        }

        private static void Banish(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            if (IsRooted(context))
                return;

            if (context.Battle != null && IsUnitOnField(context.TargetCard))
            {
                context.Battle.KillUnit(context.TargetCard, true);
                return;
            }

            if (context.TargetPlayer == null)
                return;

            if (!context.TargetPlayer.RemoveCard(
                context.TargetCard))
                return;

            context.TargetPlayer.AddToZone(
                context.TargetCard,
                CardZone.Removed);
        }

        /// <summary>
        /// Summon มี 2 แบบ
        ///  1) มี TargetCard: ย้ายการ์ดใบนั้นลงสนาม (พฤติกรรมเดิม)
        ///  2) ไม่มี TargetCard แต่ Effect.Card มีค่า: สร้างการ์ดใหม่ (เช่น Token)
        ///     ลงสนามให้ TargetPlayer
        /// </summary>
        private static void Summon(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            CardInstance card = context.TargetCard;

            if (card == null)
            {
                if (effect.Card == null)
                    return;

                CardData data = effect.Card;

                // เหตุการณ์สุ่ม: Token ที่ผู้เล่น Summon มีโอกาสกลายเป็นการ์ดลับ
                CardData secret = RollSecretCard(effect, context);

                if (secret != null)
                    data = secret;

                card = new CardInstance(data);
                card.OwnerID = context.TargetPlayer.PlayerID;
            }
            else
            {
                if (!context.TargetPlayer.RemoveCard(card))
                    return;
            }

            if (context.Battle != null)
            {
                context.Battle.SummonUnit(card, context.TargetPlayer);
                return;
            }

            if (context.TargetPlayer.Field.Count >= BattlePlayer.MaxFieldSize)
                return;

            context.TargetPlayer.AddToZone(
                card,
                CardZone.Field);
        }

        /// <summary>โอกาสที่ Summon (สร้างการ์ดใหม่) ของผู้เล่นจะได้การ์ดลับ</summary>
        public static float SecretCardChance = 0.05f;

        /// <summary>ตัวสุ่ม 0-1 (เทสต์แทนค่าได้)</summary>
        public static System.Func<float> RandomValue = () => UnityEngine.Random.value;

        private static CardData RollSecretCard(
            EffectData effect,
            EffectContext context)
        {
            if (context.Battle == null || context.TargetPlayer == null)
                return null;

            // เฉพาะฝั่งผู้เล่น (ไม่ใช่ AI)
            if (context.Battle.IsAI(context.TargetPlayer))
                return null;

            // การ์ดลับเรียกลูกน้อง ไม่ทอยซ้ำ
            if (effect.Card != null &&
                effect.Card.CardID == MultiverseCardBattle.Data.StarterCardLibrary.SecretId)
            {
                return null;
            }

            if (context.SourceCard != null &&
                context.SourceCard.Data != null &&
                context.SourceCard.Data.CardID == MultiverseCardBattle.Data.StarterCardLibrary.SecretId)
            {
                return null;
            }

            if (RandomValue() >= SecretCardChance)
                return null;

            CardData secret = MultiverseCardBattle.Data.StarterCardLibrary.Get(MultiverseCardBattle.Data.StarterCardLibrary.SecretId);

            if (secret != null)
            {
                context.Battle.SecretCardSummoned = true;

                if (!context.Battle.SecretsSummoned.Contains(secret.CardID))
                    context.Battle.SecretsSummoned.Add(secret.CardID);

                context.Battle.Log(
                    "A shadow stirs... " + secret.CardName + " answers the summon!");
            }

            return secret;
        }

        private static void CreateCard(
            EffectData effect,
            EffectContext context)
        {
            if (effect.Card == null)
                return;

            if (context.TargetPlayer == null)
                return;

            CardInstance card =
                new CardInstance(effect.Card);

            // มือเต็มจะไม่สร้างการ์ด
            context.TargetPlayer.Hand.Add(card);
        }

        private static void Discover(
            EffectData effect,
            EffectContext context)
        {
            if (context.CardPool == null)
                return;

            int optionCount = effect.Value;

            if (optionCount <= 0)
                return;

            context.DiscoverResult =
                DiscoverResolver.CreateOptions(
                    context.CardPool,
                    optionCount);

            // ในการต่อสู้จริง ให้ผู้เล่นเลือก (หรือ AI สุ่มเลือก)
            if (context.Battle != null && context.TargetPlayer != null)
            {
                context.Battle.RequestDiscover(
                    context.TargetPlayer,
                    context.DiscoverResult);
            }
        }

        private static void Transform(
            EffectData effect,
            EffectContext context)
        {
            if (effect.Card == null)
                return;

            if (context.TargetCard == null)
                return;

            CardInstance target = context.TargetCard;

            if (target.HasStatus(StatusType.TransformationLock))
            {
                if (context.Battle != null)
                {
                    context.Battle.Log(
                        target.Data.CardName + " cannot transform right now");
                }

                return;
            }

            // จำร่างแรกไว้ เพื่อกลับร่างเดิมเมื่อหมดเวลา / กลับเข้ามือ
            CardData original =
                target.OriginalData != null ? target.OriginalData : target.Data;

            string from = target.Data.CardName;
            int attacks = target.AttacksThisTurn;

            target.TransformTo(effect.Card);

            // แปลงร่างไม่ทำให้ได้โจมตีเพิ่ม
            target.AttacksThisTurn = attacks;

            if (original != effect.Card)
            {
                target.OriginalData = original;

                target.AddStatus(
                    StatusType.Transformed, 0, StatusEffect.Permanent);

                if (effect.Duration > 0)
                {
                    target.AddStatus(
                        StatusType.TemporaryTransformation, 0, effect.Duration);
                }
            }
            else
            {
                target.OriginalData = null;
            }

            if (context.Battle != null)
            {
                context.Battle.Log(
                    from + " transforms into " + effect.Card.CardName);
            }
        }

        // ---------- Keyword / Status ----------

        private static void AddKeyword(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.AddKeyword(effect.Keyword);
        }

        private static void RemoveKeyword(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetCard == null)
                return;

            context.TargetCard.RemoveKeyword(effect.Keyword);
        }

        private static void ApplyStatus(
            EffectContext context,
            StatusType type,
            int value,
            int duration)
        {
            if (context.TargetCard == null)
                return;

            bool applied =
                context.TargetCard.AddStatus(type, value, duration);

            if (!applied && context.Battle != null)
            {
                context.Battle.Log(
                    context.TargetCard.Data.CardName + " resists " +
                    StatusInfo.DisplayName(type));
            }
        }

        // สถานะทั่วไป: Value = ความแรง (ซ้อนได้บางชนิด), Duration 0 = ค่าเริ่มต้นของสถานะ
        private static void ApplyGenericStatus(
            EffectData effect,
            EffectContext context)
        {
            StatusType type = effect.Status;

            if (type == StatusType.None)
                return;

            int value = effect.Value;

            if (StatusInfo.Stacks(type) && value <= 0)
                value = 1;

            // Doom จำว่าใครเป็นผู้ติด เพื่อทำลายตอนต้นเทิร์นของคนนั้น
            if (type == StatusType.Doom && context.SourcePlayer != null)
                value = context.SourcePlayer.PlayerID;

            int duration = effect.Duration > 0
                ? effect.Duration
                : StatusInfo.DefaultDuration(type);

            ApplyStatus(context, type, value, duration);
        }

        // Adapt: ให้เจ้าของการ์ดเลือก 1 ใน N ความสามารถ (ต้องมี Battle)
        private static void Adapt(
            EffectData effect,
            EffectContext context)
        {
            if (context.Battle == null ||
                context.TargetCard == null ||
                context.SourcePlayer == null)
                return;

            context.Battle.RequestAdapt(
                context.SourcePlayer,
                context.TargetCard,
                effect.Value);
        }

        // ---------- Resource ----------

        private static void GainEnergy(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            context.TargetPlayer.CurrentEnergy += effect.Value;

            // Energy ที่ได้เพิ่ม (เช่น Coin) เกิน Max ของเทิร์นนี้ได้ แต่ไม่เกินเพดาน 10
            if (context.TargetPlayer.CurrentEnergy > MaxEnergyCap)
                context.TargetPlayer.CurrentEnergy = MaxEnergyCap;
        }

        private static void SpendEnergy(
            EffectData effect,
            EffectContext context)
        {
            if (context.TargetPlayer == null)
                return;

            context.TargetPlayer.CurrentEnergy -= effect.Value;

            if (context.TargetPlayer.CurrentEnergy < 0)
                context.TargetPlayer.CurrentEnergy = 0;
        }
    }
}
