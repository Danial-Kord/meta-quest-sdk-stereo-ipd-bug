using UnityEngine;
using TMPro;

namespace InterOccularDebug
{
    public class HeadCenterExperimentUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeadCenterExperimentController experiment;
        [SerializeField] private CustomIPDOverride customIPDOverride;

        [Header("UI Settings")]
        [SerializeField] private bool createUI = true;
        [SerializeField] private float uiDistance = 1.5f;
        [SerializeField] private float uiHeight = 1.2f;

        private TextMeshProUGUI infoText;
        private TextMeshProUGUI manualText;
        private GameObject canvasRoot;

        private void Start()
        {
            if (experiment == null)
                experiment = FindObjectOfType<HeadCenterExperimentController>();
            if (customIPDOverride == null)
                customIPDOverride = FindObjectOfType<CustomIPDOverride>();

            if (createUI)
                CreateUI();
        }

        private void Update()
        {
            if (canvasRoot != null && experiment != null)
                canvasRoot.SetActive(experiment.ShowUi);

            UpdateUI();
        }

        private static string GetPerEyeModeLabel(StereoTestMode mode)
        {
            return mode == StereoTestMode.PerEyeWithOverride
                ? "Per-Eye + Override (Fixed)"
                : "Per-Eye Default (Buggy)";
        }

        private static string PhaseLabel(HeadCenterExperimentPhase p)
        {
            switch (p)
            {
                case HeadCenterExperimentPhase.Idle: return "Idle — choose mode, then thumbstick click to start";
                case HeadCenterExperimentPhase.AlignLeft: return "LEFT eye only — align sight to red dot, A to save pose";
                case HeadCenterExperimentPhase.AlignRight: return "RIGHT eye only — move head to align, sight locked — A to save";
                case HeadCenterExperimentPhase.Result: return "Result — stereo on; translation / angle below";
                default: return p.ToString();
            }
        }

        private void UpdateUI()
        {
            if (infoText == null || experiment == null) return;

            float deviceIPD = customIPDOverride != null ? customIPDOverride.DeviceIPD : 0f;
            StereoTestMode m = experiment.CurrentMode;
            if (m == StereoTestMode.NonPerEye)
                m = StereoTestMode.PerEyeDefaultBuggy;
            HeadCenterExperimentPhase ph = experiment.Phase;

            const string highlightColor = "#00FF88";
            const string dimColor = "#888888";

            string lineBug = m == StereoTestMode.PerEyeDefaultBuggy
                ? $"<color={highlightColor}>► {GetPerEyeModeLabel(StereoTestMode.PerEyeDefaultBuggy)}</color>"
                : $"<color={dimColor}>  {GetPerEyeModeLabel(StereoTestMode.PerEyeDefaultBuggy)}</color>";
            string lineFix = m == StereoTestMode.PerEyeWithOverride
                ? $"<color={highlightColor}>► {GetPerEyeModeLabel(StereoTestMode.PerEyeWithOverride)}</color>"
                : $"<color={dimColor}>  {GetPerEyeModeLabel(StereoTestMode.PerEyeWithOverride)}</color>";

            Vector3 dpos = experiment.DeltaPositionWorld;
            infoText.text = $"<b>Head-center stereo test</b> <size=85%>(per-eye modes only)</size>\n\n" +
                           $"<b>Phase:</b> {PhaseLabel(ph)}\n\n" +
                           $"<b>Mode (stick ↑↓ when UI shown):</b>\n" +
                           $"{lineBug}\n" +
                           $"{lineFix}\n\n" +
                           $"<b>Metrics</b>\n" +
                           $"Device IPD: {deviceIPD:F3} m\n" +
                           (experiment.HasPoseDelta
                               ? $"Pose delta (center-eye):\n" +
                                 $"  Distance: {experiment.DeltaTranslationM:F4} m\n" +
                                 $"  Angle: {experiment.DeltaAngleDeg:F2} °\n" +
                                 $"  Δpos (world): {dpos.x:F4}, {dpos.y:F4}, {dpos.z:F4} m"
                               : "Pose delta: (complete both A saves)");

            if (manualText != null)
            {
                manualText.text = "<b><color=#FFDD00>CONTROLS</color></b>\n\n" +
                                  "<b>Tab / Menu (Start)</b> — Show or hide this UI\n" +
                                  "<b>Thumbstick click</b> (L or R) — Start from idle\n" +
                                  "<b>A / X</b> — Save pose (left phase, then right phase)\n" +
                                  "<b>Stick ↑↓</b> (1 / 2 keys) — Mode\n" +
                                  "<b>L/R sticks</b> — Move sight (left phase only)\n" +
                                  "<b>B / Y</b> (H) — Horizontal vs depth\n" +
                                  "<b>R</b> — Reset to idle";
            }
        }

        private void CreateUI()
        {
            GameObject canvasObj = new GameObject("HeadCenterExperimentCanvas");
            canvasRoot = canvasObj;
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, uiHeight, uiDistance);
            canvasObj.transform.localScale = Vector3.one * 0.001f;

            var worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = worldCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(560, 580);

            const float margin = 20f;
            const float gap = 14f;
            const float mainPanelPadding = 24f;

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
            infoText.fontSize = 20;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.color = Color.white;
            infoText.overflowMode = TextOverflowModes.Overflow;
            infoText.enableAutoSizing = true;
            infoText.fontSizeMin = 13;
            infoText.fontSizeMax = 20;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(margin, mainPanelPadding);
            textRect.offsetMax = new Vector2(-margin, -mainPanelPadding);

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
            manualText.fontSize = 18;
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
}
