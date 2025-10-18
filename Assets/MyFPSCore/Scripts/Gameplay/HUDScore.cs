using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MyFPSCore.Gameplay
{
    public class HUDScore : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        void OnEnable()
        {
            if (!label) label = GetComponent<TMP_Text>();
            if (ScoreManager.Instance)
            {
                // initialize label and subscribe
                label.text = $"Score: {ScoreManager.Instance.Score}";
                ScoreManager.Instance.onScoreChanged += HandleScoreChanged;
            }
        }

        void OnDisable()
        {
            if (ScoreManager.Instance)
                ScoreManager.Instance.onScoreChanged -= HandleScoreChanged;
        }

        void HandleScoreChanged(int newScore)
        {
            if (label) label.text = $"Score: {newScore}";
        }
    }
}
