using System;
using ConvexHullDemo;
using UIFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class BuildHullPlayback {
    const string Root="Assets/ConvexHullDemo";
    static Font font;
    static Color Ink=new Color(.12f,.21f,.33f),Blue=new Color(.12f,.39f,.81f);
    public static string Apply() {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/ConvexHullDemo.unity")throw new Exception("Expected stopped demo scene.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scenes before authoring.");
        font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/GenSenRounded2-M.ttc");
        Toolbar();var label=Label();var panel=Panel(label);
        var setting=AssetDatabase.LoadAssetAtPath<UISetting>(Root+"/HullUISetting.asset");
        var so=new SerializedObject(setting);var screens=so.FindProperty("screensToRegister");bool exists=false;
        for(int i=0;i<screens.arraySize;i++)if(screens.GetArrayElementAtIndex(i).objectReferenceValue==panel)exists=true;
        if(!exists){screens.arraySize++;screens.GetArrayElementAtIndex(screens.arraySize-1).objectReferenceValue=panel;}
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(setting);
        var demo=Object.FindFirstObjectByType<ConvexHullDemoController>();
        var player=demo.GetComponent<HullAlgorithmPlayback>();if(player==null)player=demo.gameObject.AddComponent<HullAlgorithmPlayback>();
        demo.playback=player;
        var asset=Overlay();
        if(player.overlay==null){var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);instance.transform.SetParent(demo.transform,false);player.overlay=instance.GetComponent<HullTeachingOverlay>();}
        player.overlay.linePrefab=asset.GetComponent<HullTeachingOverlay>().linePrefab;EditorUtility.SetDirty(player.overlay);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved standalone teaching overlay, play toolbar, playback UI panel, label prefab and scene bindings.";
    }
    public static string RefreshPanel() {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before updating panel asset.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/ConvexHullDemo.unity")throw new Exception("Expected demo scene.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scenes before authoring.");
        font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/GenSenRounded2-M.ttc");
        var label=AssetDatabase.LoadAssetAtPath<Text>(Root+"/Prefabs/HullStepLabel.prefab");
        Panel(label);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return "Saved 460x54 playback controls only; dropdown template and all descendants use UI layer.";
    }
    static void Toolbar() {
        string path=Root+"/Prefabs/HullToolbar.prefab";var go=PrefabUtility.LoadPrefabContents(path);
        try {
            var p=go.GetComponent<HullToolbarPanel>();p.toolbarRect.sizeDelta=new Vector2(1240,64);
            if(p.playButton==null)p.playButton=Button("Play algorithm",p.toolbarRect,"播放演示",890,17,125,30);
            if(p.playbackMessage==null){var t=Text("Playback message",go.transform,"",0,0,1040,24,14);p.playbackMessage=t;
                t.color=new Color(.8f,.2f,.16f);var r=t.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-82);t.alignment=TextAnchor.MiddleCenter;}
            p.playButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(1090,-17);
            if(p.algorithmDropdown==null){var dd=DefaultControls.CreateDropdown(new DefaultControls.Resources());dd.name="Teaching algorithm";dd.transform.SetParent(p.toolbarRect,false);p.algorithmDropdown=dd.GetComponent<Dropdown>();}
            var dropdown=p.algorithmDropdown;var rect=dropdown.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(884,-17);rect.sizeDelta=new Vector2(192,30);
            dropdown.ClearOptions();dropdown.AddOptions(new System.Collections.Generic.List<string>(TeachingAlgorithmRegistry.Names));dropdown.value=0;dropdown.alphaFadeSpeed=0;
            foreach(var child in dropdown.GetComponentsInChildren<Transform>(true))child.gameObject.layer=5;
            foreach(var text in dropdown.GetComponentsInChildren<Text>(true)){text.font=font;text.fontSize=14;text.color=Ink;}
            dropdown.captionText.rectTransform.offsetMin=new Vector2(8,0);dropdown.captionText.rectTransform.offsetMax=new Vector2(-24,0);
            var arrow=dropdown.transform.Find("Arrow");arrow.GetComponent<Image>().enabled=false;
            if(arrow.Find("Arrow symbol")==null){var t=Text("Arrow symbol",arrow,"▼",0,0,20,20,12);t.alignment=TextAnchor.MiddleCenter;}
            dropdown.GetComponent<Image>().color=new Color(.92f,.95f,.99f);
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }finally{PrefabUtility.UnloadPrefabContents(go);}
    }
    static Text Label() {
        var t=Text("HullStepLabel",null,"1",0,0,80,24,14);t.color=Blue;t.alignment=TextAnchor.MiddleLeft;
        t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(.5f,.5f);t.rectTransform.pivot=new Vector2(0,.5f);
        var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=Color.white;shadow.effectDistance=new Vector2(1,-1);
        var asset=PrefabUtility.SaveAsPrefabAsset(t.gameObject,Root+"/Prefabs/HullStepLabel.prefab");Object.DestroyImmediate(t.gameObject);return asset.GetComponent<Text>();
    }
    static GameObject Panel(Text label) {
        var root=Rect(HullAlgorithmPlayback.PanelId,null);Stretch(root);var p=root.gameObject.AddComponent<HullPlaybackPanel>();
        p.labelsRoot=Rect("Number labels",root);Stretch(p.labelsRoot);p.labelPrefab=label;
        var card=Rect("Playback controls",root);card.anchorMin=card.anchorMax=new Vector2(.5f,0);card.pivot=new Vector2(.5f,0);card.sizeDelta=new Vector2(460,54);card.anchoredPosition=new Vector2(0,12);
        var image=card.gameObject.AddComponent<Image>();image.color=Color.white;p.card=card;
        p.pauseButton=Button("Pause",card,"暂停",12,12,90,30);
        p.nextButton=Button("Next step",card,"下一步",112,12,90,30);
        p.stopButton=Button("Stop",card,"停止",212,12,90,30);
        var dd=DefaultControls.CreateDropdown(new DefaultControls.Resources());dd.name="Playback speed";dd.layer=5;dd.transform.SetParent(card,false);
        var r=dd.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(312,-12);r.sizeDelta=new Vector2(136,30);
        p.speedDropdown=dd.GetComponent<Dropdown>();p.speedDropdown.ClearOptions();p.speedDropdown.AddOptions(new System.Collections.Generic.List<string>{"0.25×","0.5×","1×","2×","4×"});p.speedDropdown.value=2;p.speedDropdown.alphaFadeSpeed=0;
        foreach(var t in dd.GetComponentsInChildren<Text>(true)){t.font=font;t.fontSize=14;t.color=Ink;}
        dd.GetComponent<Image>().color=new Color(.92f,.95f,.99f);
        var caption=p.speedDropdown.captionText;caption.rectTransform.offsetMin=new Vector2(8,0);caption.rectTransform.offsetMax=new Vector2(-24,0);
        var arrow=dd.transform.Find("Arrow");arrow.GetComponent<Image>().enabled=false;
        var arrowText=Text("Arrow symbol",arrow,"▼",0,0,20,20,12);arrowText.alignment=TextAnchor.MiddleCenter;
        // DefaultControls 的默认子对象在 Default 层。列表展开会创建独立 Canvas，必须全部归入 UI 层。
        foreach(var child in dd.GetComponentsInChildren<Transform>(true))child.gameObject.layer=5;
        var asset=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/HullPlayback.prefab");Object.DestroyImmediate(root.gameObject);return asset;
    }
    static GameObject Overlay() {
        string path=Root+"/Materials/TeachingOverlay.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("ConvexHullDemo/CanvasUnlit")){name="Teaching overlay",renderQueue=3040};AssetDatabase.CreateAsset(material,path);}
        var root=new GameObject("Algorithm teaching overlay");var o=root.AddComponent<HullTeachingOverlay>();
        o.lower=Line("Lower chain",root.transform,material);o.upper=Line("Upper chain",root.transform,material);
        o.comparison=Line("Turn comparison",root.transform,material);o.transition=Line("Animated edge",root.transform,material);
        var template=Line("HullTeachingLine",null,material);var lineAsset=PrefabUtility.SaveAsPrefabAsset(template.gameObject,Root+"/Prefabs/HullTeachingLine.prefab");Object.DestroyImmediate(template.gameObject);o.linePrefab=lineAsset.GetComponent<LineRenderer>();
        var marker=Ring("HullTeachingMarker",null,material);
        var markerAsset=PrefabUtility.SaveAsPrefabAsset(marker.gameObject,Root+"/Prefabs/HullTeachingMarker.prefab");Object.DestroyImmediate(marker.gameObject);
        var candidate=(GameObject)PrefabUtility.InstantiatePrefab(markerAsset,root.transform);candidate.name="Candidate marker";o.candidateRing=candidate.GetComponent<MeshRenderer>();o.confirmedMarkerPrefab=markerAsset.GetComponent<MeshRenderer>();o.triangleFillPrefab=TriangleFill();
        var removed=(GameObject)PrefabUtility.InstantiatePrefab(markerAsset,root.transform);removed.name="Removed marker";o.removedRing=removed.GetComponent<MeshRenderer>();
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/HullTeachingOverlay.prefab");Object.DestroyImmediate(root);return asset;
    }
    public static string RefreshQuickHullAssets() {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/ConvexHullDemo.unity")throw new Exception("Expected stopped demo scene");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scenes");
        var marker=AssetDatabase.LoadAssetAtPath<MeshRenderer>(Root+"/Prefabs/HullTeachingMarker.prefab");var fill=TriangleFill();
        var path=Root+"/Prefabs/HullTeachingOverlay.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{var overlay=root.GetComponent<HullTeachingOverlay>();overlay.confirmedMarkerPrefab=marker;overlay.triangleFillPrefab=fill;PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var live=Object.FindFirstObjectByType<HullTeachingOverlay>();live.confirmedMarkerPrefab=marker;live.triangleFillPrefab=fill;EditorUtility.SetDirty(live);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved QuickHull triangle mesh/material/prefab and confirmed-point marker bindings";
    }
    static MeshRenderer TriangleFill() {
        string meshPath=Root+"/Meshes/QuickHullTriangle.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){mesh=new Mesh{name="QuickHull triangle template"};mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};mesh.triangles=new[]{0,1,2};mesh.colors=new[]{Color.white,Color.white,Color.white};mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
        string materialPath=Root+"/Materials/QuickHullTriangle.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("ConvexHullDemo/CanvasUnlit")){name="QuickHull triangle fill",renderQueue=3025};AssetDatabase.CreateAsset(material,materialPath);}
        var root=new GameObject("HullQuickTriangleFill");root.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;root.SetActive(false);
        var asset=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/HullQuickTriangleFill.prefab");Object.DestroyImmediate(root);return asset.GetComponent<MeshRenderer>();
    }
    static LineRenderer Line(string name,Transform parent,Material material) {
        var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;
        line.useWorldSpace=true;line.positionCount=0;line.widthMultiplier=.075f;line.numCapVertices=line.numCornerVertices=6;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;return line;
    }
    static MeshRenderer Ring(string name,Transform parent,Material material) {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/SelectionRing.asset");
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;go.SetActive(false);return r;
    }
    static RectTransform Rect(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;if(parent!=null)r.SetParent(parent,false);return r;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static RectTransform At(string name,Transform parent,float x,float y,float w,float h){var r=Rect(name,parent);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static Text Text(string name,Transform parent,string content,float x,float y,float w,float h,int size){var r=At(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=content;t.color=Ink;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;return t;}
    static Button Button(string name,Transform parent,string caption,float x,float y,float w,float h){var r=At(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=Blue;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var t=Text("Label",r,caption,0,0,w,h,14);t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;return b;}
}
