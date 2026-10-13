using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;

namespace ConvexHullDemo {
    public sealed class HullContextMenuPanel : PanelController {
        public Button createButton, deleteButton;
        public Text coordinateLabel;
        private ConvexHullDemoController demo;
        private Camera uiCamera;
        private Vector2 anchorScreen;
        private Rect lastScreen;
        public bool HasDelete => deleteButton.gameObject.activeSelf;
        public void Bind(ConvexHullDemoController controller, Camera camera) { demo = controller; uiCamera = camera; }
        protected override void AddListeners() {
            createButton.onClick.AddListener(CreatePoint); deleteButton.onClick.AddListener(DeletePoint);
        }
        protected override void RemoveListeners() {
            createButton.onClick.RemoveListener(CreatePoint); deleteButton.onClick.RemoveListener(DeletePoint);
        }
        private void CreatePoint() { demo.CreateContextPoint(); }
        private void DeletePoint() { demo.DeleteContextPoint(); }
        public void Configure(Vector2 screen, bool canDelete) {
            anchorScreen = screen; deleteButton.gameObject.SetActive(canDelete);
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(230, canDelete ? 142 : 98);
            Vector2 world = demo.ScreenToWorld(screen);
            coordinateLabel.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "位置  ({0:0.00}, {1:0.00})", world.x, world.y);
        }
        protected override void HierarchyFixOnShow() { transform.SetAsLastSibling(); }
        public void PositionAt(Vector2 screen) {
            anchorScreen = screen;
            var rect = (RectTransform)transform;
            var parent = (RectTransform)rect.parent;
            float scale = GetComponentInParent<Canvas>().scaleFactor;
            Rect viewport = demo.worldCamera.pixelRect;
            float x = Mathf.Clamp(screen.x, viewport.xMin + 8, Mathf.Max(viewport.xMin + 8, viewport.xMax - rect.rect.width * scale - 8));
            float y = Mathf.Clamp(screen.y, Mathf.Min(viewport.yMax - 8, viewport.yMin + rect.rect.height * scale + 8), viewport.yMax - 8);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(x, y), uiCamera, out local);
            rect.anchoredPosition = local - parent.rect.min;
            lastScreen = viewport;
        }
        private void LateUpdate() { if (demo != null && lastScreen != demo.worldCamera.pixelRect) PositionAt(anchorScreen); }
        public bool Contains(Vector2 screen) => RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screen, uiCamera);
    }
}
