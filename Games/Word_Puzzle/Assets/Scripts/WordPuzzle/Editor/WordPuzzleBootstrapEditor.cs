#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WordPuzzle.EditorTools
{
    /// <summary>
    /// Adds "Build Preview" / "Clear" buttons to the bootstrap Inspector so the game can be
    /// generated in Edit Mode. Generated content lives under "_Generated"; entering Play always
    /// rebuilds a fresh copy from the current settings. (Drag-and-drop only works in Play Mode.)
    /// </summary>
    [CustomEditor(typeof(WordPuzzleBootstrap))]
    public class WordPuzzleBootstrapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var bootstrap = (WordPuzzleBootstrap)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Play rebuilds automatically. Use these buttons to preview the layout in Edit Mode. " +
                "Dragging letters works only in Play Mode.", MessageType.Info);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Build / Refresh Preview", GUILayout.Height(32)))
                {
                    bootstrap.BuildGame();
                    Mark(bootstrap);
                }
                if (GUILayout.Button("Clear Generated"))
                {
                    bootstrap.ClearGenerated();
                    Mark(bootstrap);
                }
            }
        }

        private static void Mark(Object target)
        {
            var comp = target as Component;
            if (comp != null && !Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
        }
    }
}
#endif
