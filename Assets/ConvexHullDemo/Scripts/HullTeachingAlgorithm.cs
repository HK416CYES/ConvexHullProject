using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    /// <summary>历史 Andrew 记录器，仅供旧版回归验证。场景播放统一使用 HullTeachingSession 流式会话，不预存步骤。</summary>
    public static class HullTeachingAlgorithm {
        public enum Action { Sort, Deduplicate, BeginLower, BeginUpper, Candidate, Check, Add, Remove, Finish }
        public struct Step {
            public Action Kind;
            public bool Upper;
            public int Point, A, B;
            public double Cross;
            public Step(Action kind, bool upper=false, int point=-1, int a=-1, int b=-1, double cross=0) {
                Kind=kind; Upper=upper; Point=point; A=a; B=b; Cross=cross;
            }
        }
        public sealed class Trace {
            public readonly IReadOnlyList<Vector2> Sorted, Unique, Hull;
            public readonly IReadOnlyList<Step> Steps;
            internal Trace(List<Vector2> sorted,List<Vector2> unique,List<Vector2> hull,List<Step> steps) {
                Sorted=sorted.AsReadOnly(); Unique=unique.AsReadOnly(); Hull=hull.AsReadOnly(); Steps=steps.AsReadOnly();
            }
        }
        public static Trace Record(IEnumerable<Vector2> snapshot) {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            var sorted=new List<Vector2>(snapshot);
            foreach(var p in sorted)
                if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y))
                    throw new ArgumentException("Coordinates must be finite.");
            // 左到右排序，同一列从下到上排序。排序操作不影响原始点集。
            sorted.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            var unique=new List<Vector2>();
            foreach(var p in sorted)if(unique.Count==0||!p.Equals(unique[unique.Count-1]))unique.Add(p);
            var steps=new List<Step>{new Step(Action.Sort),new Step(Action.Deduplicate)};
            var lower=new List<int>();var upper=new List<int>();
            steps.Add(new Step(Action.BeginLower));
            for(int i=0;i<unique.Count;i++)Visit(i,false,unique,lower,steps);
            var result=new List<Vector2>();
            if(unique.Count<3) {
                // 单点、两点不需要绕一圈，直接保留全部不同坐标。
                result.AddRange(unique);
            } else {
                steps.Add(new Step(Action.BeginUpper,true));
                for(int i=unique.Count-1;i>=0;i--)Visit(i,true,unique,upper,steps);
                // 两条链各自包含左右端点，拼接时各跳过末尾端点，避免重复。
                for(int i=0;i<lower.Count-1;i++)result.Add(unique[lower[i]]);
                for(int i=0;i<upper.Count-1;i++)result.Add(unique[upper[i]]);
            }
            steps.Add(new Step(Action.Finish));
            return new Trace(sorted,unique,result,steps);
        }
        private static void Visit(int p,bool upper,List<Vector2> points,List<int> stack,List<Step> steps) {
            steps.Add(new Step(Action.Candidate,upper,p));
            while(stack.Count>=2) {
                int a=stack[stack.Count-2],b=stack[stack.Count-1];
                // AB×AP：左转为正，右转为负，共线为零。双精度减少计算的舍入误差。
                double cross=((double)points[b].x-points[a].x)*((double)points[p].y-points[a].y)
                    -((double)points[b].y-points[a].y)*((double)points[p].x-points[a].x);
                steps.Add(new Step(Action.Check,upper,p,a,b,cross));
                if(cross>0)break;
                // 每次仅记录被弹出的点，播放器重放这些增量，不保存整条链的重复副本。
                steps.Add(new Step(Action.Remove,upper,b));stack.RemoveAt(stack.Count-1);
            }
            stack.Add(p);steps.Add(new Step(Action.Add,upper,p));
        }
    }
}
