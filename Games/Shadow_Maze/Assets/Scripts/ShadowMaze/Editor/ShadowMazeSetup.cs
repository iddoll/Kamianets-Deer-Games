#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShadowMaze.EditorTools
{
    /// <summary>
    /// Convenience menu items so the game can be spun up with one click from the Unity Editor.
    /// </summary>
    public static class ShadowMazeSetup
    {
        private const string RootName = "ShadowMazeGame";

        [MenuItem("Tools/Shadow Maze/Add Game To Current Scene")]
        public static void AddToCurrentScene()
        {
            var existing = Object.FindFirstObjectByType<StealthGameBootstrap>();
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                Debug.LogWarning("Shadow Maze bootstrap already exists in this scene.");
                return;
            }

            var go = new GameObject(RootName);
            go.AddComponent<StealthGameBootstrap>();
            Undo.RegisterCreatedObjectUndo(go, "Add Shadow Maze");
            Selection.activeObject = go;
            Debug.Log("Shadow Maze added. Press Play to start.");
        }

        [MenuItem("Tools/Shadow Maze/Create New Game Scene")]
        public static void CreateNewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);
            var go = new GameObject(RootName);
            go.AddComponent<StealthGameBootstrap>();
            Selection.activeObject = go;
            Debug.Log("New Shadow Maze scene created. Press Play to start.");
        }
    }
}
#endif
