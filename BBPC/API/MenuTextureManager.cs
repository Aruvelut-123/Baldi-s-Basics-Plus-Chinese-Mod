using UnityEngine;
using UnityEngine.SceneManagement;

namespace BBPC.API
{
    public class MenuTextureManager : MonoBehaviour
    {
        public static MenuTextureManager Instance { get; private set; } = null!;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                API.Logger.Info("主菜单已加载，重新应用纹理...");
                Plugin.Instance.ApplyMenuTextures();
            }
        }
    }
}