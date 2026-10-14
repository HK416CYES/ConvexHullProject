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
        private HullTeachingAlgorithm.Trace trace;
        private float elapsed;
        private bool stepping;
        public bool IsActive=>trace!=null;
        public bool IsPaused{get;private set;}
        public bool CanStep=>IsActive&&IsPaused&&!stepping;
        public float Speed{get;private set;}=1;
        public int StepIndex{get;private set;}
        public int StepCount=>trace==null?0:trace.Steps.Count;
        public HullTeachingAlgorithm.Trace CurrentTrace=>trace;
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
            var recording=HullTeachingAlgorithm.Record(snapshot);
            demo.CancelGesture();demo.CloseMenu();demo.HideHelp();
            demo.worldCamera.orthographicSize=size;demo.worldCamera.transform.position=position;
            trace=recording;IsPaused=false;stepping=false;Speed=1;StepIndex=0;elapsed=0;
            demo.SetPlaybackPresentation(true);
            overlay.ResetTrace(trace);frame.ShowPanel(PanelId);panel.SetLabels(trace,false);
            BeginStep();StateChanged?.Invoke();return true;
        }
        private bool TryFit(List<Vector2> points,out float size,out Vector3 position) {
            var camera=demo.worldCamera;position=camera.transform.position;size=camera.orthographicSize;
            Vector2 min=points[0],max=points[0];foreach(var p in points){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            Rect view=camera.pixelRect;float scale=panel.GetComponentInParent<Canvas>().scaleFactor;
            float margin=24*scale;
            Rect usable=Rect.MinMaxRect(view.xMin+margin,view.yMin+(panel.card.rect.height+12)*scale+margin,
                view.xMax-margin,Mathf.Min(view.yMax-margin,demo.Toolbar.ToolbarScreenBottom-margin));
            if(usable.width<=0||usable.height<=0)return false;
            float worldMargin=Mathf.Max(.5f,Mathf.Max(demo.pointDiameter*.9f,overlay.ringDiameter*.5f)+.5f);
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
        public void Tick(float seconds) {
            if(!IsActive||(IsPaused&&!stepping))return;
            elapsed+=Mathf.Max(0,seconds)*Speed;
            overlay.Draw(Mathf.Clamp01(elapsed/Mathf.Max(.01f,Mathf.Min(transitionDuration,stepInterval))));
            if(elapsed<Mathf.Max(.05f,stepInterval))return;
            if(stepping){stepping=false;elapsed=stepInterval;StateChanged?.Invoke();}
            else Advance();
        }
        public void TogglePause(){if(!IsActive)return;IsPaused=!IsPaused;stepping=false;StateChanged?.Invoke();}
        public void Pause(){if(!IsActive)return;IsPaused=true;stepping=false;StateChanged?.Invoke();}
        public void NextStep() {
            if(!IsActive||!IsPaused||stepping)return;
            overlay.Draw(1);Advance();if(IsActive){stepping=true;StateChanged?.Invoke();}
        }
        public void SetSpeed(float value) {Speed=Mathf.Clamp(value,.25f,4);StateChanged?.Invoke();}
        private void Advance(){
            if(StepIndex+1>=trace.Steps.Count){Stop();return;}
            StepIndex++;elapsed=0;BeginStep();StateChanged?.Invoke();
        }
        private void BeginStep(){
            var step=trace.Steps[StepIndex];overlay.Begin(step);
            if(step.Kind==HullTeachingAlgorithm.Action.Deduplicate)panel.SetLabels(trace,true);
            panel.Refresh();
        }
        public string Phase {
            get {
                if(!IsActive)return "";var s=trace.Steps[StepIndex];
                if(s.Kind==HullTeachingAlgorithm.Action.Sort)return "排序";
                if(s.Kind==HullTeachingAlgorithm.Action.Deduplicate)return "去重";
                if(s.Kind==HullTeachingAlgorithm.Action.Finish)return "完成";
                return s.Upper?"上链 · 绿色":"下链 · 蓝色";
            }
        }
        public string Explanation {
            get {
                if(!IsActive)return "";var s=trace.Steps[StepIndex];int n=s.Point+1;
                switch(s.Kind) {
                    case HullTeachingAlgorithm.Action.Sort:return "按 x、再按 y 排序；编号表示从左到右的处理顺序";
                    case HullTeachingAlgorithm.Action.Deduplicate:return "合并重复坐标："+trace.Sorted.Count+" 个点 → "+trace.Unique.Count+" 个不同坐标（原始点不变）";
                    case HullTeachingAlgorithm.Action.BeginLower:return "从左向右扫描，构造下边界";
                    case HullTeachingAlgorithm.Action.BeginUpper:return "保留下链，从右向左扫描，构造上边界";
                    case HullTeachingAlgorithm.Action.Candidate:return "考察点 "+n+"，检查它与链末尾两点的转向";
                    case HullTeachingAlgorithm.Action.Check:return "点 "+(s.A+1)+" → "+(s.B+1)+" → "+n+"：叉积 "+s.Cross.ToString("G5",System.Globalization.CultureInfo.InvariantCulture)+"，"+(s.Cross>0?"左转，保留":s.Cross<0?"右转，移除链末尾点":"共线，移除中间点");
                    case HullTeachingAlgorithm.Action.Add:return "将点 "+n+" 加入候选链";
                    case HullTeachingAlgorithm.Action.Remove:return "移除候选链末尾的点 "+n+"；该点仍可能属于另一条链";
                    default:return "连接上下链并去除重复端点：得到 "+trace.Hull.Count+" 个凸包顶点";
                }
            }
        }
        public void Stop() {Stop(true);}
        private void Stop(bool notifyUI) {
            if(!IsActive)return;trace=null;stepping=false;IsPaused=false;
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
