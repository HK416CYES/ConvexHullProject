using UnityEngine;

namespace ConvexHullDemo {
    public sealed class HullPointView : MonoBehaviour {
        public MeshRenderer dot;
        public MeshRenderer selectionRing;
        private MaterialPropertyBlock block;
        public void SetAppearance(Color color, Color selectedColor, bool selected) {
            if (block == null) block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            dot.SetPropertyBlock(block);
            block.SetColor("_Color", selectedColor);
            selectionRing.SetPropertyBlock(block);
            selectionRing.gameObject.SetActive(selected);
        }
    }
}
