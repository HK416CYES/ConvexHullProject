using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    /// <summary>独立教学渲染：分组线条复用编辑器制作的 Prefab；不访问实时凸包对象。</summary>
    public sealed class HullTeachingOverlay : MonoBehaviour {
        public LineRenderer lower,upper,comparison,transition,linePrefab;
        public MeshRenderer candidateRing,removedRing,confirmedMarkerPrefab,triangleFillPrefab;
        public Color quickBestColor=new Color(.13f,.65f,.38f);
        public Color quickTriangleColor=new Color(.96f,.55f,.08f,.95f),quickContextColor=new Color(.28f,.43f,.64f,.32f);
        [Range(0,1)] public float triangleFillOpacity=.10f,contextFillOpacity=.025f;
        [Min(.01f)] public float confirmedPointDiameter=.48f;
        public Color lowerColor=new Color(.05f,.40f,.95f),upperColor=new Color(.1f,.65f,.38f);
        public Color candidateColor=new Color(1,.58f,.08f),removedColor=new Color(.95f,.16f,.16f);
        [Min(.001f)] public float lineWidth=.075f;
        [Min(.01f)] public float ringDiameter=.65f;
        private sealed class Group { public LineRenderer Line;public readonly List<int> Chain=new List<int>();public bool Closed,IsTriangle;public float WidthScale=1;public Color Color;public MeshRenderer Fill; }
        private readonly Dictionary<int,Group> groups=new Dictionary<int,Group>();
        private readonly Stack<LineRenderer> pool=new Stack<LineRenderer>();
        private readonly Stack<MeshRenderer> fillPool=new Stack<MeshRenderer>(),markerPool=new Stack<MeshRenderer>();
        private readonly Dictionary<int,MeshRenderer> confirmed=new Dictionary<int,MeshRenderer>();
        private readonly HashSet<Mesh> generatedMeshes=new HashSet<Mesh>();
        public int ConfirmedPointCount=>confirmed.Count;
        public int TriangleCount {get{int count=0;foreach(var g in groups.Values)if(g.IsTriangle)count++;return count;}}
        private HullTeachingSession session;
        private readonly List<int> chanRays=new List<int>();
        public static Color ChanGroupColor(int group)=>Color.HSVToRGB(Mathf.Repeat(.06f+group*.381966f,1),.72f,.75f);
        private void ClearChanRays(){foreach(int id in chanRays)RemoveGroup(id);chanRays.Clear();}
        private TeachingEvent step;
        private Vector2 edgeStart,edgeEnd;
        private bool animatedEdge,removing,extendsChain;
        private Color edgeColor;
        private int animatedGroup;
        private int? clearingGroup;private Vector3[] disappearing;
        private MaterialPropertyBlock block;
        private MeshRenderer scanBestRing;
        public MeshRenderer ScanBestRing=>scanBestRing;
        public int ActiveGroupCount=>groups.Count;
        public void ResetSession(HullTeachingSession value){Clear();session=value;}
        public void Clear() {
            foreach(var g in groups.Values){if(g.Line!=null){g.Line.positionCount=0;g.Line.gameObject.SetActive(false);pool.Push(g.Line);}
                if(g.Fill!=null){g.Fill.gameObject.SetActive(false);fillPool.Push(g.Fill);}}
            foreach(var ring in confirmed.Values)if(ring!=null){ring.gameObject.SetActive(false);markerPool.Push(ring);}confirmed.Clear();
            groups.Clear();chanRays.Clear();
            foreach(var line in new[]{lower,upper,comparison,transition})if(line!=null){line.positionCount=0;line.loop=false;}
            if(scanBestRing!=null)scanBestRing.gameObject.SetActive(false);
            if(candidateRing!=null)candidateRing.gameObject.SetActive(false);if(removedRing!=null)removedRing.gameObject.SetActive(false);
            animatedEdge=false;clearingGroup=null;disappearing=null;step=null;session=null;
        }
        private void RemoveGroup(int id){if(!groups.TryGetValue(id,out var g))return;g.Line.positionCount=0;g.Line.gameObject.SetActive(false);pool.Push(g.Line);if(g.Fill!=null){g.Fill.gameObject.SetActive(false);fillPool.Push(g.Fill);}groups.Remove(id);}
        private Group GetGroup(int id){
            if(groups.TryGetValue(id,out var g))return g;
            var line=pool.Count>0?pool.Pop():Instantiate(linePrefab,transform);line.gameObject.SetActive(true);line.loop=false;
            g=new Group{Line=line,Color=session!=null&&session.Algorithm==TeachingAlgorithmKind.Chan&&id>=10000&&id<1000000?ChanGroupColor(id-10000):GroupColor(id)};groups.Add(id,g);return g;
        }
        public void Begin(TeachingEvent value) {
            if(clearingGroup.HasValue){RemoveGroup(clearingGroup.Value);clearingGroup=null;}
            step=value;animatedEdge=false;extendsChain=false;comparison.positionCount=transition.positionCount=0;
            candidateRing.gameObject.SetActive(false);removedRing.gameObject.SetActive(false);upper.positionCount=0;
            if(scanBestRing!=null)scanBestRing.gameObject.SetActive(false);
            if(session.Algorithm==TeachingAlgorithmKind.Chan){ClearChanRays();lower.positionCount=0;}
            if(step.Operation==TeachingOperation.Restart){var current=session;Clear();session=current;step=value;}
            if(step.Operation==TeachingOperation.ClearGroup){
                if(groups.TryGetValue(step.Group,out var old)){clearingGroup=step.Group;disappearing=new Vector3[old.Line.positionCount];old.Line.GetPositions(disappearing);old.Line.loop=false;}
                Draw(0);return;
            }
            if(step.Operation==TeachingOperation.ChanGroups) {
                for(int i=0;i<step.Paths.Length;i++) {
                    var g=GetGroup(10000+i);g.Chain.AddRange(step.Paths[i]);g.Closed=g.Chain.Count>=3;
                    g.Color=ChanGroupColor(i);g.Color.a=.65f;g.WidthScale=.65f;
                }
                Draw(1);return;
            }
            if(step.Operation==TeachingOperation.ChanBuildStart||step.Operation==TeachingOperation.ChanGroupComplete) {
                var g=GetGroup(step.Group);g.Color=ChanGroupColor(step.Group-10000);
                if(step.Operation==TeachingOperation.ChanGroupComplete){g.Color.a=.65f;g.WidthScale=.65f;}
                Draw(1);return;
            }
            if(step.Operation==TeachingOperation.ChanQueryStart||step.Operation==TeachingOperation.ChanTangentCheck) {
                // 本组轮廓加粗，已完成组仍作为上下文；橙色保持当前全局最佳候选。
                foreach(var pair in groups)if(pair.Key>=10000&&pair.Key<1000000)pair.Value.WidthScale=pair.Key==step.Group?1.3f:.65f;
                if(step.Best>=0){lower.startColor=lower.endColor=candidateColor;lower.widthMultiplier=lineWidth;lower.positionCount=2;
                    lower.SetPosition(0,At(session.Unique[step.A]));lower.SetPosition(1,At(session.Unique[step.Best]));}
                if(step.Operation==TeachingOperation.ChanTangentCheck) {
                    comparison.startColor=comparison.endColor=candidateColor;comparison.positionCount=2;
                    comparison.SetPosition(0,At(session.Unique[step.A]));comparison.SetPosition(1,At(session.Unique[step.B]));
                    upper.startColor=upper.endColor=step.Improved?upperColor:removedColor;upper.widthMultiplier=lineWidth;upper.positionCount=3;
                    upper.SetPosition(0,At(session.Unique[step.PreviousBest]));upper.SetPosition(1,At(session.Unique[step.B]));upper.SetPosition(2,At(session.Unique[step.Point]));
                    Mark(candidateRing,session.Unique[step.B],candidateColor);
                }
                Draw(1);return;
            }
            if(step.Operation==TeachingOperation.ChanCandidates||step.Operation==TeachingOperation.ChanQuery) {
                // 所有组都完成切线查询后才确认最佳点。多色细线是各组候选，橙色粗线是胜出者。
                if(step.Path!=null)for(int i=0;i<step.Path.Length;i++) {
                    int p=step.Path[i];if(p<0||p==step.A)continue;int id=1000000+i;chanRays.Add(id);
                    var g=GetGroup(id);g.Chain.Add(step.A);g.Chain.Add(p);g.Color=ChanGroupColor(i);g.Color.a=.65f;g.WidthScale=.55f;
                }
                int winner=step.Operation==TeachingOperation.ChanQuery?step.Best:step.Point;
                edgeStart=session.Unique[step.A];edgeEnd=session.Unique[winner];animatedEdge=true;removing=false;edgeColor=candidateColor;
                Mark(candidateRing,session.Unique[winner],candidateColor);
                if(step.Operation==TeachingOperation.ChanQuery) {
                    comparison.startColor=comparison.endColor=step.Improved?upperColor:removedColor;comparison.positionCount=2;
                    comparison.SetPosition(0,At(session.Unique[step.A]));comparison.SetPosition(1,At(session.Unique[step.Point]));
                }
                Draw(0);return;
            }
            if(step.Operation==TeachingOperation.Merge) {
                // 复用左侧已有 LineRenderer；同帧更新边界和颜色，绝不先清空再逐边生成。
                var merged=GetGroup(step.LeftGroup);groups.Remove(step.LeftGroup);
                RemoveGroup(step.RightGroup);RemoveGroup(step.Group);
                RemoveGroup(step.Group+100000);RemoveGroup(step.Group+200000);
                groups.Add(step.Group,merged);merged.Chain.Clear();merged.Chain.AddRange(step.Path);
                merged.Closed=step.Closed;merged.Color=lowerColor;Draw(1);return;
            }
            if(step.Operation==TeachingOperation.TangentStart||step.Operation==TeachingOperation.TangentCompare) {
                // 橙色为当前切线；邻边表明正在检查哪个邻点，绿/红试探边表明移动/保留切点。
                comparison.startColor=comparison.endColor=candidateColor;comparison.positionCount=2;
                comparison.SetPosition(0,At(session.Unique[step.A]));comparison.SetPosition(1,At(session.Unique[step.B]));
                if(step.Operation==TeachingOperation.TangentCompare) {
                    edgeColor=step.Improved?upperColor:removedColor;
                    Mark(candidateRing,session.Unique[step.Point],edgeColor);
                    upper.startColor=upper.endColor=edgeColor;upper.widthMultiplier=lineWidth*.65f;upper.positionCount=2;
                    upper.SetPosition(0,At(session.Unique[step.PreviousBest]));upper.SetPosition(1,At(session.Unique[step.Point]));
                    edgeStart=session.Unique[step.Best];edgeEnd=session.Unique[step.Point];animatedEdge=true;removing=false;
                }
                Draw(0);return;
            }
            if(step.Operation==TeachingOperation.FarthestCompare) {
                // 当前点与当前最远候选各自连到基线的垂足，直接比较两条垂线长度。
                var a=session.Unique[step.A];var b=session.Unique[step.B];var current=session.Unique[step.Point];var best=session.Unique[step.Best];
                Vector2 currentFoot=Projection(a,b,current,out var currentT),bestFoot=Projection(a,b,best,out var bestT);
                comparison.startColor=comparison.endColor=quickBestColor;comparison.positionCount=2;
                comparison.SetPosition(0,At(bestFoot));comparison.SetPosition(1,At(best));
                Mark(candidateRing,current,candidateColor);
                if(scanBestRing==null){scanBestRing=Instantiate(confirmedMarkerPrefab,transform);scanBestRing.name="QuickHull farthest candidate";}
                Mark(scanBestRing,best,quickBestColor);scanBestRing.transform.localScale=Vector3.one*confirmedPointDiameter*.5f;
                // 垂足可以落在边的延长线上。不能把投影截断到线段端点，否则展示的就不是垂距。
                double min=System.Math.Min(0,System.Math.Min(currentT,bestT)),max=System.Math.Max(1,System.Math.Max(currentT,bestT));
                if(min<0||max>1){upper.startColor=upper.endColor=quickContextColor;upper.widthMultiplier=lineWidth*.5f;upper.positionCount=2;
                    upper.SetPosition(0,At(Along(a,b,min)));upper.SetPosition(1,At(Along(a,b,max)));}
                edgeStart=currentFoot;edgeEnd=current;animatedEdge=true;removing=false;edgeColor=step.Improved?candidateColor:removedColor;
                Draw(0);return;
            }
            if(step.Operation==TeachingOperation.ConfirmPoint)ConfirmPoint(step.Point);
            if(step.Operation==TeachingOperation.ActiveEdge) {
                // 已构造三角形淡化为历史上下文，新子问题的基线保持橙色。
                foreach(var old in groups.Values)if(old.IsTriangle){old.Color=quickContextColor;SetFillColor(old,contextFillOpacity);}
                var g=GetGroup(step.Group);g.Chain.Clear();g.Chain.Add(step.A);g.Chain.Add(step.B);g.Closed=false;g.Color=candidateColor;
            }
            if(step.Operation==TeachingOperation.Triangle) {
                var g=GetGroup(step.Group);g.Chain.Clear();g.Chain.AddRange(step.Path);g.Closed=true;g.IsTriangle=true;g.Color=quickTriangleColor;
                if(g.Fill==null)g.Fill=GetFill();UpdateFill(g);SetFillColor(g,triangleFillOpacity);
            }
            if(step.Operation==TeachingOperation.Trial||step.Operation==TeachingOperation.Reject) {
                // 保留当前最佳候选边，同时伸出试探边；失败时红色试探边缩回。
                if(step.B>=0){comparison.startColor=comparison.endColor=candidateColor;comparison.positionCount=2;
                    comparison.SetPosition(0,At(session.Unique[step.A]));comparison.SetPosition(1,At(session.Unique[step.B]));}
                bool rejected=step.Operation==TeachingOperation.Reject;
                Mark(candidateRing,session.Unique[rejected?step.B:step.Point],candidateColor);
                if(rejected)Mark(removedRing,session.Unique[step.Point],removedColor);
                edgeStart=session.Unique[step.A];edgeEnd=session.Unique[step.Point];animatedEdge=true;removing=rejected;
                edgeColor=rejected?removedColor:candidateColor;Draw(0);return;
            }
            // 弹栈时 Point 是被移除的旧点，B 是仍在考察的新点；两个外圈分别保留。
            int candidate=step.Operation==TeachingOperation.ConfirmPoint?-1:step.Operation==TeachingOperation.Pop?step.B:step.Point;
            if(candidate>=0)Mark(candidateRing,session.Unique[candidate],candidateColor);
            if((step.Operation==TeachingOperation.Candidate||step.Operation==TeachingOperation.Compare)&&step.A>=0&&step.Point>=0){
                comparison.startColor=comparison.endColor=candidateColor;comparison.loop=false;
                comparison.positionCount=step.B>=0?3:2;comparison.SetPosition(0,At(session.Unique[step.A]));
                if(step.B>=0)comparison.SetPosition(1,At(session.Unique[step.B]));comparison.SetPosition(comparison.positionCount-1,At(session.Unique[step.Point]));
            }
            if(step.Operation==TeachingOperation.Push){var g=GetGroup(step.Group);if(step.Phase=="上链")g.Color=upperColor;
                if(g.Chain.Count>0){edgeStart=session.Unique[g.Chain[g.Chain.Count-1]];edgeEnd=session.Unique[step.Point];animatedEdge=true;removing=false;extendsChain=true;animatedGroup=step.Group;edgeColor=g.Color;}
                g.Chain.Add(step.Point);
            }
            if(step.Operation==TeachingOperation.Pop){var g=GetGroup(step.Group);Mark(removedRing,session.Unique[step.Point],removedColor);
                if(g.Chain.Count>=2){edgeStart=session.Unique[g.Chain[g.Chain.Count-2]];edgeEnd=session.Unique[g.Chain[g.Chain.Count-1]];animatedEdge=true;removing=true;animatedGroup=step.Group;edgeColor=removedColor;}
                if(g.Chain.Count>0)g.Chain.RemoveAt(g.Chain.Count-1);
            }
            if(step.Operation==TeachingOperation.Replace){var g=GetGroup(step.Group);g.Chain.Clear();g.Chain.AddRange(step.Path);g.Closed=step.Closed;if(step.Phase=="确认外边界"||step.Phase=="确认下切线")g.Color=lowerColor;if(step.Phase=="确认上切线")g.Color=upperColor;}
            if(step.Operation==TeachingOperation.Divider){
                // 分割线跟随世界坐标；相机移动只改变观察，不改变算法。
                float x=(session.Unique[step.A].x+session.Unique[step.B].x)*.5f,min=float.PositiveInfinity,max=float.NegativeInfinity;
                foreach(int p in step.Path){min=Mathf.Min(min,session.Unique[p].y);max=Mathf.Max(max,session.Unique[p].y);}
                var g=GetGroup(step.Group);g.Line.positionCount=2;g.Line.startColor=g.Line.endColor=GroupColor(step.Group);
                g.Line.SetPosition(0,At(new Vector2(x,min-.5f)));g.Line.SetPosition(1,At(new Vector2(x,max+.5f)));
            }
            if(step.Operation==TeachingOperation.Finish){var current=session;Clear();session=current;step=value;var g=GetGroup(0);g.Chain.AddRange(step.Path);g.Closed=step.Closed;
                if(session.Algorithm==TeachingAlgorithmKind.QuickHull)foreach(int p in step.Path)ConfirmPoint(p);
            }
            Draw(0);
        }
        public void Draw(float progress) {
            if(session==null)return;
            foreach(var pair in groups){var g=pair.Value;g.Line.widthMultiplier=lineWidth*g.WidthScale;
                if(clearingGroup==pair.Key){g.Line.startColor=g.Line.endColor=removedColor;g.Line.positionCount=disappearing.Length;
                    for(int i=0;i<disappearing.Length;i++)g.Line.SetPosition(i,Vector3.Lerp(disappearing[i],disappearing[0],progress));continue;}
                if(g.Chain.Count==0)continue;int count=g.Chain.Count-(animatedEdge&&extendsChain&&pair.Key==animatedGroup&&progress<1?1:0);
                g.Line.startColor=g.Line.endColor=g.Color;g.Line.loop=g.Closed&&count>=3;
                g.Line.positionCount=count>=2?count:0;for(int i=0;i<g.Line.positionCount;i++)g.Line.SetPosition(i,At(session.Unique[g.Chain[i]]));
            }
            comparison.widthMultiplier=transition.widthMultiplier=lineWidth;
            if(clearingGroup.HasValue&&progress>=1){RemoveGroup(clearingGroup.Value);clearingGroup=null;}
            if(animatedEdge&&(progress<1||step.Operation==TeachingOperation.Trial||step.Operation==TeachingOperation.FarthestCompare||step.Operation==TeachingOperation.TangentCompare||step.Operation==TeachingOperation.ChanCandidates||step.Operation==TeachingOperation.ChanQuery)){transition.startColor=transition.endColor=edgeColor;
                transition.positionCount=2;transition.SetPosition(0,At(edgeStart));transition.SetPosition(1,At(Vector2.Lerp(edgeStart,edgeEnd,removing?1-progress:progress)));
            }else transition.positionCount=0;
        }
        private static Vector2 Projection(Vector2 a,Vector2 b,Vector2 p,out double t) {
            double x=(double)b.x-a.x,y=(double)b.y-a.y,length=x*x+y*y;
            t=length>0?(((double)p.x-a.x)*x+((double)p.y-a.y)*y)/length:0;
            return Along(a,b,t);
        }
        private static Vector2 Along(Vector2 a,Vector2 b,double t)=>new Vector2((float)(a.x+((double)b.x-a.x)*t),(float)(a.y+((double)b.y-a.y)*t));
        private void ConfirmPoint(int point) {
            if(confirmed.ContainsKey(point))return;
            var ring=markerPool.Count>0?markerPool.Pop():Instantiate(confirmedMarkerPrefab,transform);
            confirmed.Add(point,ring);Mark(ring,session.Unique[point],lowerColor);ring.transform.localScale=Vector3.one*confirmedPointDiameter*.5f;
        }
        private MeshRenderer GetFill() {
            var fill=fillPool.Count>0?fillPool.Pop():Instantiate(triangleFillPrefab,transform);fill.gameObject.SetActive(true);
            var filter=fill.GetComponent<MeshFilter>();
            if(!generatedMeshes.Contains(filter.sharedMesh)){
                // 只复制模板网格，绝不能修改 Prefab 共享的持久化网格资产。
                var mesh=Instantiate(filter.sharedMesh);mesh.name="QuickHull teaching triangle";mesh.MarkDynamic();filter.sharedMesh=mesh;generatedMeshes.Add(mesh);
            }
            return fill;
        }
        private void UpdateFill(Group group) {
            var vertices=new Vector3[3];for(int i=0;i<3;i++)vertices[i]=group.Fill.transform.InverseTransformPoint(new Vector3(session.Unique[group.Chain[i]].x,session.Unique[group.Chain[i]].y,-.12f));
            var mesh=group.Fill.GetComponent<MeshFilter>().sharedMesh;mesh.vertices=vertices;mesh.RecalculateBounds();
        }
        private void SetFillColor(Group group,float alpha) {
            if(group.Fill==null)return;var color=group.Color;color.a=alpha;
            if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",color);group.Fill.SetPropertyBlock(block);
        }
        private void OnDestroy(){foreach(var mesh in generatedMeshes)if(mesh!=null)Destroy(mesh);}
        private Color GroupColor(int id){if(id==0)return lowerColor;if(id==1)return upperColor;return Color.HSVToRGB(Mathf.Repeat(id*.381966f,.95f),.65f,.8f);}
        private void Mark(MeshRenderer ring,Vector2 p,Color color){ring.gameObject.SetActive(true);ring.transform.position=At(p);ring.transform.localScale=Vector3.one*ringDiameter*.5f;
            if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",color);ring.SetPropertyBlock(block);}
        private static Vector3 At(Vector2 p)=>new Vector3(p.x,p.y,-.15f);
    }
}
