using System;
using System.Collections.Generic;
using System.Linq;
using ConvexHullDemo;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using System.Reflection;
using System.Threading.Tasks;

public static class VerifyConvexHullDemo {
    private static int checks;
    private static void Check(bool condition, string message) {
        checks++; if (!condition) throw new Exception("Check failed: " + message);
    }
    private static Vector2 V(float x, float y) => new Vector2(x,y);
    public static string Algorithm() {
        checks = 0;
        var cases = new[] {
            new Vector2[0], new[]{V(2,3)}, new[]{V(2,3),V(-1,2)},
            new[]{V(0,0),V(0,0),V(0,0)}, new[]{V(-3,-3),V(0,0),V(2,2),V(1,1),V(2,2)},
            new[]{V(0,0),V(4,0),V(0,3)},
            new[]{V(0,0),V(4,0),V(4,3),V(0,3),V(2,1),V(1,0),V(0,3)}
        };
        int[] expectedCounts = {0,1,2,1,2,3,4}; double[] expectedAreas = {0,0,0,0,0,6,12};
        for (int c=0;c<cases.Length;c++) {
            var hull = ConvexHull2D.Compute(cases[c]);
            Check(hull.Count==expectedCounts[c],"vertex count case " + c);
            Check(Math.Abs(ConvexHull2D.Area(hull)-expectedAreas[c])<1e-9,"area case " + c);
            Check(hull.SequenceEqual(ConvexHull2D.Compute(cases[c].Reverse())),"input permutation case " + c);
            ValidateHull(cases[c],hull);
        }
        var random = new System.Random(173);
        for(int trial=0;trial<40;trial++) {
            var points = new List<Vector2>();
            for(int i=0;i<150;i++) points.Add(V(random.Next(-100,101),random.Next(-100,101)));
            var hull = ConvexHull2D.Compute(points); ValidateHull(points,hull);
            // Independent supporting-line oracle for integer coordinates: every extreme endpoint must be in hull.
            var unique = points.Distinct().ToList();
            var expected = new HashSet<Vector2>();
            for(int i=0;i<unique.Count;i++) for(int j=i+1;j<unique.Count;j++) {
                bool positive=false,negative=false;
                foreach(var p in unique) { double cross=ConvexHull2D.Cross(unique[i],unique[j],p); positive|=cross>0;negative|=cross<0; if(positive&&negative)break; }
                if(positive&&negative)continue;
                Vector2 delta=unique[j]-unique[i];
                var onEdge=unique.Where(p=>ConvexHull2D.Cross(unique[i],unique[j],p)==0).OrderBy(p=>Vector2.Dot(p,delta)).ToList();
                expected.Add(onEdge[0]);expected.Add(onEdge[onEdge.Count-1]);
            }
            Check(expected.SetEquals(hull),"independent hull oracle " + trial);
        }
        var translated=cases[6].Select(p=>p+V(1000000,-1000000)).ToArray();
        Check(ConvexHull2D.Area(ConvexHull2D.Compute(translated))==12,"translated area precision");
        bool rejected=false; try { ConvexHull2D.Compute(new[]{V(float.NaN,0)}); } catch(ArgumentException) { rejected=true; }
        Check(rejected,"invalid coordinate rejection");
        return checks + " algorithm assertions passed, including 40 randomized supporting-line oracle comparisons.";
    }
    private static void ValidateHull(IEnumerable<Vector2> points, IReadOnlyList<Vector2> hull) {
        Check(hull.Distinct().Count()==hull.Count,"no duplicate hull vertices");
        if(hull.Count<3)return;
        for(int i=0;i<hull.Count;i++) {
            Check(ConvexHull2D.Cross(hull[i],hull[(i+1)%hull.Count],hull[(i+2)%hull.Count])>0,"strict CCW turn");
            foreach(var p in points)Check(ConvexHull2D.Cross(hull[i],hull[(i+1)%hull.Count],p)>=0,"point inside/on hull");
        }
    }
    public static object Status() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        var camera=d.worldCamera;
        return new { screen=new[]{camera.pixelWidth,camera.pixelHeight}, camera=d.worldCamera.transform.position.ToString(),
            count=d.Points.Count,hull=d.Hull.Count,area=d.HullArea,selected=d.SelectedId,menu=d.IsMenuOpen,
            countText=d.Toolbar.pointCount.text,hullText=d.Toolbar.hullCount.text,areaText=d.Toolbar.area.text,
            randomRect=d.GetRandomScreenRect().ToString(), points=d.Points.Select(p=>new{id=p.Id,world=p.Position.ToString(),screen=((Vector2)d.worldCamera.WorldToScreenPoint(p.Position)).ToString()}).ToArray() };
    }
    public static string Interactions() {
        if(!Application.isPlaying)throw new Exception("Enter Play Mode first.");
        checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        var camera=d.worldCamera;var start=camera.transform.position;
        bool enabled=d.enabled;d.enabled=false;
        try {
            Canvas.ForceUpdateCanvases();d.ClearPoints();
            var a=d.AddPoint(d.ScreenToWorld(new Vector2(camera.pixelWidth*.42f,camera.pixelHeight*.45f)));
            var b=d.AddPoint(d.ScreenToWorld(new Vector2(camera.pixelWidth*.62f,camera.pixelHeight*.45f)));
            var c=d.AddPoint(d.ScreenToWorld(new Vector2(camera.pixelWidth*.50f,camera.pixelHeight*.65f)));
            d.FlushChanges();var rect=d.GetRandomScreenRect();
            Vector2 at=camera.WorldToScreenPoint(a.Position);
            d.BeginLeft(at);d.EndLeft(at+Vector2.one*2);Check(d.SelectedId==a.Id,"click selects point below drag threshold");
            Check(a.ViewForTestSelected(),"selection ring active");
            var position=a.Position;int revision=d.HullRevision;
            d.BeginLeft(at);d.MoveLeft(at+new Vector2(60,25));d.EndLeft(at+new Vector2(60,25));d.FlushChanges();
            Check(!a.Position.Equals(position),"selected point moved");Check(camera.transform.position==start,"point drag does not pan camera");
            Check(d.HullRevision==revision+1,"drag recomputes once when flushed");
            Vector2 bScreen=camera.WorldToScreenPoint(b.Position);position=b.Position;revision=d.HullRevision;
            d.BeginLeft(bScreen);d.MoveLeft(bScreen+new Vector2(80,30));d.EndLeft(bScreen+new Vector2(80,30));d.FlushChanges();
            Check(b.Position.Equals(position),"unselected-point drag leaves data unchanged");Check(camera.transform.position!=start,"unselected-point drag pans");
            Check(d.HullRevision==revision,"panning does not recompute hull");Check(d.SelectedId==a.Id,"panning preserves selection");
            camera.transform.position=start;
            at=camera.WorldToScreenPoint(a.Position);d.OpenContextMenu(at);Check(d.SelectedId==a.Id&&d.Menu.HasDelete,"right click selects and exposes deletion");
            int count=d.Points.Count;var expected=d.ScreenToWorld(at);d.Menu.createButton.onClick.Invoke();d.FlushChanges();
            Check(d.Points.Count==count+1&&d.Points.Last().Position.Equals(expected),"context create records world coordinate");
            Check(!d.IsMenuOpen,"menu closes after creation");Check(d.HitTest(at)==d.SelectedId,"selected point wins overlap");
            d.Select(null);Check(d.HitTest(at)==d.Points.Last().Id,"latest coincident point wins unselected tie");
            int newest=d.Points.Last().Id;d.OpenContextMenu(at);d.Menu.deleteButton.onClick.Invoke();d.FlushChanges();
            Check(d.Points.All(p=>p.Id!=newest)&&d.SelectedId==null,"menu deletes stable target and clears selection");
            d.OpenContextMenu(new Vector2(camera.pixelWidth-2,rect.yMax));Canvas.ForceUpdateCanvases();
            Check(!d.Menu.HasDelete,"blank menu has only creation");
            var corners=new Vector3[4];((RectTransform)d.Menu.transform).GetWorldCorners(corners);
            var uiCamera=d.Menu.GetComponentInParent<Canvas>().worldCamera;
            foreach(var corner in corners) {var s=RectTransformUtility.WorldToScreenPoint(uiCamera,corner);Check(s.x>=-1&&s.x<=camera.pixelWidth+1&&s.y>=-1&&s.y<=camera.pixelHeight+1,"edge menu fully visible");}
            d.BeginLeft(new Vector2(camera.pixelWidth*.12f,camera.pixelHeight*.4f));Check(!d.IsMenuOpen,"outside click closes menu");d.EndLeft(new Vector2(camera.pixelWidth*.12f,camera.pixelHeight*.4f));
            Check(d.SelectedId==null,"blank click clears selection");
            d.OpenContextMenu(new Vector2(camera.pixelWidth*.15f,camera.pixelHeight*.4f));d.CloseMenu();Check(!d.IsMenuOpen,"explicit/Escape close path");
            var snapshot=d.Points.Select(p=>p.Position).ToArray();string error;
            foreach(var bad in new[]{"","abc","1.5","0","-2","1001","999999999999"}) {
                Check(!d.Randomize(bad,out error),"invalid random rejected: "+bad);
                Check(snapshot.SequenceEqual(d.Points.Select(p=>p.Position)),"invalid input preserves data");
            }
            camera.transform.position=new Vector3(1000,-750,start.z);
            rect=d.GetRandomScreenRect();
            foreach(int n in new[]{1,2,20,1000}) {
                var ids=new HashSet<int>(d.Points.Select(p=>p.Id));
                d.Toolbar.countInput.text=n.ToString();d.Toolbar.randomButton.onClick.Invoke();d.FlushChanges();
                Check(d.Points.Count==n,"random replaces exact count "+n);Check(d.Points.All(p=>!ids.Contains(p.Id)),"old IDs removed");
                foreach(var p in d.Points) {Vector2 s=camera.WorldToScreenPoint(p.Position);Check(rect.Contains(s),"random in current visible usable rectangle");}
                Check(camera.transform.position==new Vector3(1000,-750,start.z),"random preserves camera");
            }
            var uiPoint=new Vector2(camera.pixelWidth*.70f,camera.pixelHeight-30);var before=camera.transform.position;
            Check(d.PointerOverUI(uiPoint),"toolbar raycast blocks canvas input");
            d.BeginLeft(uiPoint);d.MoveLeft(uiPoint+Vector2.right*40);d.EndLeft(uiPoint+Vector2.right*40);
            Check(camera.transform.position==before,"UI click/drag does not pan");
            d.Toolbar.clearButton.onClick.Invoke();d.FlushChanges();Check(d.Points.Count==0&&d.Hull.Count==0&&d.HullArea==0,"clear removes geometry/statistics");
            Check(d.Toolbar.pointCount.text=="0"&&d.Toolbar.hullCount.text=="0","UI statistics refreshed");
            return checks+" interaction assertions passed (selection, both drag modes, menus, overlap, UI exclusion, invalid input, 1/2/20/1000 random points and far panning).";
        } finally {
            d.CancelGesture();d.ClearPoints();camera.transform.position=start;
            foreach(var p in d.initialPoints)d.AddPoint(p);d.FlushChanges();d.enabled=enabled;
        }
    }

    public static object SetViewSize(int width,int height) {
        var assembly=typeof(Editor).Assembly;
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
        var sizeType=assembly.GetType("UnityEditor.GameViewSizeType");
        var sizeClass=assembly.GetType("UnityEditor.GameViewSize");
        var size=Activator.CreateInstance(sizeClass,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
            new object[]{Enum.Parse(sizeType,"FixedResolution"),width,height,"Hull verification "+width+"x"+height},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;
        var gameType=assembly.GetType("UnityEditor.GameView");
        var window=EditorWindow.GetWindow(gameType);
        gameType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(window,index);
        window.Focus();window.Repaint();
        return new { requested=new[]{width,height},index };
    }

    private static async Task Pointer(Vector2 p,int button,bool pressed) {
        var mouse=Mouse.current;
        using(StateEvent.From(mouse,out var state)) {
            mouse.position.WriteValueIntoEvent(p,state);
            (button==0?mouse.leftButton:mouse.rightButton).WriteValueIntoEvent(pressed?1f:0f,state);
            InputSystem.QueueEvent(state);
        }
        await Task.Delay(120);
    }
    private static void Key(bool pressed) {
        using(StateEvent.From(Keyboard.current,out var state)) {
            Keyboard.current.escapeKey.WriteValueIntoEvent(pressed?1f:0f,state);InputSystem.QueueEvent(state);
        }
    }
    private static async Task Click(Vector2 p,int button=0) { await Pointer(p,button,true);await Pointer(p,button,false); }
    private static Vector2 UIPosition(Button button) {
        var canvas=button.GetComponentInParent<Canvas>();var rect=(RectTransform)button.transform;
        return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center));
    }
    public static async Task<string> InputFlow() {
        checks=0;var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        var camera=d.worldCamera;var start=camera.transform.position;
        try {
            d.ClearPoints();await Task.Delay(100);
            Vector2 at=new Vector2(camera.pixelWidth*.42f,camera.pixelHeight*.43f);
            var a=d.AddPoint(d.ScreenToWorld(at));await Task.Delay(100);
            await Click(at);Check(d.SelectedId==a.Id,"Input System left click selects");
            var position=a.Position;
            await Pointer(at,0,true);await Pointer(at+new Vector2(50,20),0,true);await Pointer(at+new Vector2(50,20),0,false);
            Check(a.Position!=position,"Input System selected-point drag");Check(camera.transform.position==start,"point drag keeps camera");
            await Click(camera.WorldToScreenPoint(a.Position),1);Check(d.IsMenuOpen&&d.Menu.HasDelete,"Input System right click opens point menu");
            int count=d.Points.Count;await Click(UIPosition(d.Menu.createButton));
            Check(d.Points.Count==count+1&&!d.IsMenuOpen,"UI InputModule create button");
            await Click(camera.WorldToScreenPoint(a.Position),1);await Click(UIPosition(d.Menu.deleteButton));
            Check(d.Points.Count==count&&!d.IsMenuOpen,"UI InputModule delete button");
            Vector2 blank=new Vector2(camera.pixelWidth*.12f,camera.pixelHeight*.30f);
            await Pointer(blank,0,true);await Pointer(blank+new Vector2(100,40),0,true);await Pointer(blank+new Vector2(100,40),0,false);
            Check(camera.transform.position!=start,"Input System canvas pan");
            await Click(blank,1);Check(d.IsMenuOpen&&!d.Menu.HasDelete,"Input System blank menu");
            Key(true);await Task.Delay(120);
            Key(false);await Task.Delay(120);
            Check(!d.IsMenuOpen,"Input System Escape closes menu");
            d.Toolbar.countInput.text="20";
            await Click(UIPosition(d.Toolbar.randomButton));Check(d.Points.Count==20,"UI random button replaces data");
            await Click(UIPosition(d.Toolbar.clearButton));Check(d.Points.Count==0,"UI clear button");
            return checks+" end-to-end Input System/UI assertions passed.";
        } finally {
            using(StateEvent.From(Mouse.current,out var state)) {
                Mouse.current.leftButton.WriteValueIntoEvent(0f,state);Mouse.current.rightButton.WriteValueIntoEvent(0f,state);InputSystem.QueueEvent(state);
            }
            d.CancelGesture();d.ClearPoints();camera.transform.position=start;
            foreach(var p in d.initialPoints)d.AddPoint(p);d.FlushChanges();
        }
    }

    public static async Task<object> MenuPreview() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        d.Toolbar.status.text="蓝色：凸包顶点    灰色：内部点    橙色外圈：选中点";
        d.Toolbar.status.color=new Color(.40f,.48f,.58f,1);
        d.OpenContextMenu(d.worldCamera.WorldToScreenPoint(d.Points[3].Position));
        Canvas.ForceUpdateCanvases();await Task.Delay(200);
        return new { selected=d.SelectedId,menu=d.IsMenuOpen,delete=d.Menu.HasDelete };
    }

    public static string RestoreViewSize() {
        var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
        var gt=group.GetType();int builtin=(int)gt.GetMethod("GetBuiltinCount").Invoke(group,null);
        var names=(string[])gt.GetMethod("GetDisplayTexts").Invoke(group,null);
        for(int i=names.Length-1;i>=builtin;i--)
            if(names[i].Contains("Hull verification"))gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
        int index=0;int total=(int)gt.GetMethod("GetTotalCount").Invoke(group,null);
        for(int i=0;i<total;i++) {
            var size=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});var type=size.GetType();
            if((int)type.GetProperty("width").GetValue(size)==1920&&(int)type.GetProperty("height").GetValue(size)==1080)index=i;
        }
        var gameType=assembly.GetType("UnityEditor.GameView");var window=EditorWindow.GetWindow(gameType);
        gameType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(window,index);
        window.Repaint();return "Removed temporary verification resolutions; restored original 1920x1080 view.";
    }
}

public static class HullVerificationExtensions {
    public static bool ViewForTestSelected(this ConvexHullDemoController.Point point) {
        var root=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>().pointsRoot;
        return root.Find("Point "+point.Id).GetComponent<HullPointView>().selectionRing.gameObject.activeSelf;
    }
}
