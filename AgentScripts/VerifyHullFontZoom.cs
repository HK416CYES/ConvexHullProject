using System;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
public static class VerifyHullFontZoom {
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task<object> Run() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        var c=d.worldCamera;var position=c.transform.position;float size=c.orthographicSize;
        bool help=d.IsHelpOpen,enabled=d.enabled;var originalMouse=Mouse.current;Mouse mouse=null;
        d.enabled=false;d.HideHelp();d.CloseMenu();Canvas.ForceUpdateCanvases();
        try {
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ConvexHullDemo/Fonts/GenSenRounded2-M.ttc");
            int textCount=0;
            foreach(var text in d.Toolbar.GetComponentInParent<Canvas>().GetComponentsInChildren<Text>(true)) {
                Assert(text.font==font,"All demo UI text uses requested font: "+text.name);textCount++;
            }
            font.RequestCharactersInTexture("点集凸包顶点面积随机生成清空操作说明不再显示?×✓",16);
            Assert(font.HasCharacter('点')&&font.HasCharacter('说'),"Chinese glyphs available");
            var at=new Vector2(c.pixelWidth*.62f,c.pixelHeight*.40f);
            int revision=d.HullRevision;var anchor=d.ScreenToWorld(at);
            d.Zoom(at,120);
            Assert(c.orthographicSize<size,"Wheel up zooms in");
            Assert(Vector2.Distance(anchor,d.ScreenToWorld(at))<.0001f,"Cursor world anchor stable");
            d.Zoom(at,-120);Assert(Mathf.Abs(c.orthographicSize-size)<.0001f,"Reverse wheel zooms out");
            d.Zoom(at,12000);Assert(Mathf.Abs(c.orthographicSize-d.minZoomSize)<.0001f,"Minimum zoom bound");
            d.Zoom(at,-12000);Assert(Mathf.Abs(c.orthographicSize-d.maxZoomSize)<.0001f,"Maximum zoom bound");
            Assert(d.HullRevision==revision,"Zoom does not recompute hull");
            c.transform.position=position;c.orthographicSize=size;
            var r=d.Toolbar.toolbarRect;var ui=d.Toolbar.GetComponentInParent<Canvas>().worldCamera;
            var over=RectTransformUtility.WorldToScreenPoint(ui,r.TransformPoint(r.rect.center));
            d.Zoom(over,120);Assert(c.orthographicSize==size,"UI blocks zoom");
            d.ShowHelp();d.Zoom(at,120);Assert(c.orthographicSize==size,"Help blocks zoom");d.HideHelp();
            d.BeginLeft(at);d.Zoom(at,120);Assert(c.orthographicSize==size,"Active drag blocks zoom");d.CancelGesture();
            d.OpenContextMenu(at);d.Zoom(new Vector2(c.pixelWidth*.3f,c.pixelHeight*.3f),120);
            Assert(!d.IsMenuOpen,"Canvas zoom dismisses stale menu");
            c.transform.position=position;c.orthographicSize=size;
            mouse=InputSystem.AddDevice<Mouse>("HullZoomVerificationMouse");mouse.MakeCurrent();
            using(StateEvent.From(mouse,out var state)){
                mouse.position.WriteValueIntoEvent(at,state);mouse.scroll.WriteValueIntoEvent(new Vector2(0,120),state);InputSystem.QueueEvent(state);
            }
            InputSystem.Update();d.SendMessage("Update");
            Assert(c.orthographicSize<size,"Actual scroll input reaches zoom through Update");
            var p=d.AddPoint(new Vector2(c.transform.position.x,c.transform.position.y));d.FlushChanges();
            try {
                Vector2 projected=c.WorldToScreenPoint(p.Position);Assert(d.HitTest(projected)==p.Id,"Hit testing after zoom");
                Vector2 original=p.Position;d.Select(p.Id);d.BeginLeft(projected);d.MoveLeft(projected+Vector2.right*40);d.EndLeft(projected+Vector2.right*40);
                Assert(Mathf.Abs(p.Position.x-original.x-40*2*c.orthographicSize/c.pixelHeight)<.0001f,"Drag displacement matches zoomed world units");
                Assert(d.Points[d.Points.Count-1].Position.x>c.transform.position.x,"Selected point dragging after zoom");
            }finally{d.RemovePoint(p.Id);d.FlushChanges();}
            await Task.Delay(100);
            return new{passed=true,textCount,min=d.minZoomSize,max=d.maxZoomSize,tests="font, bounds, anchored zoom, scroll input, UI isolation, drag and hit testing"};
        } finally {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(originalMouse!=null)originalMouse.MakeCurrent();
            c.transform.position=position;c.orthographicSize=size;d.CancelGesture();d.CloseMenu();d.enabled=enabled;
            if(help)d.ShowHelp();else d.HideHelp();
        }
    }
}
