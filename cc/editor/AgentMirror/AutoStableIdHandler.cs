using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AutoStableIdHandler
{
    private static double _lastCheckTime;
    private const double DebounceSeconds = 0.5f;

    static AutoStableIdHandler()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - _lastCheckTime < DebounceSeconds) return;
        _lastCheckTime = now;

        EditorApplication.delayCall -= CheckNewObjects;
        EditorApplication.delayCall += CheckNewObjects;
    }

    private static void CheckNewObjects()
    {
        int added = 0;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
                added += AddMissingStableIds(root);
        }

        if (added > 0)
            Debug.Log($"[AutoStableId] Added StableId to {added} new GameObject(s).");
    }

    private static int AddMissingStableIds(GameObject go)
    {
        int count = 0;

        // Skip if part of a prefab instance whose root already has StableId —
        // prefabs should carry their own StableId from the asset.
        bool partOfPrefab = PrefabUtility.IsPartOfPrefabInstance(go);
        if (partOfPrefab)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            // If the prefab root already has StableId, assume all children are covered
            if (root != null && root.GetComponent<StableId>() != null)
                return 0;
        }

        if (go.GetComponent<StableId>() == null)
        {
            go.AddComponent<StableId>();
            count++;
        }

        foreach (Transform child in go.transform)
            count += AddMissingStableIds(child.gameObject);

        return count;
    }
}
