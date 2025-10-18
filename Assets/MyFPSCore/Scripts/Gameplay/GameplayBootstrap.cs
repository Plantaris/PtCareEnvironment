using System.Collections;
using System.Collections.Generic;
// GameplayBootstrap.cs  (add to gameplay scenes)
using UnityEngine;
using MyFPSCore.Gameplay; // CursorLockManager

public class GameplayBootstrap : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        CursorLockManager.SetLocked(true);  // ensure you re-enter locked/aim mode
    }
}
