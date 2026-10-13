using UIFramework.Window;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UIFrameworkExamples.Scripts.ScreenController {
    public class PopupExampleWindowController : WindowController {
        [SerializeField] private Image exampleImage = null;

        private int currentPopupExample;
        private Color originalColor;

        protected override void Awake() {
            base.Awake();
            originalColor = exampleImage.color;
        }

        public void UI_ShowPopup() {
            Signals.Get<ShowConfirmationPopupSignal>().Dispatch(GetPopupData());
        }

        private ConfirmationPopupProperties GetPopupData() {
            var testProps = currentPopupExample switch {
                0 => new ConfirmationPopupProperties("标题测试", "点下按钮试试", "明白"),
                1 => new ConfirmationPopupProperties("小测试", "随便选个颜色", "蓝色", OnBlueSelected, "红色", OnRedSelected),
                2 => new ConfirmationPopupProperties("恢复颜色", "恢复成原来的颜色吧", "好的", OnRevertColors),
                3 => new ConfirmationPopupProperties("重置", "随便测一下", "重置"),
                _ => null
            };

            currentPopupExample++;
            if (currentPopupExample > 3) currentPopupExample = 0;

            return testProps;
        }

        private void OnRevertColors() {
            exampleImage.color = originalColor;
        }

        private void OnRedSelected() {
            exampleImage.color = Color.red;
        }

        private void OnBlueSelected() {
            exampleImage.color = Color.blue;
        }
    }
}