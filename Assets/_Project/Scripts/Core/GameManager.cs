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
            SceneLoader.Instance.LoadScene(GameScene.MainMenu);
        }
    }
}