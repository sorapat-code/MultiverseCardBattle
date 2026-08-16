using UnityEngine;

namespace MultiverseCardBattle.Battle
{
    public class BattleManager : MonoBehaviour
    {
        [System.NonSerialized]
        public BattlePlayer Player1;

        [System.NonSerialized]
        public BattlePlayer Player2;

        public TurnManager TurnManager { get; private set; }

        private void Awake()
        {
            Player1 = new BattlePlayer(1, 30);
            Player2 = new BattlePlayer(2, 30);

            TurnManager = GetComponent<TurnManager>();
        }

        private void Start()
        {
            StartBattle();
        }

        public void StartBattle()
        {
            Debug.Log("Battle Started!");

            Debug.Log(
                $"Player 1 HP: {Player1.LeaderHealth} | " +
                $"Player 2 HP: {Player2.LeaderHealth}"
            );

            TurnManager.StartBattle();
        }
    }
}