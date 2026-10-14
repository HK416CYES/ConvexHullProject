using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
public static class VerifyMergeTeaching {
 static int checks;
 static void Assert(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 static List<TeachingPoint> Sample(int n){var r=new System.Random(39+n);var p=new List<TeachingPoint>();for(int i=0;i<n;i++)p.Add(new TeachingPoint(i+100,new Vector2(r.Next(-20,21),r.Next(-20,21))));return p;}
 static double Cross(Vector2 a,Vector2 b,Vector2 c)=>((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
 public static object Algorithm(){checks=0;int comparisons=0,merges=0;
  foreach(var mode in new[]{TeachingGranularity.KeySteps,TeachingGranularity.DetailedSteps})foreach(int n in new[]{1,2,3,4,5,20,100,1000})using(var s=TeachingAlgorithmRegistry.Create(TeachingAlgorithmKind.MergeHull,Sample(n),mode)) {
   TeachingEvent e;int guard=0;while(++guard<1000000){var status=s.Advance(4096,out e);Assert(s.LastAdvanceComparisons<=4096,"Budget respected");if(status==TeachingAdvance.Complete)break;if(status!=TeachingAdvance.Visible)continue;
    Assert(e.Operation!=TeachingOperation.Push&&e.Operation!=TeachingOperation.Pop&&e.Operation!=TeachingOperation.Candidate,"Leaf Andrew animation removed");
    Assert(e.Operation!=TeachingOperation.ClearGroup,"No sequential shrinking on merge");
    if(e.Phase=="直接显示最小组凸包")Assert(e.Path.Length<=3,"Leaf emitted once as small hull");
    if(e.Operation==TeachingOperation.TangentCompare){comparisons++;var cross=Cross(s.Unique[e.Best],s.Unique[e.PreviousBest],s.Unique[e.Point]);Assert(cross==e.Value,"Displayed cross matches tested neighbor");Assert(e.PreviousBest==e.A||e.PreviousBest==e.B,"Checking neighbor of a current endpoint");}
    if(e.Operation==TeachingOperation.Merge){merges++;Assert(e.LeftGroup>=0&&e.RightGroup>=0,"Merge identifies existing children");}
   }
   var coords=new List<Vector2>();foreach(var p in s.Points)coords.Add(p.Position);var expected=ConvexHull2D.Compute(coords);Assert(expected.Count==s.Result.Count,"Result vertex count");for(int i=0;i<expected.Count;i++)Assert(expected[i]==s.Unique[s.Result[i]],"Exact result agrees");
  }
  Assert(comparisons>0&&merges>0,"Both granularities show merging and comparisons");return new{passed=true,checks,comparisons,merges};
 }
 static IDictionary Groups(HullTeachingOverlay o)=>(IDictionary)typeof(HullTeachingOverlay).GetField("groups",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static LineRenderer Line(object group)=>(LineRenderer)group.GetType().GetField("Line").GetValue(group);
 public static object Visual(){checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.playback.Stop();d.HideHelp();var o=d.playback.overlay;int revision=d.HullRevision,comparisons=0,merges=0;
  using(var s=TeachingAlgorithmRegistry.Create(TeachingAlgorithmKind.MergeHull,Sample(20),TeachingGranularity.KeySteps)) {
   o.ResetSession(s);try{TeachingEvent e;while(s.Advance(4096,out e)!=TeachingAdvance.Complete){if(e==null)continue;
    var groups=Groups(o);LineRenderer reused=e.Operation==TeachingOperation.Merge?Line(groups[e.LeftGroup]):null;
    o.Begin(e);o.Draw(1);
    if(e.Operation==TeachingOperation.TangentCompare){comparisons++;Assert(o.comparison.positionCount==2&&o.transition.positionCount==2&&o.upper.positionCount==2,"Candidate, neighbor and trial edges visible");Assert(o.candidateRing.gameObject.activeSelf,"Neighbor highlighted");Assert((Vector2)o.comparison.GetPosition(0)==s.Unique[e.A]&&(Vector2)o.comparison.GetPosition(1)==s.Unique[e.B],"Current tangent remains visible");}
    if(e.Operation==TeachingOperation.Merge){merges++;groups=Groups(o);Assert(!groups.Contains(e.LeftGroup)&&!groups.Contains(e.RightGroup)&&!groups.Contains(e.Group+100000)&&!groups.Contains(e.Group+200000),"Children and helper groups removed atomically");var line=Line(groups[e.Group]);Assert(line==reused,"Existing left renderer reused");Assert(line.positionCount==e.Path.Length,"Full merged outline immediately present");Assert(Mathf.Abs(line.startColor.r-o.lowerColor.r)<.005f&&Mathf.Abs(line.startColor.g-o.lowerColor.g)<.005f&&Mathf.Abs(line.startColor.b-o.lowerColor.b)<.005f,"Merged outline immediately blue (renderer color quantization)");for(int i=0;i<e.Path.Length;i++)Assert((Vector2)line.GetPosition(i)==s.Unique[e.Path[i]],"Merged edge endpoint correct");}
   }}finally{o.Clear();}
  }
  Assert(d.HullRevision==revision&&o.ActiveGroupCount==0,"No realtime recalculation or leftover lines");return new{passed=true,checks,comparisons,merges};
 }
 public static async Task<string> Preview(){var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.HideHelp();d.playback.Stop();d.playback.SelectAlgorithm(TeachingAlgorithmKind.MergeHull);if(!d.playback.Play(out var error))throw new Exception(error);d.playback.Pause();for(int i=0;i<10000;i++){d.playback.NextStep();d.playback.Tick(1);d.playback.Tick(1);if(d.playback.CurrentEvent.Operation==TeachingOperation.TangentCompare)break;}EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(200);Canvas.ForceUpdateCanvases();return "Tangent comparison preview ready";}
}
