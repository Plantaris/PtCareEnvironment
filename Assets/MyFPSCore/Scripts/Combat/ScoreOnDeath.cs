using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFPSCore.Combat;

public class ScoreOnDeath : MonoBehaviour
{
    public int scoreValue = 100;
    void Awake()
    {
        var h = GetComponent<Health>();
        if (h) h.onDied.AddListener(_ => GameManager.AddScore(scoreValue));
    }
}

public static class GameManager
{
    static int score;
    public static void AddScore(int v) { score += v; /* update HUD, etc. */ }
}
