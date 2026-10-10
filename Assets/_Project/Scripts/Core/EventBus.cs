using System;
using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Core
{
    /// <summary>
    /// เหตุการณ์ในเกม 1 ครั้ง ใช้เป็นตัวกระตุ้น Trigger ของการ์ด
    ///
    /// ความหมายของแต่ละ Trigger:
    ///  Draw / Play / Summon / Discard / Death : Card = การ์ดที่เกิดเหตุการณ์
    ///  Attack       : Card = ผู้โจมตี, Target = ผู้ถูกโจมตี (null ถ้าตี Leader)
    ///  LeaderAttack : Card = ผู้โจมตี, TargetPlayer = เจ้าของ Leader ที่ถูกตี
    ///  Hit          : Card = ผู้ทำความเสียหาย, Target = ผู้ถูกทำ, Amount = ความเสียหาย
    ///  DamageTaken  : Card = ผู้ที่โดน (null ถ้าเป็น Leader), Target = ต้นเหตุ
    ///  Heal         : Card = ผู้ที่ได้รับการรักษา
    ///  OnTarget     : Card = การ์ดที่ถูกเลือกเป็นเป้าหมาย, Target = การ์ดต้นเหตุ
    ///  TurnStart / TurnEnd : Player = ผู้เล่นที่เป็นเจ้าของเทิร์น
    ///  BattleStart / BattleEnd : BattleEnd Player = ผู้ชนะ (null = เสมอ)
    /// </summary>
    public class GameEvent
    {
        public TriggerType Trigger;

        public CardInstance Card;
        public BattlePlayer Player;

        public CardInstance Target;
        public BattlePlayer TargetPlayer;

        public int Amount;

        // เป้าหมายที่ผู้เล่นเลือกตอนลงการ์ด (ใช้กับ Action ที่เป็น Chosen)
        public TargetCandidate ChosenTarget;
    }

    /// <summary>
    /// ศูนย์กลางส่งเหตุการณ์ในการต่อสู้ 1 ครั้ง (ผูกกับ BattleManager)
    /// ใช้คิวเพื่อไม่ให้ Effect ที่ยิง Trigger ซ้อนกันเรียกตัวเองซ้อนลึกจน Stack ล้น
    /// </summary>
    public class EventBus
    {
        // กัน Loop ไม่รู้จบ (เช่น การ์ดสองใบยิง Trigger ใส่กันไปมา)
        private const int MaxEventsPerFlush = 2000;

        private readonly Queue<GameEvent> queue = new Queue<GameEvent>();

        private bool dispatching;

        public event Action<GameEvent> Raised;

        public bool IsDispatching
        {
            get { return dispatching; }
        }

        public void Publish(GameEvent gameEvent)
        {
            if (gameEvent == null)
                return;

            queue.Enqueue(gameEvent);

            // ถ้ากำลังประมวลผลอยู่ เหตุการณ์ใหม่จะถูกทำต่อในลูปเดิม
            if (dispatching)
                return;

            dispatching = true;

            int processed = 0;

            try
            {
                while (queue.Count > 0)
                {
                    if (++processed > MaxEventsPerFlush)
                    {
                        Debug.LogWarning(
                            "EventBus: too many chained events, " +
                            "dropping the rest (possible infinite trigger loop).");

                        queue.Clear();
                        break;
                    }

                    GameEvent next = queue.Dequeue();

                    try
                    {
                        if (Raised != null)
                            Raised(next);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            finally
            {
                dispatching = false;
            }
        }

        public void Clear()
        {
            queue.Clear();
        }
    }
}
