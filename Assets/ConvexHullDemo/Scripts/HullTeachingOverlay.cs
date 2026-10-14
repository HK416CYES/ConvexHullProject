using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    /// <summary>独立演示覆盖层；所有渲染器均在编辑器制作，不触碰正常点和凸包渲染器。</summary>
    public sealed class HullTeachingOverlay : MonoBehaviour {
        public LineRenderer lower,upper,comparison,transition;
        public MeshRenderer candidateRing,removedRing;
        public Color lowerColor=new Color(.05f,.40f,.95f),upperColor=new Color(.1f,.65f,.38f);
        public Color candidateColor=new Color(1,.58f,.08f),removedColor=new Color(.95f,.16f,.16f);
        [Min(.001f)] public float lineWidth=.075f;
        [Min(.01f)] public float ringDiameter=.65f;
        private readonly List<int> low=new List<int>(),high=new List<int>();
        private HullTeachingAlgorithm.Trace trace;
        private HullTeachingAlgorithm.Step step;
        private Vector2 edgeStart,edgeEnd;
        private bool animatedEdge;
        private MaterialPropertyBlock block;
        public void ResetTrace(HullTeachingAlgorithm.Trace value) { trace=value;low.Clear();high.Clear();Clear(); }
        public void Clear() {
            foreach(var line in new[]{lower,upper,comparison,transition})if(line!=null)line.positionCount=0;
            if(lower!=null)lower.loop=false;if(upper!=null)upper.loop=false;
            if(candidateRing!=null)candidateRing.gameObject.SetActive(false);if(removedRing!=null)removedRing.gameObject.SetActive(false);
            animatedEdge=false;
        }
        public void Begin(HullTeachingAlgorithm.Step value) {
            step=value;animatedEdge=false;comparison.positionCount=transition.positionCount=0;
            removedRing.gameObject.SetActive(false);
            var chain=step.Upper?high:low;
            if(step.Kind==HullTeachingAlgorithm.Action.Candidate)Mark(candidateRing,trace.Unique[step.Point],candidateColor);
            if(step.Kind==HullTeachingAlgorithm.Action.Check) {
                comparison.startColor=comparison.endColor=candidateColor;
                comparison.positionCount=3;comparison.SetPosition(0,At(trace.Unique[step.A]));
                comparison.SetPosition(1,At(trace.Unique[step.B]));comparison.SetPosition(2,At(trace.Unique[step.Point]));
            }
            if(step.Kind==HullTeachingAlgorithm.Action.Add) {
                if(chain.Count>0){edgeStart=trace.Unique[chain[chain.Count-1]];edgeEnd=trace.Unique[step.Point];animatedEdge=true;}
                chain.Add(step.Point);
            }
            if(step.Kind==HullTeachingAlgorithm.Action.Remove) {
                Mark(removedRing,trace.Unique[step.Point],removedColor);
                edgeEnd=trace.Unique[chain[chain.Count-1]];edgeStart=trace.Unique[chain[chain.Count-2]];
                chain.RemoveAt(chain.Count-1);animatedEdge=true;
            }
            if(step.Kind==HullTeachingAlgorithm.Action.BeginUpper)candidateRing.gameObject.SetActive(false);
            if(step.Kind==HullTeachingAlgorithm.Action.Finish) {
                Clear();lower.startColor=lower.endColor=lowerColor;lower.loop=trace.Hull.Count>=3;
                lower.positionCount=trace.Hull.Count>=2?trace.Hull.Count:0;
                for(int i=0;i<lower.positionCount;i++)lower.SetPosition(i,At(trace.Hull[i]));
            } else Draw(0);
        }
        public void Draw(float progress) {
            foreach(var line in new[]{lower,upper,comparison,transition})line.widthMultiplier=lineWidth;
            if(step.Kind==HullTeachingAlgorithm.Action.Finish)return;
            DrawChain(lower,low,lowerColor,step.Kind==HullTeachingAlgorithm.Action.Add&&!step.Upper&&animatedEdge);
            DrawChain(upper,high,upperColor,step.Kind==HullTeachingAlgorithm.Action.Add&&step.Upper&&animatedEdge);
            if(animatedEdge) {
                transition.startColor=transition.endColor=step.Kind==HullTeachingAlgorithm.Action.Remove?removedColor:step.Upper?upperColor:lowerColor;
                transition.positionCount=2;transition.SetPosition(0,At(edgeStart));
                transition.SetPosition(1,At(Vector2.Lerp(edgeStart,edgeEnd,step.Kind==HullTeachingAlgorithm.Action.Remove?1-progress:progress)));
            }
        }
        private void DrawChain(LineRenderer line,List<int> chain,Color color,bool skipLast) {
            int count=chain.Count-(skipLast?1:0);line.loop=false;line.startColor=line.endColor=color;
            line.positionCount=count>=2?count:0;
            for(int i=0;i<line.positionCount;i++)line.SetPosition(i,At(trace.Unique[chain[i]]));
        }
        private void Mark(MeshRenderer ring,Vector2 p,Color color) {
            ring.gameObject.SetActive(true);ring.transform.position=At(p);ring.transform.localScale=Vector3.one*ringDiameter*.5f;
            if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",color);ring.SetPropertyBlock(block);
        }
        private static Vector3 At(Vector2 p)=>new Vector3(p.x,p.y,-.15f);
    }
}
