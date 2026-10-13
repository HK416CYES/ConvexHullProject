using UIFramework.Window;
using Utils;

namespace UIFrameworkExamples.Scripts.ScreenController {
    public class StartDemoSignal : ASignal { }

    public class StartWindowController : WindowController {
        public void UI_Start() {
            Signals.Get<StartDemoSignal>().Dispatch();
        }
    }
}