using System;
using ConvexHullDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class UpdateHullFontZoom {
    public static string Apply() {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/ConvexHullDemo.unity")throw new Exception("Expected stopped editor and saved demo scene; preserve user edits.");
        const string fontPath="Assets/ConvexHullDemo/Fonts/GenSenRounded2-M.ttc";
        AssetDatabase.ImportAsset(fontPath);
        var font=AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if(font==null)throw new Exception("Requested TTC is not imported as a Unity Font.");
        int count=0;
        foreach(var name in new[]{"HullUIFrame","HullToolbar","HullContextMenu","HullHelp"}) {
            string path="Assets/ConvexHullDemo/Prefabs/"+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try {
                foreach(var text in root.GetComponentsInChildren<Text>(true)) {
                    text.font=font;count++;
                    if(text.name=="Instructions") {
                        text.text="左键单击点：选中，橙色外圈表示选中。\n\n拖动选中的点：移动点并实时更新凸包。\n在其他位置拖动：平移无限画布。\n滚轮：以鼠标位置为中心缩放画布。\n\n右键：新建点；点上右键还可删除。\n随机生成：按输入数量替换现有点集。\n\n蓝色：凸包顶点　灰色：内部点";
                        text.rectTransform.sizeDelta=new Vector2(472,256);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        var demo=UnityEngine.Object.FindFirstObjectByType<ConvexHullDemoController>();
        EditorUtility.SetDirty(demo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved GenSenRounded2-M font on "+count+" text components and configurable zoom limits: "+demo.minZoomSize+"–"+demo.maxZoomSize;
    }
}
