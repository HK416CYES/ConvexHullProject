using System;
using System.Collections.Generic;
using ConvexHullDemo;
using UIFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildConvexHullDemo {
    private const string Root = "Assets/ConvexHullDemo";
    private static readonly Color Ink = new Color(.12f,.21f,.33f,1);
    private static readonly Color Muted = new Color(.40f,.48f,.58f,1);
    private static readonly Color Blue = new Color(.12f,.39f,.81f,1);
    private static Font font;

    public static string Build() {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Existing scene has unsaved edits; preserve it before authoring.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/ConvexHullDemo.unity") != null)
            throw new InvalidOperationException("Demo scene already exists; refusing to overwrite.");
        Folder(Root); Folder(Root + "/Materials"); Folder(Root + "/Meshes"); Folder(Root + "/Prefabs");
        font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/ArialUnicode.ttf");
        if (font == null) throw new Exception("Chinese font has not been imported.");
        var shader = Shader.Find("ConvexHullDemo/CanvasUnlit");
        if (shader == null || !shader.isSupported) throw new Exception("Canvas shader unavailable.");
        var pointMaterial = Material("Point", shader, Color.white, 3030);
        var lineMaterial = Material("Outline", shader, Color.white, 3020);
        var fillMaterial = Material("Fill", shader, new Color(.18f,.49f,.93f,.12f), 3010);
        var gridMaterial = Material("Grid", shader, Color.white, 3000);
        var disk = Disk(false); var ring = Disk(true);
        AssetDatabase.CreateAsset(disk, Root + "/Meshes/PointDisk.asset");
        AssetDatabase.CreateAsset(ring, Root + "/Meshes/SelectionRing.asset");

        // Author the point once; runtime instances represent player-created data.
        var pointGO = new GameObject("HullPoint");
        var point = pointGO.AddComponent<HullPointView>();
        point.dot = MeshObject("Dot", pointGO.transform, disk, pointMaterial, 0);
        point.selectionRing = MeshObject("Selection ring", pointGO.transform, ring, pointMaterial, -.02f);
        point.selectionRing.transform.localScale = Vector3.one * 1.8f;
        point.selectionRing.gameObject.SetActive(false);
        var pointAsset = PrefabUtility.SaveAsPrefabAsset(pointGO, Root + "/Prefabs/HullPoint.prefab");
        Object.DestroyImmediate(pointGO);

        MakeUIFrame();
        var toolbar = MakeToolbar(); var context = MakeMenu();
        var setting = ScriptableObject.CreateInstance<UISetting>();
        var serialized = new SerializedObject(setting);
        serialized.FindProperty("templateUIPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UIFrame>(Root + "/Prefabs/HullUIFrame.prefab");
        var screens = serialized.FindProperty("screensToRegister"); screens.arraySize = 2;
        screens.GetArrayElementAtIndex(0).objectReferenceValue = toolbar;
        screens.GetArrayElementAtIndex(1).objectReferenceValue = context;
        serialized.FindProperty("deactivateScreenGOs").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(setting, Root + "/HullUISetting.asset");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraGO = new GameObject("Main Camera"); cameraGO.tag = "MainCamera";
        var camera = cameraGO.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6;
        camera.transform.position = new Vector3(0,.7f,-10); camera.nearClipPlane = .1f; camera.farClipPlane = 100;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.97f,.98f,.995f);
        camera.cullingMask = ~(1 << 5); camera.allowHDR = false;
        cameraGO.AddComponent<AudioListener>();
        var additional = camera.GetUniversalAdditionalCameraData(); additional.renderPostProcessing = false;
        additional.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        var lightGO = new GameObject("Directional Light"); var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1; light.shadows = LightShadows.None;
        lightGO.transform.rotation = Quaternion.Euler(50,-30,0);

        var demoGO = new GameObject("Convex Hull Demo"); var demo = demoGO.AddComponent<ConvexHullDemoController>();
        demo.worldCamera = camera; demo.uiSetting = setting; demo.pointPrefab = pointAsset.GetComponent<HullPointView>();
        demo.pointsRoot = new GameObject("Points").transform; demo.pointsRoot.SetParent(demoGO.transform);
        var outlineGO = new GameObject("Hull outline"); outlineGO.transform.SetParent(demoGO.transform);
        demo.outline = outlineGO.AddComponent<LineRenderer>(); demo.outline.sharedMaterial = lineMaterial;
        demo.outline.useWorldSpace = true; demo.outline.numCornerVertices = 6; demo.outline.numCapVertices = 6;
        demo.outline.shadowCastingMode = ShadowCastingMode.Off; demo.outline.receiveShadows = false;
        demo.outline.widthMultiplier = .035f;
        var fillGO = new GameObject("Hull fill"); fillGO.transform.SetParent(demoGO.transform);
        demo.fill = fillGO.AddComponent<MeshFilter>(); demo.fillRenderer = fillGO.AddComponent<MeshRenderer>();
        demo.fillRenderer.sharedMaterial = fillMaterial; demo.fillRenderer.shadowCastingMode = ShadowCastingMode.Off;
        var initialHull = ConvexHull2D.Compute(demo.initialPoints);
        var initialFill = FillMesh(initialHull); AssetDatabase.CreateAsset(initialFill, Root + "/Meshes/InitialHull.asset");
        demo.fill.sharedMesh = initialFill; fillGO.transform.position = new Vector3(0,0,.15f);
        demo.outline.positionCount = initialHull.Count; demo.outline.loop = true;
        demo.outline.startColor = demo.outline.endColor = demo.hullColor;
        for (int i = 0; i < initialHull.Count; i++) demo.outline.SetPosition(i, new Vector3(initialHull[i].x,initialHull[i].y,.05f));
        var hullSet = new HashSet<Vector2>(initialHull);
        foreach (var position in demo.initialPoints) {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(pointAsset, scene);
            instance.name = "Initial point " + demo.previewPoints.Count;
            instance.transform.SetParent(demo.pointsRoot); instance.transform.position = new Vector3(position.x, position.y, -.05f);
            instance.transform.localScale = Vector3.one * .085f;
            var view = instance.GetComponent<HullPointView>();
            view.SetAppearance(hullSet.Contains(position) ? demo.hullColor : demo.innerColor, demo.selectionColor, false);
            demo.previewPoints.Add(view);
        }
        var gridGO = new GameObject("Infinite coordinate grid"); gridGO.transform.SetParent(demoGO.transform);
        var grid = gridGO.AddComponent<InfiniteGrid>(); grid.worldCamera = camera;
        grid.meshFilter = gridGO.AddComponent<MeshFilter>();
        var renderer = gridGO.AddComponent<MeshRenderer>(); renderer.sharedMaterial = gridMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        var gridMesh = new Mesh { name = "Initial grid" };
        InfiniteGrid.Rebuild(gridMesh, camera, grid.spacing, grid.linePixels, grid.minorColor, grid.majorColor, grid.axisColor);
        AssetDatabase.CreateAsset(gridMesh, Root + "/Meshes/InitialGrid.asset"); grid.meshFilter.sharedMesh = gridMesh;
        gridGO.transform.position = new Vector3(0,.7f,.4f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ConvexHullDemo.unity");
        AssetDatabase.SaveAssets(); Selection.activeGameObject = demoGO;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.identity, 9, true);
        return "Created ConvexHullDemo.unity, UIFrame/toolbar/menu/point prefabs, materials, meshes and UISetting.";
    }

    private static void MakeUIFrame() {
        const string example = "Assets/Scripts/UIFramework/UIFrameworkExamples/Prefabs/UIFrame.prefab";
        var root = PrefabUtility.LoadPrefabContents(example);
        try {
            root.name = "HullUIFrame"; root.transform.localScale = Vector3.one;
            var canvas = root.GetComponent<Canvas>(); canvas.planeDistance = 10;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.referenceResolution = new Vector2(1280,800); scaler.matchWidthOrHeight = 0;
            var camera = canvas.worldCamera; camera.nearClipPlane = .1f; camera.farClipPlane = 50; camera.allowHDR = false;
            camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            var eventSystem = root.GetComponentInChildren<EventSystem>(true);
            foreach (var old in eventSystem.GetComponents<BaseInputModule>()) Object.DestroyImmediate(old);
            var module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>(); module.AssignDefaultActions();
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/HullUIFrame.prefab");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static GameObject MakeToolbar() {
        var root = RectGO(ConvexHullDemoController.ToolbarId, null); Stretch(root);
        var panel = root.gameObject.AddComponent<HullToolbarPanel>();
        var top = RectGO("Toolbar", root); top.anchorMin = new Vector2(0,1); top.anchorMax = Vector2.one;
        top.pivot = new Vector2(.5f,1); top.sizeDelta = new Vector2(0,184); top.anchoredPosition = Vector2.zero;
        Image(top, Color.white); panel.toolbarRect = top;
        var accent = Box("Accent", top, 0,0,1280,4, Blue);
        Text("Kicker", top, "GEOMETRY LAB  /  01", 32,17,700,22,13, Muted);
        Text("Title", top, "二维凸包 · 无限画布", 32,45,800,40,28, Ink);
        Text("Algorithm", top, "ANDREW MONOTONE CHAIN   ·   O(n log n)", 800,27,448,24,12, Muted, TextAnchor.MiddleRight);
        Box("Separator", top, 32,96,1216,1,new Color(.89f,.92f,.95f));
        panel.pointCount = Metric(top,"点集",32,"12");
        panel.hullCount = Metric(top,"凸包顶点",200,"6");
        panel.area = Metric(top,"面积 / 平方单位",368,"0.00");
        Text("Random label",top,"随机点数",674,109,122,22,13,Muted);
        var inputRect = Box("Random count",top,674,135,122,34,new Color(.95f,.97f,.99f));
        panel.countInput = inputRect.gameObject.AddComponent<InputField>();
        var valueText = Text("Value",inputRect,"20",12,0,98,34,17,Ink);
        var placeholder = Text("Placeholder",inputRect,"1–1000",12,0,98,34,15,Muted);
        panel.countInput.textComponent = valueText; panel.countInput.placeholder = placeholder;
        panel.countInput.contentType = InputField.ContentType.IntegerNumber; panel.countInput.characterLimit = 8;
        panel.countInput.targetGraphic = inputRect.GetComponent<Image>(); panel.countInput.text = "20";
        panel.randomButton = Button("Random",top,"随机生成",814,135,166,34,Blue,Color.white);
        panel.clearButton = Button("Clear",top,"清空点集",996,135,166,34,new Color(.92f,.95f,.98f),Ink);
        var footer = RectGO("Footer",root); footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1,0);
        footer.pivot = new Vector2(.5f,0); footer.sizeDelta = new Vector2(0,76); footer.anchoredPosition = Vector2.zero;
        Image(footer,new Color(1,1,1,.97f)); panel.footerRect = footer;
        Text("Instructions",footer,"左键单击选点  ·  拖动已选中的点移动点  ·  其他位置左键拖动画布  ·  右键打开菜单",32,10,1216,24,15,Ink);
        panel.status = Text("Status",footer,"蓝色：凸包顶点    灰色：内部点    橙色外圈：选中点",32,40,1216,22,13,Muted);
        var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root + "/Prefabs/" + ConvexHullDemoController.ToolbarId + ".prefab");
        Object.DestroyImmediate(root.gameObject); return asset;
    }
    private static GameObject MakeMenu() {
        var root = RectGO(ConvexHullDemoController.MenuId,null); root.anchorMin = root.anchorMax = Vector2.zero;
        root.pivot = new Vector2(0,1); root.sizeDelta = new Vector2(230,142);
        Image(root,Color.white);
        var edge = root.gameObject.AddComponent<UnityEngine.UI.Outline>(); edge.effectColor = new Color(.65f,.73f,.83f,.6f); edge.effectDistance = new Vector2(1,-1);
        var panel = root.gameObject.AddComponent<HullContextMenuPanel>();
        panel.coordinateLabel = Text("Coordinates",root,"位置  (0.00, 0.00)",12,6,206,26,13,Muted);
        Box("Divider",root,12,36,206,1,new Color(.88f,.91f,.95f));
        panel.createButton = Button("Create",root,"＋  新建点",10,43,210,40,new Color(.93f,.96f,1),Blue);
        panel.deleteButton = Button("Delete",root,"－  删除点",10,87,210,40,new Color(1,.95f,.94f),new Color(.77f,.24f,.18f));
        var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root + "/Prefabs/" + ConvexHullDemoController.MenuId + ".prefab");
        Object.DestroyImmediate(root.gameObject); return asset;
    }
    private static Text Metric(RectTransform parent,string name,float x,string value) {
        Text(name + " label",parent,name,x,108,160,20,12,Muted);
        return Text(name + " value",parent,value,x,132,220,46,26,Ink);
    }
    private static RectTransform RectGO(string name, Transform parent) {
        var go = new GameObject(name,typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform; if (parent != null) rect.SetParent(parent,false); return rect;
    }
    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static RectTransform At(string name,Transform parent,float x,float y,float width,float height) {
        var rect = RectGO(name,parent); rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height); return rect;
    }
    private static Image Image(RectTransform rect,Color color) { var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
    private static RectTransform Box(string name,Transform parent,float x,float y,float width,float height,Color color) {
        var rect = At(name,parent,x,y,width,height); Image(rect,color); return rect;
    }
    private static Text Text(string name,Transform parent,string content,float x,float y,float width,float height,int size,Color color,TextAnchor align = TextAnchor.MiddleLeft) {
        var rect = At(name,parent,x,y,width,height); var text = rect.gameObject.AddComponent<Text>();
        text.font = font; text.text = content; text.fontSize = size; text.color = color; text.alignment = align;
        text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
    private static Button Button(string name,Transform parent,string label,float x,float y,float width,float height,Color background,Color foreground) {
        var rect = Box(name,parent,x,y,width,height,background); var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>(); var colors = button.colors;
        colors.highlightedColor = new Color(.87f,.92f,1); colors.pressedColor = new Color(.72f,.82f,.97f); button.colors = colors;
        Text("Label",rect,label,0,0,width,height,15,foreground,TextAnchor.MiddleCenter); return button;
    }
    private static void Folder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/'); Folder(path.Substring(0,split)); AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1));
    }
    private static Material Material(string name,Shader shader,Color color,int queue) {
        var material = new Material(shader) { name = name, renderQueue = queue }; material.SetColor("_Color",color);
        AssetDatabase.CreateAsset(material,Root + "/Materials/" + name + ".mat"); return material;
    }
    private static MeshRenderer MeshObject(string name,Transform parent,Mesh mesh,Material material,float z) {
        var go = new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition = new Vector3(0,0,z);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return renderer;
    }
    private static Mesh Disk(bool ring) {
        const int sides = 48; var vertices = new List<Vector3>(); var triangles = new List<int>();
        if (!ring) {
            vertices.Add(Vector3.zero);
            for (int i=0;i<sides;i++) { float a = i*Mathf.PI*2/sides; vertices.Add(new Vector3(Mathf.Cos(a),Mathf.Sin(a))); }
            for (int i=0;i<sides;i++) { triangles.Add(0);triangles.Add(i+1);triangles.Add((i+1)%sides+1); }
        } else {
            for (int i=0;i<sides;i++) {
                float a=i*Mathf.PI*2/sides; var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a));vertices.Add(p);vertices.Add(p*.78f);
            }
            for (int i=0;i<sides;i++) { int a=2*i,b=2*((i+1)%sides);triangles.AddRange(new[]{a,b,a+1,b,b+1,a+1}); }
        }
        var colors = new Color[vertices.Count]; for(int i=0;i<colors.Length;i++) colors[i]=Color.white;
        var mesh = new Mesh { name = ring ? "Selection ring" : "Point disk" }; mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.colors=colors;mesh.RecalculateBounds(); return mesh;
    }
    private static Mesh FillMesh(IReadOnlyList<Vector2> hull) {
        var vertices=new Vector3[hull.Count];var colors=new Color[hull.Count];var triangles=new int[(hull.Count-2)*3];
        for(int i=0;i<hull.Count;i++) { vertices[i]=hull[i];colors[i]=Color.white; }
        for(int i=0;i<hull.Count-2;i++) { triangles[3*i]=0;triangles[3*i+1]=i+1;triangles[3*i+2]=i+2; }
        return new Mesh { name="Initial convex hull", vertices=vertices,colors=colors,triangles=triangles };
    }

    public static string FixVisuals() {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
        var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/HullToolbar.prefab");
        try {
            var panel=root.GetComponent<HullToolbarPanel>();
            foreach(var text in new[]{panel.pointCount,panel.hullCount,panel.area}) {
                text.rectTransform.sizeDelta=new Vector2(220,46);
                text.rectTransform.anchoredPosition=new Vector2(text.rectTransform.anchoredPosition.x,-132);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/HullToolbar.prefab");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        var demo=Object.FindFirstObjectByType<ConvexHullDemoController>();
        demo.worldCamera.transform.position=new Vector3(0,.7f,-10);
        var grid=Object.FindFirstObjectByType<InfiniteGrid>();
        InfiniteGrid.Rebuild(grid.meshFilter.sharedMesh,demo.worldCamera,grid.spacing,grid.linePixels,grid.minorColor,grid.majorColor,grid.axisColor);
        grid.transform.position=new Vector3(0,.7f,.4f);
        EditorUtility.SetDirty(grid.meshFilter.sharedMesh);
        EditorSceneManager.MarkSceneDirty(demo.gameObject.scene);EditorSceneManager.SaveScene(demo.gameObject.scene);AssetDatabase.SaveAssets();
        return "Expanded statistic labels and centered initial point set below toolbar.";
    }

    public static string Finish() {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
        AssetDatabase.Refresh();
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/ConvexHullDemo.unity"||scene.isDirty)throw new Exception("Demo must be the active saved scene.");
        int missing=0;
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        if(missing!=0)throw new Exception("Missing script bindings: "+missing);
        var demo=Object.FindFirstObjectByType<ConvexHullDemoController>();
        if(demo.uiSetting==null||demo.pointPrefab==null||demo.outline.sharedMaterial==null||demo.fillRenderer.sharedMaterial==null)
            throw new Exception("Missing saved asset references.");
        if(ShaderUtil.ShaderHasError(Shader.Find("ConvexHullDemo/CanvasUnlit")))throw new Exception("Shader has compile errors.");
        AssetDatabase.SaveAssets();
        return "Saved demo verified: no missing scripts, all asset references present, shader compiled, documentation imported.";
    }
}
