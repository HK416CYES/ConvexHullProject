using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
public static class VerifyQuickHullScan {
    static int checks,sessions,comparisons;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    sealed class Scan {public int A,B,Count,Best=-1;public int[] Expected;public double Maximum=-1;}
    static void Case(Vector2[] input,TeachingGranularity granularity,bool earlyMaximum=false) {
        var snapshot=new List<TeachingPoint>();for(int i=0;i<input.Length;i++)snapshot.Add(new TeachingPoint(i+1,input[i]));
        using(var session=TeachingAlgorithmRegistry.Create(TeachingAlgorithmKind.QuickHull,snapshot,granularity)) {
            var scans=new Dictionary<int,Scan>();TeachingAdvance status;int afterEarly=0;
            do {status=session.Advance(4096,out var e);Check(session.LastAdvanceComparisons<=4096,"Budget");if(e==null)continue;
                if(e.Operation==TeachingOperation.ActiveEdge&&e.Path!=null)scans[e.Group]=new Scan{A=e.A,B=e.B,Expected=e.Path};
                if(e.Operation==TeachingOperation.FarthestCompare) {
                    Check(scans.TryGetValue(e.Group,out var scan),"Comparison belongs to active subset");
                    Check(scan.Count<scan.Expected.Length&&e.Point==scan.Expected[scan.Count],"Every subset point compared in input order");
                    Check(e.A==scan.A&&e.B==scan.B&&e.PreviousBest==scan.Best,"Base and previous best retained");
                    var a=session.Unique[e.A];var b=session.Unique[e.B];var p=session.Unique[e.Point];
                    double cross=Math.Abs(ConvexHull2D.Cross(a,b,p)),x=(double)b.x-a.x,y=(double)b.y-a.y;
                    Check(e.Value==cross&&Math.Abs(e.DistanceValue-cross/Math.Sqrt(x*x+y*y))<1e-9,"Distance is perpendicular distance to baseline");
                    scan.Maximum=Math.Max(scan.Maximum,e.DistanceValue);
                    Check(Math.Abs(e.BestDistance-scan.Maximum)<1e-9,"Running maximum includes all points seen so far");
                    bool bestSeen=false;for(int i=0;i<=scan.Count;i++)if(scan.Expected[i]==e.Best)bestSeen=true;
                    Check(bestSeen,"Cannot choose a future untested point");
                    if(!e.Improved)Check(e.Best==e.PreviousBest,"Closer/equal point keeps prior best");
                    if(earlyMaximum&&e.Group==0&&scan.Count>0){Check(!e.Improved&&e.Best==scan.Expected[0],"First maximum remains while later points still compared");afterEarly++;}
                    scan.Best=e.Best;scan.Count++;comparisons++;
                }
                if(e.Operation==TeachingOperation.ConfirmPoint&&e.Phase=="确定最远凸包顶点") {
                    var scan=scans[e.Group];Check(scan.Count==scan.Expected.Length,"Do not confirm until every eligible point compared");Check(e.Point==scan.Best,"Confirm scanned maximum");
                }
            }while(status!=TeachingAdvance.Complete);
            if(earlyMaximum)Check(afterEarly==3,"All three later points compared after finding first maximum");
            foreach(var scan in scans.Values)Check(scan.Count==scan.Expected.Length,"No interrupted scan");
            var hull=ConvexHull2D.Compute(input);Check(hull.Count==session.Result.Count,"Hull size");for(int i=0;i<hull.Count;i++)Check(hull[i]==session.Unique[session.Result[i]],"Exact final hull");sessions++;
        }
    }
    public static object Algorithm() {
        checks=sessions=comparisons=0;var early=new[]{new Vector2(-5,0),new Vector2(5,0),new Vector2(0,-4),new Vector2(-2,-1),new Vector2(1,-2),new Vector2(3,-.5f),new Vector2(0,3)};
        foreach(TeachingGranularity g in Enum.GetValues(typeof(TeachingGranularity))) {
            Case(early,g,true);var reversed=(Vector2[])early.Clone();Array.Reverse(reversed);Case(reversed,g);
            var random=new System.Random(381);for(int sample=0;sample<80;sample++){var points=new Vector2[sample==79?1000:random.Next(3,80)];for(int i=0;i<points.Length;i++)points[i]=new Vector2(random.Next(-10,11),random.Next(-10,11));Case(points,g);}
        }
        return new{passed=true,sessions,checks,comparisons,coverage="early maximum still scans later points; all recursive subsets; both granularities; ties; final hull"};
    }
    static void Next(HullAlgorithmPlayback p){p.NextStep();p.Tick(1);p.Tick(1);}
    public static async Task<object> Visual() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();d.HideHelp();p.SelectAlgorithm(TeachingAlgorithmKind.QuickHull);Check(p.Play(out var error),error);p.Pause();
        int revision=d.HullRevision,guard=20000;
        while(p.IsActive&&(p.CurrentEvent.Operation!=TeachingOperation.FarthestCompare||p.CurrentEvent.Improved)&&guard-->0)Next(p);
        Check(p.IsActive,"Found later comparison that does not replace maximum");var e=p.CurrentEvent;var points=p.CurrentTrace.Unique;var overlay=p.overlay;
        Check(overlay.ScanBestRing!=null&&overlay.ScanBestRing.gameObject.activeSelf&&(Vector2)overlay.ScanBestRing.transform.position==points[e.Best],"Green ring retains best");
        Check((Vector2)overlay.candidateRing.transform.position==points[e.Point],"Orange ring marks current comparison");
        Check(overlay.transition.positionCount==2&&overlay.comparison.positionCount==2,"Both perpendicular distances remain visible");
        Vector2 foot=overlay.transition.GetPosition(0),end=overlay.transition.GetPosition(1),bestFoot=overlay.comparison.GetPosition(0),bestEnd=overlay.comparison.GetPosition(1);
        Check(end==points[e.Point]&&bestEnd==points[e.Best],"Distance lines end at tested/best points");
        Vector2 baseDirection=points[e.B]-points[e.A];Check(Math.Abs(Vector2.Dot(end-foot,baseDirection))<.001f,"Current distance perpendicular to baseline");
        Check(Math.Abs(Vector2.Dot(bestEnd-bestFoot,baseDirection))<.001f,"Best distance perpendicular to baseline");
        Check(Math.Abs((end-foot).magnitude-e.DistanceValue)<.001f&&Math.Abs((bestEnd-bestFoot).magnitude-e.BestDistance)<.001f,"Rendered lengths match numeric distances");
        int index=p.StepIndex;long geometry=p.CurrentTrace.Comparisons;p.Tick(100);Check(index==p.StepIndex&&geometry==p.CurrentTrace.Comparisons,"Pause freezes comparison");
        Check(d.HullRevision==revision,"Real-time hull unchanged");
        bool editorPause=EditorApplication.isPaused;EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(180);EditorApplication.isPaused=editorPause;Canvas.ForceUpdateCanvases();
        return new{passed=true,current=p.CurrentTrace.Points[e.Point].Id,best=p.CurrentTrace.Points[e.Best].Id,e.DistanceValue,e.BestDistance,description="later shorter point compared while running maximum retained"};
    }
    public static string Cleanup() {
        var p=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>().playback;p.Stop();
        Check(p.overlay.ScanBestRing==null||!p.overlay.ScanBestRing.gameObject.activeSelf,"Scan marker cleaned");Check(p.overlay.comparison.positionCount==0&&p.overlay.transition.positionCount==0&&p.overlay.upper.positionCount==0,"Distance guides cleaned");
        return "PASS scan marker and distance-guide cleanup";
    }
}
