using System;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
public static class VerifyMultiHullUI {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Vector2 Screen(Component c){var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(c.GetComponentInParent<Canvas>().rootCanvas.worldCamera,r.TransformPoint(r.rect.center));}
    static async Task StepFrame(){bool old=EditorApplication.isPaused;EditorApplication.isPaused=true;EditorApplication.Step();await Task.Delay(180);EditorApplication.isPaused=old;Canvas.ForceUpdateCanvases();}
    static async Task Click(Mouse mouse,Component c){var at=Screen(c);for(int i=0;i<2;i++){
        using(StateEvent.From(mouse,out var e)){mouse.position.WriteValueIntoEvent(at,e);mouse.leftButton.WriteValueIntoEvent(i==0?1f:0f,e);InputSystem.QueueEvent(e);}
        InputSystem.Update();UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();}await Task.Delay(80);}
    public static async Task<string> Run() {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();var p=d.playback;p.Stop();
        bool help=d.IsHelpOpen,paused=EditorApplication.isPaused;var selected=p.SelectedAlgorithm;var cam=d.worldCamera.transform.position;float zoom=d.worldCamera.orthographicSize;
        d.HideHelp();EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await StepFrame();
        var old=Mouse.current;var mouse=InputSystem.AddDevice<Mouse>("MultiHullUIVerificationMouse");mouse.MakeCurrent();
        try {
            Check(d.Toolbar.toolbarRect.rect.width==1240&&d.Toolbar.toolbarRect.rect.height==64,"1240x64 toolbar");
            var dropdown=d.Toolbar.algorithmDropdown;Check(dropdown.options.Count==5,"Five algorithms");
            for(int i=0;i<5;i++) {
                int revision=d.HullRevision;double area=d.HullArea;var cameraBefore=d.worldCamera.transform.position;float sizeBefore=d.worldCamera.orthographicSize;
                await Click(mouse,dropdown);await StepFrame();
                var items=dropdown.GetComponentsInChildren<Toggle>();Check(items.Length==5,"Algorithm dropdown actually opens");
                foreach(var child in dropdown.GetComponentsInChildren<Transform>(true))Check(child.gameObject.layer==5,"UI layer "+child.name);
                foreach(var item in items)Check(d.worldCamera.pixelRect.Contains(Screen(item)),"All options on screen");
                await Click(mouse,items[i]);await StepFrame();
                Check((int)p.SelectedAlgorithm==i&&dropdown.value==i,"Selected option matches algorithm "+i);
                Check(d.HullRevision==revision&&d.HullArea==area&&d.worldCamera.transform.position==cameraBefore&&d.worldCamera.orthographicSize==sizeBefore,"Selection leaves realtime hull and camera unchanged");
                await Click(mouse,d.Toolbar.playButton);await StepFrame();Check(p.IsActive&&(int)p.ActiveAlgorithm==i,"Play runs selected algorithm");
                Check(!dropdown.interactable,"Algorithm dropdown locked while running");
                var panel=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>();
                await Click(mouse,panel.pauseButton);Check(p.IsPaused&&!dropdown.interactable,"Paused algorithm locked");
                int index=p.StepIndex;long comparisons=p.CurrentTrace.Comparisons;p.Tick(10);Check(index==p.StepIndex&&comparisons==p.CurrentTrace.Comparisons,"Pause stops computation and timer");
                await Click(mouse,panel.nextButton);p.Tick(1);p.Tick(1);Check(p.StepIndex==index+1&&p.IsPaused&&p.CanStep,"Single visible step then paused");
                await Click(mouse,panel.speedDropdown);await StepFrame();var speeds=panel.speedDropdown.GetComponentsInChildren<Toggle>();Check(speeds.Length==5,"Speed dropdown opens");
                await Click(mouse,speeds[3]);await StepFrame();Check(p.Speed==2,"Speed option works");
                await Click(mouse,panel.pauseButton);Check(!p.IsPaused,"Resume");await Click(mouse,panel.stopButton);
                Check(!p.IsActive&&dropdown.interactable&&dropdown.value==i&&p.overlay.ActiveGroupCount==0,"Stop cleans overlay and retains selection");
                Check(d.HullRevision==revision&&d.HullArea==area,"Playback leaves realtime statistics unchanged");
            }
            return "PASS actual dropdown selection/play/pause/single-step/speed/resume/stop for all five algorithms at "+d.worldCamera.pixelWidth+"x"+d.worldCamera.pixelHeight;
        }finally {p.Stop();p.SelectAlgorithm(selected);InputSystem.RemoveDevice(mouse);if(old!=null)old.MakeCurrent();EditorApplication.isPaused=paused;d.worldCamera.transform.position=cam;d.worldCamera.orthographicSize=zoom;if(help)d.ShowHelp();else d.HideHelp();}
    }
    public static async Task<string> Popup(bool speed) {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.HideHelp();
        Dropdown dropdown;
        if(speed){await Preview(4,45);dropdown=UnityEngine.Object.FindFirstObjectByType<HullPlaybackPanel>().speedDropdown;}
        else {d.playback.Stop();dropdown=d.Toolbar.algorithmDropdown;}
        dropdown.Show();await StepFrame();await StepFrame();
        var items=dropdown.GetComponentsInChildren<Toggle>();Check(items.Length==5,"All popup items visible");
        foreach(var item in items)Check(d.worldCamera.pixelRect.Contains(Screen(item)),"Popup option inside viewport");
        return "Rendered five "+(speed?"speed":"algorithm")+" options";
    }
    public static async Task<string> Preview(int algorithm,int visibleSteps) {
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.playback.Stop();d.HideHelp();d.playback.SelectAlgorithm((TeachingAlgorithmKind)algorithm);
        if(!d.playback.Play(out var error))throw new Exception(error);d.playback.Pause();
        for(int i=0;i<visibleSteps&&d.playback.IsActive;i++){d.playback.NextStep();d.playback.Tick(1);d.playback.Tick(1);}
        bool old=EditorApplication.isPaused;await StepFrame();EditorApplication.isPaused=old;return d.playback.Phase;
    }
}
