using System.Collections.Generic;
using UIFramework.Panel;
using UnityEngine;
using UnityEngine.UI;

namespace ConvexHullDemo {
    public sealed class HullPlaybackPanel : PanelController {
        public RectTransform card,labelsRoot;
        public Text labelPrefab;
        public Button pauseButton,nextButton,stopButton;
        public Dropdown speedDropdown;
        private readonly List<Text> labels=new List<Text>();
        private IReadOnlyList<Vector2> positions;
        private HullTeachingSession labelSession;
        private bool labelsNumbered;
        private int groupingRevision=-1;
        private HullAlgorithmPlayback playback;
        private Camera worldCamera,uiCamera;
        private static readonly float[] speeds={.25f,.5f,1,2,4};
        public void Bind(HullAlgorithmPlayback value,Camera world,Camera ui) {
            playback=value;worldCamera=world;uiCamera=ui;playback.StateChanged+=Refresh;
        }
        protected override void AddListeners() {
            pauseButton.onClick.AddListener(Pause);nextButton.onClick.AddListener(Next);stopButton.onClick.AddListener(Stop);
            speedDropdown.onValueChanged.AddListener(Speed);
        }
        protected override void RemoveListeners() {
            pauseButton.onClick.RemoveListener(Pause);nextButton.onClick.RemoveListener(Next);stopButton.onClick.RemoveListener(Stop);
            speedDropdown.onValueChanged.RemoveListener(Speed);if(playback!=null)playback.StateChanged-=Refresh;
        }
        private void Pause(){playback?.TogglePause();}
        private void Next(){playback?.NextStep();}
        private void Stop(){playback?.Stop();}
        private void Speed(int index){if(playback!=null)playback.SetSpeed(speeds[Mathf.Clamp(index,0,4)]);}
        public void Refresh() {
            if(playback==null||!playback.IsActive)return;
            pauseButton.GetComponentInChildren<Text>().text=playback.IsPaused?"继续":"暂停";
            nextButton.interactable=playback.CanStep;
            int index=2;for(int i=0;i<speeds.Length;i++)if(Mathf.Approximately(speeds[i],playback.Speed))index=i;
            speedDropdown.SetValueWithoutNotify(index);
            UpdateLabels();
        }
        public void SetLabels(HullTeachingSession trace) {
            positions=trace.Unique;labelSession=trace;labelsNumbered=false;groupingRevision=-1;
            while(labels.Count<positions.Count){var label=Instantiate(labelPrefab,labelsRoot);label.gameObject.SetActive(true);labels.Add(label);}
            // 对象池只复用 Text，不复用上一场演示的号码。
            for(int i=0;i<labels.Count;i++){labels[i].gameObject.SetActive(false);labels[i].text="";}
            UpdateLabels();
        }
        public void ClearLabels(){positions=null;labelSession=null;labelsNumbered=false;foreach(var label in labels)if(label!=null)label.gameObject.SetActive(false);}
        private void LateUpdate(){UpdateLabels();}
        private void UpdateLabels() {
            if(positions==null||labelSession==null||!labelSession.NumberingReady)return;
            if(!labelsNumbered||groupingRevision!=labelSession.GroupingRevision) {
                for(int i=0;i<positions.Count;i++) {
                    int group=labelSession.PointGroups[i];
                    labels[i].text=labelSession.DisplayNumbers[i].ToString()+(group>=0?"·组"+(group+1):"");
                    labels[i].color=group>=0?HullTeachingOverlay.ChanGroupColor(group):labelPrefab.color;
                }
                groupingRevision=labelSession.GroupingRevision;
                labelsNumbered=true;
            }
            for(int i=0;i<positions.Count;i++) {
                Vector2 screen=worldCamera.WorldToScreenPoint(positions[i]+new Vector2(.25f,.35f));
                bool visible=worldCamera.pixelRect.Contains(screen);labels[i].gameObject.SetActive(visible);if(!visible)continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(labelsRoot,screen,uiCamera,out var local);
                labels[i].rectTransform.anchoredPosition=local;
            }
        }
        protected override void HierarchyFixOnShow(){transform.SetAsLastSibling();}
    }
}
