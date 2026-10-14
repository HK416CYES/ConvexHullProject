using System;
using System.Collections.Generic;
using ConvexHullDemo;
using UnityEngine;
public static class VerifyMultiHull {
    static int assertions,cases,retries;static long comparisons;
    static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception(message);}
    static void Case(List<Vector2> input,string name) {
        var expected=ConvexHull2D.Compute(input);var snapshot=new List<TeachingPoint>();for(int i=0;i<input.Count;i++)snapshot.Add(new TeachingPoint(100+i,input[i]));
        foreach(TeachingAlgorithmKind kind in Enum.GetValues(typeof(TeachingAlgorithmKind)))foreach(TeachingGranularity granularity in Enum.GetValues(typeof(TeachingGranularity))) {
            using(var s=TeachingAlgorithmRegistry.Create(kind,snapshot,granularity)) {
                long guard=0;TeachingAdvance state;int restarts=0;
                do {state=s.Advance(4096,out var e);Check(s.LastAdvanceComparisons<=4096,"Frame budget");if(e!=null&&e.Operation==TeachingOperation.Restart)restarts++;
                    if(++guard>20000000)throw new Exception("Unbounded stream "+name+" "+kind);
                }while(state!=TeachingAdvance.Complete);
                string context=name+" "+kind+" "+granularity;
                Check(s.Result.Count==expected.Count,context+" size "+s.Result.Count+" expected "+expected.Count);
                var actual=new List<Vector2>();for(int i=0;i<s.Result.Count;i++){actual.Add(s.Unique[s.Result[i]]);Check(actual[i].Equals(expected[i]),context+" vertex "+i+": "+actual[i]+" expected "+expected[i]);}
                Check(Math.Abs(ConvexHull2D.Area(actual)-ConvexHull2D.Area(expected))<.000001,context+" area");
                for(int i=0;i<s.Points.Count;i++){int first=input.FindIndex(p=>p.Equals(s.Points[i].Position));Check(s.Points[i].Id==100+first,context+" first duplicate representative");}
                if(kind==TeachingAlgorithmKind.Chan&&name=="circle1000"){Check(s.RetryCount>=2,"Chan retry rounds");Check(s.TangentQueries>0,"Chan tangent queries");Check(s.MaximumTangentComparisons<=15*(Math.Ceiling(Math.Log(input.Count,2))+2),"Binary tangent logarithmic bound");}
                comparisons+=s.Comparisons;retries+=s.RetryCount;cases++;
            }
        }
    }
    public static string Algorithms() {
        assertions=cases=retries=0;comparisons=0;
        var sets=new[]{new List<Vector2>(),new List<Vector2>{Vector2.one},new List<Vector2>{Vector2.zero,Vector2.one},new List<Vector2>{Vector2.zero,Vector2.zero,Vector2.one,Vector2.one},
            new List<Vector2>{new Vector2(0,0),new Vector2(0,4),new Vector2(0,2),new Vector2(0,1),new Vector2(0,-3)},
            new List<Vector2>{new Vector2(-2,-2),new Vector2(2,-2),new Vector2(2,2),new Vector2(-2,2),Vector2.zero,new Vector2(1,-2)}};
        for(int i=0;i<sets.Length;i++)Case(sets[i],"degenerate"+i);
        var rng=new System.Random(917);
        for(int seed=0;seed<180;seed++) {
            int n=seed<120?rng.Next(3,70):rng.Next(100,300);var p=new List<Vector2>();
            for(int i=0;i<n;i++)p.Add(new Vector2(rng.Next(-12,13),rng.Next(-12,13))+(seed%3==0?new Vector2(100000,-100000):Vector2.zero));
            Case(p,"random"+seed);p.Reverse();Case(p,"reverse"+seed);
        }
        var cloud=new List<Vector2>();var circle=new List<Vector2>();
        for(int i=0;i<1000;i++){cloud.Add(new Vector2((float)rng.NextDouble()*20-10,(float)rng.NextDouble()*12-6));float a=i*Mathf.PI*2/1000;circle.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5);}
        Case(cloud,"cloud1000");Case(circle,"circle1000");
        return "PASS "+cases+" sessions, "+assertions+" assertions, "+comparisons+" comparisons; Chan retries "+retries;
    }
}
