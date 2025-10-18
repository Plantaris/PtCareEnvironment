using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MyFPSCore.Gameplay
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] int startScore = 0;
        public int Score { get; private set; }

        public event Action<int> onScoreChanged;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Score = startScore;
            onScoreChanged?.Invoke(Score);
        }

        public void Add(int delta)
        {
            if (delta == 0) return;
            Score += delta;
            Debug.Log($"[Score] +{delta} → total {Score}");
            onScoreChanged?.Invoke(Score);
        }

        public void ResetTo(int value = 0)
        {
            Score = value;
            onScoreChanged?.Invoke(Score);
        }
    }
}
