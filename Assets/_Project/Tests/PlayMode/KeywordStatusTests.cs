using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบ Keyword และ Status ที่เพิ่มใหม่ (ผ่าน Command / BattleManager)
    /// </summary>
    public class KeywordStatusTests
    {
        private GameObject host;
        private BattleManager battle;
        private readonly List<CardData> created = new List<CardData>();

        private BattlePlayer P1 { get { return battle.Player1; } }
        private BattlePlayer P2 { get { return battle.Player2; } }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            BattleSetup.Reset();

            CardData filler = MakeData("FILLER", 1, 1, 1, Keyword.None);

            List<CardData> deck = new List<CardData>();

            for (int i = 0; i < 30; i++)
                deck.Add(filler);

            BattleSetup.PlayerDeck = deck;
            BattleSetup.EnemyDeck = new List<CardData>(deck);

            host = new GameObject("TestBattle");
            battle = host.AddComponent<BattleManager>();

            yield return null;

            Assert.IsTrue(battle.HasStarted);

            P1.Hand.Clear();
            P2.Hand.Clear();
            P1.CurrentEnergy = 10;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(host);

            foreach (CardData data in created)
                Object.DestroyImmediate(data);

            created.Clear();

            BattleSetup.Reset();
        }

        // ---------- ตัวช่วย ----------

        private CardData MakeData(
            string id, int cost, int attack, int health, Keyword keywords)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            data.CardID = id;
            data.CardName = id;
            data.Type = CardType.Unit;
            data.Cost = cost;
            data.Attack = attack;
            data.Health = health;
            data.Keywords = keywords;

            created.Add(data);

            return data;
        }

        private CardInstance Place(BattlePlayer owner, CardData data)
        {
            CardInstance card = new CardInstance(data);

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        private CardInstance PutOnField(
            BattlePlayer owner, string id, int attack, int health, Keyword keywords)
        {
            return Place(owner, MakeData(id, 1, attack, health, keywords));
        }

        private static CardActionData Action(
            TriggerType trigger,
            TargetRule rule,
            TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = new CardActionData();

            action.Trigger = trigger;
            action.Target = rule;
            action.Selection = selection;
            action.Effects = new List<EffectData>(effects);

            return action;
        }

        private static EffectData Effect(EffectType type, int value)
        {
            EffectData effect = new EffectData();

            effect.Type = type;
            effect.Value = value;

            return effect;
        }

        private static EffectData StatusEffectData(
            StatusType status, int value, int duration)
        {
            EffectData effect = Effect(EffectType.ApplyStatus, value);

            effect.Status = status;
            effect.Duration = duration;

            return effect;
        }

        private ActionResult Attack(
            BattlePlayer player,
            CardInstance attacker,
            CardInstance targetUnit,
            BattlePlayer targetLeader)
        {
            return battle.Submit(
                new AttackCommand(player, attacker, targetUnit, targetLeader));
        }

        // ---------- Keyword: การโจมตี ----------

        [Test]
        public void Rush_CanAttackUnitsButNotLeaderOnSummonTurn()
        {
            CardInstance target = PutOnField(P2, "T", 1, 5, Keyword.None);

            CardInstance rusher =
                new CardInstance(MakeData("R", 1, 2, 2, Keyword.Rush));

            P1.Hand.Add(rusher);
            battle.Submit(new PlayCardCommand(P1, rusher));

            Assert.IsFalse(Attack(P1, rusher, null, P2).Success);
            Assert.IsTrue(Attack(P1, rusher, target, null).Success);
        }

        [Test]
        public void Charge_CanAttackLeaderOnSummonTurn()
        {
            CardInstance charger =
                new CardInstance(MakeData("C", 1, 2, 2, Keyword.Charge));

            P1.Hand.Add(charger);
            battle.Submit(new PlayCardCommand(P1, charger));

            int before = P2.LeaderHealth;

            Assert.IsTrue(Attack(P1, charger, null, P2).Success);
            Assert.AreEqual(before - 2, P2.LeaderHealth);
        }

        [Test]
        public void DoubleStrike_AttacksTwicePerTurn()
        {
            CardInstance unit = PutOnField(P1, "D", 1, 5, Keyword.DoubleStrike);

            Assert.IsTrue(Attack(P1, unit, null, P2).Success);
            Assert.IsTrue(Attack(P1, unit, null, P2).Success);
            Assert.IsFalse(Attack(P1, unit, null, P2).Success);
        }

        [Test]
        public void Pierce_ExcessDamageHitsLeader()
        {
            CardInstance attacker = PutOnField(P1, "A", 5, 5, Keyword.Pierce);
            CardInstance defender = PutOnField(P2, "D", 1, 2, Keyword.None);

            int before = P2.LeaderHealth;

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            // 5 damage into 2 HP -> 3 excess
            Assert.AreEqual(before - 3, P2.LeaderHealth);
        }

        [Test]
        public void QuickAttack_KillsBeforeCounterattack()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 3, Keyword.QuickAttack);
            CardInstance defender = PutOnField(P2, "D", 3, 3, Keyword.None);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            Assert.Contains(defender, P2.Graveyard);
            Assert.Contains(attacker, P1.Field);
            Assert.AreEqual(3, attacker.CurrentHealth);
        }

        [Test]
        public void QuickAttack_DefenderStrikesFirst()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 3, Keyword.None);
            CardInstance defender = PutOnField(P2, "D", 3, 3, Keyword.QuickAttack);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            Assert.Contains(attacker, P1.Graveyard);
            Assert.Contains(defender, P2.Field);
            Assert.AreEqual(3, defender.CurrentHealth);
        }

        [Test]
        public void Stealth_FirstAttackIsNotCounterattacked()
        {
            CardInstance attacker = PutOnField(P1, "A", 2, 5, Keyword.Stealth);
            CardInstance defender = PutOnField(P2, "D", 4, 5, Keyword.None);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            Assert.AreEqual(3, defender.CurrentHealth);
            Assert.AreEqual(5, attacker.CurrentHealth);
            Assert.IsFalse(attacker.HasKeyword(Keyword.Stealth));
        }

        [Test]
        public void ControlledDefender_CannotCounterattack()
        {
            CardInstance stunnedTarget = PutOnField(P2, "S", 4, 5, Keyword.None);
            CardInstance frozenTarget = PutOnField(P2, "F", 4, 5, Keyword.None);
            CardInstance disarmedTarget = PutOnField(P2, "D", 4, 5, Keyword.None);

            stunnedTarget.AddStatus(StatusType.Stun, 0, 2);
            frozenTarget.AddStatus(StatusType.Freeze, 0, 2);
            disarmedTarget.AddStatus(StatusType.Disarm, 0, 2);

            CardInstance a = PutOnField(P1, "A", 1, 5, Keyword.None);
            CardInstance b = PutOnField(P1, "B", 1, 5, Keyword.None);
            CardInstance c = PutOnField(P1, "C", 1, 5, Keyword.None);

            Assert.IsTrue(Attack(P1, a, stunnedTarget, null).Success);
            Assert.IsTrue(Attack(P1, b, frozenTarget, null).Success);
            Assert.IsTrue(Attack(P1, c, disarmedTarget, null).Success);

            Assert.AreEqual(5, a.CurrentHealth);
            Assert.AreEqual(5, b.CurrentHealth);
            Assert.AreEqual(5, c.CurrentHealth);
        }

        [Test]
        public void Burn_AddsTurnsInsteadOfDamage()
        {
            CardInstance unit = PutOnField(P2, "B", 1, 9, Keyword.None);

            unit.AddStatus(StatusType.Burn, 1, 1);
            unit.AddStatus(StatusType.Burn, 1, 1);

            int burns = 0;

            foreach (StatusEffect status in unit.Statuses)
            {
                if (status.Type != StatusType.Burn)
                    continue;

                burns++;

                Assert.AreEqual(1, status.Value);
                Assert.AreEqual(2, status.Duration);
            }

            Assert.AreEqual(1, burns);

            StatusProcessor.OnTurnStart(battle, P2);

            Assert.AreEqual(8, unit.CurrentHealth);
        }

        [Test]
        public void TargetedSpell_CannotBePlayedWithoutTarget()
        {
            CardData data = MakeData("Bolt", 1, 0, 0, Keyword.None);

            data.Type = CardType.Spell;
            data.Actions = new List<CardActionData>
            {
                Action(TriggerType.Play, TargetRule.EnemyUnit,
                    TargetSelection.Chosen, Effect(EffectType.DealDamage, 2))
            };

            CardInstance spell = new CardInstance(data);

            P1.Hand.Add(spell);

            ActionResult noTarget = battle.Submit(new PlayCardCommand(P1, spell));

            Assert.IsFalse(noTarget.Success);
            Assert.IsTrue(P1.Hand.Contains(spell));

            CardInstance enemy = PutOnField(P2, "E", 1, 5, Keyword.None);

            TargetCandidate candidate = new TargetCandidate(
                P2.PlayerID, false, true, enemy);

            Assert.IsTrue(
                battle.Submit(new PlayCardCommand(P1, spell, candidate)).Success);
        }

        [Test]
        public void AI_DoesNotStunAnAlreadyStunnedUnit()
        {
            CardData data = MakeData("Freezer", 1, 0, 0, Keyword.None);

            data.Type = CardType.Spell;
            data.Actions = new List<CardActionData>
            {
                Action(TriggerType.Play, TargetRule.EnemyUnit,
                    TargetSelection.Chosen, Effect(EffectType.Stun, 1))
            };

            CardInstance stunned = PutOnField(P2, "Big", 9, 9, Keyword.None);
            CardInstance fresh = PutOnField(P2, "Small", 1, 1, Keyword.None);

            stunned.AddStatus(StatusType.Stun, 0, 2);

            P1.Hand.Add(new CardInstance(data));

            PlayCardCommand command =
                AIPlanner.NextCommand(battle, P1) as PlayCardCommand;

            Assert.IsNotNull(command);
            Assert.AreSame(fresh, command.Target.TargetObject);
        }

        [UnityTest]
        public IEnumerator CoinFlip_SecondPlayerCanGoFirst_AndOtherGetsCoin()
        {
            BattleSetup.UseCoinFlip = true;
            BattleSetup.FirstPlayerOverride = 2;

            GameObject other = new GameObject("CoinBattle");
            BattleManager coin = other.AddComponent<BattleManager>();

            yield return null;

            Assert.AreEqual(2, coin.FirstPlayerID);
            Assert.AreEqual(2, coin.TurnManager.CurrentPlayerID);

            // ผู้เล่น 2 เริ่มก่อน: จั่ว 3 + จั่วตอนเริ่มเทิร์น
            Assert.AreEqual(4, coin.Player2.Hand.Count);

            // ผู้เล่น 1 เริ่มหลัง: จั่ว 4 + Coin
            Assert.AreEqual(5, coin.Player1.Hand.Count);

            bool hasCoin = false;

            foreach (CardInstance card in coin.Player1.Hand.Cards)
            {
                if (card.Data.CardID == StarterCardLibrary.CoinId)
                    hasCoin = true;
            }

            Assert.IsTrue(hasCoin);

            // รอบเทิร์นนับเมื่อกลับมาถึงคนที่เริ่มก่อน
            coin.Submit(new EndTurnCommand(coin.Player2));

            Assert.AreEqual(1, coin.TurnManager.CurrentPlayerID);
            Assert.AreEqual(1, coin.TurnManager.TurnNumber);

            coin.Submit(new EndTurnCommand(coin.Player1));

            Assert.AreEqual(2, coin.TurnManager.CurrentPlayerID);
            Assert.AreEqual(2, coin.TurnManager.TurnNumber);

            Object.Destroy(other);
        }

        [Test]
        public void Overwhelm_ExcessDamageHitsNeighbor()
        {
            CardInstance attacker = PutOnField(P1, "A", 5, 5, Keyword.Overwhelm);
            CardInstance first = PutOnField(P2, "D1", 1, 2, Keyword.None);
            CardInstance second = PutOnField(P2, "D2", 1, 5, Keyword.None);

            Assert.IsTrue(Attack(P1, attacker, first, null).Success);

            // 3 excess spills onto the neighbor
            Assert.AreEqual(2, second.CurrentHealth);
        }

        [Test]
        public void Unstoppable_IgnoresAttackStoppingStatuses()
        {
            CardInstance unit = PutOnField(P1, "U", 2, 4, Keyword.Unstoppable);

            Assert.IsFalse(unit.AddStatus(StatusType.Stun, 0, 2));
            Assert.IsFalse(unit.AddStatus(StatusType.Freeze, 0, 2));
            Assert.IsFalse(unit.AddStatus(StatusType.Disarm, 0, 2));
            Assert.IsFalse(unit.AddStatus(StatusType.Confusion, 0, 2));

            Assert.IsTrue(Attack(P1, unit, null, P2).Success);
        }

        [Test]
        public void Guard_ProtectsLeaderFromAttacks()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 3, Keyword.None);
            CardInstance guard = PutOnField(P2, "G", 1, 5, Keyword.Guard);

            Assert.IsFalse(Attack(P1, attacker, null, P2).Success);
            Assert.IsTrue(Attack(P1, attacker, guard, null).Success);
        }

        // ---------- Keyword: การป้องกัน ----------

        [Test]
        public void Ward_BlocksNextTargetedEffectOnly()
        {
            CardInstance source = PutOnField(P1, "Src", 1, 1, Keyword.None);
            CardInstance target = PutOnField(P2, "W", 1, 10, Keyword.Ward);

            CardActionData action = Action(
                TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                Effect(EffectType.DealDamage, 3));

            for (int i = 0; i < 2; i++)
            {
                EffectContext context = new EffectContext();

                context.SourceCard = source;
                context.SourcePlayer = P1;
                context.Battle = battle;
                context.ChosenTarget = new TargetCandidate(
                    P2.PlayerID, false, true, target);

                CardActionResolver.ExecuteAction(
                    action, context, BattleRules.BuildCandidates(battle));

                if (i == 0)
                {
                    Assert.AreEqual(10, target.CurrentHealth);
                    Assert.IsFalse(target.HasKeyword(Keyword.Ward));
                }
            }

            Assert.AreEqual(7, target.CurrentHealth);
        }

        [Test]
        public void Immune_TakesNoDamageOrHarmfulStatus()
        {
            CardInstance unit = PutOnField(P2, "I", 1, 5, Keyword.Immune);

            // Immune 3: โดน 4 เหลือ 1, โดน 3 เหลือ 0
            Assert.AreEqual(1, battle.DamageUnit(null, unit, 4));
            Assert.AreEqual(4, unit.CurrentHealth);
            Assert.AreEqual(0, battle.DamageUnit(null, unit, 3));
            Assert.AreEqual(4, unit.CurrentHealth);
            Assert.IsFalse(unit.AddStatus(StatusType.Burn, 1, 3));
            Assert.IsTrue(unit.AddStatus(StatusType.AttackUp, 1, 3));
        }

        [Test]
        public void Stealth_IsLostWhenMarked()
        {
            CardInstance unit = PutOnField(P2, "S", 1, 3, Keyword.Stealth);

            Assert.IsTrue(unit.IsHidden);

            unit.AddStatus(StatusType.Marked, 0, 2);

            Assert.IsFalse(unit.IsHidden);
        }

        [Test]
        public void Reborn_ReturnsOnceWithOneHealth()
        {
            CardInstance unit = PutOnField(P2, "Re", 2, 4, Keyword.Reborn);

            battle.DamageUnit(null, unit, 10);
            battle.Resolve();

            Assert.Contains(unit, P2.Field);
            Assert.AreEqual(1, unit.CurrentHealth);
            Assert.IsFalse(unit.HasKeyword(Keyword.Reborn));

            battle.DamageUnit(null, unit, 10);
            battle.Resolve();

            Assert.Contains(unit, P2.Graveyard);
        }

        // ---------- ความสามารถอัตโนมัติ ----------

        [Test]
        public void Spellburst_TriggersOnlyOnFirstSpell()
        {
            CardData data = MakeData("SB", 1, 1, 3, Keyword.Spellburst);

            data.Actions.Add(Action(
                TriggerType.Spellburst, TargetRule.Self, TargetSelection.All,
                Effect(EffectType.GainAttack, 2)));

            CardInstance unit = Place(P1, data);

            for (int i = 0; i < 2; i++)
            {
                CardData spell = MakeData("Sp" + i, 1, 0, 0, Keyword.None);

                spell.Type = CardType.Spell;

                CardInstance spellCard = new CardInstance(spell);

                P1.Hand.Add(spellCard);

                Assert.IsTrue(
                    battle.Submit(new PlayCardCommand(P1, spellCard)).Success);

                Assert.AreEqual(3, unit.CurrentAttack);
                Assert.IsFalse(unit.HasKeyword(Keyword.Spellburst));
            }
        }

        [Test]
        public void Combo_OnlyWorksAfterAnotherCardThisTurn()
        {
            CardData comboData = MakeData("Cb", 1, 1, 3, Keyword.None);

            CardActionData action = Action(
                TriggerType.Play, TargetRule.Self, TargetSelection.All,
                Effect(EffectType.GainAttack, 2));

            ConditionData condition = new ConditionData();

            condition.Condition = ConditionType.Combo;

            action.Conditions.Add(condition);
            comboData.Actions.Add(action);

            // เล่นเป็นใบแรก: ไม่ได้โบนัส
            CardInstance first = new CardInstance(comboData);

            P1.Hand.Add(first);
            battle.Submit(new PlayCardCommand(P1, first));

            Assert.AreEqual(1, first.CurrentAttack);

            // เล่นหลังใบอื่น: ได้โบนัส
            CardInstance second = new CardInstance(comboData);

            P1.Hand.Add(second);
            battle.Submit(new PlayCardCommand(P1, second));

            Assert.AreEqual(3, second.CurrentAttack);
        }

        [Test]
        public void Aura_BuffsOtherAlliesWhileOnField()
        {
            CardData auraData = MakeData("Au", 1, 1, 3, Keyword.None);

            auraData.AuraAttack = 1;
            auraData.AuraHealth = 1;

            CardInstance aura = Place(P1, auraData);
            CardInstance ally = PutOnField(P1, "Al", 2, 2, Keyword.None);

            battle.Resolve();

            Assert.AreEqual(3, ally.CurrentAttack);
            Assert.AreEqual(3, ally.CurrentHealth);
            Assert.AreEqual(1, aura.CurrentAttack);

            battle.DamageUnit(null, aura, 10);
            battle.Resolve();

            Assert.AreEqual(2, ally.CurrentAttack);
            Assert.AreEqual(2, ally.CurrentHealth);
        }

        [Test]
        public void Evolve_ChangesFormAfterEnoughTurns()
        {
            CardData evolved = MakeData("Evo2", 1, 5, 5, Keyword.Pierce);
            CardData basic = MakeData("Evo1", 1, 2, 2, Keyword.None);

            basic.EvolvesInto = evolved;
            basic.EvolveAfterTurns = 1;

            CardInstance unit = Place(P1, basic);

            battle.Submit(new EndTurnCommand(P1));

            Assert.AreSame(evolved, unit.Data);
            Assert.AreEqual(5, unit.CurrentAttack);
            Assert.IsTrue(unit.HasKeyword(Keyword.Pierce));
        }

        [Test]
        public void TemporaryTransformation_RevertsWithFatigueAndLock()
        {
            CardData original = MakeData("Base", 1, 3, 4, Keyword.None);
            CardData form = MakeData("Super", 1, 8, 8, Keyword.DoubleStrike);

            CardInstance unit = Place(P1, original);

            EffectData transform = Effect(EffectType.Transform, 0);

            transform.Card = form;
            transform.Duration = 1;

            EffectContext context = new EffectContext();

            context.SourceCard = unit;
            context.SourcePlayer = P1;
            context.TargetCard = unit;
            context.TargetPlayer = P1;
            context.Battle = battle;

            EffectExecutor.Execute(transform, context);

            Assert.AreSame(form, unit.Data);
            Assert.IsTrue(unit.HasStatus(StatusType.Transformed));
            Assert.IsTrue(unit.HasStatus(StatusType.TemporaryTransformation));

            battle.Submit(new EndTurnCommand(P1));

            Assert.AreSame(original, unit.Data);
            Assert.IsFalse(unit.HasStatus(StatusType.Transformed));
            Assert.IsTrue(unit.HasStatus(StatusType.TransformationLock));
            Assert.IsTrue(unit.HasStatus(StatusType.TransformationFatigue));

            // Lock กันการแปลงซ้ำ
            EffectExecutor.Execute(transform, context);

            Assert.AreSame(original, unit.Data);
        }

        [Test]
        public void Adapt_AiPicksInstantlyAndHumanChooses()
        {
            CardInstance aiUnit = PutOnField(P2, "AiA", 1, 1, Keyword.None);

            battle.RequestAdapt(P2, aiUnit, 3);

            Assert.IsFalse(battle.HasPendingChoice);

            bool changed =
                aiUnit.CurrentAttack != 1 ||
                aiUnit.MaxHealth != 1 ||
                aiUnit.Keywords != Keyword.None;

            Assert.IsTrue(changed);

            CardInstance humanUnit = PutOnField(P1, "HuA", 1, 1, Keyword.None);

            battle.RequestAdapt(P1, humanUnit, 3);

            Assert.IsTrue(battle.HasPendingAdapt);
            Assert.IsFalse(battle.Submit(new EndTurnCommand(P1)).Success);

            Assert.IsTrue(battle.ResolveAdapt(0));
            Assert.IsFalse(battle.HasPendingChoice);
        }

        // ---------- Status: ควบคุม ----------

        [Test]
        public void Freeze_BlocksAttackAndBurnThawsIt()
        {
            CardInstance unit = PutOnField(P1, "F", 2, 5, Keyword.None);

            unit.AddStatus(StatusType.Freeze, 0, 2);

            Assert.IsFalse(Attack(P1, unit, null, P2).Success);

            unit.AddStatus(StatusType.Burn, 1, 2);

            StatusProcessor.OnTurnStart(battle, P1);

            Assert.IsFalse(unit.HasStatus(StatusType.Freeze));
            Assert.IsTrue(Attack(P1, unit, null, P2).Success);
        }

        [Test]
        public void Disarm_BlocksAttack()
        {
            CardInstance unit = PutOnField(P1, "Di", 2, 5, Keyword.None);

            unit.AddStatus(StatusType.Disarm, 0, 1);

            Assert.IsFalse(Attack(P1, unit, null, P2).Success);
        }

        [Test]
        public void Stun_DisablesTurnStartAbilities()
        {
            CardData data = MakeData("StAb", 1, 1, 5, Keyword.None);

            data.Actions.Add(Action(
                TriggerType.TurnStart, TargetRule.Self, TargetSelection.All,
                Effect(EffectType.GainAttack, 1)));

            CardInstance unit = Place(P1, data);

            unit.AddStatus(StatusType.Stun, 0, 3);

            battle.Publish(TriggerType.TurnStart, player: P1);

            Assert.AreEqual(1, unit.CurrentAttack);

            unit.RemoveStatus(StatusType.Stun);

            battle.Publish(TriggerType.TurnStart, player: P1);

            Assert.AreEqual(2, unit.CurrentAttack);
        }

        [Test]
        public void Root_BlocksReturnToHand()
        {
            CardInstance unit = PutOnField(P2, "Ro", 1, 3, Keyword.None);

            unit.AddStatus(StatusType.Root, 0, 2);

            EffectContext context = new EffectContext();

            context.SourcePlayer = P1;
            context.TargetCard = unit;
            context.TargetPlayer = P2;
            context.Battle = battle;

            EffectExecutor.Execute(Effect(EffectType.ReturnToHand, 0), context);

            Assert.Contains(unit, P2.Field);
        }

        [Test]
        public void Confusion_StillAttacksALegalTarget()
        {
            CardInstance unit = PutOnField(P1, "Cf", 1, 5, Keyword.None);

            PutOnField(P1, "Ally", 1, 20, Keyword.None);
            PutOnField(P2, "Foe", 1, 20, Keyword.None);

            unit.AddStatus(StatusType.Confusion, 0, 2);

            ActionResult result = Attack(P1, unit, null, P2);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1, unit.AttacksThisTurn);
        }

        // ---------- Status: ความเสียหายต่อเนื่อง ----------

        [Test]
        public void PoisonStatus_DealsTrueDamageAndStacks()
        {
            CardInstance unit = PutOnField(P1, "Po", 1, 10, Keyword.Shield);

            unit.AddStatus(StatusType.Poison, 2, 3);
            unit.AddStatus(StatusType.Poison, 1, 3);

            Assert.AreEqual(3, unit.StatusValue(StatusType.Poison));

            StatusProcessor.OnTurnStart(battle, P1);

            // True damage ทะลุ Shield
            Assert.AreEqual(7, unit.CurrentHealth);
            Assert.IsTrue(unit.HasKeyword(Keyword.Shield));
        }

        [Test]
        public void Bleed_StacksAndDamages()
        {
            CardInstance unit = PutOnField(P1, "Bl", 1, 10, Keyword.None);

            unit.AddStatus(StatusType.Bleed, 1, 3);
            unit.AddStatus(StatusType.Bleed, 1, 3);

            StatusProcessor.OnTurnStart(battle, P1);

            Assert.AreEqual(8, unit.CurrentHealth);
        }

        [Test]
        public void Curse_PreventsHealing()
        {
            CardInstance unit = PutOnField(P1, "Cu", 1, 10, Keyword.None);

            battle.DamageUnit(null, unit, 5);

            unit.AddStatus(StatusType.Curse, 0, 2);

            Assert.AreEqual(0, battle.HealUnit(null, unit, 3));
            Assert.AreEqual(5, unit.CurrentHealth);
        }

        [Test]
        public void Marked_IsAConditionForOtherAbilities()
        {
            CardInstance unit = PutOnField(P2, "Mk", 1, 5, Keyword.None);

            ConditionData condition = new ConditionData();

            condition.Condition = ConditionType.TargetMarked;

            EffectContext context = new EffectContext();

            context.SourcePlayer = P1;
            context.TargetCard = unit;
            context.Battle = battle;

            Assert.IsFalse(ConditionEvaluator.Evaluate(condition, context));

            unit.AddStatus(StatusType.Marked, 0, 2);

            Assert.IsTrue(ConditionEvaluator.Evaluate(condition, context));
        }

        // ---------- Status: เพิ่ม / ลดความสามารถ ----------

        [Test]
        public void AttackUpAndDown_ChangeAttackAndExpire()
        {
            CardInstance unit = PutOnField(P1, "At", 3, 5, Keyword.None);

            unit.AddStatus(StatusType.AttackUp, 2, 1);
            battle.Resolve();

            Assert.AreEqual(5, unit.CurrentAttack);

            unit.AddStatus(StatusType.AttackDown, 4, 2);
            battle.Resolve();

            Assert.AreEqual(1, unit.CurrentAttack);

            unit.RemoveStatus(StatusType.AttackUp);
            unit.RemoveStatus(StatusType.AttackDown);
            battle.Resolve();

            Assert.AreEqual(3, unit.CurrentAttack);
        }

        [Test]
        public void DamageModifiers_DefenseVulnerableWeakness()
        {
            CardInstance target = PutOnField(P2, "Tg", 1, 30, Keyword.None);

            target.AddStatus(StatusType.DefenseUp, 2, 3);
            Assert.AreEqual(2, battle.DamageUnit(null, target, 4));

            target.RemoveStatus(StatusType.DefenseUp);
            target.AddStatus(StatusType.DefenseDown, 1, 3);
            Assert.AreEqual(5, battle.DamageUnit(null, target, 4));

            target.RemoveStatus(StatusType.DefenseDown);
            target.AddStatus(StatusType.Vulnerable, 0, 3);
            Assert.AreEqual(6, battle.DamageUnit(null, target, 4));

            target.RemoveStatus(StatusType.Vulnerable);

            CardInstance weak = PutOnField(P1, "Wk", 1, 5, Keyword.None);

            weak.AddStatus(StatusType.Weakness, 0, 3);
            Assert.AreEqual(2, battle.DamageUnit(weak, target, 4));
        }

        [Test]
        public void Barrier_AbsorbsDamageThenBreaks()
        {
            CardInstance unit = PutOnField(P2, "Ba", 1, 10, Keyword.None);

            unit.AddStatus(StatusType.Barrier, 5, StatusEffect.Permanent);

            battle.DamageUnit(null, unit, 3);

            Assert.AreEqual(10, unit.CurrentHealth);
            Assert.AreEqual(2, unit.StatusValue(StatusType.Barrier));

            battle.DamageUnit(null, unit, 4);

            Assert.AreEqual(8, unit.CurrentHealth);
            Assert.IsFalse(unit.HasStatus(StatusType.Barrier));
        }

        [Test]
        public void Exposed_IgnoresShieldAndBarrier()
        {
            CardInstance unit = PutOnField(P2, "Ex", 1, 10, Keyword.Shield);

            unit.AddStatus(StatusType.Barrier, 5, StatusEffect.Permanent);
            unit.AddStatus(StatusType.Exposed, 0, 2);

            battle.DamageUnit(null, unit, 4);

            Assert.AreEqual(6, unit.CurrentHealth);
            Assert.IsTrue(unit.HasKeyword(Keyword.Shield));
        }

        [Test]
        public void Protected_BlocksHarmfulStatuses()
        {
            CardInstance unit = PutOnField(P2, "Pr", 1, 5, Keyword.None);

            unit.AddStatus(StatusType.Protected, 0, 2);

            Assert.IsFalse(unit.AddStatus(StatusType.Stun, 0, 1));
            Assert.IsFalse(unit.AddStatus(StatusType.Poison, 1, 3));
            Assert.IsTrue(unit.AddStatus(StatusType.Barrier, 2, 3));
        }

        [Test]
        public void Blessed_CleansesOneHarmfulStatusAndHeals()
        {
            CardInstance unit = PutOnField(P1, "Bs", 1, 10, Keyword.None);

            battle.DamageUnit(null, unit, 4);

            unit.AddStatus(StatusType.Blessed, 0, 3);
            unit.AddStatus(StatusType.Curse, 0, 3);

            StatusProcessor.OnTurnStart(battle, P1);

            Assert.IsFalse(unit.HasStatus(StatusType.Curse));
            Assert.AreEqual(7, unit.CurrentHealth);
        }

        [Test]
        public void CostUpAndDown_ChangeCardCostInHand()
        {
            CardInstance card =
                new CardInstance(MakeData("Co", 3, 1, 1, Keyword.None));

            P1.Hand.Add(card);

            card.AddStatus(StatusType.CostUp, 2, 2);
            battle.Resolve();

            Assert.AreEqual(5, card.CurrentCost);

            card.AddStatus(StatusType.CostDown, 4, 2);
            battle.Resolve();

            Assert.AreEqual(1, card.CurrentCost);

            card.RemoveStatus(StatusType.CostUp);
            card.RemoveStatus(StatusType.CostDown);
            battle.Resolve();

            Assert.AreEqual(3, card.CurrentCost);
        }

        [Test]
        public void ApplyStatusEffect_UsesDefaultsAndRespectsImmunity()
        {
            CardInstance unit = PutOnField(P2, "Ap", 1, 5, Keyword.None);

            EffectContext context = new EffectContext();

            context.SourcePlayer = P1;
            context.TargetCard = unit;
            context.TargetPlayer = P2;
            context.Battle = battle;

            EffectExecutor.Execute(
                StatusEffectData(StatusType.Vulnerable, 0, 0), context);

            Assert.IsTrue(unit.HasStatus(StatusType.Vulnerable));

            CardInstance immune = PutOnField(P2, "Im", 1, 5, Keyword.Immune);

            context.TargetCard = immune;

            EffectExecutor.Execute(
                StatusEffectData(StatusType.Vulnerable, 0, 0), context);

            Assert.IsFalse(immune.HasStatus(StatusType.Vulnerable));
        }
    }
}
