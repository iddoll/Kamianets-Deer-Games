#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WordPuzzle.EditorTools
{
    /// <summary>One-click menu items to spin up the Word Puzzle game from the Unity Editor.</summary>
    public static class WordPuzzleSetup
    {
        private const string RootName = "WordPuzzleGame";

        [MenuItem("Tools/Word Puzzle/Add Game To Current Scene")]
        public static void AddToCurrentScene()
        {
            var existing = Object.FindFirstObjectByType<WordPuzzleBootstrap>();
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                Debug.LogWarning("Word Puzzle bootstrap already exists in this scene.");
                return;
            }

            var go = new GameObject(RootName);
            go.AddComponent<WordPuzzleBootstrap>();
            Undo.RegisterCreatedObjectUndo(go, "Add Word Puzzle");
            Selection.activeObject = go;
            Debug.Log("Word Puzzle added. Press Play to start.");
        }

        [MenuItem("Tools/Word Puzzle/Create New Game Scene")]
        public static void CreateNewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject(RootName);
            go.AddComponent<WordPuzzleBootstrap>();
            Selection.activeObject = go;
            Debug.Log("New Word Puzzle scene created. Press Play to start.");
        }
    }
}
#endif
