using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MyFPSCore.Gameplay
{
    [RequireComponent(typeof(Button))]
    public class PauseButton : MonoBehaviour
    {
        public enum Action { Resume, QuitToMenu }
        public Action action = Action.Resume;

        PauseController controller;
        Button btn;

        void Awake()
        {
            btn = GetComponent<Button>();
            btn.onClick.RemoveAllListeners();           // ensure clean slate
            btn.onClick.AddListener(OnClick);           // always wire to THIS instance
        }

        void OnEnable()
        {
            // (Re)locate controller even after scene loads
            if (!controller) controller = FindObjectOfType<PauseController>(true);
        }

        void OnClick()
        {
            if (!controller) controller = FindObjectOfType<PauseController>(true);
            if (!controller) return;

            if (action == Action.Resume) controller.Resume();
            else controller.ReturnToMainMenu();
        }
    }
}
