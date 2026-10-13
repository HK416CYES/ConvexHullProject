using System;
using ConvexHullDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class ConfigureHullSizes {
    public static string Apply() {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/ConvexHullDemo.unity")
            throw new Exception("Expected stopped editor and saved demo scene; preserve user edits.");
        var demo=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        demo.pointDiameter=.3f;demo.outlineWidth=.04f;demo.hitRadius=.24f;demo.ApplyVisualSizes();
        var grid=UnityEngine.Object.FindFirstObjectByType<InfiniteGrid>();grid.lineWidth=.01f;
        InfiniteGrid.Rebuild(grid.meshFilter.sharedMesh,demo.worldCamera,grid.spacing,grid.lineWidth,grid.minorColor,grid.majorColor,grid.axisColor);
        EditorUtility.SetDirty(grid.meshFilter.sharedMesh);
        string path="Assets/ConvexHullDemo/Prefabs/HullPoint.prefab";
        var point=PrefabUtility.LoadPrefabContents(path);
        try{point.transform.localScale=Vector3.one*demo.pointDiameter*.5f;PrefabUtility.SaveAsPrefabAsset(point,path);}
        finally{PrefabUtility.UnloadPrefabContents(point);}
        path="Assets/ConvexHullDemo/Prefabs/HullUIFrame.prefab";
        var ui=PrefabUtility.LoadPrefabContents(path);
        try{
            var scaler=ui.GetComponentInChildren<CanvasScaler>(true);
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            PrefabUtility.SaveAsPrefabAsset(ui,path);
        }finally{PrefabUtility.UnloadPrefabContents(ui);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved world diameter .3, outline .04, grid .01, hit radius .24 and 1280x720 Expand UI.";
    }
}
