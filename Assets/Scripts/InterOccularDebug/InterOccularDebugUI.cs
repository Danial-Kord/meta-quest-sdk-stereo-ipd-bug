/* ---------------------------
Creation Date: 15/12/2025
Description: Simple debug UI showing L/R positions and separation.
--------------------------- */

using UnityEngine;
using TMPro;

namespace InterOccularDebug
{
    public class InterOccularDebugUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InterOccularPositionController controller;
        
        [Header("UI Settings")]
        [SerializeField] private bool createUI = true;
        [SerializeField] private float uiDistance = 1.5f;
        [SerializeField] private float uiHeight = 1.2f;

        private TextMeshProUGUI infoText;
        private Canvas worldCanvas;

        private void Start()
        {
            if (controller == null)
                controller = FindObjectOfType<InterOccularPositionController>();

            if (createUI)
                CreateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (controller == null || infoText == null) return;

            infoText.text = $"<b>InterOccular Debug</b>\n\n" +
                           $"<color=#FF6666>L: {controller.CurrentLeftOffset:F3} m</color>\n" +
                           $"<color=#6699FF>R: {controller.CurrentRightOffset:F3} m</color>\n" +
                           $"<color=#6699FF>Distance: {Mathf.Abs(controller.CurrentRightOffset - controller.CurrentLeftOffset):F3} m</color>\n" +
                           $"<color=#66FF66>Sep: {controller.Separation:F3} m</color>\n\n" +
                           $"<size=70%><color=#AAAAAA>Left Stick → L object\n" +
                           $"Right Stick → R object\n" +
                           $"A/X → Reset</color></size>";
        }

        private void CreateUI()
        {
            // Canvas
            GameObject canvasObj = new GameObject("DebugCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, uiHeight, uiDistance);
            canvasObj.transform.localScale = Vector3.one * 0.001f;

            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 300);

            // Background
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObj.transform, false);
            var img = panel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            infoText = textObj.AddComponent<TextMeshProUGUI>();
            infoText.fontSize = 24;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.color = Color.white;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            // Face camera
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
