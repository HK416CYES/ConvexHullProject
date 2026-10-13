using UIFramework.Core;

namespace UIFramework.Panel {
    public abstract class PanelController : APanelController<PanelProperties> {
    }

    public abstract class APanelController<T> : UIScreenController<T>, IPanelController where T : IPanelProperties {
        public PanelPriority Priority {
            get {
                if (Properties != null)
                    return Properties.Priority;
                else
                    return PanelPriority.None;
            }
        }

        protected sealed override void SetProperties(T props) {
            base.SetProperties(props);
        }
    }
}