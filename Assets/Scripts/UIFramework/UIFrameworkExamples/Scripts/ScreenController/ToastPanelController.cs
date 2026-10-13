using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UIFramework.Panel;
using UIFrameworkExamples.Scripts.Extras;
using UnityEngine;
using Utils;

namespace UIFrameworkExamples.Scripts.ScreenController {
    public class ToastPanelController : PanelController {
        [SerializeField] private RectTransform toastRect = null;
        [SerializeField] private float toastDuration = 0.5f;
        [SerializeField] private float toastPause = 2f;
        [SerializeField] private Ease toastEase = Ease.Linear;

        private bool isToasting;

        protected override void AddListeners() {
            Signals.Get<PlayerDataUpdatedSignal>().AddListener(OnDataUpdated);
        }

        protected override void RemoveListeners() {
            Signals.Get<PlayerDataUpdatedSignal>().RemoveListener(OnDataUpdated);
        }

        private void OnDataUpdated(List<PlayerDataEntry> data) {
            if (isToasting) return;

            StartCoroutine(YieldForDOTween());
        }

        private IEnumerator YieldForDOTween() {
            yield return null;

            isToasting = true;
            var seq = DOTween.Sequence();
            seq.Append(toastRect.DOAnchorPosY(0f, toastDuration).SetEase(toastEase));
            seq.AppendInterval(toastPause);
            seq.Append(toastRect.DOAnchorPosY(toastRect.rect.height, toastDuration).SetEase(toastEase));
            seq.OnComplete(() => isToasting = false);

            seq.Play();
        }
    }
}