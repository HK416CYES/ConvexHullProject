using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
public static class VerifyQuickHullTeaching {
    static int checks;
    static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
    public static object Algorithm() {
        checks=0;int sessions=0,triangleEvents=0;var random=new System.Random(639);
        foreach(TeachingGranularity granularity in Enum.GetValues(typeof(TeachingGranularity)))for(int run=0;run<100;run++) {
            var coords=new List<Vector2>();int count=run<4?run:run==99?1000:random.Next(4,120);
            for(int i=0;i<count;i++)coords.Add(new Vector2(random.Next(-8,9),run<4?0:random.Next(-8,9)));
            var snapshot=new List<TeachingPoint>();for(int i=0;i<count;i++)snapshot.Add(new TeachingPoint(i+1,coords[i]));
            var expected=ConvexHull2D.Compute(coords);var confirmed=new HashSet<int>();var triangles=new HashSet<int>();
            using(var s=TeachingAlgorithmRegistry.Create(TeachingAlgorithmKind.QuickHull,snapshot,granularity)) {
                TeachingAdvance state;
                do {state=s.Advance(4096,out var e);Check(s.LastAdvanceComparisons<=4096,"Frame budget");if(e==null)continue;
                    if(e.Operation==TeachingOperation.ConfirmPoint){Check(confirmed.Add(e.Point),"No repeated confirmation");Check(expected.Contains(s.Unique[e.Point]),"Every confirmed point truly on final hull");}
                    if(e.Operation==TeachingOperation.Triangle){Check(e.Closed&&e.Path.Length==3,"Triangle has three vertices and a closing baseline");Check(e.Path[0]==e.A&&e.Path[1]==e.Point&&e.Path[2]==e.B,"Triangle records correct base and far point");Check(confirmed.Contains(e.Point),"Far point confirmed before triangle");Check(ConvexHull2D.Cross(s.Unique[e.A],s.Unique[e.B],s.Unique[e.Point])<0,"Triangle lies on active outside half-plane");triangles.Add(e.Group);triangleEvents++;}
                    Check(!(e.Operation==TeachingOperation.ClearGroup&&triangles.Contains(e.Group)),"Triangle context retained during recursion");
                }while(state!=TeachingAdvance.Complete);
                Check(confirmed.Count==expected.Count,"All and only hull vertices confirmed");Check(s.Result.Count==expected.Count,"Result count");for(int i=0;i<expected.Count;i++)Check(s.Unique[s.Result[i]]==expected[i],"Result CCW and exact");sessions++;
            }
        }
        return new{passed=true,sessions,checks,triangleEvents};
    }
    static void Next(HullAlgorithmPlayback playback){playback.NextStep();playback.Tick(1);if(playback.IsActive&&playback.CurrentEvent.Operation!=TeachingOperation.Finish)playback.Tick(1);}
    static HullAlgorithmPlayback Start(){var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.playback.Stop();d.HideHelp();d.playback.SelectAlgorithm(TeachingAlgorithmKind.QuickHull);Check(d.playback.Play(out var error),error);d.playback.Pause();return d.playback;}
    static async Task Frame(){bool paused=EditorApplication.isPaused;EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(180);EditorApplication.isPaused=paused;Canvas.ForceUpdateCanvases();}
    public static Task<string> Preview()=>RenderPreview(2);
    public static Task<string> PreviewChild()=>RenderPreview(3);
    static async Task<string> RenderPreview(int triangleCount) {
        var p=Start();int guard=20000;while(p.IsActive&&(p.CurrentEvent.Operation!=TeachingOperation.Triangle||p.overlay.TriangleCount<triangleCount)&&guard-->0)Next(p);
        Check(p.IsActive&&p.overlay.TriangleCount>=triangleCount,"Recursive triangles visible");await Frame();return "Paused with current orange triangle, faded parent triangle and persistent blue confirmed points";
    }
    public static string MarkerFit() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();d.HideHelp();p.SelectAlgorithm(TeachingAlgorithmKind.QuickHull);
        float original=p.overlay.confirmedPointDiameter;var cam=d.worldCamera.transform.position;float zoom=d.worldCamera.orthographicSize;
        try {
            Check(p.Play(out var error),error);float initial=d.worldCamera.orthographicSize;p.Stop();p.overlay.confirmedPointDiameter=1.5f;
            Check(p.Play(out error),error);p.Pause();Check(d.worldCamera.orthographicSize>initial,"Fit accounts for enlarged confirmed rings");
            float radius=1.5f*d.worldCamera.pixelHeight/(4*d.worldCamera.orthographicSize);
            foreach(var point in d.Points){var screen=d.worldCamera.WorldToScreenPoint(point.Position);Check(screen.x-radius>0&&screen.x+radius<d.worldCamera.pixelWidth&&screen.y-radius>0&&screen.y+radius<d.Toolbar.ToolbarScreenBottom,"Confirmed ring fully visible outside toolbar");}
            return "PASS configurable confirmed-point diameter included in automatic framing";
        }finally {p.Stop();p.overlay.confirmedPointDiameter=original;d.worldCamera.transform.position=cam;d.worldCamera.orthographicSize=zoom;}
    }
    public static async Task<object> Visual() {
        checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=Start();int revision=d.HullRevision;double area=d.HullArea;
        var coordinates=new List<Vector2>();var ids=new List<int>();foreach(var point in d.Points){coordinates.Add(point.Position);ids.Add(point.Id);}
        int guard=20000;while(p.IsActive&&p.CurrentEvent.Operation!=TeachingOperation.Triangle&&guard-->0)Next(p);
        Check(p.IsActive,"Reached first triangle");int confirmed=p.overlay.ConfirmedPointCount;Check(confirmed>=3,"Extremes and far point confirmed");
        var e=p.CurrentEvent;bool loop=false,fill=false;
        foreach(var line in p.overlay.GetComponentsInChildren<LineRenderer>())if(line.loop&&line.positionCount==3){var points=p.CurrentTrace.Unique;
            if((Vector2)line.GetPosition(0)==points[e.Path[0]]&&(Vector2)line.GetPosition(1)==points[e.Path[1]]&&(Vector2)line.GetPosition(2)==points[e.Path[2]])loop=true;}
        foreach(var renderer in p.overlay.GetComponentsInChildren<MeshRenderer>())if(renderer.name.StartsWith("HullQuickTriangleFill")&&renderer.gameObject.activeSelf){fill=true;Check(renderer.GetComponent<MeshFilter>().sharedMesh!=p.overlay.triangleFillPrefab.GetComponent<MeshFilter>().sharedMesh,"Template mesh not mutated");Check(renderer.sharedMaterial.renderQueue<d.pointPrefab.dot.sharedMaterial.renderQueue,"Fill below original points");}
        Check(loop&&fill,"Closed three-sided outline and independent fill");
        while(p.IsActive&&p.CurrentEvent.Operation!=TeachingOperation.ActiveEdge&&guard-->0)Next(p);
        Check(p.overlay.TriangleCount>=1&&p.overlay.ConfirmedPointCount>=confirmed,"Parent triangle and confirmed rings survive child activation");
        int oldIndex=p.StepIndex;long oldComparisons=p.CurrentTrace.Comparisons;p.Tick(100);Check(oldIndex==p.StepIndex&&oldComparisons==p.CurrentTrace.Comparisons,"Pause freezes flow");
        int previousConfirmed=p.overlay.ConfirmedPointCount;
        while(p.IsActive&&p.CurrentEvent.Operation!=TeachingOperation.Finish&&guard-->0){Next(p);Check(p.overlay.ConfirmedPointCount>=previousConfirmed,"Confirmed points never disappear during traversal");previousConfirmed=p.overlay.ConfirmedPointCount;}
        Check(p.IsActive&&p.CurrentEvent.Operation==TeachingOperation.Finish,"Final boundary shown before cleanup");
        Check(p.overlay.ConfirmedPointCount==d.Hull.Count&&p.overlay.TriangleCount==0,"Final confirmed points remain, auxiliary triangles cleared");
        p.Tick(1);Check(!p.IsActive&&p.overlay.ConfirmedPointCount==0&&p.overlay.TriangleCount==0,"Stop removes all markers/fills");
        Check(d.HullRevision==revision&&d.HullArea==area,"No live hull recalculation");for(int i=0;i<coordinates.Count;i++)Check(d.Points[i].Position==coordinates[i]&&d.Points[i].Id==ids[i],"Original points unchanged");
        foreach(var renderer in p.overlay.GetComponentsInChildren<MeshRenderer>(true))if(renderer.name.StartsWith("HullQuickTriangleFill")||renderer.name.StartsWith("HullTeachingMarker"))Check(!renderer.gameObject.activeSelf,"No pooled fill/confirmed ring left active");
        Check(p.Play(out var error),error);p.Pause();guard=20000;while(p.IsActive&&p.overlay.TriangleCount<2&&guard-->0)Next(p);p.Stop();Check(p.overlay.ConfirmedPointCount==0&&p.overlay.TriangleCount==0,"Replay/mid-recursion stop clears all");
        await Preview();return new{passed=true,checks,description="triangles, persistent confirmed vertices, child context, pause, cleanup, replay and unchanged live hull"};
    }
}
