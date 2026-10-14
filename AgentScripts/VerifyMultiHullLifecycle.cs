using System;
using System.Collections.Generic;
using ConvexHullDemo;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyMultiHullLifecycle {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    public static object Run() {
        checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();
        var original=new List<Vector2>();foreach(var point in d.Points)original.Add(point.Position);
        var cam=d.worldCamera.transform.position;float zoom=d.worldCamera.orthographicSize;bool help=d.IsHelpOpen;var selected=p.SelectedAlgorithm;var granularity=p.granularity;
        d.HideHelp();
        try {
            foreach(TeachingAlgorithmKind kind in Enum.GetValues(typeof(TeachingAlgorithmKind)))foreach(int n in new[]{0,1,2,20,1000}) {
                p.Stop();p.SelectAlgorithm(kind);d.ClearPoints();
                for(int i=0;i<n;i++)d.AddPoint(n<=2?new Vector2(i,i):new Vector2((i%40)*.2f,(i/40)*.2f));d.FlushChanges();
                int revision=d.HullRevision;var hull=new List<Vector2>(d.Hull);double area=d.HullArea;
                bool played=p.Play(out var error);
                if(n==0){Check(!played&&error=="没有点，无法演示","Empty rejects "+kind);continue;}
                Check(played,"Play "+kind+" "+n+" "+error);p.Pause();
                int guard=200000;while(p.IsActive&&guard-->0){p.NextStep();p.Tick(1);p.Tick(1);}
                Check(!p.IsActive,"Complete "+kind+" "+n);Check(p.overlay.ActiveGroupCount==0,"No subgroup lines");
                Check(d.HullRevision==revision&&d.HullArea==area&&d.Points.Count==n,"No live recomputation");
                for(int i=0;i<hull.Count;i++)Check(d.Hull[i]==hull[i],"Live vertices unchanged");
                Check(p.Play(out error),"Replay "+kind);p.Pause();for(int i=0;i<15&&p.IsActive;i++){p.NextStep();p.Tick(1);p.Tick(1);}
                p.Stop();Check(p.overlay.ActiveGroupCount==0,"Mid-animation stop clears all groups");
                var labels=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>(FindObjectsInactive.Include);
                foreach(var text in labels.labelsRoot.GetComponentsInChildren<Text>(true))Check(!text.gameObject.activeSelf,"No active labels after stop");
                Check(d.Toolbar.algorithmDropdown.interactable,"Algorithm selection restored");
            }
            p.SelectAlgorithm(TeachingAlgorithmKind.Chan);Check(p.Play(out var lastError),"Disable scenario: "+lastError);p.Pause();
            for(int i=0;i<30;i++){p.NextStep();p.Tick(1);p.Tick(1);}p.enabled=false;
            Check(!p.IsActive&&p.overlay.ActiveGroupCount==0&&d.outline.enabled&&d.fillRenderer.enabled,"Component disable releases session and restores realtime renderers");p.enabled=true;
            return new{passed=true,checks,algorithms=5,sizes="0/1/2/20/1000",cleanup="completion, mid-animation stop and component disable"};
        } finally {p.enabled=true;p.Stop();p.SelectAlgorithm(selected);p.granularity=granularity;d.ClearPoints();foreach(var point in original)d.AddPoint(point);d.FlushChanges();d.worldCamera.transform.position=cam;d.worldCamera.orthographicSize=zoom;if(help)d.ShowHelp();else d.HideHelp();}
    }
}
