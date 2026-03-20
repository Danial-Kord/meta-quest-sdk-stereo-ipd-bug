/* Head-center monocular alignment: left-eye + movable sight, then right-eye + locked sight, then stereo + pose delta. */

using UnityEngine;

namespace InterOccularDebug
{
    public enum HeadCenterExperimentPhase
    {
        Idle = 0,
        AlignLeft = 1,
        AlignRight = 2,
        Result = 3
    }

    public class HeadCenterExperimentController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InterOccularSightAdjuster sightAdjuster;
        [SerializeField] private CustomIPDOverride customIPDOverride;
        [SerializeField] private OVRCameraRig cameraRig;
        [SerializeField] private Transform reticleRoot;

        [Header("Movement (Phase: AlignLeft)")]
        [SerializeField] private float moveSpeed = 0.1f;
        [SerializeField] [Range(0f, 0.5f)] private float deadZone = 0.1f;
        [SerializeField] private float joystickDeadZone = 0.5f;
        [SerializeField] private float modeSwitchCooldown = 0.6f;

        [Header("Behaviour")]
        [SerializeField] private bool hideSecondSight = true;
        [SerializeField] private float reticleDistance = 0.55f;
        [SerializeField] private bool enableKeyboard = true;

        private StereoTestMode currentMode = StereoTestMode.PerEyeDefaultBuggy;
        private HeadCenterExperimentPhase phase = HeadCenterExperimentPhase.Idle;
        private bool showUi = true;
        private bool changeHorizontal = true;
        private float modeSwitchCooldownRemaining;

        private GameObject reticleVisual;
        private Transform leftEyeAnchor;
        private Transform rightEyeAnchor;
        private Transform centerEyeAnchor;

        private Camera leftEyeCam;
        private Camera rightEyeCam;

        private CameraClearFlags leftClearFlagsBackup;
        private CameraClearFlags rightClearFlagsBackup;
        private Color leftBgBackup;
        private Color rightBgBackup;
        private int leftCullingBackup;
        private int rightCullingBackup;

        private bool camerasBackedUp;

        private Vector3 savedCenterPose0;
        private Quaternion savedCenterRot0;
        private Vector3 savedCenterPose1;
        private Quaternion savedCenterRot1;
        private bool hasPose0;
        private bool hasPose1;

        public HeadCenterExperimentPhase Phase => phase;
        public bool ShowUi => showUi;
        public StereoTestMode CurrentMode => currentMode;
        public bool HasPoseDelta => hasPose0 && hasPose1;

        public float DeltaTranslationM => HasPoseDelta ? Vector3.Distance(savedCenterPose0, savedCenterPose1) : 0f;

        public float DeltaAngleDeg =>
            HasPoseDelta ? Quaternion.Angle(savedCenterRot0, savedCenterRot1) : 0f;

        public Vector3 DeltaPositionWorld =>
            HasPoseDelta ? savedCenterPose1 - savedCenterPose0 : Vector3.zero;

        private void Awake()
        {
            if (sightAdjuster == null)
                sightAdjuster = FindObjectOfType<InterOccularSightAdjuster>();
            if (customIPDOverride == null)
                customIPDOverride = FindObjectOfType<CustomIPDOverride>();
            if (cameraRig == null)
                cameraRig = FindObjectOfType<OVRCameraRig>();

            if (cameraRig != null)
            {
                leftEyeAnchor = cameraRig.leftEyeAnchor;
                rightEyeAnchor = cameraRig.rightEyeAnchor;
                centerEyeAnchor = cameraRig.centerEyeAnchor;
            }
        }

        private void Start()
        {
            if (hideSecondSight && sightAdjuster != null && sightAdjuster.RightObject != null)
                sightAdjuster.RightObject.gameObject.SetActive(false);

            CacheEyeCameras();
            BackupCameraState();
            ApplyMode(currentMode);
            GoIdle();
        }
        

        private void CacheEyeCameras()
        {
            if (cameraRig == null) return;
            leftEyeCam = leftEyeAnchor != null ? leftEyeAnchor.GetComponent<Camera>() : null;
            rightEyeCam = rightEyeAnchor != null ? rightEyeAnchor.GetComponent<Camera>() : null;
        }

        private void BackupCameraState()
        {
            if (leftEyeCam != null)
            {
                leftClearFlagsBackup = leftEyeCam.clearFlags;
                leftBgBackup = leftEyeCam.backgroundColor;
                leftCullingBackup = leftEyeCam.cullingMask;
            }

            if (rightEyeCam != null)
            {
                rightClearFlagsBackup = rightEyeCam.clearFlags;
                rightBgBackup = rightEyeCam.backgroundColor;
                rightCullingBackup = rightEyeCam.cullingMask;
            }

            camerasBackedUp = leftEyeCam != null && rightEyeCam != null;
        }

