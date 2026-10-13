using System;
using UIFramework.Core;
using UIFramework.Panel;
using UIFramework.Window;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace UIFramework {
    /// <summary>
    /// 所有的对外接口封装在此，相当于UIManager
    /// </summary>
    public class UIFrame : MonoBehaviour {
        [SerializeField] [Tooltip("如果需要手动初始化该UI框架，则将此置为false")]
        private bool initializeOnAwake = true;

        private PanelUILayer panelLayer;
        private WindowUILayer windowLayer;

        private Canvas mainCanvas;
        private GraphicRaycaster graphicRaycaster;

        /// <summary>
        /// 主Canvas
        /// </summary>
        public Canvas MainCanvas {
            get {
                if (!mainCanvas) mainCanvas = GetComponent<Canvas>();
                return mainCanvas;
            }
        }

        /// <summary> 主Canvas的相机 </summary>
        public Camera UICamera => MainCanvas.worldCamera;

        private void Awake() {
            if (initializeOnAwake) Initialize();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize() {
            if (panelLayer == null) {
                panelLayer = gameObject.GetComponentInChildren<PanelUILayer>(true);
                if (panelLayer == null) Debug.LogError("[UI Frame] UI Frame lacks Panel Layer!");
                else panelLayer.Initialize();
            }

            if (windowLayer == null) {
                windowLayer = gameObject.GetComponentInChildren<WindowUILayer>(true);
                if (windowLayer == null) {
                    Debug.LogError("[UI Frame] UI Frame lacks Window Layer!");
                }
                else {
                    windowLayer.Initialize();
                    windowLayer.RequestScreenBlock += OnRequestScreenBlock;
                    windowLayer.RequestScreenBlock += OnRequestScreenUnblock;
                }
            }

            graphicRaycaster = MainCanvas.GetComponent<GraphicRaycaster>();
            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(MainCanvas.worldCamera);
        }

        /// <summary>
        /// 通过id显示面板
        /// </summary>
        public void ShowPanel(string screenId) {
            panelLayer.ShowScreenById(screenId);
        }

        /// <summary>
        /// 通过id显示面板（带属性）
        /// </summary>
        public void ShowPanel<T>(string screenId, T properties) where T : IPanelProperties {
            panelLayer.ShowScreenById(screenId, properties);
        }

        /// <summary>
        /// 通过id隐藏面板
        /// </summary>
        public void HidePanel(string screenId) {
            panelLayer.HideScreenById(screenId);
        }

        /// <summary>
        /// 通过id显示窗口
        /// </summary>
        public void OpenWindow(string screenId) {
            windowLayer.ShowScreenById(screenId);
        }

        /// <summary>
        /// 通过id显示窗口（带属性）
        /// </summary>
        public void OpenWindow<T>(string screenId, T properties) where T : IWindowProperties {
            windowLayer.ShowScreenById(screenId, properties);
        }

        /// <summary>
        /// 通过id隐藏窗口
        /// </summary>
        public void CloseWindow(string screenId) {
            windowLayer.HideScreenById(screenId);
        }

        /// <summary>
        /// 关闭当前窗口
        /// </summary>
        public void CloseCurrentWindow() {
            if (windowLayer.CurrentWindow != null) CloseWindow(windowLayer.CurrentWindow.ScreenId);
        }

        /// <summary>
        /// 二次包装方法，给ID即显示，搜到什么算什么
        /// </summary>
        /// <param name="screenId"></param>
        public void ShowScreen(string screenId) {
            if (IsScreenRegistered(screenId, out var type)) {
                if (type == typeof(IWindowController)) OpenWindow(screenId);
                else if (type == typeof(IScreenController)) ShowPanel(screenId);
            }
            else {
                Debug.LogError($"Tried to open Screen id {screenId} but it's not registered as Window or Panel!");
            }
        }

        /// <summary>
        /// 注册界面，不注册显示不出来
        /// </summary>
        public void RegisterScreen(string screenId, IScreenController controller, Transform screenTransform) {
            switch (controller) {
                case IWindowController window: {
                    windowLayer.RegisterScreen(screenId, window);
                    if (screenTransform != null) windowLayer.ReparentScreen(controller, screenTransform);
                    break;
                }
                case IPanelController panel: {
                    panelLayer.RegisterScreen(screenId, panel);
                    if (screenTransform != null) panelLayer.ReparentScreen(controller, screenTransform);
                    break;
                }
            }
        }

        /// <summary>
        /// 注册面板，不注册显示不出来
        /// </summary>
        public void RegisterPanel<TPanel>(string screenId, TPanel controller) where TPanel : IPanelController {
            panelLayer.RegisterScreen(screenId, controller);
        }

        /// <summary>
        /// 注销面板
        /// </summary>
        public void UnregisterPanel<TPanel>(string screenId, TPanel controller) where TPanel : IPanelController {
            panelLayer.UnregisterScreen(screenId, controller);
        }

        /// <summary>
        /// 注册窗口，不注册显示不出来
        /// </summary>
        public void RegisterWindow<TWindow>(string screenId, TWindow controller) where TWindow : IWindowController {
            windowLayer.RegisterScreen(screenId, controller);
        }

        /// <summary>
        /// 注销窗口
        /// </summary>
        public void UnregisterWindow<TWindow>(string screenId, TWindow controller) where TWindow : IWindowController {
            windowLayer.UnregisterScreen(screenId, controller);
        }

        /// <summary>
        /// 根据面板Id检测该面板是否开启中
        /// </summary>
        public bool IsPanelOpen(string panelId) {
            return panelLayer.IsPanelVisible(panelId);
        }

        /// <summary>
        /// 隐藏所有界面
        /// </summary>
        /// <param name="animate"></param>
        public void HideAll(bool animate = true) {
            CloseAllWindows(animate);
            HideAllPanels(animate);
        }

        /// <summary>
        /// 隐藏所有面板层界面
        /// </summary>
        /// <param name="animate"></param>
        public void HideAllPanels(bool animate = true) {
            panelLayer.HideAll(animate);
        }

        /// <summary>
        /// 隐藏所有窗口层界面
        /// </summary>
        /// <param name="animate"></param>
        public void CloseAllWindows(bool animate = true) {
            windowLayer.HideAll(animate);
        }

        /// <summary>
        /// 检测界面是否已注册
        /// </summary>
        public bool IsScreenRegistered(string screenId) {
            return windowLayer.IsScreenRegistered(screenId) || panelLayer.IsScreenRegistered(screenId);
        }

        /// <summary>
        /// 检测界面是否已注册
        /// </summary>
        /// <param name="type">界面类型，是面板还是窗口</param>
        public bool IsScreenRegistered(string screenId, out Type type) {
            if (windowLayer.IsScreenRegistered(screenId)) {
                type = typeof(IWindowController);
                return true;
            }

            if (panelLayer.IsScreenRegistered(screenId)) {
                type = typeof(IPanelController);
                return true;
            }

            type = null;
            return false;
        }

        private void OnRequestScreenBlock() {
            if (graphicRaycaster != null) graphicRaycaster.enabled = false;
        }

        private void OnRequestScreenUnblock() {
            if (graphicRaycaster != null) graphicRaycaster.enabled = true;
        }
    }
}