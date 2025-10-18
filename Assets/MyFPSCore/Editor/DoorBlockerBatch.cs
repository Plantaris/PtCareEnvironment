using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System;

public static class DoorBlockerBatch
{
    [MenuItem("Tools/Doors/Add NavMesh Blockers To All Doors In Scene")]
    static void AddBlockers()
    {
        int doorsFound = 0, blockersAdded = 0;

        // Find all behaviours, including inactive, no hard reference to your door type
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var b in behaviours)
        {
            var t = b.GetType();
            if (t.Name != "DoorLockAnimator") continue;   // your door script name
            // Optional: tighten match if you want
            // if (t.Namespace != "MyFPSCore.Doors") continue;

            doorsFound++;

            var doorGO = b.gameObject;

            // 1) Ensure a "DoorBlocker" child with a carving NavMeshObstacle exists
            var blockerTr = doorGO.transform.Find("DoorBlocker");
            if (blockerTr == null)
            {
                var go = new GameObject("DoorBlocker");
                go.transform.SetParent(doorGO.transform, false);

                var obs = go.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.carveOnlyStationary = true;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = GuessSize(doorGO.name); // tweak once if needed

                blockersAdded++;
            }

            // 2) Ensure the runtime toggler (DoorNavBlocker) exists, via reflection
            var navBlockerType = FindType("MyFPSCore.Doors.DoorNavBlocker");
            if (navBlockerType != null && doorGO.GetComponent(navBlockerType) == null)
            {
                doorGO.AddComponent(navBlockerType);
            }
        }

        Debug.Log($"DoorBlockerBatch: scanned {doorsFound} doors, added blockers to {blockersAdded}.");
    }

    static Vector3 GuessSize(string name)
    {
        // Adjust these once to fit your frames
        if (name.ToLower().Contains("double")) return new Vector3(2.4f, 2.2f, 0.3f);
        return new Vector3(1.2f, 2.2f, 0.3f);
    }

    static Type FindType(string fullName)
    {
        // Search all loaded assemblies for the type by FullName
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
}
#endif

