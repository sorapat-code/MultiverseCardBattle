using UnityEngine;
using UnityEngine.SceneManagement;
using MultiverseCardBattle.Core;

namespace MultiverseCardBattle.Screens
{
    /// <summary>
    /// ใส่หน้าจอที่ตรงกับชื่อซีนให้อัตโนมัติ (Boot, MainMenu, Campaign, Battle,
    /// Collection, DeckBuilder) ไม่ต้องตั้งค่า GameObject ในซีนด้วยมือ
    /// </summary>
    public static class ScreenBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Attach(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Attach(scene);
        }

        private static void Attach(Scene scene)
        {
            GameScene id;

            if (!System.Enum.TryParse(scene.name, out id))
                return;

            string objectName = id + "Screen";

            if (GameObject.Find(objectName) != null)
                return;

            switch (id)
            {
                case GameScene.Boot:
                    // GameManager ในซีน Boot จะพาไปเมนูเอง ถ้าไม่มีก็พาไปเลย
                    if (Object.FindAnyObjectByType<GameManager>() == null)
                        SceneLoader.Go(GameScene.MainMenu);
                    return;

                case GameScene.MainMenu:
                    Create<MainMenuScreen>(objectName);
                    break;

                case GameScene.Campaign:
                    Create<CampaignScreen>(objectName);
                    break;

                case GameScene.Collection:
                    Create<CollectionScreen>(objectName);
                    break;

                case GameScene.DeckBuilder:
                    Create<DeckBuilderScreen>(objectName);
                    break;

                case GameScene.Battle:
                    // ปิดสคริปต์ทดสอบ Effect ก่อนที่ Start จะทำงาน
                    // (ไม่งั้นมันจะแก้สถานะการต่อสู้จริง)
                    foreach (MultiverseCardBattle.Effects.CardActionTest test in
                        Object.FindObjectsByType<MultiverseCardBattle.Effects.CardActionTest>(
                            FindObjectsSortMode.None))
                    {
                        test.enabled = false;
                    }

                    Create<BattleScreen>(objectName);
                    break;
            }
        }

        private static void Create<T>(string objectName) where T : MonoBehaviour
        {
            GameObject go = new GameObject(objectName);

            go.AddComponent<T>();
        }
    }
}
