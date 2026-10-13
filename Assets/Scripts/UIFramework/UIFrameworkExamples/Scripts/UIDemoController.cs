using System;
using UIFramework;
using UIFrameworkExamples.Scripts.Extras;
using UIFrameworkExamples.Scripts.ScreenController;
using UnityEngine;
using Utils;

namespace UIFrameworkExamples.Scripts {
    public class UIDemoController : MonoBehaviour {
        [SerializeField] private UISetting defualtUISetting = null;

        [SerializeField] private FakePlayerData fakePlayerData = null;
        [SerializeField] private Camera cam = null;
        [SerializeField] private Transform transformToFollow = null;

        private UIFrame uiFrame;

        private void Awake() {
            uiFrame = defualtUISetting.CreateUIInstance();
            Signals.Get<StartDemoSignal>().AddListener(OnStartDemo);
            Signals.Get<NavigateToWindowSignal>().AddListener(OnNavigateToWindow);
            Signals.Get<ShowConfirmationPopupSignal>().AddListener(OnShowConfirmationPopup);
        }

        private void OnDestroy() {
            Signals.Get<StartDemoSignal>().RemoveListener(OnStartDemo);
            Signals.Get<NavigateToWindowSignal>().RemoveListener(OnNavigateToWindow);
            Signals.Get<ShowConfirmationPopupSignal>().RemoveListener(OnShowConfirmationPopup);
        }

        private void Start() {
            uiFrame.OpenWindow(ScreenIds.StartGameWindow);
        }

        private void OnStartDemo() {
            uiFrame.ShowPanel(ScreenIds.NavigationPanel);
            uiFrame.ShowPanel(ScreenIds.ToastPanel);
        }

        private void OnNavigateToWindow(string windowId) {
            uiFrame.CloseCurrentWindow();

            switch (windowId) {
                case ScreenIds.PlayerWindow:
                    uiFrame.OpenWindow(windowId, new PlayerWindowProperties(fakePlayerData.LevelProgress));
                    break;
                case ScreenIds.CameraProjectionWindow:
                    transformToFollow.parent.gameObject.SetActive(true);
                    uiFrame.OpenWindow(windowId, new CameraProjectionWindowProperties(cam, transformToFollow));
                    break;
                default:
                    uiFrame.OpenWindow(windowId);
                    break;
            }
        }

        private void OnShowConfirmationPopup(ConfirmationPopupProperties popupPayload) {
            uiFrame.OpenWindow(ScreenIds.ConfirmationPopup, popupPayload);
        }
    }
}