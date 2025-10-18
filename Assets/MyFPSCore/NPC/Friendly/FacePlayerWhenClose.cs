using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class FacePlayerWhenClose : MonoBehaviour
{
    public float range = 3f; Transform player;
    void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform; }
    void Update()
    {
        if (!player) return;
        if (Vector3.Distance(transform.position, player.position) > range) return;
        var dir = player.position - transform.position; dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
    }
}