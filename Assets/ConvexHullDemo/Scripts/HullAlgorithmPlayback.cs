using System;
using System.Collections.Generic;
using UIFramework;
using UnityEngine;

namespace ConvexHullDemo {
    public sealed class HullAlgorithmPlayback : MonoBehaviour {
        public const string PanelId="HullPlayback";
        public HullTeachingOverlay overlay;
        [Min(.05f)] public float stepInterval=.6f;
        [Min(.01f)] public float transitionDuration=.2f;
        private ConvexHullDemoController demo;
        private HullPlaybackPanel panel;
        private UIFrame frame;
        [Tooltip("关键步骤只显示有效变化；详细步骤额外显示每次几何比较。")]
        public TeachingGranularity granularity=TeachingGranularity.KeySteps;
        private HullTeachingSession trace;
        private TeachingEvent current;
        private bool waiting;
        public TeachingAlgorithmKind SelectedAlgorithm{get;private set;}
        public TeachingAlgorithmKind ActiveAlgorithm=>trace==null?SelectedAlgorithm:trace.Algorithm;
        public bool SelectAlgorithm(TeachingAlgorithmKind kind){if(IsActive)return false;SelectedAlgorithm=kind;StateChanged?.Invoke();return true;}
        private float elapsed;
        private bool stepping;
        public bool IsActive=>trace!=null;
        public bool IsPaused{get;private set;}
        public bool CanStep=>IsActive&&IsPaused&&!stepping;
        public float Speed{get;private set;}=1;
        public int StepIndex{get;private set;}
        public int StepCount=>IsActive?-1:0; // 流式算法尚未结束时，总步数未知。
        public HullTeachingSession CurrentTrace=>trace;
        public TeachingEvent CurrentEvent=>current;
        public event Action StateChanged;
        public void Bind(ConvexHullDemoController owner,HullPlaybackPanel ui,UIFrame uiFrame) {
            demo=owner;panel=ui;frame=uiFrame;panel.Bind(this,demo.worldCamera,frame.UICamera);
        }
        public bool Play(out string error) {
            error="";if(IsActive)return false;
            var snapshot=new List<Vector2>();foreach(var p in demo.Points)snapshot.Add(p.Position);
            if(snapshot.Count==0){error="没有点，无法演示";return false;}
            // 先检查可取景性；失败时不能改变相机或已有显示。
            Canvas.ForceUpdateCanvases();
            if(!TryFit(snapshot,out var size,out var position)){error="凸包过大，无法演示";return false;}
            var teachingPoints=new List<TeachingPoint>();foreach(var p in demo.Points)teachingPoints.Add(new TeachingPoint(p.Id,p.Position));
            var recording=TeachingAlgorithmRegistry.Create(SelectedAlgorithm,teachingPoints,granularity);
            demo.CancelGesture();demo.CloseMenu();demo.HideHelp();
            demo.worldCamera.orthographicSize=size;demo.worldCamera.transform.position=position;
            trace=recording;IsPaused=false;stepping=false;Speed=1;StepIndex=-1;elapsed=0;waiting=true;current=null;
            demo.SetPlaybackPresentation(true);
            overlay.ResetSession(trace);frame.ShowPanel(PanelId);panel.SetLabels(trace);
            Pump();StateChanged?.Invoke();return true;
        }
        private bool TryFit(List<Vector2> points,out float size,out Vector3 position) {
            var camera=demo.worldCamera;position=camera.transform.position;size=camera.orthographicSize;
            Vector2 min=points[0],max=points[0];foreach(var p in points){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            Rect view=camera.pixelRect;float scale=panel.GetComponentInParent<Canvas>().scaleFactor;
            float margin=24*scale;
            Rect usable=Rect.MinMaxRect(view.xMin+margin,view.yMin+(panel.card.rect.height+12)*scale+margin,
                view.xMax-margin,Mathf.Min(view.yMax-margin,demo.Toolbar.ToolbarScreenBottom-margin));
            if(usable.width<=0||usable.height<=0)return false;
            float markerDiameter=SelectedAlgorithm==TeachingAlgorithmKind.QuickHull?Mathf.Max(overlay.ringDiameter,overlay.confirmedPointDiameter):overlay.ringDiameter;
            float worldMargin=Mathf.Max(.5f,Mathf.Max(demo.pointDiameter*.9f,markerDiameter*.5f)+.5f);
            Vector2 span=max-min+Vector2.one*worldMargin*2;
            float required=Mathf.Max(span.y/(2*usable.height/view.height),span.x/(2*camera.aspect*usable.width/view.width));
            if(required>demo.maxZoomSize)return false;
            size=Mathf.Max(demo.minZoomSize,required);
            Vector2 center=(min+max)*.5f;
            // 让点集中心出现在“可用画布中心”，而不是被上下工具栏遮挡的全屏中心。
            Vector2 offset=(usable.center-view.center)/view.height*(2*size);
            position=new Vector3(center.x-offset.x,center.y-offset.y,camera.transform.position.z);return true;
        }
        private void Update(){Tick(Time.unscaledDeltaTime);}
        private float CurrentStepInterval {
            get {
                float interval=Mathf.Max(.05f,stepInterval);
                // Chan 只缩短重复构造/查询步骤的自动停留；分组、组完成和重试仍停留完整间隔。
                // 暂停单步和详细模式保留原节奏，不牺牲可逐步检查的能力。
                if(trace!=null&&trace.Algorithm==TeachingAlgorithmKind.Chan&&trace.Granularity==TeachingGranularity.KeySteps&&!IsPaused&&current!=null&&
                   current.Operation!=TeachingOperation.Restart&&current.Operation!=TeachingOperation.ChanPartition&&
                   current.Operation!=TeachingOperation.ChanBuildStart&&current.Operation!=TeachingOperation.ChanGroupComplete&&
                   current.Operation!=TeachingOperation.Stage&&current.Operation!=TeachingOperation.Finish)
                    interval=Mathf.Max(.016f,interval*.35f*Mathf.Min(1,20f/Mathf.Max(1,trace.Unique.Count)));
                return interval;
            }
        }
        public void Tick(float seconds) {
            if(!IsActive||(IsPaused&&!stepping))return;
            if(waiting){Pump();return;}
            elapsed+=Mathf.Max(0,seconds)*Speed;
            overlay.Draw(Mathf.Clamp01(elapsed/Mathf.Max(.01f,Mathf.Min(transitionDuration,CurrentStepInterval))));
            if(elapsed<CurrentStepInterval)return;
            if(current.Operation==TeachingOperation.Finish){Stop();return;}
            if(stepping){stepping=false;StateChanged?.Invoke();return;}
            waiting=true;Pump();
        }
        public void TogglePause(){if(!IsActive)return;IsPaused=!IsPaused;stepping=false;StateChanged?.Invoke();}
        public void Pause(){if(!IsActive)return;IsPaused=true;stepping=false;StateChanged?.Invoke();}
        public void NextStep() {
            if(!CanStep)return;
            overlay.Draw(1);
            if(current!=null&&current.Operation==TeachingOperation.Finish){Stop();return;}
            waiting=true;stepping=true;elapsed=0;StateChanged?.Invoke();
        }
        public void SetSpeed(float value){Speed=Mathf.Clamp(value,.25f,4);StateChanged?.Invoke();}
        private void Pump(){
            // 一次 Update 只取一个可见事件，至多预留 4096 次比较。Pending 不改变当前画面。
            var status=trace.Advance(4096,out var next);
            if(status==TeachingAdvance.Complete){Stop();return;}
            if(status==TeachingAdvance.Pending)return;
            current=next;StepIndex++;waiting=false;elapsed=0;overlay.Begin(current);
            if(trace.Algorithm==TeachingAlgorithmKind.Chan&&(current.Operation==TeachingOperation.ChanGroups||current.Operation==TeachingOperation.ChanPartition||current.Operation==TeachingOperation.Restart))demo.ApplyTeachingGroups(trace);
            panel.Refresh();StateChanged?.Invoke();
        }
        public string Phase=>current?.Phase??"";
        public string Explanation=>current==null?"":current.Operation.ToString();
        public void Stop() {Stop(true);}
        private void Stop(bool notifyUI) {
            if(!IsActive)return;trace.Dispose();trace=null;current=null;waiting=false;stepping=false;IsPaused=false;
            if(overlay!=null)overlay.Clear();if(panel!=null)panel.ClearLabels();
            if(demo!=null)demo.SetPlaybackPresentation(false);
            if(notifyUI) {
                if(frame!=null&&panel!=null)frame.HidePanel(PanelId);
                if(demo!=null)demo.CancelGesture();StateChanged?.Invoke();
            } else if(panel!=null)panel.gameObject.SetActive(false);
        }
        private void OnDisable() {
            // 退出场景时 UI 可能已经先销毁。只清理仍存在的对象，不调用框架或已销毁面板的回调。
            Stop(false);
        }
    }
}
