using BepInEx;
using BepInEx.Configuration;
using Cinemachine;
using GorillaGameModes;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Utilla;

namespace SpectatePro
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        ConfigEntry<float>? moveSpeed, smoothTime, mouseSensitivity;
        bool freecam = true, inUI;
        Vector3 velocity = Vector3.zero, inputDirection;
        float rotationX = 0f, rotationY = 0f;
        GameObject? ThirdCam, FollowTarget;
        CinemachineBrain? FollowBrain;

        List<RigContainer> Rigs = new List<RigContainer>();

        public static bool NotVR => !XRSettings.isDeviceActive;

        CursorLockMode UILock() => inUI ? CursorLockMode.None : CursorLockMode.Locked;

        void Start()
        {
            moveSpeed = Config.Bind("Settings", "Move Speed", 3f, "The raw move speed of the freecam");
            smoothTime = Config.Bind("Settings", "Smoothing", 0.1f, "The subtle smoothing on the movement");
            mouseSensitivity = Config.Bind("Settings", "Mouse Sensitivity", 1f, "The sensitivity of the mouse for rotating the camera");
        }

        void Update()
        {
            if (!NotVR)
            {
                if (PhotonNetwork.InRoom)
                {
                    Rigs = VRRigCache.rigsInUse.Values;
                }
                if (ThirdCam != null)
                {
                    ThirdCam.transform.SetParent(GorillaTagger.Instance.thirdPersonCamera.transform, false);
                    ThirdCam.transform.rotation = Quaternion.Euler(Vector3.zero);
                    ThirdCam = null;
                }
            }
            else
            {
                Cursor.lockState = UILock();
                if (ThirdCam == null)
                {
                    ThirdCam = GorillaTagger.Instance.thirdPersonCamera.transform.GetChild(0).gameObject;
                    ThirdCam.transform.SetParent(null);
                    FollowBrain = ThirdCam.GetComponent<CinemachineBrain>();
                }
                else if(FollowBrain != null)
                {
                    FollowBrain.enabled = !freecam;
                    if (freecam)
                    {
                        RotateCamera();
                        GetFreecamInput();
                        Vector3 targetPosition = ThirdCam.transform.position + inputDirection * moveSpeed.Value * Time.deltaTime;
                        ThirdCam.transform.position = Vector3.SmoothDamp(ThirdCam.transform.position, targetPosition, ref velocity, smoothTime.Value);
                    }
                }
            }
        }

        void GetSwapperInput()
        {

        }

        void GetFreecamInput()
        {
            inputDirection = Vector3.zero;
            Transform cameraTransform = ThirdCam.transform;
            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

            if (Keyboard.current.wKey.isPressed) inputDirection += cameraForward;
            if (Keyboard.current.sKey.isPressed) inputDirection -= cameraForward;
            if (Keyboard.current.aKey.isPressed) inputDirection -= cameraRight;
            if (Keyboard.current.dKey.isPressed) inputDirection += cameraRight;
            if (Keyboard.current.spaceKey.isPressed) inputDirection += Vector3.up;
            if (Keyboard.current.leftCtrlKey.isPressed) inputDirection += Vector3.down;

            inputDirection = inputDirection.normalized;
        }

        void RotateCamera()
        {
            rotationX = Mathf.Clamp(rotationX - Mouse.current.delta.y.ReadValue() * mouseSensitivity.Value, -90f, 90f);
            rotationY += Mouse.current.delta.x.ReadValue() * mouseSensitivity.Value;
            ThirdCam.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
    }
}
