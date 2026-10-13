using System;
using UIFramework.ViewAnimation;
using UnityEngine;

namespace UIFramework.Core {
    /// <summary>
    /// UI界面的基类，窗口，面板这些都继承它，比如 AWindowController，PanelController
    /// </summary>
    /// <typeparam name="TProps"></typeparam>
    public abstract class UIScreenController<TProps> : MonoBehaviour, IScreenController where TProps : IScreenProperties {
        [Header("Screen Animations")]

        //--
        [SerializeField]
        [Tooltip("界面显示的动画")]
        private AniComponent animIn;

        [SerializeField] [Tooltip("界面隐藏的动画")] private AniComponent animOut;

        [Header("Screen Properties")] [Tooltip("界面的属性参数")] [SerializeField]
        private TProps properties;

        /// <summary> 界面id，用字符串形式保存 </summary>
        public string ScreenId { get; set; }


        /// <summary> 动画组件，为了界面有统一弹出效果 </summary>
        public AniComponent AnimIn {
            get => animIn;
            set => animIn = value;
        }

        /// <summary> 动画组件，为了界面有统一隐藏效果 </summary>
        public AniComponent AnimOut {
            get => animOut;
            set => animOut = value;
        }

        /// <summary> 弹出（渐入）动画完成后的回调 </summary>
        public Action<IScreenController> InTransitionFinished { get; set; }

        /// <summary> 关闭（渐出）动画完成后的回调 </summary>
        public Action<IScreenController> OutTransitionFinished { get; set; }

        /// <summary> 关闭界面的回调 </summary>
        public Action<IScreenController> CloseRequest { get; set; }

        /// <summary> 界面销毁的回调 </summary>
        public Action<IScreenController> ScreenDestroyed { get; set; }

        /// <summary> 界面是否在显示中 </summary>
        public bool IsVisible { get; private set; }

        /// <summary> 界面属性参数 </summary>
        protected TProps Properties {
            get => properties;
            set => properties = value;
        }

        protected virtual void OnDestroy() {
            ScreenDestroyed?.Invoke(this);
            InTransitionFinished = null;
            OutTransitionFinished = null;
            CloseRequest = null;
            ScreenDestroyed = null;
            RemoveListeners();
        }

        protected virtual void Awake() {
            AddListeners();
        }

        /// <summary> 添加监听事件，Awake中自动调用 </summary>
        protected virtual void AddListeners() { }

        /// <summary> 移除监听事件，Destroy中自动调用 </summary>
        protected virtual void RemoveListeners() { }

        /// <summary> 属性参数设置到界面时触发，在SetProperties之后触发，比较安全的能取到值 </summary>
        protected virtual void OnPropertiesSet() { }

        /// <summary> 界面隐藏时触发 </summary>
        protected virtual void WhileHiding() { }

        /// <summary> 设置属性参数 </summary>
        protected virtual void SetProperties(TProps props) {
            Properties = props;
        }

        /// <summary> 在显示时处理一些层级，或者属性处理 </summary>
        protected virtual void HierarchyFixOnShow() { }

        /// <summary> 隐藏界面 </summary>
        /// <param name="animate">是否播放动画</param>
        public void Hide(bool animate = true) {
            DoAnimation(animate ? animOut : null, OnTransitionOutFinished, false);
            WhileHiding();
        }

        /// <summary> 显示界面 </summary>
        /// <param name="props"></param>
        public void Show(IScreenProperties props = null) {
            if (props != null) {
                if (props is TProps screenProperties) {
                    SetProperties(screenProperties);
                }
                else {
                    Debug.LogError("Properties passed have wrong type! (" + props.GetType() + " instead of " + typeof(TProps) + ")");
                    return;
                }
            }

            HierarchyFixOnShow();
            OnPropertiesSet();

            if (!gameObject.activeSelf) DoAnimation(animIn, OnTransitionInFinished, true);
            else InTransitionFinished?.Invoke(this);
        }

        private void DoAnimation(AniComponent caller, Action callWhenFinished, bool isVisible) {
            if (caller == null) {
                gameObject.SetActive(isVisible);
                callWhenFinished?.Invoke();
            }
            else {
                if (isVisible && !gameObject.activeSelf) gameObject.SetActive(true);

                caller.Animate(transform, callWhenFinished);
            }
        }

        private void OnTransitionInFinished() {
            IsVisible = true;
            InTransitionFinished?.Invoke(this);
        }

        private void OnTransitionOutFinished() {
            IsVisible = false;
            gameObject.SetActive(false);
            OutTransitionFinished?.Invoke(this);
        }
    }
}