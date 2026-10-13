using System;
using System.Collections.Generic;
using UIFramework.Core;
using UnityEngine;

namespace UIFramework.Window {
    /// <summary>
    /// 控制窗口的Layer层
    /// 有显示记录和队列，并且一次只显示一个
    /// </summary>
    public class WindowUILayer : UILayer<IWindowController> {
        [SerializeField] private WindowParaLayer priorityParaLayer = null;
        public IWindowController CurrentWindow { get; private set; }

        private Queue<WindowHistoryEntry> windowQueue;
        private Stack<WindowHistoryEntry> windowHistory;

        public event Action RequestScreenBlock;
        public event Action RequestScreenUnblock;

        private bool IsScreenTransitionInProgress => screenTransitioning.Count != 0;

        private HashSet<IScreenController> screenTransitioning;

        public override void Initialize() {
            base.Initialize();
            //registeredScreens = new Dictionary<string, IWindowController>();
            windowQueue = new Queue<WindowHistoryEntry>();
            windowHistory = new Stack<WindowHistoryEntry>();
            screenTransitioning = new HashSet<IScreenController>();
        }

        protected override void ProcessScreenRegister(string screenId, IWindowController controller) {
            base.ProcessScreenRegister(screenId, controller);
            controller.InTransitionFinished += OnInAnimationFinished;
            controller.OutTransitionFinished += OnOutAnimationFinished;
            controller.CloseRequest += OnCloseRequestedByWindow;
        }

        protected override void ProcessScreenUnregister(string screenId, IWindowController controller) {
            base.ProcessScreenUnregister(screenId, controller);
            controller.InTransitionFinished -= OnInAnimationFinished;
            controller.OutTransitionFinished -= OnOutAnimationFinished;
            controller.CloseRequest -= OnCloseRequestedByWindow;
        }

        public override void ShowScreen(IWindowController screen) {
            ShowScreen<IWindowProperties>(screen, null);
        }

        public override void ShowScreen<TProps>(IWindowController screen, TProps properties) {
            var windowProp = properties as IWindowProperties;
            if (ShouldEnqueue(screen, windowProp))
                EnqueueWindow(screen, properties);
            else
                DoShow(screen, windowProp);
        }

        public override void HideScreen(IWindowController screen) {
            if (screen == CurrentWindow) {
                windowHistory.Pop();
                AddTransition(screen);
                screen.Hide();

                CurrentWindow = null;

                if (windowQueue.Count > 0) ShowNextInQueue();
                else if (windowHistory.Count > 0) ShowPreviousInHistory();
            }
            else {
                Debug.LogError(
                    string.Format(
                        "[WindowUILayer] Hide requested on WindowId {0} but that's not the currently open one ({1})! Ignoring request.",
                        screen.ScreenId, CurrentWindow != null ? CurrentWindow.ScreenId : "current is null"));
            }
        }

        public override void HideAll(bool shouldAnimateWhenHiding = true) {
            base.HideAll(shouldAnimateWhenHiding);
            CurrentWindow = null;
            priorityParaLayer.RefreshDarken();
            windowHistory.Clear();
        }

        public override void ReparentScreen(IScreenController controller, Transform screenTransform) {
            if (controller is IWindowController window) {
                if (window.IsPopup) {
                    priorityParaLayer.AddScreen(screenTransform);
                    return;
                }
            }
            else {
                Debug.LogError("[WindowUILayer] Screen " + screenTransform.name + " is not a Window!");
            }

            base.ReparentScreen(controller, screenTransform);
        }

        private void EnqueueWindow<TProp>(IWindowController screen, TProp properties) where TProp : IScreenProperties {
            windowQueue.Enqueue(new WindowHistoryEntry(screen, (IWindowProperties)properties));
        }

        private bool ShouldEnqueue(IWindowController controller, IWindowProperties windowProp) {
            if (CurrentWindow == null && windowQueue.Count == 0) return false;

            if (windowProp != null && windowProp.SuppressPrefabProperties) return windowProp.WindowQueuePriority != WindowPriority.ForceForeground;

            return controller.WindowPriority != WindowPriority.ForceForeground;
        }

        private void ShowPreviousInHistory() {
            if (windowHistory.Count > 0) {
                var window = windowHistory.Pop();
                DoShow(window);
            }
        }

        private void ShowNextInQueue() {
            if (windowQueue.Count > 0) {
                var window = windowQueue.Dequeue();
                DoShow(window);
            }
        }

        private void DoShow(IWindowController screen, IWindowProperties properties) {
            DoShow(new WindowHistoryEntry(screen, properties));
        }

        private void DoShow(WindowHistoryEntry windowEntry) {
            if (CurrentWindow == windowEntry.Screen)
                Debug.LogWarning(
                    string.Format(
                        "[WindowUILayer] The requested WindowId ({0}) is already open! This will add a duplicate to the " +
                        "history and might cause inconsistent behaviour. It is recommended that if you need to open the same" +
                        "screen multiple times (eg: when implementing a warning message pop-up), it closes itself upon the player input" +
                        "that triggers the continuation of the flow."
                        , CurrentWindow.ScreenId));
            else if (CurrentWindow != null
                     && CurrentWindow.HideOnForegroundLost
                     && !windowEntry.Screen.IsPopup)
                CurrentWindow.Hide();

            windowHistory.Push(windowEntry);
            AddTransition(windowEntry.Screen);

            if (windowEntry.Screen.IsPopup) priorityParaLayer.DarkenBG();

            windowEntry.Show();

            CurrentWindow = windowEntry.Screen;
        }

        private void OnInAnimationFinished(IScreenController screen) {
            RemoveTransition(screen);
        }

        private void OnOutAnimationFinished(IScreenController screen) {
            RemoveTransition(screen);
            var window = screen as IWindowController;
            if (window.IsPopup) priorityParaLayer.RefreshDarken();
        }

        private void OnCloseRequestedByWindow(IScreenController screen) {
            HideScreen(screen as IWindowController);
        }

        private void AddTransition(IScreenController screen) {
            screenTransitioning.Add(screen);
            RequestScreenBlock?.Invoke();
        }

        private void RemoveTransition(IScreenController screen) {
            screenTransitioning.Remove(screen);
            if (!IsScreenTransitionInProgress) RequestScreenUnblock?.Invoke();
        }
    }
}