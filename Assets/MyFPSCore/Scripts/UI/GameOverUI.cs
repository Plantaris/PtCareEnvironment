using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace MyFPSCore.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("Optional")]
        public CanvasGroup canvasGroup;        // leave null if you don't want it
        [Tooltip("If set, this object will be toggled active on show/hide (e.g., your Panel). If left empty, we'll toggle this GameObject.")]
        public GameObject rootToToggle;

        [Header("Refs")]
        public TMP_Text titleText;
        public TMP_Text subtitleText;
        public Button restartButton;
        public Button mainMenuButton;

        void Awake()
        {
            if (!rootToToggle) rootToToggle = gameObject;
            HideImmediate();
        }

        public void Show(string title, string subtitle, Action onRestart, Action onMainMenu = null)
        {
            if (titleText) titleText.text = string.IsNullOrEmpty(title) ? "Game Over" : title;
            if (subtitleText) subtitleText.text = subtitle ?? "";

            if (restartButton)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => onRestart?.Invoke());
            }

            if (mainMenuButton)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                if (onMainMenu != null)
                {
                    mainMenuButton.gameObject.SetActive(true);
                    mainMenuButton.onClick.AddListener(() => onMainMenu());
                }
                else
                {
                    mainMenuButton.gameObject.SetActive(false);
                }
            }

            SetVisible(true);
        }

        public void HideImmediate() => SetVisible(false);

        void SetVisible(bool show)
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
                // keep object active so references remain valid
                if (rootToToggle && rootToToggle != gameObject) rootToToggle.SetActive(true);
            }
            else
            {
                if (rootToToggle) rootToToggle.SetActive(show);
            }
        }
    }
}

