using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class TurnManager : MonoBehaviour
    {
        public int TurnNumber { get; private set; }
        public int CurrentPlayerID { get; private set; }

        // ผู้เล่นที่เริ่มก่อน (1 หรือ 2)
        public int FirstPlayerID { get; private set; } = 1;

        // จำนวนเทิร์นรวมของทั้งสองฝั่ง (เพิ่มทุกครั้งที่เริ่มเทิร์น)
        public int TotalTurns { get; private set; }

        private BattleManager battleManager;

        private const int MaxEnergy = 10;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
        }

        public void StartBattle(int firstPlayerID = 1)
        {
            FirstPlayerID = firstPlayerID == 2 ? 2 : 1;

            TurnNumber = 1;
            CurrentPlayerID = FirstPlayerID;
            TotalTurns = 0;

            StartTurn();
        }

        public void StartTurn()
        {
            BattlePlayer player = GetCurrentPlayer();

            TotalTurns++;

            // เพิ่ม Max Energy ทีละ 1 ทุกเทิร์น
            player.MaxEnergy = Mathf.Min(TurnNumber, MaxEnergy);

            // เติม Energy เต็ม
            player.CurrentEnergy = player.MaxEnergy;

            // Overload: Energy ส่วนหนึ่งถูกล็อกในเทิร์นนี้
            if (player.OverloadedEnergy > 0)
            {
                int locked = Mathf.Min(player.OverloadedEnergy, player.CurrentEnergy);

                player.CurrentEnergy -= locked;
                player.OverloadedEnergy = 0;

                if (locked > 0)
                {
                    battleManager.Log(
                        player.Name + " has " + locked + " Energy locked (Overload)");
                }
            }

            // ยูนิตของผู้เล่นกลับมาโจมตีได้อีกครั้ง
            foreach (CardInstance card in player.Field)
            {
                card.AttacksThisTurn = 0;
                card.UnitAttacksThisTurn = 0;
            }

            player.CardsPlayedThisTurn = 0;
            player.SpellsPlayedThisTurn = 0;
            player.DarkWisdomEnergyThisTurn = 0;

            Debug.Log(
                $"Turn {TurnNumber} | " +
                $"Player {CurrentPlayerID} Turn Start | " +
                $"Energy: {player.CurrentEnergy}/{player.MaxEnergy}"
            );

            battleManager.Log(
                "Turn " + TurnNumber + ": " + player.Name + "'s turn");

            // จั่วการ์ดตอนเริ่มเทิร์น
            battleManager.DrawCard(player);

            battleManager.Publish(
                TriggerType.TurnStart,
                player: player);

            StatusProcessor.OnTurnStart(battleManager, player);

            // Doom ที่ครบกำหนด และความสามารถต้นเทิร์นที่ต้องเลือกเป้าหมาย (Grasp Heart)
            battleManager.OnTurnStartPassives(player);

            battleManager.Resolve();
        }

        public void EndTurn()
        {
            if (battleManager.IsOver)
                return;

            BattlePlayer ending = GetCurrentPlayer();

            Debug.Log(
                $"Player {CurrentPlayerID} End Turn"
            );

            battleManager.Publish(
                TriggerType.TurnEnd,
                player: ending);

            StatusProcessor.OnTurnEnd(battleManager, ending);

            battleManager.TickLeaderGear(ending);

            ClearTurnEffects();

            battleManager.Resolve();

            if (battleManager.IsOver)
                return;

            // สลับผู้เล่น: ครบรอบเมื่อกลับมาถึงคนที่เริ่มก่อน
            if (CurrentPlayerID == FirstPlayerID)
            {
                CurrentPlayerID = FirstPlayerID == 1 ? 2 : 1;
            }
            else
            {
                CurrentPlayerID = FirstPlayerID;
                TurnNumber++;
            }

            StartTurn();
        }

        // ล้างโบนัสที่ "จนจบเทิร์น" ของทุกยูนิต และคืน Taunt ที่ถูกริบไว้ชั่วคราว
        private void ClearTurnEffects()
        {
            BattlePlayer[] players = { battleManager.Player1, battleManager.Player2 };

            foreach (BattlePlayer player in players)
            {
                player.ShieldHealUsedThisTurn = false;

                foreach (CardInstance card in player.Field)
                {
                    card.TurnAttackBonus = 0;
                    card.ExtraAttacks = 0;
                    card.ClearTurnKeywords();

                    if (card.TempUnstoppableUntil >= 0 && TotalTurns >= card.TempUnstoppableUntil)
                    {
                        card.TempUnstoppableUntil = -1;

                        if (card.TempUnstoppableGranted)
                        {
                            card.TempUnstoppableGranted = false;
                            card.RemoveKeyword(Keyword.Unstoppable);
                        }
                    }

                    if (card.TauntRestoreTurn >= 0 && TotalTurns >= card.TauntRestoreTurn)
                    {
                        card.TauntRestoreTurn = -1;

                        if (!card.IsSilenced)
                            card.AddKeyword(Keyword.Taunt);
                    }
                }
            }
        }

        private BattlePlayer GetCurrentPlayer()
        {
            if (CurrentPlayerID == 1)
            {
                return battleManager.Player1;
            }

            return battleManager.Player2;
        }
    }
}
