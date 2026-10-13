using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;

namespace ConvexHullDemo {
    public sealed class HullToolbarPanel : PanelController {
        public InputField countInput;
        public Button randomButton, clearButton, helpButton;
        public Text pointCount, hullCount, area;
        public RectTransform toolbarRect;
        private ConvexHullDemoController demo;
        private Camera uiCamera;
        private Color inputColor;
        public void Bind(ConvexHullDemoController controller) {
            if (demo != null) demo.Changed -= Refresh;
            demo = controller; demo.Changed += Refresh;
            uiCamera = GetComponentInParent<Canvas>().worldCamera;
            inputColor = countInput.image.color;
        }
        protected override void AddListeners() {
            randomButton.onClick.AddListener(Randomize);
            clearButton.onClick.AddListener(Clear);
            helpButton.onClick.AddListener(ShowHelp);
            countInput.onValueChanged.AddListener(OnInputChanged);
        }
        protected override void RemoveListeners() {
            randomButton.onClick.RemoveListener(Randomize);
            clearButton.onClick.RemoveListener(Clear);
            helpButton.onClick.RemoveListener(ShowHelp);
            countInput.onValueChanged.RemoveListener(OnInputChanged);
            if (demo != null) demo.Changed -= Refresh;
        }
        public void Randomize() {
            if (demo == null) return;
            string error;
            bool ok = demo.Randomize(countInput.text, out error);
            countInput.image.color = ok ? inputColor : new Color(1,.82f,.80f);
        }
        public void Clear() { if (demo != null) demo.ClearPoints(); }
        private void ShowHelp() { if (demo != null) demo.ShowHelp(); }
        private void OnInputChanged(string value) { if (demo != null) countInput.image.color = inputColor; }
        public void Refresh() {
            if (demo == null) return;
            pointCount.text = demo.Points.Count.ToString(); hullCount.text = demo.Hull.Count.ToString();
            area.text = demo.HullArea.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        private float ScreenY(RectTransform target, int corner) {
            var corners = new Vector3[4]; target.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(uiCamera, corners[corner]).y;
        }
        public float ToolbarScreenBottom => ScreenY(toolbarRect, 0);
        public float HelpButtonScreenTop => ScreenY((RectTransform)helpButton.transform, 1);
    }
}
