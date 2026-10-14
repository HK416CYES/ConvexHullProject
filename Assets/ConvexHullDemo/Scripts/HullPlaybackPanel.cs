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
        }
        public void SetLabels(HullTeachingAlgorithm.Trace trace,bool deduplicated) {
            positions=trace.Unique;
            while(labels.Count<positions.Count){var label=Instantiate(labelPrefab,labelsRoot);label.gameObject.SetActive(true);labels.Add(label);}
            int sortedIndex=0;
            for(int i=0;i<labels.Count;i++) {
                labels[i].gameObject.SetActive(i<positions.Count);if(i>=positions.Count)continue;
                int start=sortedIndex;
                while(sortedIndex<trace.Sorted.Count&&trace.Sorted[sortedIndex].Equals(positions[i]))sortedIndex++;
                labels[i].text=deduplicated?(i+1).ToString():sortedIndex-start>1?(start+1)+"–"+sortedIndex:(start+1).ToString();
            }
            UpdateLabels();
        }
        public void ClearLabels(){positions=null;foreach(var label in labels)if(label!=null)label.gameObject.SetActive(false);}
        private void LateUpdate(){UpdateLabels();}
        private void UpdateLabels() {
            if(positions==null)return;
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
