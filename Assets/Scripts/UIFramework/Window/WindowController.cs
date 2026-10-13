using UIFramework.Core;

namespace UIFramework.Window {
    /// <summary>
    /// 窗口管理类
    /// </summary>
    public abstract class WindowController : WindowController<WindowProperties> { }

    /// <summary>
    /// 窗口管理基类
    /// </summary>
    public abstract class WindowController<TProps> : UIScreenController<TProps>, IWindowController where TProps : IWindowProperties {
        public bool HideOnForegroundLost => Properties.HideOnForegroundLost;
        public bool IsPopup => Properties.IsPopup;
        public WindowPriority WindowPriority => Properties.WindowQueuePriority;

        /// <summary>
        /// 关闭窗口，使用UI_前缀方便找到对应方法
        /// </summary>
        public virtual void UI_Close() {
            CloseRequest(this);
        }

        protected sealed override void SetProperties(TProps props) {
            if (props != null) {
                if (!props.SuppressPrefabProperties) {
                    props.HideOnForegroundLost = Properties.HideOnForegroundLost;
                    props.WindowQueuePriority = Properties.WindowQueuePriority;
                    props.IsPopup = Properties.IsPopup;
                }

                Properties = props;
            }
        }

        protected override void HierarchyFixOnShow() {
            transform.SetAsLastSibling();
        }
    }
}