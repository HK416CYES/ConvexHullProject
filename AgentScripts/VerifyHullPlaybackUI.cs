using System;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
public static class VerifyHullPlaybackUI {
    static void Assert(bool ok,string text){if(!ok)throw new Exception(text);}
    static Vector2 UI(Component component) {var r=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(component.GetComponentInParent<Canvas>().rootCanvas.worldCamera,r.TransformPoint(r.rect.center));}
    static async Task Click(Mouse mouse,Vector2 at) {
        for(int i=0;i<2;i++) {
            using(StateEvent.From(mouse,out var state)){mouse.position.WriteValueIntoEvent(at,state);mouse.leftButton.WriteValueIntoEvent(i==0?1f:0f,state);InputSystem.QueueEvent(state);}
            InputSystem.Update();UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
        }
        await Task.Delay(120);
    }
    public static async Task<object> Run() {
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();Canvas.ForceUpdateCanvases();await Task.Delay(100);
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;
        p.Stop();bool help=d.IsHelpOpen;d.HideHelp();var cameraPosition=d.worldCamera.transform.position;float size=d.worldCamera.orthographicSize;
        bool originalEditorPause=EditorApplication.isPaused;
        EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(200);EditorApplication.isPaused=originalEditorPause;Canvas.ForceUpdateCanvases();
        var original=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>("HullPlaybackUIVerificationMouse");mouse.MakeCurrent();
        try {
            await Click(mouse,UI(d.Toolbar.playButton));Assert(p.IsActive,"Play button input");
            var panel=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>();Canvas.ForceUpdateCanvases();
            Assert(panel.card.GetComponentsInChildren<Text>(true).Length==6,"Only three button labels and dropdown text remain");
            Assert(panel.card.rect.width==460&&panel.card.rect.height==54,"Compact control row dimensions");
            bool paused=EditorApplication.isPaused;EditorApplication.isPaused=true;EditorApplication.Step();
            await Task.Delay(250);EditorApplication.isPaused=paused;Canvas.ForceUpdateCanvases();
            await Click(mouse,UI(panel.pauseButton));Assert(p.IsPaused,"Pause button input");
            int index=p.StepIndex;await Click(mouse,UI(panel.nextButton));p.Tick(1);Assert(p.StepIndex==index+1&&p.IsPaused,"Next button input");
            await Click(mouse,UI(panel.speedDropdown));Canvas.ForceUpdateCanvases();EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(200);EditorApplication.isPaused=false;
            var toggles=panel.speedDropdown.GetComponentsInChildren<Toggle>();Assert(toggles.Length==5,"Dropdown options visible");
            foreach(var t in panel.speedDropdown.GetComponentsInChildren<Transform>(true))
                Assert(t.gameObject.layer==5,"Dropdown list renders on UI layer: "+t.name);
            Assert((panel.GetComponentInParent<Canvas>().worldCamera.cullingMask&(1<<toggles[4].gameObject.layer))!=0,"Option layer visible to UI camera");
            await Click(mouse,UI(toggles[4]));Assert(p.Speed==4,"4x option input");EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(200);EditorApplication.isPaused=originalEditorPause;
            await Click(mouse,UI(panel.pauseButton));Assert(!p.IsPaused,"Resume button input");
            await Click(mouse,UI(panel.stopButton));Assert(!p.IsActive,"Stop button input");
            await Click(mouse,UI(d.Toolbar.playButton));Assert(p.IsActive,"Replay button input");p.Stop();
            return new{passed=true,tests="actual UI input: play, pause, next, dropdown 4x, resume, stop, replay"};
        } finally {
            p.Stop();EditorApplication.isPaused=originalEditorPause;InputSystem.RemoveDevice(mouse);if(original!=null)original.MakeCurrent();
            d.worldCamera.transform.position=cameraPosition;d.worldCamera.orthographicSize=size;if(help)d.ShowHelp();else d.HideHelp();
        }
    }
    public static async Task<string> PreviewVisual() {
        bool paused=EditorApplication.isPaused;
        try {
            EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(200);Canvas.ForceUpdateCanvases();
            var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.playback.Stop();d.HideHelp();
            if(!d.playback.Play(out var error))throw new Exception(error);d.playback.Pause();
            for(int i=0;i<18;i++){d.playback.NextStep();d.playback.Tick(1);}
            EditorApplication.Step();await Task.Delay(200);Canvas.ForceUpdateCanvases();
            var panel=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>();var c=panel.GetComponentInParent<Canvas>();
            var corners=new Vector3[4];d.Toolbar.toolbarRect.GetWorldCorners(corners);
            var left=RectTransformUtility.WorldToScreenPoint(c.worldCamera,corners[0]);var right=RectTransformUtility.WorldToScreenPoint(c.worldCamera,corners[2]);
            Assert(left.x>=0&&right.x<=d.worldCamera.pixelWidth,"Toolbar within viewport");
            panel.card.GetWorldCorners(corners);left=RectTransformUtility.WorldToScreenPoint(c.worldCamera,corners[0]);right=RectTransformUtility.WorldToScreenPoint(c.worldCamera,corners[2]);
            Assert(left.x>=0&&left.y>=0&&right.x<=d.worldCamera.pixelWidth,"Playback controls within viewport");
            return "Rendered teaching preview; toolbar and playback controls inside viewport.";
        } finally {EditorApplication.isPaused=paused;}
    }
    public static async Task<string> PopupPreview() {
        await PreviewVisual();
        var panel=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>();
        panel.speedDropdown.Show();Canvas.ForceUpdateCanvases();
        bool paused=EditorApplication.isPaused;
        try {
            for(int i=0;i<3;i++){EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(200);}
            Canvas.ForceUpdateCanvases();
            var toggles=panel.speedDropdown.GetComponentsInChildren<Toggle>();Assert(toggles.Length==5,"Five popup options");
            var canvas=panel.GetComponentInParent<Canvas>();
            foreach(var toggle in toggles) {
                Assert(toggle.gameObject.layer==5,"Popup option on UI layer");
                Vector2 screen=UI(toggle);Assert(canvas.worldCamera.pixelRect.Contains(screen),"Popup option inside screen");
            }
            return "Popup rendered with all five speed options inside the viewport on the UI layer.";
        } finally {EditorApplication.isPaused=paused;}
    }
    public static string RestoreView(int originalIndex) {
        var assembly=typeof(Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);
        var binding=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
        var group=sizesType.GetMethod("GetGroup",binding).Invoke(sizes,new[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});var gt=group.GetType();
        int built=(int)gt.GetMethod("GetBuiltinCount").Invoke(group,null),custom=(int)gt.GetMethod("GetCustomCount").Invoke(group,null);
        for(int i=built+custom-1;i>=built;i--) {
            var size=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
            string name=(string)size.GetType().GetProperty("baseText").GetValue(size);
            if(name.StartsWith("Hull verification "))gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{i});
        }
        type.GetProperty("selectedSizeIndex",binding).SetValue(view,originalIndex);view.Focus();
        return "Removed test resolutions and restored original view index "+originalIndex+".";
    }
}
