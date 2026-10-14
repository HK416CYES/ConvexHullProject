using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    public enum TeachingAlgorithmKind { Andrew, JarvisMarch, QuickHull, MergeHull, Chan }
    public enum TeachingGranularity { KeySteps, DetailedSteps }
    public enum TeachingOperation { Work, Stage, Candidate, Compare, Push, Pop, Replace, ClearGroup, Restart, Divider, Finish, Trial, Reject, ActiveEdge, ConfirmPoint, Triangle, FarthestCompare, TangentStart, TangentCompare, Merge, ChanGroups, ChanCandidates, ChanQuery, ChanPartition, ChanBuildStart, ChanGroupComplete, ChanQueryStart, ChanTangentCheck }
    /// <summary>Id 是控制器的永久标识，不是显示编号；数组下标只用于教学快照，绝不写回原始点。</summary>
    public readonly struct TeachingPoint {
        public readonly int Id; public readonly Vector2 Position;
        public TeachingPoint(int id, Vector2 position) { Id=id; Position=position; }
    }
    public sealed class TeachingEvent {
        public TeachingOperation Operation;
        public string Phase;
        public int Group, A=-1, B=-1, Point=-1, Cost, LeftGroup=-1, RightGroup=-1;
        public double Value,DistanceValue,BestDistance;
        public int Best=-1,PreviousBest=-1;
        public bool Improved;
        public int[] Path;
        public int[][] Paths;
        public bool Closed;
        public TeachingEvent(TeachingOperation operation, string phase, int group=0) { Operation=operation;Phase=phase;Group=group; }
    }
    public enum TeachingAdvance { Pending, Visible, Complete }
    public static class TeachingAlgorithmRegistry {
        public static readonly string[] Names={"Andrew 单调链","Jarvis March","QuickHull","MergeHull","Chan"};
        public static HullTeachingSession Create(TeachingAlgorithmKind kind, IEnumerable<TeachingPoint> points, TeachingGranularity granularity) => new HullTeachingSession(kind,points,granularity);
    }
    /// <summary>
    /// 教学计算使用迭代器：yield return 像“书签”，下次从原位置继续。
    /// 只保留当前链、子问题及一个事件，不录制整场动画。Work 在计算前预留比较预算，
    /// 因而即使某次扫描没有改变候选，也能在预算耗尽后让出主线程。
    /// </summary>
    public sealed class HullTeachingSession : IDisposable {
        public readonly TeachingAlgorithmKind Algorithm;
        public readonly TeachingGranularity Granularity;
        public IReadOnlyList<TeachingPoint> Points => points;
        public IReadOnlyList<Vector2> Unique => coordinates;
        public IReadOnlyList<int> Result => result;
        /// <summary>按快照下标查询本次演示编号，和永久 ID 分离；每次播放从 1 连续编号。</summary>
        public IReadOnlyList<int> DisplayNumbers => displayNumbers;
        public bool NumberingReady { get; private set; }
        private readonly int[] displayNumbers;
        public long Comparisons { get; private set; }
        public int LastAdvanceComparisons { get; private set; }
        public int RetryCount { get; private set; }
        public int TangentQueries { get; private set; }
        public int MaximumTangentComparisons { get; private set; }
        public int VisibleEvents { get; private set; }
        // 分组按本轮快照下标查询，重复坐标共享代表点的组；重试会重新分组。
        public IReadOnlyList<int> PointGroups => pointGroups;
        public int GroupingRevision { get; private set; }
        public int ChanRound { get; private set; }
        public int ChanGroupSize { get; private set; }
        private readonly int[] pointGroups;
        private readonly List<TeachingPoint> points=new List<TeachingPoint>();
        private readonly List<Vector2> coordinates=new List<Vector2>();
        private readonly List<int> result=new List<int>();
        private IEnumerator<TeachingEvent> stream;
        private TeachingEvent deferred;
        private int nextGroup=10;
        public HullTeachingSession(TeachingAlgorithmKind kind,IEnumerable<TeachingPoint> snapshot,TeachingGranularity granularity) {
            Algorithm=kind;Granularity=granularity;
            // 控制器按创建顺序提供快照。字典只去重，不进行全局排序（Chan 需要保留分组顺序）。
            var seen=new HashSet<Vector2>();
            foreach(var point in snapshot)if(seen.Add(point.Position)){points.Add(point);coordinates.Add(point.Position);}
            displayNumbers=new int[points.Count];pointGroups=new int[points.Count];Array.Fill(pointGroups,-1);
            // 删除的点没有进入快照，重复坐标也只占一个号码。
            // 扫描/分组算法使用快照遍历顺序；Andrew 等自己的排序完成后再显示编号。
            for(int i=0;i<points.Count;i++)displayNumbers[i]=i+1;
            NumberingReady=kind!=TeachingAlgorithmKind.Andrew;
            stream=Run().GetEnumerator();
        }
        public TeachingAdvance Advance(int comparisonBudget,out TeachingEvent step) {
            step=null;LastAdvanceComparisons=0;if(stream==null)return TeachingAdvance.Complete;
            int budget=Math.Max(1,comparisonBudget);
            while(true) {
                if(deferred==null) {if(!stream.MoveNext()){Dispose();return TeachingAdvance.Complete;}deferred=stream.Current;}
                if(deferred.Operation==TeachingOperation.Work) {
                    if(LastAdvanceComparisons+deferred.Cost>budget)return TeachingAdvance.Pending;
                    LastAdvanceComparisons+=deferred.Cost;Comparisons+=deferred.Cost;deferred=null;
                    // 不能提前恢复迭代器：下一次 MoveNext 才真正执行预留的几何比较。
                    continue;
                }
                step=deferred;deferred=null;VisibleEvents++;return TeachingAdvance.Visible;
            }
        }
        public void Dispose(){stream?.Dispose();stream=null;deferred=null;}
        private TeachingEvent Work(int cost=1)=>new TeachingEvent(TeachingOperation.Work,""){Cost=cost};
        private TeachingEvent Compare(string phase,int group,int a,int b,int p,double cross)=>new TeachingEvent(TeachingOperation.Compare,phase,group){A=a,B=b,Point=p,Value=cross};
        private TeachingEvent Candidate(string phase,int group,int a,int p)=>new TeachingEvent(TeachingOperation.Candidate,phase,group){A=a,Point=p};
        private TeachingEvent Path(string phase,int group,List<int> path,bool closed=true)=>new TeachingEvent(TeachingOperation.Replace,phase,group){Path=path.ToArray(),Closed=closed};
        private double Cross(int a,int b,int c) {
            var p=coordinates[a];var q=coordinates[b];var r=coordinates[c];
            return ((double)q.x-p.x)*((double)r.y-p.y)-((double)q.y-p.y)*((double)r.x-p.x);
        }
        private double Distance(int a,int b){double x=(double)coordinates[a].x-coordinates[b].x,y=(double)coordinates[a].y-coordinates[b].y;return x*x+y*y;}
        private int Lex(int a,int b){int c=coordinates[a].x.CompareTo(coordinates[b].x);if(c==0)c=coordinates[a].y.CompareTo(coordinates[b].y);return c!=0?c:points[a].Id.CompareTo(points[b].Id);}
        private IEnumerable<TeachingEvent> Run() {
            var all=new List<int>();for(int i=0;i<points.Count;i++)all.Add(i);
            if(Algorithm==TeachingAlgorithmKind.Andrew) {
                // 复用算法原本的可暂停排序，不额外全局排序，也不改变快照下标或点 ID。
                // 先完成编号准备，避免切换算法后短暂显示上次编号；退化点集同样按坐标编号。
                foreach(var e in Sort(all))yield return e;
                for(int rank=0;rank<all.Count;rank++)displayNumbers[all[rank]]=rank+1;
                NumberingReady=true;
            }
            yield return new TeachingEvent(TeachingOperation.Stage,"快照与去重");
            if(points.Count==0){yield return new TeachingEvent(TeachingOperation.Finish,"空集"){Path=Array.Empty<int>()};yield break;}
            int left=0,right=0;bool collinear=true;
            for(int i=1;i<points.Count;i++){yield return Work(2);if(Lex(i,left)<0)left=i;if(Lex(i,right)>0)right=i;}
            for(int i=0;i<points.Count;i++){
                yield return Work();double c=Cross(left,right,i);
                if(Granularity==TeachingGranularity.DetailedSteps)yield return Compare("退化检查",0,left,right,i,c);
                if(c!=0){collinear=false;break;}
            }
            // Jarvis 即使全部共线也逐点试探；只有单点无需寻找下一顶点。
            if(collinear&&(Algorithm!=TeachingAlgorithmKind.JarvisMarch||points.Count<2)){
                result.Add(left);if(right!=left)result.Add(right);
                if(Algorithm==TeachingAlgorithmKind.Chan) {
                    ChanRound=1;ChanGroupSize=points.Count;Array.Fill(pointGroups,0);GroupingRevision++;
                    yield return new TeachingEvent(TeachingOperation.ChanGroups,"单点或共线：一个退化子凸包"){Paths=new[]{result.ToArray()}};
                }
                if(Algorithm==TeachingAlgorithmKind.QuickHull)foreach(int p in result)yield return new TeachingEvent(TeachingOperation.ConfirmPoint,"确定端点"){Point=p};
                yield return Path("单点或共线：只保留端点",0,result,false);
            }
            else {
                IEnumerable<TeachingEvent> algorithm;
                switch(Algorithm){
                    case TeachingAlgorithmKind.JarvisMarch:algorithm=Jarvis(all,left,result);break;
                    case TeachingAlgorithmKind.QuickHull:algorithm=Quick(all,left,right,result);break;
                    case TeachingAlgorithmKind.MergeHull:algorithm=MergeRoot(all,result);break;
                    case TeachingAlgorithmKind.Chan:algorithm=Chan(all,left,result);break;
                    default:algorithm=Andrew(all,0,result,true);break;
                }
                foreach(var e in algorithm)yield return e;
            }
            // 各算法已经得到 CCW 顺序；这里只旋转起点，不用实时算法“修正”答案。
            int start=0;for(int i=1;i<result.Count;i++){yield return Work();if(Lex(result[i],result[start])<0)start=i;}
            if(start>0){var copy=result.ToArray();for(int i=0;i<copy.Length;i++)result[i]=copy[(start+i)%copy.Length];}
            yield return new TeachingEvent(TeachingOperation.Finish,"完成"){Path=result.ToArray(),Closed=result.Count>=3};
        }
        /// <summary>可暂停的归并排序；只排序传入子集的下标，不改变点的位置及编号。</summary>
        private IEnumerable<TeachingEvent> Sort(List<int> ids) {
            int n=ids.Count;var scratch=new int[n];
            for(int width=1;width<n;width*=2)for(int lo=0;lo<n;lo+=width*2) {
                int mid=Math.Min(lo+width,n),end=Math.Min(lo+width*2,n),i=lo,j=mid;
                for(int k=lo;k<end;k++) {
                    if(i>=mid)scratch[k]=ids[j++];else if(j>=end)scratch[k]=ids[i++];
                    else {yield return Work();scratch[k]=Lex(ids[i],ids[j])<=0?ids[i++]:ids[j++];}
                }
                for(int k=lo;k<end;k++)ids[k]=scratch[k];
            }
        }
        private IEnumerable<TeachingEvent> Andrew(List<int> ids,int group,List<int> output,bool alreadySorted=false) {
            yield return new TeachingEvent(TeachingOperation.Stage,"按坐标排序",group);
            if(!alreadySorted)foreach(var e in Sort(ids))yield return e;
            var low=new List<int>();var high=new List<int>();
            for(int pass=0;pass<2;pass++) {
                var chain=pass==0?low:high;int g=pass==0?group:nextGroup++;string phase=pass==0?"下链":"上链";
                for(int k=0;k<ids.Count;k++){
                    int p=ids[pass==0?k:ids.Count-1-k];yield return Candidate(phase,g,chain.Count>0?chain[chain.Count-1]:-1,p);
                    while(chain.Count>=2){int a=chain[chain.Count-2],b=chain[chain.Count-1];yield return Work();double c=Cross(a,b,p);
                        if(Granularity==TeachingGranularity.DetailedSteps)yield return Compare(phase,g,a,b,p,c);
                        if(c>0)break;
                        chain.RemoveAt(chain.Count-1);yield return new TeachingEvent(TeachingOperation.Pop,phase,g){Point=b,A=a,B=p,Value=c};
                    }
                    chain.Add(p);yield return new TeachingEvent(TeachingOperation.Push,phase,g){Point=p};
                }
                if(pass==1)yield return new TeachingEvent(TeachingOperation.ClearGroup,"合并上下链",g);
            }
            output.Clear();for(int i=0;i<low.Count-1;i++)output.Add(low[i]);for(int i=0;i<high.Count-1;i++)output.Add(high[i]);
            if(ids.Count==1)output.Add(ids[0]);yield return Path("子凸包",group,output);
        }
        // Jarvis 像把绳子沿外侧逐点绕一圈。下一顶点要让其他点全在有向边左侧，
        // 所以发现“更靠右”的点（负叉积）时更换候选；共线时取远点，跳过边中间点。
        private IEnumerable<TeachingEvent> Jarvis(List<int> ids,int start,List<int> output) {
            int current=start;output.Add(start);yield return new TeachingEvent(TeachingOperation.Push,"包装起点"){Point=start};
            do {
                int best=-1;
                foreach(int p in ids) {
                    if(p==current)continue; // 起点不能成为指向自身的下一条边。
                    // “关键步骤”也逐点展示尝试，不能只显示成功更新的候选。
                    // best 是目前最佳候选，p 是本次试探点；试探不会加入已确认边界。
                    yield return new TeachingEvent(TeachingOperation.Trial,"尝试扫描点"){A=current,B=best,Point=p};
                    if(best<0){best=p;yield return Candidate("初始候选",0,current,best);continue;}
                    yield return Work(3);double cross=Cross(current,best,p);
                    if(Granularity==TeachingGranularity.DetailedSteps)yield return Compare("扫描下一顶点",0,current,best,p,cross);
                    if(cross<0||(cross==0&&Distance(current,p)>Distance(current,best))) {
                        best=p;yield return Candidate("更新候选",0,current,best);
                    } else {
                        // 试探失败只收回临时边，回到 best；不弹出已经确认的凸包顶点。
                        yield return new TeachingEvent(TeachingOperation.Reject,"试探失败，退回最佳候选"){A=current,B=best,Point=p,Value=cross};
                    }
                }
                current=best;if(current!=start)output.Add(current);
                yield return new TeachingEvent(TeachingOperation.Push,"确认边界"){Point=current};
                if(output.Count>ids.Count)throw new InvalidOperationException("Jarvis 未闭合");
            }while(current!=start);
        }
        private IEnumerable<TeachingEvent> Quick(List<int> ids,int left,int right,List<int> output) {
            var below=new List<int>();var above=new List<int>();
            yield return new TeachingEvent(TeachingOperation.ActiveEdge,"左右极点基线"){A=left,B=right};
            yield return new TeachingEvent(TeachingOperation.ConfirmPoint,"确定左极点"){Point=left};
            yield return new TeachingEvent(TeachingOperation.ConfirmPoint,"确定右极点"){Point=right};
            foreach(int p in ids){yield return Work();double c=Cross(left,right,p);if(Granularity==TeachingGranularity.DetailedSteps)yield return Compare("基线两侧划分",0,left,right,p,c);if(c<0)below.Add(p);if(c>0)above.Add(p);}
            // 从左到右先走下边界，再沿上边界回来，保证逆时针。
            output.Add(left);
            foreach(var e in QuickPart(left,right,below,0,output))yield return e;
            foreach(var e in QuickPart(right,left,above,1,output))yield return e;
            output.RemoveAt(output.Count-1);
        }
        // QuickHull 的子问题只处理有向边右侧的点。最远点把它分成两条边，
        // 位于新三角形内部的点不再递归，但原始点仍然可见，不会被删除。
        private IEnumerable<TeachingEvent> QuickPart(int a,int b,List<int> ids,int group,List<int> output) {
            yield return new TeachingEvent(TeachingOperation.ActiveEdge,"活动递归边",group){A=a,B=b,Path=ids.ToArray()};
            if(ids.Count==0){output.Add(b);yield return Path("确认外边界",group,new List<int>{a,b},false);yield break;}
            int far=ids[0];double largest=-1;
            yield return Work();double baseLength=Math.Sqrt(Distance(a,b));
            foreach(int p in ids) {
                yield return Work(2);double c=Math.Abs(Cross(a,b,p));
                int previous=largest<0?-1:far;
                bool improved=c>largest||(c==largest&&Lex(p,far)<0);
                string phase=previous<0?"初始最远候选":improved?(c==largest?"同距，按坐标更新候选":"更远，更新候选"):"较近或同距，保留最远候选";
                if(improved){largest=c;far=p;}
                // 同一条基线长度固定，因此 |叉积| 的大小就是垂线距离的大小顺序。
                // 每个点都产生可见比较，找到当前最远点后也必须继续扫完剩余点。
                yield return new TeachingEvent(TeachingOperation.FarthestCompare,phase,group){A=a,B=b,Point=p,PreviousBest=previous,Best=far,Improved=improved,Value=c,DistanceValue=c/baseLength,BestDistance=largest/baseLength};
            }
            // 最远点已经确定属于凸包。完整三角形包含原基线及两条新边，
            // 三角形内部的点无需再处理；它们仍保留为场景中的普通点。
            yield return new TeachingEvent(TeachingOperation.ConfirmPoint,"确定最远凸包顶点",group){Point=far};
            yield return new TeachingEvent(TeachingOperation.Triangle,"构造三角形并划分外侧点",group){A=a,B=b,Point=far,Path=new[]{a,far,b},Closed=true};
            var first=new List<int>();var second=new List<int>();
            foreach(int p in ids){if(p==far)continue;yield return Work(2);double c1=Cross(a,far,p),c2=Cross(far,b,p);
                if(Granularity==TeachingGranularity.DetailedSteps){yield return Compare("划分左子问题",group,a,far,p,c1);yield return Compare("划分右子问题",group,far,b,p,c2);}
                if(c1<0)first.Add(p);else if(c2<0)second.Add(p);
            }
            // 保留父三角形作为上下文；进入子问题时只淡化它，不能立即删除。
            foreach(var e in QuickPart(a,far,first,nextGroup++,output))yield return e;
            foreach(var e in QuickPart(far,b,second,nextGroup++,output))yield return e;
        }
        private IEnumerable<TeachingEvent> MergeRoot(List<int> ids,List<int> output){foreach(var e in Sort(ids))yield return e;foreach(var e in MergePart(ids,0,output))yield return e;}
        private IEnumerable<TeachingEvent> MergePart(List<int> ids,int group,List<int> output) {
            if(ids.Count<=3) {
                // 叶子已按坐标排序：最多三个点，可以直接给出点、线段或三角形。
                // 不再播放另一种算法的压栈/弹栈过程，让教学聚焦于分治和公共切线。
                output.Add(ids[0]);
                if(ids.Count==2)output.Add(ids[1]);
                if(ids.Count==3) {
                    yield return Work();double cross=Cross(ids[0],ids[1],ids[2]);
                    if(cross==0)output.Add(ids[2]);
                    else {output.Add(ids[cross>0?1:2]);output.Add(ids[cross>0?2:1]);}
                }
                yield return Path("直接显示最小组凸包",group,output,output.Count>=3);yield break;
            }
            int mid=ids.Count/2,lg=nextGroup++,rg=nextGroup++;
            yield return new TeachingEvent(TeachingOperation.Divider,"平衡分割",group){A=ids[mid-1],B=ids[mid],Path=ids.ToArray()};
            var l=new List<int>();var r=new List<int>();
            foreach(var e in MergePart(ids.GetRange(0,mid),lg,l))yield return e;
            foreach(var e in MergePart(ids.GetRange(mid,ids.Count-mid),rg,r))yield return e;
            int il=0,ir=0;
            for(int i=1;i<l.Count;i++){yield return Work();if(Lex(l[i],l[il])>0)il=i;}
            for(int i=1;i<r.Count;i++){yield return Work();if(Lex(r[i],r[ir])<0)ir=i;}
            int ul=il,ur=ir,ll=il,lr=ir;
            // 左右子凸包已经是逆时针顺序。不断沿外侧移动切点，直到两侧
            // 都不能再“抬高/压低”切线。距离平局规则用于点、线段及切线沿边的情况。
            for(int side=0;side<2;side++) {
                int i=il,j=ir,moves=0;bool changed;
                string phase=side==0?"上公共切线":"下公共切线";
                // 每条切线都从左凸包最右点、右凸包最左点开始；即使不移动也展示比较。
                yield return new TeachingEvent(TeachingOperation.TangentStart,phase,group){A=l[i],B=r[j]};
                do {
                    changed=false;
                    for(int which=0;which<2;which++)while(true) {
                        int ni=(i+(side==0?1:l.Count-1))%l.Count,nj=(j+(side==0?r.Count-1:1))%r.Count;
                        int a=which==0?r[j]:l[i],b=which==0?l[i]:r[j],p=which==0?l[ni]:r[nj];
                        if(p==b)break;
                        yield return Work(3);double c=Cross(a,b,p);
                        bool move=(side==0?(which==0?c<0:c>0):(which==0?c>0:c<0))||(c==0&&Distance(a,p)>Distance(a,b));
                        // 必须显示失败的比较，否则观众无法看到“为何停止移动切点”。
                        yield return new TeachingEvent(TeachingOperation.TangentCompare,phase,group){A=l[i],B=r[j],Point=p,PreviousBest=b,Best=a,Value=c,Improved=move};
                        if(!move)break;
                        if(which==0)i=ni;else j=nj;changed=true;
                        yield return new TeachingEvent(TeachingOperation.TangentStart,"调整"+phase,group){A=l[i],B=r[j]};
                        if(++moves>(l.Count+r.Count)*4+8)throw new InvalidOperationException("公共切线未收敛");
                    }
                }while(changed);
                if(side==0){ul=i;ur=j;}else{ll=i;lr=j;}
                yield return Path(side==0?"确认上切线":"确认下切线",group+(side==0?100000:200000),new List<int>{l[i],r[j]},false);
            }
            // 公共切线连接外侧弧，内侧弧不属于合并后的凸包。
            for(int i=ul;;i=(i+1)%l.Count){output.Add(l[i]);if(i==ll)break;}
            for(int i=lr;;i=(i+1)%r.Count){output.Add(r[i]);if(i==ur)break;}
            // 原子合并：同一步裁掉内部弧、保留外侧弧和切线，并统一颜色，不逐组缩回。
            yield return new TeachingEvent(TeachingOperation.Merge,"合并子凸包",group){LeftGroup=lg,RightGroup=rg,Path=output.ToArray(),Closed=output.Count>=3};
        }
        private sealed class LocalHull {
            public int Group;
            public readonly List<int> Vertices=new List<int>();
            public readonly Dictionary<int,int> Indices=new Dictionary<int,int>();
        }
        // Chan 不知道最终有多少个顶点。先猜 m，最多绕 m 步；猜小了就将 m 平方。
        // 只在每个小组内部排序。每次从每组拿到一个切点，再比较这些切点。
        private IEnumerable<TeachingEvent> Chan(List<int> ids,int start,List<int> output) {
            int m=Math.Min(4,ids.Count),round=0;
            while(true) {
                ChanRound=++round;ChanGroupSize=m;Array.Fill(pointGroups,-1);GroupingRevision++;
                yield return new TeachingEvent(TeachingOperation.Restart,"Chan 第 "+round+" 轮，分组规模 "+m);
                // 先给所有点分组，再逐组构造；未开始的组只有点，不能提前显示它的凸包。
                for(int k=0;k<ids.Count;k++)pointGroups[ids[k]]=k/m;
                GroupingRevision++;
                yield return new TeachingEvent(TeachingOperation.ChanPartition,"第 "+round+" 轮分组：m="+m);
                var groups=new List<LocalHull>();
                for(int k=0;k<ids.Count;k+=m) {
                    int gi=groups.Count;var h=new LocalHull{Group=10000+gi};var subset=ids.GetRange(k,Math.Min(m,ids.Count-k));
                    yield return new TeachingEvent(TeachingOperation.ChanBuildStart,"构造第 "+(gi+1)+" 组凸包",h.Group){Path=subset.ToArray()};
                    // 逐组完整播放独立 Andrew；已完成的组保留，其他组不提前出现。
                    foreach(var e in Andrew(subset,h.Group,h.Vertices))yield return e;
                    yield return new TeachingEvent(TeachingOperation.ChanGroupComplete,"第 "+(gi+1)+" 组凸包完成",h.Group){Path=h.Vertices.ToArray()};
                    for(int i=0;i<h.Vertices.Count;i++)h.Indices[h.Vertices[i]]=i;groups.Add(h);
                }
                yield return new TeachingEvent(TeachingOperation.Stage,"本轮所有子凸包构造完成，开始全局包装");
                output.Clear();output.Add(start);int current=start;bool closed=false;
                yield return new TeachingEvent(TeachingOperation.Push,"全局包装起点",0){Point=start};
                for(int wrap=0;wrap<m;wrap++) {
                    int best=-1;var candidates=new int[groups.Count];Array.Fill(candidates,-1);
                    for(int gi=0;gi<groups.Count;gi++) {
                        var answer=new List<int>(1);
                        foreach(var e in Tangent(current,groups[gi],groups[gi].Group,answer,best))yield return e;
                        int p=answer[0];candidates[gi]=p;if(p==current)continue;
                        bool improved=best<0;int previous=best;double c=0;
                        if(best>=0){yield return Work(3);c=Cross(current,best,p);improved=c<0||(c==0&&Distance(current,p)>Distance(current,best));}
                        if(improved)best=p;
                        // 两种粒度都显示逐组比较，包括未能替换当前最佳候选的情况。
                        yield return new TeachingEvent(TeachingOperation.ChanQuery,"比较第 "+(gi+1)+" 组切线",groups[gi].Group){A=current,Point=p,Best=best,PreviousBest=previous,Value=c,Improved=improved};
                    }
                    if(best<0)throw new InvalidOperationException("Chan 无下一顶点");
                    yield return new TeachingEvent(TeachingOperation.ChanCandidates,"各组切线候选 → 下一全局顶点",0){A=current,Point=best,Path=candidates};
                    yield return new TeachingEvent(TeachingOperation.Push,"确认全局边界",0){Point=best};
                    if(best==start){closed=true;break;}output.Add(best);current=best;
                }
                if(closed)yield break;
                RetryCount++;yield return new TeachingEvent(TeachingOperation.Stage,"未能在 "+m+" 步闭合，扩大分组重试");
                if(m==ids.Count)throw new InvalidOperationException("Chan 在 n 步内未闭合");
                m=(int)Math.Min((long)m*m,ids.Count);
            }
        }
        // 从全局凸包顶点向子凸包求右切线：所有子凸包点位于射线左侧。
        // 凸多边形的极角沿两条弧单调变化，每轮根据端点/中点的方向舍弃半条弧。
        // 若查询点本身就是子凸包顶点，字典 O(1) 找到它，直接取 CCW 下一顶点。
        private IEnumerable<TeachingEvent> Tangent(int origin,LocalHull hull,int group,List<int> answer,int globalBest=-1) {
            TangentQueries++;var h=hull.Vertices;int n=h.Count,queries=0;
            yield return new TeachingEvent(TeachingOperation.ChanQueryStart,"查找本组切线候选",group){A=origin,Best=globalBest};
            if(hull.Indices.TryGetValue(origin,out int own)){answer.Add(h[(own+1)%n]);yield break;}
            if(n==1){answer.Add(h[0]);yield break;}
            if(n==2){yield return Work(3);queries=3;answer.Add(Order(origin,h[0],h[1])<0?h[1]:h[0]);MaximumTangentComparisons=Math.Max(MaximumTangentComparisons,queries);yield break;}
            int low=0,high=n,chosen=-1;
            while(high-low>1) {
                int mid=low==0&&high==n?0:(low+high)/2;
                yield return Work(7);queries+=6;
                int following=Order(origin,h[mid],h[(mid+1)%n]),preceding=Order(origin,h[mid],h[(mid+n-1)%n]);
                yield return new TeachingEvent(TeachingOperation.ChanTangentCheck,"二分切线：检查相邻顶点",group){A=origin,B=h[mid],Point=h[(mid+1)%n],PreviousBest=h[(mid+n-1)%n],Best=globalBest,Value=Cross(origin,h[mid],h[(mid+1)%n]),Improved=following>=0&&preceding>=0};
                if(following>=0&&preceding>=0){chosen=mid;break;}
                if(mid==0){mid=(low+high)/2;yield return Work(3);queries+=3;following=Order(origin,h[mid],h[(mid+1)%n]);}
                yield return Work(6);queries+=6;
                bool fallingAtLow=Order(origin,h[low],h[(low+1)%n])<0;
                int across=Order(origin,h[low],h[mid]);bool risingAtMid=following>0;
                if(fallingAtLow?(risingAtMid||across>0):(!risingAtMid||across>=0)) {if(fallingAtLow)high=mid;else low=mid;}
                else {if(fallingAtLow)low=mid;else high=mid;}
                yield return Candidate("二分收缩切线区间",group,origin,h[mid]);
            }
            if(chosen<0){yield return Work(3);queries+=3;chosen=Order(origin,h[low%n],h[high%n])<0?high%n:low%n;}
            answer.Add(h[chosen]);MaximumTangentComparisons=Math.Max(MaximumTangentComparisons,queries);
            yield return Candidate("子凸包切线",group,origin,h[chosen]);
        }
        private int Order(int p,int a,int b) {
            double c=Cross(p,a,b);if(c!=0)return c<0?-1:1;
            // 相同方向的两个端点，远点排在前面，消除切线恰好沿边时的歧义。
            return Distance(p,a).CompareTo(Distance(p,b));
        }
    }
}
