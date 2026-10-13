using System;
using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;

namespace ConvexHullDemo {
    public sealed class HullToolbarPanel : PanelController {
        public InputField countInput;
        public Button randomButton, clearButton;
        public Text pointCount, hullCount, area, status;
        public RectTransform toolbarRect, footerRect;
        private ConvexHullDemoController demo;
        private Camera uiCamera;
        public void Bind(ConvexHullDemoController controller) {
            if (demo != null) demo.Changed -= Refresh;
            demo = controller; demo.Changed += Refresh;
            uiCamera = GetComponentInParent<Canvas>().worldCamera;
        }
        protected override void AddListeners() {
            randomButton.onClick.AddListener(Randomize);
            clearButton.onClick.AddListener(Clear);
        }
        protected override void RemoveListeners() {
            randomButton.onClick.RemoveListener(Randomize);
            clearButton.onClick.RemoveListener(Clear);
            if (demo != null) demo.Changed -= Refresh;
        }
        public void Randomize() {
            if (demo == null) return;
            string error;
            bool ok = demo.Randomize(countInput.text, out error);
            status.text = ok ? "已在当前视野生成 " + demo.Points.Count + " 个点" : error;
            status.color = ok ? new Color(.24f,.43f,.61f) : new Color(.76f,.26f,.17f);
        }
        public void Clear() {
            if (demo == null) return;
            demo.ClearPoints(); status.text = "点集已清空 · 右键画布新建点";
            status.color = new Color(.24f,.43f,.61f);
        }
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
        public float FooterScreenTop => ScreenY(footerRect, 1);
    }
}
