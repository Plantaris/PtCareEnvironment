#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class AudioSourceValidator
{
    [MenuItem("Tools/MyFPSCore/Validate AudioSources (All Prefabs)")]
    public static void ValidateAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int issues = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            issues += ValidatePrefab(prefab, path);
        }
        Debug.Log($"[AudioSourceValidator] Scan complete. Prefabs with issues: {issues}");
    }

    [MenuItem("Tools/MyFPSCore/Validate AudioSources (Selected)")]
    public static void ValidateSelected()
    {
        var targets = Selection.gameObjects;
        if (targets == null || targets.Length == 0)
        {
            Debug.Log("[AudioSourceValidator] Select one or more prefabs or instances to validate.");
            return;
        }

        int issues = 0;
        foreach (var go in targets)
        {
            // Prefer prefab asset if possible
            var asset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) as GameObject
                        ?? PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject
                        ?? (PrefabUtility.IsPartOfPrefabAsset(go) ? go : null);

            if (asset != null)
            {
                var path = AssetDatabase.GetAssetPath(asset);
                issues += ValidatePrefab(asset, path);
            }
            else
            {
                // Fall back to validating the scene instance
                issues += ValidateHierarchy(go, "<scene instance>");
            }
        }
        Debug.Log($"[AudioSourceValidator] Selection scan complete. Objects with issues: {issues}");
    }

    [MenuItem("Tools/MyFPSCore/Disable PlayOnAwake (Selected Prefabs/Instances)")]
    public static void DisablePlayOnAwakeSelected()
    {
        var targets = Selection.gameObjects;
        if (targets == null || targets.Length == 0) return;

        foreach (var go in targets)
        {
            var sources = go.GetComponentsInChildren<AudioSource>(true);
            foreach (var src in sources)
            {
                if (src.playOnAwake)
                {
                    Undo.RecordObject(src, "Disable PlayOnAwake");
                    src.playOnAwake = false;
                    EditorUtility.SetDirty(src);
                }
            }
        }
        Debug.Log("[AudioSourceValidator] Disabled PlayOnAwake on selected objects.");
    }

    private static int ValidatePrefab(GameObject prefab, string path)
    {
        return ValidateHierarchy(prefab, path);
    }

    private static int ValidateHierarchy(GameObject root, string label)
    {
        int issues = 0;
        var sources = root.GetComponentsInChildren<AudioSource>(true);
        if (sources.Length > 1)
        {
            Debug.LogWarning($"[AudioSource] '{label}' has {sources.Length} AudioSources in its hierarchy.", root);
            issues++;
        }

        // Multiple AudioSources on the same GameObject
        foreach (var group in sources.GroupBy(s => s.gameObject).Where(g => g.Count() > 1))
        {
            var go = group.Key;
            Debug.LogError($"[AudioSource] '{GetPath(go.transform)}' in '{label}' has {group.Count()} AudioSources on a single object.", go);
            issues++;
        }

        // Any PlayOnAwake still on?
        foreach (var src in sources.Where(s => s.playOnAwake))
        {
            Debug.LogWarning($"[AudioSource] PlayOnAwake is ON at '{GetPath(src.transform)}' in '{label}'.", src);
            issues++;
        }

        return issues;
    }

    private static string GetPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }
}
#endif

