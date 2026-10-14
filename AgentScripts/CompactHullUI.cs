using System;
using ConvexHullDemo;
using UIFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CompactHullUI {
    const string Root="Assets/ConvexHullDemo";
    static Font font;
    static Color Ink=new Color(.12f,.21f,.33f),Muted=new Color(.40f,.48f,.58f),Blue=new Color(.12f,.39f,.81f);
    public static string Apply() {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/ConvexHullDemo.unity"||scene.isDirty)throw new Exception("Expected saved demo scene; preserve existing unsaved edits.");
        font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/GenSenRounded2-M.ttc");
        Toolbar();Menu();var help=Help();
        var setting=AssetDatabase.LoadAssetAtPath<UISetting>(Root+"/HullUISetting.asset");
        var so=new SerializedObject(setting);var screens=so.FindProperty("screensToRegister");
        bool exists=false;for(int i=0;i<screens.arraySize;i++)if(screens.GetArrayElementAtIndex(i).objectReferenceValue==help)exists=true;
        if(!exists){screens.arraySize++;screens.GetArrayElementAtIndex(screens.arraySize-1).objectReferenceValue=help;}
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(setting);
        var demo=Object.FindFirstObjectByType<ConvexHullDemoController>();demo.ApplyVisualSizes();
        
        foreach(var view in demo.previewPoints)view.transform.localScale=Vector3.one*demo.pointDiameter*.5f;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved compact 1040x64 toolbar, 160px context menu, world-unit points/hit radius, help panel and persistent toggle.";
    }
    static void ClearChildren(Transform root){for(int i=root.childCount-1;i>=0;i--)Object.DestroyImmediate(root.GetChild(i).gameObject);}
    static void Toolbar(){
        string path=Root+"/Prefabs/HullToolbar.prefab";var go=PrefabUtility.LoadPrefabContents(path);
        try{
            ClearChildren(go.transform);var root=(RectTransform)go.transform;Stretch(root);
            var panel=go.GetComponent<HullToolbarPanel>();
            var bar=Rect("Toolbar",root);bar.anchorMin=bar.anchorMax=new Vector2(.5f,1);bar.pivot=new Vector2(.5f,1);
            bar.anchoredPosition=new Vector2(0,-12);bar.sizeDelta=new Vector2(1040,64);Image(bar,Color.white);Border(bar);
            panel.toolbarRect=bar;
            panel.pointCount=Metric(bar,"点集",18,120);
            panel.hullCount=Metric(bar,"凸包顶点",145,130);
            panel.area=Metric(bar,"面积",285,200);
            var input=Box("Random count",bar,500,17,90,30,new Color(.94f,.96f,.99f));
            panel.countInput=input.gameObject.AddComponent<InputField>();
            panel.countInput.textComponent=Text("Value",input,"20",8,0,74,30,15,Ink);
            panel.countInput.placeholder=Text("Placeholder",input,"1–1000",8,0,74,30,13,Muted);
            panel.countInput.contentType=InputField.ContentType.IntegerNumber;panel.countInput.characterLimit=8;
            panel.countInput.targetGraphic=input.GetComponent<Image>();panel.countInput.text="20";
            panel.randomButton=Button("Random",bar,"随机生成",605,17,125,30,Blue,Color.white);
            panel.clearButton=Button("Clear",bar,"清空点集",745,17,125,30,new Color(.92f,.95f,.98f),Ink);
            panel.playButton=Button("Play algorithm",bar,"播放演示",890,17,125,30,Blue,Color.white);
            panel.playbackMessage=Text("Playback message",root,"",0,82,1040,24,14,new Color(.8f,.2f,.16f),TextAnchor.MiddleCenter);
            var message=panel.playbackMessage.rectTransform;message.anchorMin=message.anchorMax=new Vector2(.5f,1);message.pivot=new Vector2(.5f,1);message.anchoredPosition=new Vector2(0,-82);
            panel.helpButton=Button("Help",root,"?",0,0,36,36,Color.white,Blue);
            var question=(RectTransform)panel.helpButton.transform;question.anchorMin=question.anchorMax=Vector2.zero;
            question.pivot=Vector2.zero;question.anchoredPosition=new Vector2(16,16);Border(question);
            var qt=question.GetComponentInChildren<Text>();qt.fontSize=22;
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }finally{PrefabUtility.UnloadPrefabContents(go);}
    }
    static void Menu(){
        string path=Root+"/Prefabs/HullContextMenu.prefab";var go=PrefabUtility.LoadPrefabContents(path);
        try{
            ClearChildren(go.transform);var root=(RectTransform)go.transform;root.sizeDelta=new Vector2(160,72);
            var panel=go.GetComponent<HullContextMenuPanel>();
            panel.createButton=Button("Create",root,"新建点",5,5,150,28,new Color(.93f,.96f,1),Blue);
            panel.deleteButton=Button("Delete",root,"删除点",5,39,150,28,new Color(1,.95f,.94f),new Color(.77f,.24f,.18f));
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }finally{PrefabUtility.UnloadPrefabContents(go);}
    }
    static GameObject Help(){
        var root=Rect(ConvexHullDemoController.HelpId,null);Stretch(root);
        Image(root,new Color(.10f,.16f,.24f,.20f));
        var panel=root.gameObject.AddComponent<HullHelpPanel>();
        var card=Rect("Help dialog",root);card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);
        card.sizeDelta=new Vector2(520,400);card.anchoredPosition=Vector2.zero;Image(card,Color.white);Border(card);
        Text("Title",card,"操作说明",24,16,400,44,22,Ink);
        panel.closeButton=Button("Close",card,"×",470,14,36,36,new Color(.94f,.96f,.99f),Muted);
        panel.closeButton.GetComponentInChildren<Text>().fontSize=20;
        string body="左键单击点：选中，橙色外圈表示选中。\n\n拖动选中的点：移动点并实时更新凸包。\n在其他位置拖动：平移无限画布。\n滚轮：以鼠标位置为中心缩放画布。\n\n右键：新建点；点上右键还可删除。\n随机生成：按输入数量替换现有点集。\n\n蓝色：凸包顶点　灰色：内部点";
        Text("Instructions",card,body,24,76,472,240,16,Ink,TextAnchor.UpperLeft);
        var toggleRect=At("Do not show again",card,24,344,430,32);Image(toggleRect,Color.clear);panel.doNotShow=toggleRect.gameObject.AddComponent<Toggle>();
        var square=Box("Box",toggleRect,0,6,20,20,new Color(.90f,.94f,1));
        var check=Text("Check",square,"✓",0,-1,20,24,15,Blue,TextAnchor.MiddleCenter);
        panel.doNotShow.targetGraphic=square.GetComponent<Image>();panel.doNotShow.graphic=check;
        Text("Label",toggleRect,"不再显示",32,0,350,32,15,Muted);
        panel.doNotShow.isOn=false;
        var asset=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/HullHelp.prefab");
        Object.DestroyImmediate(root.gameObject);return asset;
    }
    static Text Metric(RectTransform parent,string label,float x,float width){
        Text(label+" label",parent,label,x,5,width,20,12,Muted);
        return Text(label+" value",parent,"0",x,25,width,34,22,Ink);
    }
    static RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;if(parent!=null)r.SetParent(parent,false);return r;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static RectTransform At(string name,Transform parent,float x,float y,float w,float h){var r=Rect(name,parent);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Image Image(RectTransform r,Color color){var i=r.gameObject.AddComponent<Image>();i.color=color;return i;}
    static void Border(RectTransform r){var edge=r.gameObject.AddComponent<UnityEngine.UI.Outline>();edge.effectColor=new Color(.66f,.74f,.83f,.5f);edge.effectDistance=new Vector2(1,-1);}
    static RectTransform Box(string name,Transform p,float x,float y,float w,float h,Color c){var r=At(name,p,x,y,w,h);Image(r,c);return r;}
    static Text Text(string name,Transform p,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft){
        var r=At(name,p,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;
        t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
    }
    static Button Button(string name,Transform p,string label,float x,float y,float w,float h,Color background,Color foreground){
        var r=Box(name,p,x,y,w,h,background);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
        var colors=b.colors;colors.highlightedColor=new Color(.87f,.92f,1);colors.pressedColor=new Color(.72f,.82f,.97f);b.colors=colors;
        Text("Label",r,label,0,0,w,h,14,foreground,TextAnchor.MiddleCenter);return b;
    }
}
