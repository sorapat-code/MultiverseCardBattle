using UnityEngine;

namespace MultiverseCardBattle.Battle
{
    public class TurnManager : MonoBehaviour
    {
        public int TurnNumber { get; private set; }
        public int CurrentPlayerID { get; private set; }

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

            StartTurn();
        }

        public void StartTurn()
        {
            BattlePlayer player = GetCurrentPlayer();

            // เพิ่ม Max Energy ทีละ 1 ทุกเทิร์น
            player.MaxEnergy = Mathf.Min(TurnNumber, MaxEnergy);

            // เติม Energy เต็ม
            player.CurrentEnergy = player.MaxEnergy;

            Debug.Log(
                $"Turn {TurnNumber} | " +
                $"Player {CurrentPlayerID} Turn Start | " +
                $"Energy: {player.CurrentEnergy}/{player.MaxEnergy}"
            );
        }

        public void EndTurn()
        {
            Debug.Log(
                $"Player {CurrentPlayerID} End Turn"
            );

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