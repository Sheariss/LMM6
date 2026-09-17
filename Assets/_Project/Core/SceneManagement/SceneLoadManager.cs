using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Atlas.Core.SceneManagement
{
    public sealed class SceneLoadManager : MonoBehaviour
    {
        public static SceneLoadManager Instance { get; private set; }

        public event Action<GameScene> SceneLoadStarted;
        public event Action<GameScene> SceneLoadCompleted;
        public event Action<GameScene, string> SceneLoadFailed;

        private GameScene? currentScene;

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

            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(GameScene scene)
        {
            StartCoroutine(LoadSceneRoutine(scene));
        }

        private IEnumerator LoadSceneRoutine(GameScene scene)
        {
            string sceneName = SceneDefinitions.GetSceneName(scene);

            SceneLoadStarted?.Invoke(scene);

            // -------------------- UNLOAD CURRENT CONTENT --------------------
            if (currentScene.HasValue)
            {
                string currentSceneName =
                    SceneDefinitions.GetSceneName(
                        currentScene.Value
                    );

                Scene loadedScene =
                    SceneManager.GetSceneByName(
                        currentSceneName
                    );

                if (loadedScene.IsValid() &&
                    loadedScene.isLoaded)
                {
                    AsyncOperation unloadOperation =
                        SceneManager.UnloadSceneAsync(
                            loadedScene
                        );

                    if (unloadOperation != null)
                    {
                        yield return unloadOperation;
                    }
                }
            }

            // -------------------- LOAD NEW CONTENT --------------------
            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive
                );

            if (loadOperation == null)
            {
                string message =
                    $"Unable to begin loading scene '{sceneName}'.";

                Debug.LogError(
                    $"[{nameof(SceneLoadManager)}] {message}"
                );

                SceneLoadFailed?.Invoke(
                    scene,
                    message
                );
                yield break;
            }
            yield return loadOperation;

            // -------------------- VALIDATE --------------------
            Scene loaded =
                SceneManager.GetSceneByName(
                    sceneName
                );

            if (!loaded.IsValid() ||
                !loaded.isLoaded)
            {
                string message =
                    $"Scene '{sceneName}' failed to load.";

                Debug.LogError(
                    $"[{nameof(SceneLoadManager)}] {message}"
                );

                SceneLoadFailed?.Invoke(
                    scene,
                    message
                );
                yield break;
            }

            // -------------------- ACTIVE SCENE --------------------
            SceneManager.SetActiveScene(loaded);
            currentScene = scene;
            SceneLoadCompleted?.Invoke(scene);
        }
    }
}