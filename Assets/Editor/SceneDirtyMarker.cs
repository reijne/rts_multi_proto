using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SceneDirtyMarker
{
    private const string markerPath = "Temp/scene_dirty_marker";

    static SceneDirtyMarker()
    {
        EditorApplication.update += CheckSceneDirty;
    }

    static void CheckSceneDirty()
    {
        bool isDirty = EditorSceneManager.GetActiveScene().isDirty;

        if (isDirty && !File.Exists(markerPath))
        {
            File.WriteAllText(markerPath, "Scene is dirty");
        }
        else if (!isDirty && File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }
    }
}
