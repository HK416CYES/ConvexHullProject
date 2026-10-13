using System;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class VerifyHullSizes {
    static void Assert(bool condition,string name){if(!condition)throw new Exception(name);}
    public static async Task<object> Check() {
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        await Task.Delay(250);Canvas.ForceUpdateCanvases();await Task.Delay(100);
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        d.HideHelp();d.CloseMenu();
        var camera=d.worldCamera;var canvas=d.Toolbar.GetComponentInParent<Canvas>();
        var scaler=canvas.GetComponent<CanvasScaler>();
        Assert(scaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,"UI reference fit mode");
        float expected=Mathf.Min(camera.pixelWidth/1280f,camera.pixelHeight/720f);
        Assert(Mathf.Abs(canvas.scaleFactor-expected)<.001f,"UI unified reference scale");
        foreach(Transform point in d.pointsRoot)if(point.gameObject.activeSelf)
            Assert(Mathf.Abs(point.localScale.x*2-d.pointDiameter)<.0001f,"World point size invariant");
        Assert(Mathf.Abs(d.outline.widthMultiplier-d.outlineWidth)<.0001f,"World outline invariant");
        var grid=UnityEngine.Object.FindFirstObjectByType<InfiniteGrid>();
        var vertices=grid.meshFilter.sharedMesh.vertices;
        Assert(Mathf.Abs(vertices[1].x-vertices[0].x-grid.lineWidth)<.0001f,"World grid width invariant");
        var corners=new Vector3[4];d.Toolbar.toolbarRect.GetWorldCorners(corners);
        var uiCamera=canvas.worldCamera;
        var lower=uiCamera.WorldToScreenPoint(corners[0]);var upper=uiCamera.WorldToScreenPoint(corners[2]);
        Assert(lower.x>=-.1f&&upper.x<=camera.pixelWidth+.1f,"Toolbar contained in viewport");
        Assert(Mathf.Abs((upper.y-lower.y)/canvas.scaleFactor-64)<.01f,"Toolbar design height invariant");
        var p=d.AddPoint(new Vector2(100,100));d.FlushChanges();
        try {
            Assert(d.HitTest(camera.WorldToScreenPoint(p.Position+Vector2.right*d.hitRadius*.9f))==p.Id,"World hit radius inside");
            Assert(d.HitTest(camera.WorldToScreenPoint(p.Position+Vector2.right*d.hitRadius*1.1f))==null,"World hit radius outside");
            float old=d.pointDiameter;
            try{d.pointDiameter=.6f;d.ApplyVisualSizes();
                foreach(Transform point in d.pointsRoot)if(point.gameObject.activeSelf)
                    Assert(Mathf.Abs(point.localScale.x-.3f)<.0001f,"Inspector-configurable point diameter");
            }finally{d.pointDiameter=old;d.ApplyVisualSizes();}
        }finally{d.RemovePoint(p.Id);d.FlushChanges();}
        d.OpenContextMenu(new Vector2(camera.pixelWidth-1,1));await Task.Delay(100);
        ((RectTransform)d.Menu.transform).GetWorldCorners(corners);
        lower=uiCamera.WorldToScreenPoint(corners[0]);upper=uiCamera.WorldToScreenPoint(corners[2]);
        Assert(lower.x>=0&&lower.y>=0&&upper.x<=camera.pixelWidth&&upper.y<=camera.pixelHeight,"Menu edge fit");
        d.CloseMenu();
        return new{passed=true,resolution=new Vector2(camera.pixelWidth,camera.pixelHeight),worldDiameter=d.pointDiameter,outline=d.outlineWidth,grid=grid.lineWidth,uiScale=canvas.scaleFactor};
    }
}
