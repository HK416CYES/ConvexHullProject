using System;
using UIFramework.Core;
using UnityEngine;

namespace UIFramework.Panel {
    /// <summary>
    /// 面板属性类
    /// </summary>
    [Serializable]
    public class PanelProperties : IPanelProperties {
        [Tooltip("面板根据其优先级进入不同的副层级。可以在“面板层级”中设置副层级。")] [SerializeField]
        private PanelPriority priority;

        public PanelPriority Priority {
            get => priority;
            set => priority = value;
        }
    }
}