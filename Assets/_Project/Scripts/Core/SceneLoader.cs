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
            Go(scene);
        }

        /// <summary>
        /// โหลดซีนโดยไม่ต้องมี SceneLoader อยู่ในซีน (เรียกจาก UI ได้เลย)
        /// </summary>
        public static void Go(GameScene scene)
        {
            SceneManager.LoadScene(scene.ToString());
        }
    }
}   