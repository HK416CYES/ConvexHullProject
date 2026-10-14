using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
public static class VerifyJarvisScan {
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static object CheckDataset(Vector2[] input,TeachingGranularity granularity) {
        var snapshot=new List<TeachingPoint>();for(int i=0;i<input.Length;i++)snapshot.Add(new TeachingPoint(i+1,input[i]));
        using(var s=TeachingAlgorithmRegistry.Create(TeachingAlgorithmKind.JarvisMarch,snapshot,granularity)) {
            var visits=new Dictionary<int,HashSet<int>>();TeachingEvent last=null;int rejected=0,accepted=0;
            TeachingAdvance state;
            do {state=s.Advance(4096,out var e);Check(s.LastAdvanceComparisons<=4096,"Budget");if(e==null)continue;
                if(e.Operation==TeachingOperation.Trial){if(!visits.TryGetValue(e.A,out var set))visits[e.A]=set=new HashSet<int>();Check(set.Add(e.Point),"Each point tried once per boundary vertex");Check(e.Point!=e.A,"No self edge");last=e;}
                if(e.Operation==TeachingOperation.Reject){Check(last!=null&&last.Point==e.Point&&last.A==e.A&&last.B==e.B,"Reject returns to prior best");double cross=ConvexHull2D.Cross(s.Unique[e.A],s.Unique[e.B],s.Unique[e.Point]);Check(cross==e.Value&&cross>=0,"Rejected trial not better");rejected++;}
                if(e.Operation==TeachingOperation.Candidate)accepted++;
            }while(state!=TeachingAdvance.Complete);
            var expected=ConvexHull2D.Compute(input);Check(s.Result.Count==expected.Count,"Hull count");for(int i=0;i<expected.Count;i++)Check(s.Unique[s.Result[i]]==expected[i],"Hull order");
            if(expected.Count>=2){Check(visits.Count==expected.Count,"Scan from every confirmed boundary vertex");foreach(var set in visits.Values)Check(set.Count==s.Unique.Count-1,"All points including interior points tried");if(s.Unique.Count>2)Check(rejected>0,"Rejected trials visible in either granularity");}
            return new{granularity,points=s.Points.Count,trials=(s.Unique.Count-1)*visits.Count,rejected,accepted};
        }
    }
    public static object Algorithm() {
        var result=new List<object>();var input=new[]{new Vector2(-2,-2),new Vector2(2,-2),new Vector2(2,2),new Vector2(-2,2),Vector2.zero,new Vector2(1,0),new Vector2(-1,1),new Vector2(0,-2),Vector2.zero};
        foreach(TeachingGranularity g in Enum.GetValues(typeof(TeachingGranularity))){result.Add(CheckDataset(input,g));Array.Reverse(input);result.Add(CheckDataset(input,g));CheckDataset(new[]{Vector2.zero,Vector2.one,new Vector2(2,2)},g);CheckDataset(new[]{Vector2.zero},g);}
        return new{passed=true,cases=8,coverage="all unique points per boundary vertex; interior rejection, farthest collinear point, both granularities and reverse input"};
    }
    public static async Task<string> Visual() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();d.HideHelp();p.SelectAlgorithm(TeachingAlgorithmKind.JarvisMarch);
        Check(p.Play(out var error),error);p.Pause();int revision=d.HullRevision;int guard=10000;
        while(p.IsActive&&p.CurrentEvent.Operation!=TeachingOperation.Trial&&guard-->0){p.NextStep();p.Tick(1);p.Tick(1);}
        Check(p.IsActive&&p.CurrentEvent.Operation==TeachingOperation.Trial,"Reached trial");Check(p.overlay.transition.positionCount==2,"Trial edge remains visible at full extension");
        while(p.IsActive&&p.CurrentEvent.Operation!=TeachingOperation.Reject&&guard-->0){p.NextStep();p.Tick(1);p.Tick(1);}
        Check(p.IsActive&&p.CurrentEvent.Operation==TeachingOperation.Reject,"Reached rejection");var e=p.CurrentEvent;var points=p.CurrentTrace.Unique;
        Check(p.overlay.candidateRing.gameObject.activeSelf&&(Vector2)p.overlay.candidateRing.transform.position==points[e.B],"Orange ring restored at retained best");
        Check(p.overlay.removedRing.gameObject.activeSelf&&(Vector2)p.overlay.removedRing.transform.position==points[e.Point],"Rejected point red");
        Check(p.overlay.comparison.positionCount==2&&(Vector2)p.overlay.comparison.GetPosition(1)==points[e.B],"Best candidate edge retained");
        Check(p.overlay.transition.positionCount==0,"Rejected edge retracts completely");
        var overlay=p.overlay;overlay.Begin(e);overlay.Draw(0);Check(overlay.transition.positionCount==2&&(Vector2)overlay.transition.GetPosition(1)==points[e.Point],"Retraction starts at rejected point");overlay.Draw(.5f);Check((Vector2)overlay.transition.GetPosition(1)==Vector2.Lerp(points[e.A],points[e.Point],.5f),"Retraction interpolates");overlay.Draw(1);
        Check(d.HullRevision==revision,"No realtime recomputation");bool paused=EditorApplication.isPaused;EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(180);EditorApplication.isPaused=paused;Canvas.ForceUpdateCanvases();
        return "PASS every-point trial and rejection animation, red rejected point, orange best candidate, unchanged real-time hull";
    }
}