        private void Update()
        {
            if (sightAdjuster == null || cameraRig == null) return;

            // Monocular phases need separate eye cameras to black out one eye.
            if (phase is HeadCenterExperimentPhase.AlignLeft or HeadCenterExperimentPhase.AlignRight)
                EnsurePerEyeForMonoPhases();

            HandleUiToggle();

            if (showUi)
            {
                HandleModeSwitch();
                if (enableKeyboard)
                    HandleKeyboardModeSwitch();
                HandleStartAlignmentInput();
            }
            else
            {
                switch (phase)
                {
                    case HeadCenterExperimentPhase.AlignLeft:
                        HandleSightMovement();
                        if (enableKeyboard)
                            HandleKeyboardSightMovement();
                        break;
                    case HeadCenterExperimentPhase.AlignRight:
                        break;
                    case HeadCenterExperimentPhase.Result:
                        showUi = true;
                        HandleModeSwitch();
                        if (enableKeyboard)
                            HandleKeyboardModeSwitch();
                        break;
                }
            }

            if (phase == HeadCenterExperimentPhase.AlignLeft ||
                phase == HeadCenterExperimentPhase.AlignRight)
                HandleSaveButton();

            if (enableKeyboard)
                HandleKeyboardReset();

            UpdateMonoVisuals();
            sightAdjuster.SetVisibility(!showUi);
            if (hideSecondSight && sightAdjuster.RightObject != null)
                sightAdjuster.RightObject.gameObject.SetActive(false);
        }

