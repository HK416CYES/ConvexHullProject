using System;
using System.Collections.Generic;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyHullPlayback {
    static int assertions;
    static void Assert(bool ok,string text){assertions++;if(!ok)throw new Exception(text);}
    public static object Algorithm() {
        assertions=0;
        var cases=new List<List<Vector2>> {
            new List<Vector2>(),new List<Vector2>{Vector2.one},new List<Vector2>{Vector2.zero,Vector2.one},
            new List<Vector2>{Vector2.one,Vector2.one,Vector2.one},
            new List<Vector2>{new Vector2(-2,-2),Vector2.zero,Vector2.one,new Vector2(2,2)},
            new List<Vector2>{Vector2.zero,Vector2.right,Vector2.up},
            new List<Vector2>{Vector2.zero,new Vector2(2,0),new Vector2(2,2),new Vector2(0,2),Vector2.one,Vector2.zero}
        };
        var random=new System.Random(17);
        for(int n=1;n<=100;n++){var points=new List<Vector2>();for(int i=0;i<n;i++)points.Add(new Vector2(random.Next(-20,21),random.Next(-20,21)));cases.Add(points);}
        var large=new List<Vector2>();for(int i=0;i<1000;i++)large.Add(new Vector2(random.Next(-20,21),random.Next(-20,21)));cases.Add(large);
        foreach(var points in cases) {
            var original=points.ToArray();var trace=HullTeachingAlgorithm.Record(points);var expected=ConvexHull2D.Compute(points);
            Assert(trace.Hull.Count==expected.Count,"Hull count agrees");
            for(int i=0;i<expected.Count;i++)Assert(trace.Hull[i]==expected[i],"Exact CCW result agrees");
            for(int i=0;i<points.Count;i++)Assert(points[i]==original[i],"Snapshot not mutated");
            var low=new List<int>();var high=new List<int>();
            foreach(var step in trace.Steps) {
                var stack=step.Upper?high:low;
                if(step.Kind==HullTeachingAlgorithm.Action.Add)stack.Add(step.Point);
                if(step.Kind==HullTeachingAlgorithm.Action.Remove){Assert(stack[stack.Count-1]==step.Point,"Removal event matches replay stack");stack.RemoveAt(stack.Count-1);}
                if(step.Kind==HullTeachingAlgorithm.Action.Check) {
                    Assert(stack[stack.Count-2]==step.A&&stack[stack.Count-1]==step.B,"Check event matches replay stack");
                    Assert(step.Cross==ConvexHull2D.Cross(trace.Unique[step.A],trace.Unique[step.B],trace.Unique[step.Point]),"Cross sign/value agrees");
                }
            }
            Assert(trace.Steps.Count<=trace.Unique.Count*10+5,"Incremental trace uses linear event count");
        }
        return new{passed=true,assertions,cases=cases.Count};
    }
    static bool verticesContains(IReadOnlyList<Vector2> hull,Vector2 point) {foreach(var vertex in hull)if(vertex.Equals(point))return true;return false;}
    static void Locked(System.Action action,string name){try{action();throw new Exception(name+" was not locked");}catch(InvalidOperationException){assertions++;}}
    public static object Playback() {
        var playback=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>().playback;
        playback.Stop();var selected=playback.SelectedAlgorithm;int total=0;
        try{foreach(TeachingAlgorithmKind kind in Enum.GetValues(typeof(TeachingAlgorithmKind))){playback.SelectAlgorithm(kind);PlaybackCore();total+=assertions;}}
        finally{playback.Stop();playback.SelectAlgorithm(selected);}return new{passed=true,algorithms=5,assertions=total};
    }
    static object PlaybackCore() {
        assertions=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;
        var c=d.worldCamera;var position=c.transform.position;float size=c.orthographicSize;bool help=d.IsHelpOpen;
        var coordinates=new List<Vector2>();var ids=new List<int>();foreach(var point in d.Points){coordinates.Add(point.Position);ids.Add(point.Id);}
        int revision=d.HullRevision;double area=d.HullArea;int hull=d.Hull.Count;var outline=new Vector3[d.outline.positionCount];d.outline.GetPositions(outline);
        var fill=d.fill.sharedMesh;int? originalSelection=d.SelectedId;
        d.Select(ids[0]);int? selected=d.SelectedId;
        var selectedView=d.pointsRoot.Find("Point "+ids[0]).GetComponent<HullPointView>();
        bool outlineEnabled=d.outline.enabled,fillEnabled=d.fillRenderer.enabled;
        d.HideHelp();Canvas.ForceUpdateCanvases();
        try {
            Assert(p.Play(out var error),"Play success: "+error);p.Pause();
            Assert(d.IsPlayingAlgorithm&&p.IsPaused,"Active/paused flags");
            Assert(!d.Toolbar.countInput.interactable&&!d.Toolbar.randomButton.interactable&&!d.Toolbar.clearButton.interactable,"Editing controls disabled");
            Assert(!d.Toolbar.algorithmDropdown.interactable,"Algorithm dropdown disabled");
            Assert(!p.SelectAlgorithm((TeachingAlgorithmKind)(((int)p.ActiveAlgorithm+1)%5)),"Controller algorithm selection locked");
            Assert(!d.outline.enabled&&!d.fillRenderer.enabled,"Existing hull outline and fill hidden");
            Assert(!selectedView.selectionRing.gameObject.activeSelf&&d.SelectedId==selected,"Selection hidden without changing selected ID");
            var colorBlock=new MaterialPropertyBlock();
            foreach(var point in d.Points) {
                var view=d.pointsRoot.Find("Point "+point.Id).GetComponent<HullPointView>();
                view.dot.GetPropertyBlock(colorBlock);
                Assert(colorBlock.GetColor("_Color")==d.innerColor,"All original points use neutral color during playback");
            }
            var panel=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>();
            var corners=new Vector3[4];panel.card.GetWorldCorners(corners);float bottom=RectTransformUtility.WorldToScreenPoint(panel.GetComponentInParent<Canvas>().worldCamera,corners[1]).y;
            foreach(var point in d.Points) {
                var screen=c.WorldToScreenPoint(point.Position);
                Assert(screen.x>0&&screen.x<c.pixelWidth&&screen.y>bottom&&screen.y<d.Toolbar.ToolbarScreenBottom,"Fit includes every point outside UI");
            }
            p.Tick(100);Assert(p.StepIndex==0,"Pause freezes timer");p.NextStep();p.Tick(1);p.Tick(1);Assert(p.StepIndex==1&&p.IsPaused,"One step then remain paused");
            foreach(float speed in new[]{.25f,.5f,1,2,4}){p.SetSpeed(speed);Assert(p.Speed==speed,"Speed selection");}
            p.SetSpeed(1);
            Locked(()=>d.AddPoint(Vector2.zero),"Add");Locked(()=>d.MovePoint(ids[0],Vector2.zero),"Move");
            Locked(()=>d.RemovePoint(ids[0]),"Delete");Locked(()=>d.ClearPoints(),"Clear");Assert(!d.Randomize("20",out error),"Random locked");
            d.Select(ids[0]);Assert(d.SelectedId==selected,"Selection locked");
            d.OpenContextMenu(new Vector2(c.pixelWidth*.5f,c.pixelHeight*.4f));Assert(!d.IsMenuOpen,"Right menu locked");
            var at=(Vector2)c.WorldToScreenPoint(d.Points[0].Position);var cameraBefore=c.transform.position;
            d.BeginLeft(at);d.MoveLeft(at+Vector2.right*40);d.EndLeft(at+Vector2.right*40);
            Assert(c.transform.position!=cameraBefore,"Dragging point pans camera during playback");
            for(int i=0;i<coordinates.Count;i++)Assert(d.Points[i].Position==coordinates[i],"Pan leaves points unchanged");
            float before=c.orthographicSize;d.Zoom(new Vector2(c.pixelWidth*.5f,c.pixelHeight*.4f),120);Assert(c.orthographicSize<before,"Wheel zoom allowed");
            p.TogglePause();Assert(!p.IsPaused,"Resume");d.ShowHelp();Assert(p.IsPaused,"Help pauses");d.HideHelp();Assert(p.IsPaused,"Closing help stays paused");
            int budget=100000;while(p.IsActive&&budget-->0){p.NextStep();p.Tick(1);p.Tick(1);}
            Assert(!p.IsActive,"Complete automatically cleans up");
            Assert(d.outline.enabled==outlineEnabled&&d.fillRenderer.enabled==fillEnabled,"Completed playback restores renderer states");
            Assert(selectedView.selectionRing.gameObject.activeSelf,"Completion restores selected point ring");
            foreach(var point in d.Points) {
                var view=d.pointsRoot.Find("Point "+point.Id).GetComponent<HullPointView>();view.dot.GetPropertyBlock(colorBlock);
                var expected=verticesContains(d.Hull,point.Position)?d.hullColor:d.innerColor;
                Assert(colorBlock.GetColor("_Color")==expected,"Completion restores hull/interior point colors");
            }
            Assert(p.overlay.ActiveGroupCount==0&&p.overlay.lower.positionCount==0&&p.overlay.upper.positionCount==0&&p.overlay.comparison.positionCount==0&&p.overlay.transition.positionCount==0,"No remaining teaching lines");
            Assert(!p.overlay.candidateRing.gameObject.activeSelf&&!p.overlay.removedRing.gameObject.activeSelf,"No remaining markers");
            Assert(d.Toolbar.randomButton.interactable&&d.Toolbar.playButton.interactable,"Editing restored");
            Assert(d.HullRevision==revision&&d.HullArea==area&&d.Hull.Count==hull&&d.fill.sharedMesh==fill,"Existing hull not recalculated/replaced");
            for(int i=0;i<outline.Length;i++)Assert(d.outline.GetPosition(i)==outline[i],"Existing outline unchanged");
            for(int i=0;i<coordinates.Count;i++)Assert(d.Points[i].Position==coordinates[i]&&d.Points[i].Id==ids[i],"Original points and IDs unchanged");
            Assert(p.Play(out error),"Replay");p.Stop();Assert(!p.IsActive,"Stop");
            Assert(d.outline.enabled==outlineEnabled&&d.fillRenderer.enabled==fillEnabled&&selectedView.selectionRing.gameObject.activeSelf,"Manual stop restores presentation");
            d.outline.enabled=false;Assert(p.Play(out error),"Replay with original outline disabled");p.Stop();
            Assert(!d.outline.enabled&&d.fillRenderer.enabled==fillEnabled,"Does not force previously hidden renderer on");d.outline.enabled=outlineEnabled;
            var distant=d.AddPoint(new Vector2(d.maxZoomSize*100,d.maxZoomSize*100));d.FlushChanges();
            try {var old=c.transform.position;float oldSize=c.orthographicSize;Assert(!p.Play(out error)&&error=="凸包过大，无法演示","Large hull rejection");Assert(c.transform.position==old&&c.orthographicSize==oldSize,"Rejection does not move camera");}
            finally {d.RemovePoint(distant.Id);d.FlushChanges();}
            return new{passed=true,assertions,resolution=new Vector2(c.pixelWidth,c.pixelHeight)};
        } finally {
            p.Stop();d.outline.enabled=outlineEnabled;d.fillRenderer.enabled=fillEnabled;d.Select(originalSelection);d.CancelGesture();c.transform.position=position;c.orthographicSize=size;if(help)d.ShowHelp();else d.HideHelp();
        }
    }
    public static object Degenerates() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;
        var original=new List<Vector2>();foreach(var point in d.Points)original.Add(point.Position);
        bool help=d.IsHelpOpen;var camera=d.worldCamera.transform.position;float size=d.worldCamera.orthographicSize;
        d.HideHelp();p.Stop();int cases=0;
        try {
            foreach(var points in new[]{new Vector2[0],new[]{Vector2.zero},new[]{Vector2.zero,Vector2.one},
                new[]{Vector2.one,Vector2.one,Vector2.one},new[]{new Vector2(-2,0),Vector2.zero,Vector2.right,new Vector2(2,0)},
                new[]{Vector2.zero,Vector2.right,Vector2.up}}) {
                d.ClearPoints();foreach(var point in points)d.AddPoint(point);d.FlushChanges();
                int revision=d.HullRevision;int count=d.Hull.Count;var vertices=new List<Vector2>(d.Hull);
                bool played=p.Play(out var error);
                if(points.Length==0){Assert(!played&&error=="没有点，无法演示","Empty rejection");cases++;continue;}
                Assert(played,"Degenerate play");p.Pause();
                int budget=100000;while(p.IsActive&&budget-->0){p.NextStep();p.Tick(1);p.Tick(1);}
                Assert(!p.IsActive&&d.HullRevision==revision&&d.Hull.Count==count,"Degenerate completion isolated");
                for(int i=0;i<vertices.Count;i++)Assert(vertices[i]==d.Hull[i],"Degenerate hull unchanged");cases++;
            }
            d.ClearPoints();for(int i=0;i<1000;i++)d.AddPoint(new Vector2((i%40)*.2f,(i/40)*.2f));d.FlushChanges();
            Assert(p.Play(out var largeError),"1000 points playback: "+largeError);p.Pause();
            Assert(p.CurrentTrace.Unique.Count==1000,"1000 point snapshot");for(int i=0;i<20;i++){p.NextStep();p.Tick(1);p.Tick(1);}p.Stop();
            return new{passed=true,cases,largePointCount=1000};
        } finally {
            p.Stop();d.ClearPoints();foreach(var point in original)d.AddPoint(point);d.FlushChanges();
            d.worldCamera.transform.position=camera;d.worldCamera.orthographicSize=size;if(help)d.ShowHelp();else d.HideHelp();
        }
    }
    public static string Preview() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.HideHelp();d.playback.Stop();
        if(!d.playback.Play(out var error))throw new Exception(error);d.playback.Pause();
        for(int i=0;i<18&&d.playback.IsActive;i++){d.playback.NextStep();d.playback.Tick(1);d.playback.Tick(1);}
        Canvas.ForceUpdateCanvases();return "Paused teaching preview prepared.";
    }
}
