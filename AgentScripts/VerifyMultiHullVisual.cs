using System;
using ConvexHullDemo;
using UnityEngine;
public static class VerifyMultiHullVisual {
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static string Run() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();
        var selected=p.SelectedAlgorithm;var cam=d.worldCamera.transform.position;float zoom=d.worldCamera.orthographicSize;bool help=d.IsHelpOpen;d.HideHelp();
        try {
            p.SelectAlgorithm(TeachingAlgorithmKind.Andrew);Check(p.Play(out var error),error);p.Pause();
            int guard=1000;while(p.IsActive&&p.CurrentEvent.Operation!=TeachingOperation.Pop&&guard-->0){p.NextStep();p.Tick(1);p.Tick(1);}
            Check(p.IsActive&&p.CurrentEvent.Operation==TeachingOperation.Pop,"Reached Andrew pop");
            var e=p.CurrentEvent;var coordinates=p.CurrentTrace.Unique;
            Check(e.Point!=e.B,"Removed and candidate are distinct");
            Check(p.overlay.candidateRing.gameObject.activeSelf&&p.overlay.removedRing.gameObject.activeSelf,"Both markers active");
            Check((Vector2)p.overlay.candidateRing.transform.position==coordinates[e.B],"Orange marker remains at new candidate");
            Check((Vector2)p.overlay.removedRing.transform.position==coordinates[e.Point],"Red marker at removed point");
            Check(e.Value==ConvexHull2D.Cross(coordinates[e.A],coordinates[e.Point],coordinates[e.B]),"Removal event records turn value");
            p.Stop();p.SelectAlgorithm(TeachingAlgorithmKind.QuickHull);Check(p.Play(out error),error);p.Pause();guard=1000;
            while(p.IsActive&&p.CurrentEvent.Phase!="确认外边界"&&guard-->0){p.NextStep();p.Tick(1);p.Tick(1);}
            Check(p.IsActive,"Reached QuickHull confirmation");bool blue=false;
            foreach(var line in p.overlay.GetComponentsInChildren<LineRenderer>())if(line.positionCount==2&&Vector4.Distance(line.startColor,p.overlay.lowerColor)<.01f)blue=true;
            Check(blue,"Confirmed QuickHull boundary blue");
            return "PASS distinct candidate/removal rings, exact pop cross value and blue confirmed QuickHull edge";
        }finally {p.Stop();p.SelectAlgorithm(selected);d.worldCamera.transform.position=cam;d.worldCamera.orthographicSize=zoom;if(help)d.ShowHelp();else d.HideHelp();}
    }
}
