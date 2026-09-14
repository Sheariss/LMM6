using UnityEngine;
using UnityEngine.SceneManagement;

namespace Atlas.Core.SceneManagement
{
    /// <summary>
    /// Provides centralized access to Unity scene loading.
    /// </summary>
    public sealed class SceneLoadManager : MonoBehaviour
    {
        public static SceneLoadManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(SceneLoadManager)}] Duplicate instance detected. " +
                    "Destroying duplicate."
                );

                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Loads the specified IIS scene.
        /// </summary>
        /// <param name="scene">
        /// The scene to load.
        /// </param>
        public void LoadScene(GameScene scene)
        {
            SceneManager.LoadScene(
                SceneDefinitions.GetSceneName(scene)
            );
        }
    }
}