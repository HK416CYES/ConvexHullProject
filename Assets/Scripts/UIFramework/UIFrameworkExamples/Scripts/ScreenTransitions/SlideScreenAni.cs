using System;
using DG.Tweening;
using UIFramework.ViewAnimation;
using UnityEngine;

namespace UIFrameworkExamples.Scripts.ScreenTransitions {
    public class SlideScreenAni : AniComponent {
        public enum Position {
            None = 0,
            Left = 1,
            Right = 2,
            Top = 3,
            Bottom = 4
        }

        [SerializeField] protected Position origin = Position.Left;
        [SerializeField] protected bool isOutAnimation;
        [SerializeField] protected float duration = 0.5f;
        [SerializeField] protected bool doFade;
        [SerializeField] protected float fadeDurationPercent = 0.5f;
        [SerializeField] protected Ease ease = Ease.Linear;

        public Position Origin {
            get => origin;
            set => origin = value;
        }

        public override void Animate(Transform target, Action callWhenFinished) {
            var rTransform = target as RectTransform;
            var origAnchoredPos = rTransform.anchoredPosition;

            var startPosition = origin switch {
                Position.Left => new Vector3(-rTransform.rect.width, 0.0f, 0.0f),
                Position.Right => new Vector3(rTransform.rect.width, 0.0f, 0.0f),
                Position.Top => new Vector3(0.0f, rTransform.rect.height, 0.0f),
                Position.Bottom => new Vector3(0.0f, -rTransform.rect.height, 0.0f),
                _ => Vector3.zero
            };

            rTransform.anchoredPosition = isOutAnimation ? Vector3.zero : startPosition;

            rTransform.DOKill();

            CanvasGroup canvasGroup = null;
            if (doFade) {
                canvasGroup = rTransform.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = rTransform.gameObject.AddComponent<CanvasGroup>();

                canvasGroup.DOFade(isOutAnimation ? 0f : 1f, duration * fadeDurationPercent);
            }

            rTransform.DOAnchorPos(isOutAnimation ? startPosition : Vector3.zero, duration, true)
                .SetEase(ease).OnComplete(() => {
                    callWhenFinished();
                    rTransform.anchoredPosition = origAnchoredPos;
                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                }).SetUpdate(true);
        }
    }
}