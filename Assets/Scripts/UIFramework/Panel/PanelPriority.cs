using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIFramework.Panel {
    /// <summary> 规定面板属于哪个层，便于管理 </summary>
    public enum PanelPriority {
        None = 0,
        Prioritary = 1,
        Tutorial = 2,
        Blocker = 3
    }

    [Serializable]
    public class PanelPriorityLayerListEntry {
        [SerializeField] [Tooltip("指定下面板层的优先级")]
        private PanelPriority priority;

        [SerializeField] [Tooltip("此优先级下所有面板的父节点")]
        private Transform targetParent;

        public Transform TargetParent {
            get => targetParent;
            set => targetParent = value;
        }

        public PanelPriority Priority {
            get => priority;
            set => priority = value;
        }

        public PanelPriorityLayerListEntry(PanelPriority priority, Transform targetParent) {
            this.priority = priority;
            this.targetParent = targetParent;
        }
    }

    [Serializable]
    public class PanelPriorityLayerList {
        [SerializeField] [Tooltip("根据面板的优先级查找并储存对应的GameObject。渲染优先级由这些GameObject在层次结构中的顺序决定")]
        private List<PanelPriorityLayerListEntry> paraLayers = null;

        private Dictionary<PanelPriority, Transform> lookup;

        public Dictionary<PanelPriority, Transform> ParaLayerLookup {
            get {
                if (lookup == null || lookup.Count == 0) CacheLookup();
                return lookup;
            }
        }

        private void CacheLookup() {
            lookup = new Dictionary<PanelPriority, Transform>();
            foreach (var paraLayer in paraLayers) lookup.Add(paraLayer.Priority, paraLayer.TargetParent);
        }

        public PanelPriorityLayerList(List<PanelPriorityLayerListEntry> entries) {
            paraLayers = entries;
        }
    }
}