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
            string sceneName =
                SceneDefinitions.GetSceneName(scene);

            GameScene? previousScene = currentScene;

            SceneLoadStarted?.Invoke(scene);

            // -------------------- GET / LOAD NEW CONTENT --------------------

            Scene loaded =
                SceneManager.GetSceneByName(sceneName);

            if (!loaded.IsValid() ||
                !loaded.isLoaded)
            {
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

                loaded =
                    SceneManager.GetSceneByName(sceneName);
            }

            // -------------------- VALIDATE --------------------

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

            // -------------------- UNLOAD PREVIOUS CONTENT --------------------

            if (previousScene.HasValue &&
                previousScene.Value != scene)
            {
                string previousSceneName =
                    SceneDefinitions.GetSceneName(
                        previousScene.Value
                    );

                Scene previousLoadedScene =
                    SceneManager.GetSceneByName(
                        previousSceneName
                    );

                if (previousLoadedScene.IsValid() &&
                    previousLoadedScene.isLoaded)
                {
                    AsyncOperation unloadOperation =
                        SceneManager.UnloadSceneAsync(
                            previousLoadedScene
                        );

                    if (unloadOperation != null)
                    {
                        yield return unloadOperation;
                    }
                }
            }

            // -------------------- COMPLETE --------------------

            SceneLoadCompleted?.Invoke(scene);
        }

        public Coroutine PreloadScene(GameScene scene)
        {
            return StartCoroutine(PreloadSceneRoutine(scene));
        }

        private IEnumerator PreloadSceneRoutine(GameScene scene)
        {
            string sceneName =
                SceneDefinitions.GetSceneName(scene);

            Scene existingScene =
                SceneManager.GetSceneByName(sceneName);

            // Already loaded.
            if (existingScene.IsValid() &&
                existingScene.isLoaded)
            {
                yield break;
            }

            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive
                );

            if (loadOperation == null)
            {
                Debug.LogError(
                    $"[{nameof(SceneLoadManager)}] " +
                    $"Unable to preload scene '{sceneName}'."
                );

                yield break;
            }

            yield return loadOperation;

            Scene loadedScene =
                SceneManager.GetSceneByName(sceneName);

            if (!loadedScene.IsValid() ||
                !loadedScene.isLoaded)
            {
                Debug.LogError(
                    $"[{nameof(SceneLoadManager)}] " +
                    $"Preloaded scene '{sceneName}' could not be validated."
                );

                yield break;
            }

            Debug.Log(
                $"[{nameof(SceneLoadManager)}] " +
                $"Scene '{sceneName}' preloaded successfully."
            );
        }
    }
}