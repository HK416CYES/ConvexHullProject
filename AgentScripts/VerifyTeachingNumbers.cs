using System;
using System.Collections.Generic;
using System.Reflection;
using ConvexHullDemo;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyTeachingNumbers {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Ready(HullTeachingSession s){TeachingEvent e;for(int i=0;!s.NumberingReady&&i<10000;i++)s.Advance(7,out e);Check(s.NumberingReady,"Numbering ready");}
    static void Numbers(HullTeachingSession s) {
        Ready(s);var seen=new HashSet<int>();
        for(int i=0;i<s.Points.Count;i++) {int number=s.DisplayNumbers[i];Check(number>=1&&number<=s.Points.Count&&seen.Add(number),"Dense unique numbers");
            if(s.Algorithm==TeachingAlgorithmKind.Andrew){int rank=1;for(int j=0;j<s.Points.Count;j++){var a=s.Unique[j];var b=s.Unique[i];if(a.x<b.x||(a.x==b.x&&a.y<b.y))rank++;}Check(number==rank,"Andrew coordinate rank");}
            else Check(number==i+1,"Snapshot scan order");
        }
    }
    public static object Algorithm() {
        checks=0;var random=new System.Random(11);int sessions=0;
        foreach(var kind in (TeachingAlgorithmKind[])Enum.GetValues(typeof(TeachingAlgorithmKind)))foreach(var mode in new[]{TeachingGranularity.KeySteps,TeachingGranularity.DetailedSteps})foreach(int count in new[]{0,1,2,20,1000}) {
            var input=new List<TeachingPoint>();for(int i=0;i<count;i++)input.Add(new TeachingPoint(1000+i*17,new Vector2(random.Next(-20,21),random.Next(-20,21))));
            if(count>0)input.Add(new TeachingPoint(90000,input[0].Position));
            using(var s=TeachingAlgorithmRegistry.Create(kind,input,mode)){Numbers(s);for(int i=0;i<s.Points.Count;i++)Check(s.Points[i].Id<90000,"Earliest duplicate representative retains permanent ID");}
            sessions++;
        }
        using(var s=TeachingAlgorithmRegistry.Create(TeachingAlgorithmKind.Andrew,new[]{new TeachingPoint(91,new Vector2(3,0)),new TeachingPoint(7,new Vector2(-1,0)),new TeachingPoint(500,Vector2.zero)},TeachingGranularity.KeySteps)){Numbers(s);Check(s.DisplayNumbers[0]==3&&s.DisplayNumbers[1]==1&&s.DisplayNumbers[2]==2,"Collinear sorted numbering");}
        return new{passed=true,sessions,checks};
    }
    public static object Visual() {
        checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();d.HideHelp();
        var original=p.SelectedAlgorithm;
        try {
            d.ClearPoints();for(int i=0;i<50;i++){var discarded=d.AddPoint(new Vector2(i,0));d.RemovePoint(discarded.Id);}
            var a=d.AddPoint(new Vector2(3,0));var b=d.AddPoint(new Vector2(-3,0));var c=d.AddPoint(new Vector2(0,2));var dup=d.AddPoint(b.Position);d.FlushChanges();
            Check(a.Id>50,"Actual controller IDs include deletion gaps");
            foreach(var kind in new[]{TeachingAlgorithmKind.QuickHull,TeachingAlgorithmKind.JarvisMarch,TeachingAlgorithmKind.Chan,TeachingAlgorithmKind.Andrew}) {
                p.SelectAlgorithm(kind);int revision=d.HullRevision;Check(p.Play(out var error),"Play: "+error);p.Pause();
                var s=p.CurrentTrace;for(int i=0;!s.NumberingReady&&i<10000;i++){p.NextStep();p.Tick(1);p.Tick(1);}Numbers(s);
                var panel=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>();panel.Refresh();
                var labels=(List<Text>)typeof(HullPlaybackPanel).GetField("labels",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel);
                for(int i=0;i<s.Points.Count;i++)Check(labels[i].text==s.DisplayNumbers[i].ToString(),"Pooled label refreshed for selected algorithm");
                for(int i=s.Points.Count;i<labels.Count;i++)Check(!labels[i].gameObject.activeSelf,"Stale pooled labels hidden");
                Check(s.Points.Count==3&&d.Points.Count==4,"Duplicate occupies only representative number");Check(d.HullRevision==revision,"Numbering does not refresh real hull");Check(d.Points[0].Id==a.Id&&d.Points[1].Id==b.Id,"Original IDs unchanged");p.Stop();
            }
            d.RemovePoint(b.Id);d.MovePoint(a.Id,new Vector2(-4,0));d.FlushChanges();
            Check(p.Play(out var message),message);p.Pause();var latest=p.CurrentTrace;Numbers(latest);
            Check(latest.DisplayNumbers[0]==1&&latest.Points[0].Id==a.Id,"Moved point changes sorted rank, not ID");
            Check(latest.DisplayNumbers.Count==3,"Deleted representative does not leave a numbering gap");
            return new{passed=true,checks};
        } finally {p.Stop();p.SelectAlgorithm(original);}
    }
}
