using System;
using System.Collections.Generic;
using UIFramework.Panel;
using UIFrameworkExamples.Scripts.Widgets;
using UnityEngine;
using Utils;

namespace UIFrameworkExamples.Scripts.ScreenController {
    [Serializable]
    public class NavigationPanelEntry {
        [SerializeField] private Sprite sprite = null;
        [SerializeField] private string buttonText = "";
        [SerializeField] private string targetScreen = "";

        public Sprite Sprite => sprite;
        public string ButtonText => buttonText;
        public string TargetScreen => targetScreen;
    }

    public class NavigateToWindowSignal : ASignal<string> { }

    public class NavigationPanelController : PanelController {
        [SerializeField] private List<NavigationPanelEntry> navigationTargets = new();
        [SerializeField] private NavigationPanelButton templateButton = null;

        private readonly List<NavigationPanelButton> currentButtons = new();

        protected override void AddListeners() {
            Signals.Get<NavigateToWindowSignal>().AddListener(OnExternalNavigation);
        }

        protected override void RemoveListeners() {
            Signals.Get<NavigateToWindowSignal>().RemoveListener(OnExternalNavigation);
        }

        /// <summary>
        /// 当界面打开时候，这个函数被调用
        /// </summary>
        protected override void OnPropertiesSet() {
            ClearEntries();
            foreach (var target in navigationTargets) {
                var newBtn = Instantiate(templateButton);
                newBtn.transform.SetParent(templateButton.transform.parent, false);
                newBtn.SetData(target);
                newBtn.gameObject.SetActive(true);
                newBtn.ButtonClicked += OnNavigationButtonClicked;
                currentButtons.Add(newBtn);
            }

            // 默认选中第一个按钮
            OnNavigationButtonClicked(currentButtons[0]);
        }

        private void OnNavigationButtonClicked(NavigationPanelButton currentlyClickedButton) {
            Signals.Get<NavigateToWindowSignal>().Dispatch(currentlyClickedButton.Target);
            foreach (var button in currentButtons) button.SetCurrentNavigationTarget(currentlyClickedButton);
        }

        private void OnExternalNavigation(string screenId) {
            foreach (var button in currentButtons) button.SetCurrentNavigationTarget(screenId);
        }

        private void ClearEntries() {
            foreach (var button in currentButtons) {
                button.ButtonClicked -= OnNavigationButtonClicked;
                Destroy(button.gameObject);
            }
        }
    }
}