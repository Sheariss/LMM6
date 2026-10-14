using System;
using UnityEditor;
using UnityEngine;

namespace Atlas.Development
{
    [CreateAssetMenu(
        fileName = "FolderSpriteConverter",
        menuName = "ATLAS/Development/Folder Sprite Converter")]
    public sealed class FolderSpriteConverter : ScriptableObject
    {
        [Tooltip("Project-relative folder, e.g. Assets/_Project/Art/Icons")]
        [SerializeField]
        private string folderPath = "Assets/_Project/Art";

        [SerializeField]
        private bool includeSubfolders = true;

        public void ConvertImages()
        {
            string folder = (folderPath ?? string.Empty)
                .Trim()
                .Replace('\\', '/')
                .TrimEnd('/');

            if (!(folder == "Assets" ||
                  folder.StartsWith("Assets/", StringComparison.Ordinal)) ||
                !AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError(
                    "Enter a valid folder path inside Assets.", this);
                return;
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D", new[] { folder });

            int converted = 0;

            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (!includeSubfolders)
                    {
                        int separator = path.LastIndexOf('/');
                        string parent = path.Substring(0, separator);

                        if (parent != folder)
                            continue;
                    }

                    if (AssetImporter.GetAtPath(path)
                        is not TextureImporter importer)
                    {
                        continue;
                    }

                    if (importer.textureType == TextureImporterType.Sprite &&
                        importer.spriteImportMode == SpriteImportMode.Single)
                    {
                        continue;
                    }

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();

                    converted++;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            Debug.Log(
                $"Converted {converted} image(s) to Single sprites in '{folder}'.",
                this);
        }
    }

    [CustomEditor(typeof(FolderSpriteConverter))]
    public sealed class FolderSpriteConverterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(
                EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Convert to Single Sprites"))
                {
                    ((FolderSpriteConverter)target).ConvertImages();
                }
            }
        }
    }
}