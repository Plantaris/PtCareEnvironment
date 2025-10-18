using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System;

public static class DoorSceneRetrofit
{
    [MenuItem("Tools/Doors/Retrofit Active Scene Doors (add blocker + script)")]
    static void RetrofitScene()
    {
        int found = 0, blockersAdded = 0, scriptsAdded = 0;

        // reflection types (avoid asmdef coupling)
        var doorNavType = FindType("MyFPSCore.Doors.DoorNavBlocker");

        // find all behaviours in scene (incl. inactive)
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var b in behaviours)
        {
            var t = b.GetType();
            if (t.Name != "DoorLockAnimator") continue; // your door controller name
            found++;

            var root = b.gameObject;

            // 1) Ensure DoorBlocker child with carved obstacle
            var blockerTr = root.transform.Find("DoorBlocker");
            NavMeshObstacle obs = null;
            if (blockerTr == null)
            {
                var go = new GameObject("DoorBlocker");
                Undo.RegisterCreatedObjectUndo(go, "Create DoorBlocker");
                go.transform.SetParent(root.transform, false);

                obs = Undo.AddComponent<NavMeshObstacle>(go);
                obs.carving = true;
                obs.carveOnlyStationary = true;
                obs.shape = NavMeshObstacleShape.Box;
                obs.size = GuessSizeByName(root.name);

                blockersAdded++;
            }
            else
            {
                obs = blockerTr.GetComponent<NavMeshObstacle>() ?? Undo.AddComponent<NavMeshObstacle>(blockerTr.gameObject);
                obs.carving = true;
                obs.carveOnlyStationary = true;
                obs.shape = NavMeshObstacleShape.Box;
                if (obs.size == Vector3.zero) obs.size = GuessSizeByName(root.name);
            }

            // 2) Ensure DoorNavBlocker on root
            if (doorNavType != null && root.GetComponent(doorNavType) == null)
            {
                var comp = Undo.AddComponent(root, doorNavType) as Component;

                // pre-fill animator bool names on the newly added component
                if (comp != null)
                {
                    var so = new SerializedObject(comp);
                    var openProp = so.FindProperty("openBool");
                    var closeProp = so.FindProperty("closeBool");
                    if (openProp != null) openProp.stringValue = "MyFPSCore_OpenDoor";
                    if (closeProp != null) closeProp.stringValue = "MyFPSCore_CloseDoor";
                    so.ApplyModifiedPropertiesWithoutUndo();
                    scriptsAdded++;
                }
            }
        }

        Debug.Log($"DoorSceneRetrofit: found {found} door(s), added {blockersAdded} blocker(s), added {scriptsAdded} DoorNavBlocker(s).");
    }

    static Vector3 GuessSizeByName(string name)
    {
        // Tweak once if needed; thickness (Z) can be small because we're carving navmesh, not physics.
        string n = name.ToLowerInvariant();
        if (n.Contains("double")) return new Vector3(5.4f, 4.2f, 0.30f);  // width, height, thickness
        return new Vector3(2.2f, 2.2f, 0.30f);
    }

    static Type FindType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
}
#endif

