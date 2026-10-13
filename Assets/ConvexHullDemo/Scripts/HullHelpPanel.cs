using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;

namespace ConvexHullDemo {
    public sealed class HullHelpPanel : PanelController {
        public const string PreferenceKey = "ConvexHullDemo.HideHelpOnEntry";
        public Button closeButton;
        public Toggle doNotShow;
        private ConvexHullDemoController demo;
        public static bool ShouldShowOnEntry => PlayerPrefs.GetInt(PreferenceKey, 0) == 0;
        public void Bind(ConvexHullDemoController controller) { demo = controller; SyncPreference(); }
        public void SyncPreference() { doNotShow.SetIsOnWithoutNotify(!ShouldShowOnEntry); }
        protected override void AddListeners() {
            closeButton.onClick.AddListener(Close);
            doNotShow.onValueChanged.AddListener(SavePreference);
        }
        protected override void RemoveListeners() {
            closeButton.onClick.RemoveListener(Close);
            doNotShow.onValueChanged.RemoveListener(SavePreference);
        }
        private void Close() { if (demo != null) demo.HideHelp(); }
        private void SavePreference(bool hide) {
            PlayerPrefs.SetInt(PreferenceKey, hide ? 1 : 0);
            PlayerPrefs.Save();
        }
        protected override void HierarchyFixOnShow() { transform.SetAsLastSibling(); }
    }
}
