using UnityEngine;

namespace MultiverseCardBattle.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("GameManager Initialized");
        }

        private void Start()
        {
            // บูตแล้วไปเมนูหลัก (ไม่ต้องมี SceneLoader ในซีนก็ได้)
            SceneLoader.Go(GameScene.MainMenu);
        }
    }
}