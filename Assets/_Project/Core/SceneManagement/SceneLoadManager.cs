using System;
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

        public event Action<GameScene> SceneLoadStarted;
        public event Action<GameScene> SceneLoadCompleted;
        public event Action<GameScene, string> SceneLoadFailed;

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
            string sceneName = SceneDefinitions.GetSceneName(scene);

            SceneLoadStarted?.Invoke(scene);

            try
            {
                SceneManager.LoadScene(sceneName);

                SceneLoadCompleted?.Invoke(scene);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SceneLoadManager)}] Failed to load " +
                    $"scene '{scene}': {exception.Message}"
                );

                SceneLoadFailed?.Invoke(scene, exception.Message);
            }
        }
    }
}