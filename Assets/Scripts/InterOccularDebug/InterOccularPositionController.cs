/* ---------------------------
Creation Date: 15/12/2025
Description: Simple debug controller for adjusting L/R object positions using VR thumbsticks.
             Objects move along a specified direction vector.
--------------------------- */

using UnityEngine;

namespace InterOccularDebug
{
    public class InterOccularPositionController : MonoBehaviour
    {
        public static InterOccularPositionController Instance { get; private set; }

        [Header("Target Objects")]
        [SerializeField] private Transform leftObject;
        [SerializeField] private Transform rightObject;

        [Header("Direction")]
        [Tooltip("Direction to move objects. Positive thumbstick = move in this direction, Negative = opposite")]
        [SerializeField] private Vector3 moveDirection = Vector3.right;

        [SerializeField] private Vector3 distanceDirection = Vector3.forward;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 0.1f;
        [SerializeField] [Range(0f, 0.5f)] private float deadZone = 0.1f;

        [Header("Initial Offsets")]
        [SerializeField] private float initialLeftOffset = -0.1f;
        [SerializeField] private float initialRightOffset = 0.1f;

        [Header("Debug Keyboard")]
        [SerializeField] private bool enableKeyboard = true;

        private float currentLeftOffset;
        private float currentRightOffset;
        private Vector3 leftBasePosition;
        private Vector3 rightBasePosition;
        private Vector3 normalizedDirection;

        public float CurrentLeftOffset => currentLeftOffset;
        public float CurrentRightOffset => currentRightOffset;
        public float Separation => currentRightOffset - currentLeftOffset;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            normalizedDirection = moveDirection.normalized;
            
            if (leftObject != null)
                leftBasePosition = leftObject.localPosition;
            if (rightObject != null)
                rightBasePosition = rightObject.localPosition;

            currentLeftOffset = initialLeftOffset;
            currentRightOffset = initialRightOffset;
            ApplyOffsets();
        }

        private void Update()
        {
            HandleVRInput();
            
            if (enableKeyboard)
                HandleKeyboardInput();
        }
        bool cahngeHorizontal = true;

        private void HandleVRInput()
        {
            // Left thumbstick controls left object
            Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
            if (Mathf.Abs(leftStick.x) > deadZone && cahngeHorizontal)
            {
                currentLeftOffset += leftStick.x * moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }
            if (Mathf.Abs(leftStick.y) > deadZone && !cahngeHorizontal)
            {
                leftBasePosition += distanceDirection.normalized * leftStick.y * moveSpeed * Time.deltaTime;
                rightBasePosition += distanceDirection.normalized * leftStick.y * moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }
            
            // Right thumbstick controls right object
            Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            if (Mathf.Abs(rightStick.x) > deadZone && cahngeHorizontal)
            {
                currentRightOffset += rightStick.x * moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }
            
            if (Mathf.Abs(rightStick.y) > deadZone && !cahngeHorizontal)
            {
                leftBasePosition += distanceDirection.normalized * rightStick.y * moveSpeed * Time.deltaTime;
                rightBasePosition += distanceDirection.normalized * rightStick.y * moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }
            

            // Reset on A or X button
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) ||
                OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                cahngeHorizontal = !cahngeHorizontal;
            }
        }

        private void HandleKeyboardInput()
        {
            // Left object: A/D
            if (Input.GetKey(KeyCode.A))
            {
                currentLeftOffset -= moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }
            if (Input.GetKey(KeyCode.D))
            {
                currentLeftOffset += moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }

            // Right object: Arrow keys
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                currentRightOffset -= moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                currentRightOffset += moveSpeed * Time.deltaTime;
                ApplyOffsets();
            }

            // Reset: R
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetPositions();
            }
        }

        private void ApplyOffsets()
        {
            if (leftObject != null)
                leftObject.localPosition = leftBasePosition + normalizedDirection * currentLeftOffset;

            if (rightObject != null)
                rightObject.localPosition = rightBasePosition + normalizedDirection * currentRightOffset;
        }

        public void ResetPositions()
        {
            currentLeftOffset = initialLeftOffset;
            currentRightOffset = initialRightOffset;
            ApplyOffsets();
        }

        public void SetTargets(Transform left, Transform right)
        {
            leftObject = left;
            rightObject = right;
            Initialize();
        }

        public void SetDirection(Vector3 direction)
        {
            moveDirection = direction;
            normalizedDirection = direction.normalized;
        }
    }
}
