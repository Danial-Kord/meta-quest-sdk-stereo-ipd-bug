/* ---------------------------
 * Position logic only: applies offsets to left/right sight transforms.
 * No input handling — call MoveLeft, MoveRight, MoveDepth, ResetPositions from input handler.
--------------------------- */

using UnityEngine;

namespace InterOccularDebug
{
    public class InterOccularSightAdjuster : MonoBehaviour
    {
        [Header("Target Objects")]
        [SerializeField] private Transform leftObject;
        [SerializeField] private Transform rightObject;

        [Header("Direction")]
        [Tooltip("Direction for horizontal separation. Positive = right.")]
        [SerializeField] private Vector3 moveDirection = Vector3.right;
        [Tooltip("Direction for depth (Z) movement.")]
        [SerializeField] private Vector3 distanceDirection = Vector3.forward;

        [Header("Initial Offsets (m)")]
        [SerializeField] private float initialLeftOffset = -0.1f;
        [SerializeField] private float initialRightOffset = 0.1f;

        private Vector3 leftBasePosition;
        private Vector3 rightBasePosition;
        private float currentLeftOffset;
        private float currentRightOffset;
        private Vector3 normalizedMoveDirection;
        private Vector3 normalizedDistanceDirection;

        public float CurrentLeftOffset => currentLeftOffset;
        public float CurrentRightOffset => currentRightOffset;
        public float Separation => currentRightOffset - currentLeftOffset;

        public Transform LeftObject => leftObject;
        public Transform RightObject => rightObject;

        private void Awake()
        {
            normalizedMoveDirection = moveDirection.normalized;
            normalizedDistanceDirection = distanceDirection.normalized;
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (leftObject != null)
                leftBasePosition = leftObject.localPosition;
            if (rightObject != null)
                rightBasePosition = rightObject.localPosition;

            currentLeftOffset = initialLeftOffset;
            currentRightOffset = initialRightOffset;
            ApplyOffsets();
        }

        /// <summary>Add delta to left sight offset along move direction.</summary>
        public void MoveLeft(float delta)
        {
            currentLeftOffset += delta;
            ApplyOffsets();
        }

        /// <summary>Add delta to right sight offset along move direction.</summary>
        public void MoveRight(float delta)
        {
            currentRightOffset += delta;
            ApplyOffsets();
        }

        /// <summary>Move both sights along distance direction (depth).</summary>
        public void MoveDepth(float delta)
        {
            Vector3 step = normalizedDistanceDirection * delta;
            leftBasePosition += step;
            rightBasePosition += step;
            ApplyOffsets();
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

        public void SetVisibility(bool visible)
        {
            if (leftObject != null) leftObject.gameObject.SetActive(visible);
            if (rightObject != null) rightObject.gameObject.SetActive(visible);
        }

        private void ApplyOffsets()
        {
            if (leftObject != null)
                leftObject.localPosition = leftBasePosition + normalizedMoveDirection * currentLeftOffset;
            if (rightObject != null)
                rightObject.localPosition = rightBasePosition + normalizedMoveDirection * currentRightOffset;
        }
    }
}
