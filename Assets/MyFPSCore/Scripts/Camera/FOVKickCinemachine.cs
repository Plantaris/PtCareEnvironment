using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using MyFPSCore.Player;

public class FOVKickCinemachine : MonoBehaviour
{
    public CinemachineVirtualCamera vcam;
    public BetterFPSController controller;
    [Tooltip("Leave 0 to auto-use current vcam FOV")]
    public float baseFOV = 0f;
    public float sprintFOV = 65f;
    public float smoothTime = 0.15f;
    float vel, current;

    void Awake()
    {
        if (vcam)
        {
            if (baseFOV <= 0f) baseFOV = vcam.m_Lens.FieldOfView; // auto-detect
            current = baseFOV;
            vcam.m_Lens.FieldOfView = baseFOV;
        }
    }

    void Update()
    {
        if (!vcam || !controller) return;
        float target = controller.IsSprinting ? sprintFOV : baseFOV;
        current = Mathf.SmoothDamp(current, target, ref vel, smoothTime);
        vcam.m_Lens.FieldOfView = current;
    }
}

