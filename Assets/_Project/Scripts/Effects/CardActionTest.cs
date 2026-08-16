using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Effects
{
    public class CardActionTest : MonoBehaviour
    {
        private void Start()
        {
            TestDealDamage();
            TestDiscover();
        }

        private void TestDealDamage()
        {
           // ใช้ BattleManager ที่มีอยู่ใน Scene
            BattleManager battle =
                FindFirstObjectByType<BattleManager>();

            if (battle == null)
            {
                Debug.LogError("TEST FAILED: BattleManager not found!");
                return;
            }
            // สร้าง Source Card
            CardData sourceData =
                ScriptableObject.CreateInstance<CardData>();

            sourceData.CardID = "TEST_SOURCE";
            sourceData.CardName = "Test Card";
            sourceData.Cost = 1;
            sourceData.Attack = 2;
            sourceData.Health = 10;

            CardInstance sourceCard =
                new CardInstance(sourceData);

            // สร้าง Target Card
            CardData targetData =
                ScriptableObject.CreateInstance<CardData>();

            targetData.CardID = "TEST_TARGET";
            targetData.CardName = "Target Card";
            targetData.Cost = 2;
            targetData.Attack = 3;
            targetData.Health = 10;

            CardInstance targetCard =
                new CardInstance(targetData);

            // สร้าง Target Candidate
            List<TargetCandidate> candidates =
                new List<TargetCandidate>();

            candidates.Add(
                new TargetCandidate(
                    2,
                    false,
                    true,
                    targetCard
                )
            );

            // สร้าง Action
            CardActionData action =
                new CardActionData();

            action.Target =
                TargetRule.EnemyUnit;

            EffectData damage =
                new EffectData();

            damage.Type =
                EffectType.DealDamage;

            damage.Value = 3;

            action.Effects.Add(damage);

            // Context
            EffectContext context =
                new EffectContext();

            context.SourceCard =
                sourceCard;

            context.SourcePlayer =
                battle.Player1;

            context.Battle =
                battle;

            // Execute
            CardActionResolver.ExecuteAction(
                action,
                context,
                candidates
            );

            Debug.Log(
                "TEST RESULT: Target HP = " +
                targetCard.CurrentHealth
            );

            if (targetCard.CurrentHealth == 7)
            {
                Debug.Log(
                    "TEST PASSED!"
                );
            }
            else
            {
                Debug.LogError(
                    "TEST FAILED!"
                );
            }
        }
        private void TestDiscover()
        {
            // สร้าง Battle
            BattleManager battle =
                FindAnyObjectByType<BattleManager>();

            if (battle == null)
            {
                Debug.LogError(
                    "DISCOVER TEST FAILED: BattleManager not found!"
                );

                return;
            }

            // สร้าง Card Pool
            CardPool pool =
                new CardPool();

            // สร้างการ์ดทดสอบ 3 ใบ
            for (int i = 1; i <= 3; i++)
            {
                CardData data =
                    ScriptableObject.CreateInstance<CardData>();

                data.CardID = "DISCOVER_" + i;
                data.CardName = "Discover Card " + i;
                data.Cost = i;
                data.Attack = i * 2;
                data.Health = i * 5;

                pool.Add(data);
            }

            // สร้าง Context
            EffectContext context =
                new EffectContext();

            context.SourcePlayer =
                battle.Player1;

            context.Battle =
                battle;

            context.CardPool =
                pool;

            // สร้าง Effect
            EffectData effect =
                new EffectData();

            effect.Type =
                EffectType.Discover;

            effect.Value = 2;

            // Execute Discover
            EffectExecutor.Execute(
                effect,
                context);

            // ตรวจผล
            if (context.DiscoverResult == null)
            {
                Debug.LogError(
                    "DISCOVER TEST FAILED: Result is null!"
                );

                return;
            }

            Debug.Log(
                "DISCOVER OPTIONS: " +
                context.DiscoverResult.Options.Count
            );

            foreach (
                CardData card
                in context.DiscoverResult.Options)
            {
                Debug.Log(
                    "OPTION: " +
                    card.CardName
                );
            }

            if (
                context.DiscoverResult.Options.Count == 2)
            {
                Debug.Log(
                    "DISCOVER TEST PASSED!"
                );
            }
            else
            {
                Debug.LogError(
                    "DISCOVER TEST FAILED!"
                );
            }
        }
    }
}