using System;
using System.Threading.Tasks;
using ConvexHullDemo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class VerifyCompactHullUI {
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static Vector2 UI(Component c){var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(c.GetComponentInParent<Canvas>().worldCamera,r.TransformPoint(r.rect.center));}
    static async Task Click(Vector2 p){
        for(int i=0;i<2;i++){
            using(StateEvent.From(Mouse.current,out var state)){Mouse.current.position.WriteValueIntoEvent(p,state);Mouse.current.leftButton.WriteValueIntoEvent(i==0?1f:0f,state);InputSystem.QueueEvent(state);}
            InputSystem.Update();UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            await Task.Delay(120);
        }
    }
    public static async Task<object> Run(){
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();await Task.Delay(200);
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        bool had=PlayerPrefs.HasKey(HullHelpPanel.PreferenceKey);int old=PlayerPrefs.GetInt(HullHelpPanel.PreferenceKey,0);
        bool wasOpen=d.IsHelpOpen;
        var originalMouse=Mouse.current;var testMouse=InputSystem.AddDevice<Mouse>("HullVerificationMouse");testMouse.MakeCurrent();
        try{
            Check(d.IsHelpOpen==HullHelpPanel.ShouldShowOnEntry,"Entry respects saved preference");
            if(!d.IsHelpOpen)d.ShowHelp();
            Canvas.ForceUpdateCanvases();await Task.Delay(100);
            var before=d.worldCamera.transform.position;
            var at=new Vector2(d.worldCamera.pixelWidth*.12f,d.worldCamera.pixelHeight*.30f);
            d.BeginLeft(at);d.MoveLeft(at+Vector2.right*80);d.EndLeft(at+Vector2.right*80);
            Check(d.worldCamera.transform.position==before,"Help modal blocks canvas drag");
            d.Help.doNotShow.SetIsOnWithoutNotify(false);
            await Click(UI(d.Help.doNotShow));
            Check(d.Help.doNotShow.isOn&&PlayerPrefs.GetInt(HullHelpPanel.PreferenceKey)==1,"Checkbox input saved preference");
            Check(!HullHelpPanel.ShouldShowOnEntry,"Subsequent entry suppressed");
            var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ConvexHullDemo/Prefabs/HullHelp.prefab"));
            try{var help=clone.GetComponent<HullHelpPanel>();help.Bind(d);Check(help.doNotShow.isOn,"Fresh panel reads persistent toggle");}finally{UnityEngine.Object.Destroy(clone);}
            await Click(UI(d.Help.closeButton));Check(!d.IsHelpOpen,"Close symbol input closes help");
            await Click(UI(d.Toolbar.helpButton));Check(d.IsHelpOpen&&d.Help.doNotShow.isOn,"Question input reopens despite preference");
            await Click(UI(d.Help.doNotShow));Check(HullHelpPanel.ShouldShowOnEntry,"Unchecking restores automatic help");
            await Click(UI(d.Help.closeButton));Check(!d.IsHelpOpen,"Help closes again");
            Check(d.pointDiameter>0&&d.hitRadius>=d.pointDiameter*.5f,"Larger point and hit area saved in scene");
            Check(d.Toolbar.toolbarRect.rect.height==64,"Compact toolbar height");
            Check(d.Toolbar.toolbarRect.GetComponentsInChildren<Text>().Length==10,"Only 3 stats, input and two buttons remain");
            var point=d.pointsRoot.GetChild(0);float radius=point.localScale.x;
            Vector2 center=d.worldCamera.WorldToScreenPoint(point.position);
            float actualDiameter=2*((Vector2)d.worldCamera.WorldToScreenPoint(point.position+Vector3.right*radius)-center).magnitude;
            Check(Mathf.Abs(radius*2-d.pointDiameter)<.0001f,"Point uses configured world diameter");
            d.OpenContextMenu(d.worldCamera.WorldToScreenPoint(d.Points[0].Position));
            Check(((RectTransform)d.Menu.transform).rect.width==160&&d.Menu.GetComponentsInChildren<Text>().Length==2,"Context contains compact action buttons only");
            d.CloseMenu();
            return new{passed=true,pointDiameter=actualDiameter,toolbarHeight=64,menuWidth=160,helpPreference="startup, toggle persistence, fresh panel, X, question, re-enable and modal isolation passed"};
        }finally{
            if(had)PlayerPrefs.SetInt(HullHelpPanel.PreferenceKey,old);else PlayerPrefs.DeleteKey(HullHelpPanel.PreferenceKey);
            PlayerPrefs.Save();d.Help.SyncPreference();d.CloseMenu();
            if(wasOpen)d.ShowHelp();else d.HideHelp();
            InputSystem.RemoveDevice(testMouse);if(originalMouse!=null)originalMouse.MakeCurrent();
        }
    }
    public static async Task<string> Preview(){
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.HideHelp();
        Vector2 at=d.worldCamera.WorldToScreenPoint(d.Points[3].Position);d.OpenContextMenu(at);
        Canvas.ForceUpdateCanvases();await Task.Delay(200);
        var rect=(RectTransform)d.Menu.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
        var canvas=d.Menu.GetComponentInParent<Canvas>();Vector2 topLeft=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,corners[1]);
        var view=d.worldCamera.pixelRect;
        var expected=new Vector2(Mathf.Clamp(at.x,view.xMin+8,view.xMax-rect.rect.width*canvas.scaleFactor-8),Mathf.Clamp(at.y,view.yMin+rect.rect.height*canvas.scaleFactor+8,view.yMax-8));
        Check(Vector2.Distance(topLeft,expected)<1,"Menu remains attached to click position after viewport/scaler update");
        return "Compact toolbar/menu preview; menu anchor verified after resolution change";
    }
    const string Backup="/private/tmp/convex-hull-help-preference-backup.txt";
    public static string ArmEntryTest(){
        if(System.IO.File.Exists(Backup))throw new Exception("Preference backup already exists; restore first.");
        System.IO.File.WriteAllText(Backup,(PlayerPrefs.HasKey(HullHelpPanel.PreferenceKey)?"1":"0")+","+PlayerPrefs.GetInt(HullHelpPanel.PreferenceKey,0));
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();d.ShowHelp();d.Help.doNotShow.isOn=true;
        Check(!HullHelpPanel.ShouldShowOnEntry,"Toggle saved for entry test");return "Preference backed up; suppress next scene entry.";
    }
    public static string VerifyEntryAndRestore(){
        var d=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        try{
            Check(!d.IsHelpOpen&&!HullHelpPanel.ShouldShowOnEntry,"Re-entering scene must not open help");
            d.Toolbar.helpButton.onClick.Invoke();Check(d.IsHelpOpen&&d.Help.doNotShow.isOn,"Question reopens on subsequent entry");
            return "Actual scene re-entry suppressed help; question reopened it. Original preference restored.";
        }finally{
            var old=System.IO.File.ReadAllText(Backup).Split(',');
            if(old[0]=="1")PlayerPrefs.SetInt(HullHelpPanel.PreferenceKey,int.Parse(old[1]));else PlayerPrefs.DeleteKey(HullHelpPanel.PreferenceKey);
            PlayerPrefs.Save();System.IO.File.Delete(Backup);d.Help.SyncPreference();
            if(HullHelpPanel.ShouldShowOnEntry)d.ShowHelp();else d.HideHelp();
        }
    }
}