        private void HandleUiToggle()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start) || Input.GetKeyDown(KeyCode.Tab))
                showUi = !showUi;
        }

        private void HandleStartAlignmentInput()
        {
            if (phase != HeadCenterExperimentPhase.Idle) return;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch) ||
                OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                BeginAlignment();
            }
        }

        private void BeginAlignment()
        {
            showUi = false;
            phase = HeadCenterExperimentPhase.AlignLeft;
            hasPose0 = false;
            hasPose1 = false;
            changeHorizontal = true;
            SnapLeftSightToLeftEyeWorld();
            sightAdjuster.Initialize();

            EnsurePerEyeForMonoPhases();
            ApplyModeForMonoPhase(currentMode);
        }

        /// <summary>
        /// World-align the movable sight with the left eye (no parenting). Offsets are then rebased in <see cref="InterOccularSightAdjuster.Initialize"/>.
        /// </summary>
        private void SnapLeftSightToLeftEyeWorld()
        {
            Transform sight = reticleRoot.transform;
            sight.SetPositionAndRotation(leftEyeAnchor.position, leftEyeAnchor.rotation);
        }

        private void HandleSaveButton()
        {
            if (!OVRInput.GetDown(OVRInput.Button.One)) return;

            switch (phase)
            {
                case HeadCenterExperimentPhase.AlignLeft:
                    SavePose0();
                    phase = HeadCenterExperimentPhase.AlignRight;
                    break;
                case HeadCenterExperimentPhase.AlignRight:
                    SavePose1();
                    EnterResult();
                    break;
            }
        }

        private void SavePose0()
        {
            if (centerEyeAnchor == null) return;
            savedCenterPose0 = centerEyeAnchor.position;
            savedCenterRot0 = centerEyeAnchor.rotation;
            hasPose0 = true;
        }

        private void SavePose1()
        {
            if (centerEyeAnchor == null) return;
            savedCenterPose1 = centerEyeAnchor.position;
            savedCenterRot1 = centerEyeAnchor.rotation;
            hasPose1 = true;
        }

        private void EnterResult()
        {
            phase = HeadCenterExperimentPhase.Result;
            RestoreBothEyesFull();
            ApplyMode(currentMode);
            CacheEyeCameras();
            showUi = true;
        }

        public void GoIdle()
        {
            phase = HeadCenterExperimentPhase.Idle;
            RestoreBothEyesFull();
            ApplyMode(currentMode);
            CacheEyeCameras();
            hasPose0 = false;
            hasPose1 = false;
        }

        private void HandleKeyboardReset()
        {
            if (Input.GetKeyDown(KeyCode.R))
                GoIdle();
        }

        private void EnsurePerEyeForMonoPhases()
        {
            // Per-eye cameras required to black out one eye in mono phases.
            if (!cameraRig.usePerEyeCameras)
                cameraRig.usePerEyeCameras = true;
            CacheEyeCameras();
        }

        private void UpdateMonoVisuals()
        {
            if (!camerasBackedUp) return;

            if (phase == HeadCenterExperimentPhase.AlignLeft)
                SetMonocular(leftActive: true);
            else if (phase == HeadCenterExperimentPhase.AlignRight)
                SetMonocular(leftActive: false);
            else
                RestoreBothEyesFull();
        }

        private void SetMonocular(bool leftActive)
        {
            if (leftActive)
            {
                RestoreEyeCamera(leftEyeCam, leftClearFlagsBackup, leftBgBackup, leftCullingBackup);
                ApplyBlackoutCamera(rightEyeCam);
            }
            else
            {
                RestoreEyeCamera(rightEyeCam, rightClearFlagsBackup, rightBgBackup, rightCullingBackup);
                ApplyBlackoutCamera(leftEyeCam);
            }
        }

        private static void RestoreEyeCamera(Camera cam, CameraClearFlags flags, Color bg, int mask)
        {
            if (cam == null) return;
            cam.clearFlags = flags;
            cam.backgroundColor = bg;
            cam.cullingMask = mask;
        }

        private static void ApplyBlackoutCamera(Camera cam)
        {
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
        }

        private void RestoreBothEyesFull()
        {
            if (leftEyeCam != null)
            {
                leftEyeCam.clearFlags = leftClearFlagsBackup;
                leftEyeCam.backgroundColor = leftBgBackup;
                leftEyeCam.cullingMask = leftCullingBackup;
            }

            if (rightEyeCam != null)
            {
                rightEyeCam.clearFlags = rightClearFlagsBackup;
                rightEyeCam.backgroundColor = rightBgBackup;
                rightEyeCam.cullingMask = rightCullingBackup;
            }
        }
        

        private void HandleModeSwitch()
        {
            if (modeSwitchCooldownRemaining > 0f)
                modeSwitchCooldownRemaining -= Time.deltaTime;

            float rStickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;
            float lStickY = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).y;
            float stickY = Mathf.Abs(rStickY) > Mathf.Abs(lStickY) ? rStickY : lStickY;

            if (modeSwitchCooldownRemaining > 0f) return;

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

        private void HandleKeyboardModeSwitch()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(StereoTestMode.PerEyeDefaultBuggy);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(StereoTestMode.PerEyeWithOverride);
        }

        private StereoTestMode GetNextMode(int step)
        {
            int idx = currentMode == StereoTestMode.PerEyeWithOverride ? 1 : 0;
            int dir = step > 0 ? 1 : -1;
            idx = (idx + dir + 2) % 2;
            return idx == 0 ? StereoTestMode.PerEyeDefaultBuggy : StereoTestMode.PerEyeWithOverride;
        }

        public void SetMode(StereoTestMode mode)
        {
            if (mode == StereoTestMode.NonPerEye)
                mode = StereoTestMode.PerEyeDefaultBuggy;

            currentMode = mode;
            if (phase == HeadCenterExperimentPhase.Idle ||
                phase == HeadCenterExperimentPhase.Result)
                ApplyMode(currentMode);
            else
                ApplyModeForMonoPhase(currentMode);
        }

        private void ApplyModeForMonoPhase(StereoTestMode mode)
        {
            if (customIPDOverride == null) return;
            switch (mode)
            {
                case StereoTestMode.PerEyeDefaultBuggy:
                    customIPDOverride.OverrideEnabled = false;
                    break;
                default:
                    customIPDOverride.OverrideEnabled = true;
                    customIPDOverride.IpdProportion = 0f;
                    break;
            }
        }

        private void ApplyMode(StereoTestMode mode)
        {
            if (customIPDOverride == null) return;

            SetPerEyeCamerasEnabled(true);
            switch (mode)
            {
                case StereoTestMode.PerEyeDefaultBuggy:
                    customIPDOverride.OverrideEnabled = false;
                    break;
                default:
                    customIPDOverride.OverrideEnabled = true;
                    customIPDOverride.IpdProportion = 0f;
                    break;
            }

            CacheEyeCameras();
        }

        private void SetPerEyeCamerasEnabled(bool usePerEye)
        {
            if (cameraRig == null) return;
            cameraRig.usePerEyeCameras = usePerEye;
        }

        private void HandleSightMovement()
        {
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
                float depth = (Mathf.Abs(leftStick.y) > deadZone ? leftStick.y : 0f) +
                              (Mathf.Abs(rightStick.y) > deadZone ? rightStick.y : 0f);
                if (Mathf.Abs(depth) > deadZone)
                    sightAdjuster.MoveDepth(depth * moveSpeed * Time.deltaTime);
            }

            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch) ||
                OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
                changeHorizontal = !changeHorizontal;
        }

        private void HandleKeyboardSightMovement()
        {
            float dt = moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) sightAdjuster.MoveLeft(-dt);
            if (Input.GetKey(KeyCode.D)) sightAdjuster.MoveLeft(dt);
            if (Input.GetKey(KeyCode.LeftArrow)) sightAdjuster.MoveRight(-dt);
            if (Input.GetKey(KeyCode.RightArrow)) sightAdjuster.MoveRight(dt);
            if (Input.GetKeyDown(KeyCode.H)) changeHorizontal = !changeHorizontal;
        }
    }
}
