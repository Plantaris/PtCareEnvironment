using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Combat;

public class LootOnDeath : MonoBehaviour
{
    public GameObject lootPrefab;
    public int count = 1;

    void Awake()
    {
        var h = GetComponent<Health>();
        if (h) h.onDied.AddListener(_ => Drop());
    }

    void Drop()
    {
        if (!lootPrefab) return;
        for (int i = 0; i < count; i++)
            Instantiate(lootPrefab, transform.position + Vector3.up * 0.5f, Random.rotation);
    }
}

