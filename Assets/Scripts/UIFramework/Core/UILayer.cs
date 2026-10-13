using System.Collections.Generic;
using UnityEngine;

namespace UIFramework.Core {
    /// <summary>
    /// 基础的UI Layer层
    /// </summary>
    public abstract class UILayer<TScreen> : MonoBehaviour where TScreen : IScreenController {
        protected Dictionary<string, TScreen> registeredScreens;

        /// <summary>
        /// 显示界面
        /// </summary>
        /// <param name="screen">界面类型</param>
        public abstract void ShowScreen(TScreen screen);

        /// <summary>
        /// 显示界面，带参数
        /// </summary>
        /// <param name="screen">界面类型</param>
        /// <param name="props">属性</param>
        /// <typeparam name="TProps">属性的类型</typeparam>
        public abstract void ShowScreen<TProps>(TScreen screen, TProps props) where TProps : IScreenProperties;

        /// <summary>
        /// 隐藏界面
        /// </summary>
        /// <param name="screen">界面类型</param>
        public abstract void HideScreen(TScreen screen);

        /// <summary>
        /// 初始化Layer层
        /// </summary>
        public virtual void Initialize() {
            registeredScreens = new Dictionary<string, TScreen>();
        }

        /// <summary>
        /// 传入的界面作为子节点
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="screenTransform"></param>
        public virtual void ReparentScreen(IScreenController controller, Transform screenTransform) {
            screenTransform.SetParent(transform, false);
        }

        /// <summary>
        /// 注册界面上的Controller带上明确的界面ID
        /// </summary>
        /// <param name="screenId">界面ID</param>
        /// <param name="controller">界面控制器</param>
        public void RegisterScreen(string screenId, TScreen controller) {
            if (!registeredScreens.ContainsKey(screenId))
                ProcessScreenRegister(screenId, controller);
            else
                Debug.LogError("[AUILayerController] Screen controller already registered for id: " + screenId);
        }

        /// <summary>
        /// 根据ID取消注册界面的Controller
        /// </summary>
        /// <param name="screenId">界面ID</param>
        /// <param name="controller">被取消的界面Controller</param>
        public void UnregisterScreen(string screenId, TScreen controller) {
            if (registeredScreens.ContainsKey(screenId))
                ProcessScreenUnregister(screenId, controller);
            else
                Debug.LogError("[AUILayerController] Screen controller not registered for id: " + screenId);
        }

        /// <summary>
        /// 根据id去找界面Controller，并且显示出来
        /// </summary>
        /// <param name="screenId">界面ID</param>
        public void ShowScreenById(string screenId) {
            if (registeredScreens.TryGetValue(screenId, out var ctl))
                ShowScreen(ctl);
            else
                Debug.LogError("[AUILayerController] Screen ID " + screenId + " not registered to this layer!");
        }

        /// <summary>
        /// 根据id去找界面Controller（带参数），并且显示出来
        /// </summary>
        /// <param name="screenId">界面ID</param>
        /// <param name="props">参数</param>
        /// <typeparam name="TProps">参数类型</typeparam>
        public void ShowScreenById<TProps>(string screenId, TProps props) where TProps : IScreenProperties {
            if (registeredScreens.TryGetValue(screenId, out var ctl))
                ShowScreen(ctl, props);
            else
                Debug.LogError("[AUILayerController] Screen ID " + screenId + " not registered!");
        }

        /// <summary>
        /// 根据Id隐藏界面
        /// </summary>
        /// <param name="screenId">界面id</param>
        public void HideScreenById(string screenId) {
            if (registeredScreens.TryGetValue(screenId, out var ctl))
                HideScreen(ctl);
            else
                Debug.LogError("[AUILayerController] Could not hide Screen ID " + screenId + " as it is not registered!");
        }

        /// <summary>
        /// 检查id是否被注册
        /// </summary>
        /// <param name="screenId">界面ID</param>
        public bool IsScreenRegistered(string screenId) {
            return registeredScreens.ContainsKey(screenId);
        }

        /// <summary>
        /// 隐藏全部界面
        /// </summary>
        /// <param name="shouldAnimateWhenHiding">隐藏界面时是否需要动画</param>
        public virtual void HideAll(bool shouldAnimateWhenHiding = true) {
            foreach (var screen in registeredScreens) screen.Value.Hide(shouldAnimateWhenHiding);
        }

        protected virtual void ProcessScreenRegister(string screenId, TScreen controller) {
            controller.ScreenId = screenId;
            registeredScreens.Add(screenId, controller);
            controller.ScreenDestroyed += OnScreenDestoryed;
        }

        protected virtual void ProcessScreenUnregister(string screenId, TScreen controller) {
            controller.ScreenDestroyed -= OnScreenDestoryed;
            registeredScreens.Remove(screenId);
        }

        private void OnScreenDestoryed(IScreenController screen) {
            if (!string.IsNullOrEmpty(screen.ScreenId) && registeredScreens.ContainsKey(screen.ScreenId)) UnregisterScreen(screen.ScreenId, (TScreen)screen);
        }
    }
}