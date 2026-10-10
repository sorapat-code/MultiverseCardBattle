using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    public class TurnManager : MonoBehaviour
    {
        public int TurnNumber { get; private set; }
        public int CurrentPlayerID { get; private set; }

        // จำนวนเทิร์นรวมของทั้งสองฝั่ง (เพิ่มทุกครั้งที่เริ่มเทิร์น)
        public int TotalTurns { get; private set; }

        private BattleManager battleManager;

        private const int MaxEnergy = 10;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
        }

        public void StartBattle()
        {
            TurnNumber = 1;
            CurrentPlayerID = 1;
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

            // ยูนิตของผู้เล่นกลับมาโจมตีได้อีกครั้ง
            foreach (CardInstance card in player.Field)
                card.AttacksThisTurn = 0;

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

            battleManager.Resolve();

            if (battleManager.IsOver)
                return;

            // สลับผู้เล่น
            if (CurrentPlayerID == 1)
            {
                CurrentPlayerID = 2;
            }
            else
            {
                CurrentPlayerID = 1;
                TurnNumber++;
            }

            StartTurn();
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
