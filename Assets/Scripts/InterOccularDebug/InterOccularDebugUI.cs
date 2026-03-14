/* ---------------------------
 * Display only: shows mode, distances, and help. No input — reads state from InterOccularPositionController.
--------------------------- */

using UnityEngine;
using TMPro;

namespace InterOccularDebug
{
    public class InterOccularDebugUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InterOccularPositionController controller;
        [SerializeField] private CustomIPDOverride customIPDOverride;

        [Header("UI Settings")]
        [SerializeField] private bool createUI = true;
        [SerializeField] private float uiDistance = 1.5f;
        [SerializeField] private float uiHeight = 1.2f;

        private TextMeshProUGUI infoText;
        private TextMeshProUGUI manualText;
        private Canvas worldCanvas;
        private GameObject canvasRoot;

        private void Start()
        {
            if (controller == null)
                controller = FindObjectOfType<InterOccularPositionController>();
            if (customIPDOverride == null)
                customIPDOverride = FindObjectOfType<CustomIPDOverride>();

            if (createUI)
                CreateUI();
        }

        private void Update()
        {
            if (canvasRoot != null && controller != null)
                canvasRoot.SetActive(controller.HideObjects);

            UpdateUI();
        }

        private static string GetModeLabel(StereoTestMode mode)
        {
            switch (mode)
            {
                case StereoTestMode.PerEyeDefaultBuggy: return "Per-Eye Default (Buggy)";
                case StereoTestMode.NonPerEye: return "Non Per-Eye (Correct)";
                case StereoTestMode.PerEyeWithOverride: return "Per-Eye + Override (Fixed)";
                default: return mode.ToString();
            }
        }

        private void UpdateUI()
        {
            if (infoText == null || controller == null) return;

            float deviceIPD = customIPDOverride != null ? customIPDOverride.DeviceIPD : 0f;
            float separation = controller.Separation;
            StereoTestMode currentMode = controller.CurrentMode;

            const string highlightColor = "#00FF88";
            const string dimColor = "#888888";

            string line1 = currentMode == StereoTestMode.PerEyeDefaultBuggy
                ? $"<color={highlightColor}>► {GetModeLabel(StereoTestMode.PerEyeDefaultBuggy)}</color>"
                : $"<color={dimColor}>  {GetModeLabel(StereoTestMode.PerEyeDefaultBuggy)}</color>";
            string line2 = currentMode == StereoTestMode.NonPerEye
                ? $"<color={highlightColor}>► {GetModeLabel(StereoTestMode.NonPerEye)}</color>"
                : $"<color={dimColor}>  {GetModeLabel(StereoTestMode.NonPerEye)}</color>";
            string line3 = currentMode == StereoTestMode.PerEyeWithOverride
                ? $"<color={highlightColor}>► {GetModeLabel(StereoTestMode.PerEyeWithOverride)}</color>"
                : $"<color={dimColor}>  {GetModeLabel(StereoTestMode.PerEyeWithOverride)}</color>";

            infoText.text = $"<b>Stereo IPD Test</b>\n\n" +
                           $"<b>Mode (stick up/down):</b>\n" +
                           $"{line1}\n" +
                           $"{line2}\n" +
                           $"{line3}\n\n" +
                           $"<b>Distances</b>\n" +
                           $"Device IPD: {deviceIPD:F3} m\n" +
                           $"Sight separation: {separation:F3} m";

            if (manualText != null)
            {
                manualText.text = "<b><color=#FFDD00>CONTROLLER GUIDE</color></b>\n\n" +
                                  "<b>A/X</b> (Tab) — Toggle UI / Gun sights\n" +
                                  "<b>Stick ↑↓</b> (1/2/3) — Change mode\n" +
                                  "<b>L/R Stick</b> — Move L/R sight (A/D, ←/→)\n" +
                                  "<b>B/Y</b> (H) — Horizontal vs depth\n" +
                                  "<b>R</b> — Reset sights";
            }
        }

        private void CreateUI()
        {
            GameObject canvasObj = new GameObject("DebugCanvas");
            canvasRoot = canvasObj;
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, uiHeight, uiDistance);
            canvasObj.transform.localScale = Vector3.one * 0.001f;

            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = worldCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(520, 520);

            const float margin = 20f;
            const float gap = 14f; // vertical gap between panels
            const float mainPanelPadding = 24f; // inner padding for first UI text

            // Main panel (top: mode + distances) — more height so text fits
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObj.transform, false);
            var img = panel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.34f);
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(margin, margin);
            panelRect.offsetMax = new Vector2(-margin, -margin - gap * 0.5f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            infoText = textObj.AddComponent<TextMeshProUGUI>();
            infoText.fontSize = 22;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.color = Color.white;
            infoText.overflowMode = TextOverflowModes.Overflow;
            infoText.enableAutoSizing = true;
            infoText.fontSizeMin = 14;
            infoText.fontSizeMax = 22;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(margin, mainPanelPadding);
            textRect.offsetMax = new Vector2(-margin, -mainPanelPadding);

            // Manual / Controller guide panel (bottom, highlighted) — gap above
            GameObject manualPanel = new GameObject("ManualPanel");
            manualPanel.transform.SetParent(canvasObj.transform, false);
            var manualBg = manualPanel.AddComponent<UnityEngine.UI.Image>();
            manualBg.color = new Color(0.18f, 0.14f, 0.05f, 0.95f);
            var manualPanelRect = manualPanel.GetComponent<RectTransform>();
            manualPanelRect.anchorMin = Vector2.zero;
            manualPanelRect.anchorMax = new Vector2(1f, 0.36f);
            manualPanelRect.offsetMin = new Vector2(margin, margin);
            manualPanelRect.offsetMax = new Vector2(-margin, margin + gap * 0.5f);

            GameObject manualTextObj = new GameObject("ManualText");
            manualTextObj.transform.SetParent(manualPanel.transform, false);
            manualText = manualTextObj.AddComponent<TextMeshProUGUI>();
            manualText.fontSize = 20;
            manualText.fontStyle = FontStyles.Bold;
            manualText.alignment = TextAlignmentOptions.Left;
            manualText.color = new Color(1f, 0.95f, 0.75f);
            var manualTextRect = manualTextObj.GetComponent<RectTransform>();
            manualTextRect.anchorMin = Vector2.zero;
            manualTextRect.anchorMax = Vector2.one;
            manualTextRect.offsetMin = new Vector2(margin + 4f, margin);
            manualTextRect.offsetMax = new Vector2(-margin - 4f, -margin);

            canvasObj.AddComponent<FaceCamera>();
        }
    }
    

    public class FaceCamera : MonoBehaviour
    {
        private Camera cam;
        void Start() => cam = Camera.main;
        void LateUpdate()
        {
            if (cam != null)
            {
                transform.LookAt(cam.transform);
                transform.Rotate(0, 180, 0);
            }
        }
    }
}
