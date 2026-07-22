#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShadowMaze.EditorTools
{
    /// <summary>
    /// Adds "Build Preview" / "Clear" buttons to the bootstrap Inspector so the whole game can
    /// be generated in Edit Mode. The generated objects live under "_Generated" and persist in
    /// the scene (they can be inspected, tweaked and saved). Rebuild after changing any setting.
    /// </summary>
    [CustomEditor(typeof(StealthGameBootstrap))]
    public class StealthGameBootstrapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var bootstrap = (StealthGameBootstrap)target;
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Play rebuilds automatically. Use these buttons to preview/tune the layout in " +
                "Edit Mode. Generated content is grouped under the \"_Generated\" child object.",
                MessageType.Info);

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
