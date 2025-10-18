using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
// Assets/MyFPSCore/Gameplay/CursorLockManager.cs
using UnityEngine;

namespace MyFPSCore.Gameplay
{
    public class CursorLockManager : MonoBehaviour
    {
        [Header("Behavior")]
        [SerializeField] bool lockOnStart = true;
        [SerializeField] KeyCode toggleKey = KeyCode.Escape; // press Esc to free mouse
        [SerializeField] bool clickToRelock = true;          // click LMB to recapture

        public static bool IsLocked { get; private set; }
        public static event System.Action<bool> OnLockChanged;

        void Start()
        {
            // --- Persist exactly one instance across scenes ---
            var others = FindObjectsOfType<CursorLockManager>();
            foreach (var c in others)
            {
                if (c != this) { Destroy(gameObject); return; }
            }

            DontDestroyOnLoad(gameObject);
            // ---------------------------------------------------

            SetLocked(lockOnStart); // apply initial state once
        }

        void Update()
        {
            // Toggle with Esc
            if (Input.GetKeyDown(toggleKey))
                SetLocked(!IsLocked);

            // Do NOT re-lock while paused or when clicking UI
            bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            if (clickToRelock && !IsLocked && Application.isFocused && Time.timeScale > 0f && !overUI && Input.GetMouseButtonDown(0))
                SetLocked(true);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            // When focus returns (alt-tab etc.), re-apply current state
            if (hasFocus) Apply();
        }

        public static void SetLocked(bool locked)
        {
            IsLocked = locked;
            Apply();
            OnLockChanged?.Invoke(IsLocked);
        }

        // Optional convenience wrappers for menus/UI
        public static void PauseForUI() => SetLocked(false);
        public static void ResumeFromUI() => SetLocked(true);

        static void Apply()
        {
            Cursor.lockState = IsLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !IsLocked;
        }
        public void OnQuitButton()
        {
            // always clean up pause state first
            Time.timeScale = 1f;
            AudioListener.pause = false;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;   // stops Play mode in Editor
#elif UNITY_WEBGL
    Debug.Log("Quit is not supported on WebGL builds.");  // optional UX message
#else
    Application.Quit();                               // quits the app in builds
#endif
        }
    }
}

