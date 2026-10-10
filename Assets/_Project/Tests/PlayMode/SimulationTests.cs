using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ให้ AI เล่นกับ AI ครบทั้งเกมในทุกด่าน เพื่อตรวจว่าเกมไม่ค้าง/ไม่พัง
    /// และพิมพ์อัตราชนะของเด็คเริ่มต้นต่อเด็คศัตรูแต่ละด่านไว้ใน Console (ใช้ปรับสมดุล)
    /// </summary>
    public class SimulationTests
    {
        private const int GamesPerStage = 6;
        private const int MaxSteps = 4000;

        [TearDown]
        public void TearDown()
        {
            BattleSetup.Reset();
        }

        [UnityTest]
        public IEnumerator EveryCampaignStage_AIVsAI_GamesAlwaysFinish()
        {
            foreach (CampaignStage stage in CampaignLibrary.Stages)
            {
                int starterWins = 0;
                int draws = 0;
                int totalTurns = 0;

                for (int game = 0; game < GamesPerStage; game++)
                {
                    BattleSetup.Reset();

                    BattleSetup.PlayerDeck = StarterDecks.CreatePlayerStarterDeck();
                    BattleSetup.EnemyDeck = StarterDecks.ToCards(stage.EnemyDeckIds);
                    BattleSetup.EnemyHealth = stage.EnemyHealth;

                    GameObject host = new GameObject("SimBattle");
                    BattleManager battle = host.AddComponent<BattleManager>();

                    // รอให้ Start ของ BattleManager เริ่มการต่อสู้
                    yield return null;

                    Assert.IsTrue(battle.HasStarted, stage.Name);

                    bool finished = Simulate(battle);

                    Assert.IsTrue(
                        finished,
                        stage.Name + ": game " + game + " did not finish in " +
                        MaxSteps + " steps (turn " + battle.TurnManager.TurnNumber + ")");

                    if (battle.Winner == battle.Player1)
                        starterWins++;
                    else if (battle.Winner == null)
                        draws++;

                    totalTurns += battle.TurnManager.TurnNumber;

                    Object.Destroy(host);

                    yield return null;
                }

                Debug.Log(
                    "[Sim] " + stage.Name + ": starter deck won " + starterWins +
                    "/" + GamesPerStage + " (draws " + draws + "), avg turns " +
                    (totalTurns / (float)GamesPerStage).ToString("0.0"));
            }
        }

        private static bool Simulate(BattleManager battle)
        {
            for (int step = 0; step < MaxSteps && !battle.IsOver; step++)
            {
                if (battle.HasPendingDiscover)
                {
                    battle.ResolveDiscover(
                        battle.PendingDiscover.Result.Options[0]);

                    continue;
                }

                BattlePlayer player = battle.CurrentPlayer;

                IBattleCommand command = AIPlanner.NextCommand(battle, player);

                ActionResult result = command == null
                    ? battle.Submit(new EndTurnCommand(player))
                    : battle.Submit(command);

                // คำสั่งที่ใช้ไม่ได้: จบเทิร์นเพื่อไม่ให้วนซ้ำ
                if (!result.Success && command != null)
                    battle.Submit(new EndTurnCommand(player));
            }

            return battle.IsOver;
        }
    }
}
