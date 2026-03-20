/* ---------------------------
 * Input handler only: OVR and Keyboard. No position math — delegates to InterOccularSightAdjuster.
 * Handles: UI/sights toggle, stereo mode switch, sight movement (horizontal/depth).
--------------------------- */

using System.Reflection;
using UnityEngine;

namespace InterOccularDebug
{
    public class InterOccularPositionController : MonoBehaviour
    {
        public static InterOccularPositionController Instance { get; private set; }

        [Header("References")] [SerializeField]
        private InterOccularSightAdjuster sightAdjuster;

        [SerializeField] private CustomIPDOverride customIPDOverride;
        [SerializeField] private OVRCameraRig cameraRig;

        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 0.1f;

        [SerializeField] [Range(0f, 0.5f)] private float deadZone = 0.1f;

        [Header("Mode Switch (when UI visible)")] [SerializeField]
        private float joystickDeadZone = 0.5f;

        [SerializeField] private float modeSwitchCooldown = 0.6f;

        [Header("Input")] [SerializeField] private bool enableKeyboard = true;

        private bool hideObjects = true;
        private bool changeHorizontal = true;
        private StereoTestMode currentMode = StereoTestMode.PerEyeDefaultBuggy;
        private float modeSwitchCooldownRemaining;

        public bool HideObjects => hideObjects;
        public StereoTestMode CurrentMode => currentMode;
        public float CurrentLeftOffset => sightAdjuster != null ? sightAdjuster.CurrentLeftOffset : 0f;
        public float CurrentRightOffset => sightAdjuster != null ? sightAdjuster.CurrentRightOffset : 0f;
        public float Separation => sightAdjuster != null ? sightAdjuster.Separation : 0f;
        

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (sightAdjuster == null)
                sightAdjuster = FindObjectOfType<InterOccularSightAdjuster>();
            if (customIPDOverride == null)
                customIPDOverride = FindObjectOfType<CustomIPDOverride>();
            if (cameraRig == null)
                cameraRig = FindObjectOfType<OVRCameraRig>();

            ApplyMode(currentMode);
        }

        
        public float GetCameraSeparation()
        {
            if (cameraRig == null) return 0f;
            if (cameraRig.usePerEyeCameras)
                return cameraRig.leftEyeAnchor.GetComponent<Camera>().stereoSeparation;
            else
                return cameraRig.centerEyeAnchor.GetComponent<Camera>().stereoSeparation;

        }
        
        private void Update()
        {
            if (sightAdjuster == null) return;

            sightAdjuster.SetVisibility(!hideObjects);

            // ---- OVR input ----
            HandleOVRToggleUI();
            if (hideObjects)
            {
                HandleOVRModeSwitch();
            }
            else
            {
                HandleOVRSightMovement();
            }

            // ---- Keyboard input (separate) ----
            if (enableKeyboard)
            {
                HandleKeyboardToggleUI();
                if (hideObjects)
                {
                    HandleKeyboardModeSwitch();
                }
                else
                {
                    HandleKeyboardSightMovement();
                }
            }
        }

        // ---------- OVR input ----------

        private void HandleOVRToggleUI()
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
                hideObjects = !hideObjects;
        }

        private void HandleOVRModeSwitch()
        {
            if (modeSwitchCooldownRemaining > 0f)
                modeSwitchCooldownRemaining -= Time.deltaTime;

            float rStickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y;
            float lStickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;
            float stickY = (Mathf.Abs(rStickY) > Mathf.Abs(lStickY)) ? rStickY : lStickY;
            if (modeSwitchCooldownRemaining <= 0f)
            {
                if (stickY > joystickDeadZone)
                {
                    SetMode(GetNextMode(-1));
                    modeSwitchCooldownRemaining = modeSwitchCooldown;
                }
                else if (stickY < -joystickDeadZone)
                {
                    SetMode(GetNextMode(+1));
                    modeSwitchCooldownRemaining = modeSwitchCooldown;
                }
            }
        }

        private void HandleOVRSightMovement()
        {
            if (sightAdjuster == null) return;

            Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
            Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

            if (changeHorizontal)
            {
                if (Mathf.Abs(leftStick.x) > deadZone)
                    sightAdjuster.MoveLeft(leftStick.x * moveSpeed * Time.deltaTime);
                if (Mathf.Abs(rightStick.x) > deadZone)
                    sightAdjuster.MoveRight(rightStick.x * moveSpeed * Time.deltaTime);
            }
            else
            {
                float depth = (Mathf.Abs(leftStick.y) > deadZone ? leftStick.y : 0f) + (Mathf.Abs(rightStick.y) > deadZone ? rightStick.y : 0f);
                if (Mathf.Abs(depth) > deadZone)
                    sightAdjuster.MoveDepth(depth * moveSpeed * Time.deltaTime);
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch) ||
                OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                changeHorizontal = !changeHorizontal;
            }
        }

        // ---------- Keyboard input ----------

        private void HandleKeyboardToggleUI()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                hideObjects = !hideObjects;
        }

        private void HandleKeyboardModeSwitch()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(StereoTestMode.PerEyeDefaultBuggy);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(StereoTestMode.NonPerEye);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(StereoTestMode.PerEyeWithOverride);
        }

        private void HandleKeyboardSightMovement()
        {
            if (sightAdjuster == null) return;

            float dt = moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) sightAdjuster.MoveLeft(-dt);
            if (Input.GetKey(KeyCode.D)) sightAdjuster.MoveLeft(dt);
            if (Input.GetKey(KeyCode.LeftArrow)) sightAdjuster.MoveRight(-dt);
            if (Input.GetKey(KeyCode.RightArrow)) sightAdjuster.MoveRight(dt);
            if (Input.GetKeyDown(KeyCode.R)) sightAdjuster.ResetPositions();
            if (Input.GetKeyDown(KeyCode.H)) changeHorizontal = !changeHorizontal;
        }

        // ---------- Mode ----------

        private StereoTestMode GetNextMode(int step)
        {
            int next = (int)currentMode + step;
            if (next > 2) next = 0;
            if (next < 0) next = 2;
            return (StereoTestMode)next;
        }

        public void SetMode(StereoTestMode mode)
        {
            currentMode = mode;
            ApplyMode(currentMode);
        }

        private void ApplyMode(StereoTestMode mode)
        {
            if (customIPDOverride == null) return;

            switch (mode)
            {
                case StereoTestMode.PerEyeDefaultBuggy:
                    customIPDOverride.OverrideEnabled = false;
                    SetPerEyeCamerasEnabled(true);
                    break;
                case StereoTestMode.NonPerEye:
                    customIPDOverride.OverrideEnabled = false;
                    SetPerEyeCamerasEnabled(false);
                    break;
                case StereoTestMode.PerEyeWithOverride:
                    customIPDOverride.OverrideEnabled = true;
                    customIPDOverride.IpdProportion = 0f;
                    SetPerEyeCamerasEnabled(true);
                    break;
            }
        }

        private void SetPerEyeCamerasEnabled(bool usePerEye)
        {
            if (cameraRig == null) return;
            cameraRig.usePerEyeCameras = usePerEye;
        }
    }

    public enum StereoTestMode
    {
        PerEyeDefaultBuggy = 0,
        NonPerEye = 1,
        PerEyeWithOverride = 2
    }
}
