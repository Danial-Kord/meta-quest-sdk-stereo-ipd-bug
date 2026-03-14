using UnityEngine;


/// <summary>
/// Forces the Meta SDK's per-eye cameras to use a custom IPD,
/// overriding the hardcoded device-reported value.
/// Uses three mechanisms to guarantee the override sticks:
/// 1. High execution order so LateUpdate runs after OVRCameraRig
/// 2. OVRCameraRig.UpdatedAnchors callback (fires right after OVR positions anchors)
/// 3. Application.onBeforeRender (runs after ALL LateUpdates, right before rendering)
/// </summary>
[DefaultExecutionOrder(10000)]
public class CustomIPDOverride : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OVRCameraRig cameraRig;


    [Tooltip("When enabled, overrides the device IPD with the custom value")]
    [SerializeField] private bool overrideEnabled = true;

    [Header("IPD Override")]
    [SerializeField] [Range(0f,1)] private float IdpCustomProportion = 0.5f;

    [Header("Stereo Separation Override")]
    [Tooltip("When enabled, forces Camera.stereoSeparation to the custom value instead of zeroing it")]
    [SerializeField] private bool overrideStereoSeparation = false;
    


    public bool OverrideEnabled
    {
        get => overrideEnabled;
        set => overrideEnabled = value;
    }

    /// <summary>Proportion of device IPD used for half-separation. 0 = both eyes at center; 0.5 = correct single IPD.</summary>
    public float IpdProportion
    {
        get => IdpCustomProportion;
        set => IdpCustomProportion = Mathf.Clamp01(value);
    }

    public float DeviceIPD => OVRPlugin.ipd;
    

    void Start()
    {
        if (cameraRig == null)
            cameraRig = FindObjectOfType<OVRCameraRig>();

        if (cameraRig == null)
        {
            Debug.LogError("[CustomIPDOverride] No OVRCameraRig found. Assign one or ensure it exists in the scene.");
            return;
        }

        cameraRig.UpdatedAnchors += OnUpdatedAnchors;
    }

    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;

        if (cameraRig != null)
            cameraRig.UpdatedAnchors -= OnUpdatedAnchors;
    }

    void OnUpdatedAnchors(OVRCameraRig rig)
    {
        ApplyCustomIPD();
    }

    void OnBeforeRender()
    {
        ApplyCustomIPD();
    }

    void LateUpdate()
    {
        ApplyCustomIPD();
    }

    void ApplyCustomIPD()
    {
        if (!overrideEnabled || cameraRig == null) return;

        Transform center = cameraRig.centerEyeAnchor;
        Transform left = cameraRig.leftEyeAnchor;
        Transform right = cameraRig.rightEyeAnchor;

        if (center == null || left == null || right == null) return;
    
        float deviceIPD = OVRPlugin.ipd;
        
        float customIPD = deviceIPD * IdpCustomProportion / 2;

        Vector3 centerLocalPos = center.localPosition;
        Quaternion centerLocalRot = center.localRotation;
        Vector3 localRight = centerLocalRot * Vector3.right;

        left.localPosition = centerLocalPos - localRight * customIPD;
        left.localRotation = centerLocalRot;

        right.localPosition = centerLocalPos + localRight * customIPD;
        right.localRotation = centerLocalRot;

    }

}

