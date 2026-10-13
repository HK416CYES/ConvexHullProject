using UIFramework.Core;
using UnityEngine;

namespace UIFramework.Panel {
    /// <summary>
    /// 控制面板的Layer层
    /// 面板是界面的一种，没有历史记录，没有队列
    /// 仅简单显示在界面中
    /// 如体力槽、小地图等
    /// </summary>
    public class PanelUILayer : UILayer<IPanelController> {
        [SerializeField] [Tooltip("优先级并行层的设置。注册到此层的面板将根据其优先级重新归属到不同的并行层对象")]
        private PanelPriorityLayerList priorityLayers = null;

        public override void ReparentScreen(IScreenController controller, Transform screenTransform) {
            if (controller is IPanelController ctl) ReparentToParaLayer(ctl.Priority, screenTransform);
            else base.ReparentScreen(controller, screenTransform);
        }

        public override void ShowScreen(IPanelController screen) {
            screen.Show();
        }

        public override void ShowScreen<TProps>(IPanelController screen, TProps props) {
            screen.Show(props);
        }

        public override void HideScreen(IPanelController screen) {
            screen.Hide();
        }

        public bool IsPanelVisible(string panelId) {
            return registeredScreens.TryGetValue(panelId, out var panel) && panel.IsVisible;
        }

        private void ReparentToParaLayer(PanelPriority priority, Transform screenTransform) {
            if (!priorityLayers.ParaLayerLookup.TryGetValue(priority, out var trans)) trans = transform;
            screenTransform.SetParent(trans, false);
        }
    }
}