using System;
using System.Collections.Generic;
using UIFramework.Core;
using UnityEngine;

namespace UIFramework {
    /// <summary>
    /// UI模版
    /// </summary>
    [CreateAssetMenu(fileName = "UISetting", menuName = "UI/UI Setting")]
    public class UISetting : ScriptableObject {
        [Tooltip("UI Frame预制体")] [SerializeField]
        private UIFrame templateUIPrefab = null;

        [Tooltip("界面预制体（包括面板与窗口）")] [SerializeField]
        private List<GameObject> screensToRegister = null;

        [Tooltip("实例化时是否停用")] [SerializeField] private bool deactivateScreenGOs = true;

        public UIFrame CreateUIInstance(bool instanceAndRegisterScreens = true) {
            var newUI = Instantiate(templateUIPrefab);

            if (!instanceAndRegisterScreens) return newUI;
            foreach (var screen in screensToRegister) {
                var screenInstance = Instantiate(screen);
                var screenController = screenInstance.GetComponent<IScreenController>();

                if (screenController != null) {
                    newUI.RegisterScreen(screen.name, screenController, screenInstance.transform);
                    if (deactivateScreenGOs && screenInstance.activeSelf) screenInstance.SetActive(false);
                }
                else {
                    Debug.LogError("[UIConfig] Screen doesn't contain a ScreenController! Skipping " + screen.name);
                }
            }

            return newUI;
        }

        private void OnValidate() {
            List<GameObject> objectsToRemove = new();
            foreach (var screen in screensToRegister) {
                var screenCtl = screen.GetComponent<IScreenController>();
                if (screenCtl == null) objectsToRemove.Add(screen);
            }

            if (objectsToRemove.Count > 0) {
                Debug.LogError("[UISetting] Some GameObjects that were added to the Screen Prefab List didn't have ScreenControllers attached to them! Removing.");
                foreach (var obj in objectsToRemove) {
                    Debug.LogError("[UISetting] Removed " + obj.name + " from " + name + " as it has no Screen Controller attached!");
                    screensToRegister.Remove(obj);
                }
            }
        }
    }
}