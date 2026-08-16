using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiverseCardBattle.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void LoadScene(GameScene scene)
        {
            SceneManager.LoadScene(scene.ToString());
        }
    }
}   