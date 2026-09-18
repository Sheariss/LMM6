using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Atlas.Development
{
    [InitializeOnLoad]
    public static class PlayFromBootstrap
    {
        private const int BootstrapBuildIndex = 0;

        static PlayFromBootstrap()
        {
            EditorApplication.delayCall += SetPlayModeStartScene;
        }

        private static void SetPlayModeStartScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes == null || scenes.Length <= BootstrapBuildIndex)
            {
                Debug.LogError("[PlayFromBootstrap] No scene exists at build index 0.");
                return;
            }

            EditorBuildSettingsScene bootstrapSceneEntry = scenes[BootstrapBuildIndex];

            if (!bootstrapSceneEntry.enabled)
            {
                Debug.LogError("[PlayFromBootstrap] Scene at build index 0 is disabled.");
                return;
            }

            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(bootstrapSceneEntry.path);

            if (bootstrapScene == null)
            {
                Debug.LogError("[PlayFromBootstrap] Could not load the scene at build index 0.");
                return;
            }

            if (EditorSceneManager.playModeStartScene != bootstrapScene)
            {
                EditorSceneManager.playModeStartScene = bootstrapScene;
            }
        }
    }
}