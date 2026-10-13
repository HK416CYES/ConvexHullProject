# 二维凸包演示

打开 `Assets/Scenes/ConvexHullDemo.unity`，按 Play 运行。初始示例为 12 个点、6 个凸包顶点，面积为 45。

## 操作

- 左键单击点：选中，显示橙色外圈；单击空白处取消选择。
- 鼠标在已选中点上按住左键拖动：移动该点，实时更新凸包。
- 其他位置按住左键拖动：平移无限画布。拖动未选中的点也会平移画布，需先单击选中再拖动。
- 右键单击：在点击位置打开菜单。空白处只有“新建点”；命中点时选中该点，并增加“删除点”。
- 新建点使用打开菜单时记录的世界坐标。重合点允许存在；优先命中选中点，否则同位置优先命中最后创建的点。
- 菜单操作后关闭；Escape、菜单外单击也可关闭。菜单外左键仍可正常开始画布操作。
- 随机点数：输入 1–1000 的整数，点击“随机生成”，替换原有点集并在当前可见画布内生成新点。
- “清空点集”移除所有点。清空和随机替换保留相机位置；无效输入保留现有点集并显示提示。

蓝色点为凸包顶点，灰色点为内部点，橙色圈为当前选择。边上共线的非端点不计入凸包顶点。两个点或全共线点集显示线段，面积为零。无滚轮缩放。

## 结构与调整

`ConvexHull2D.Compute(IEnumerable<Vector2>)` 返回去除重复坐标、逆时针排列且不重复首点的凸包；使用 Andrew 单调链，复杂度 O(n log n)。`Area` 使用三角扇和双精度叉积计算面积。

`ConvexHullDemoController` 管理稳定点标识、鼠标手势、点对象池及变更事件。修改点集后每帧统一刷新；平移相机不会重新计算凸包。`InfiniteGrid` 仅重建可见网格。新点、填充网格与网格变化属于演示的运行时数据；场景、Prefab、材质与 UISetting 均已在编辑器中制作并保存。

在场景的 `Convex Hull Demo` 对象上调整颜色、点尺寸、轮廓宽度、10 像素命中半径、4 像素拖动阈值及初始点集。在 `Infinite coordinate grid` 上调整网格间距及配色。

UI 沿用 UIFramework 示例的 `UISetting.CreateUIInstance`、`UIFrame.ShowPanel`、`PanelController` 和 URP 叠加相机。工具栏和菜单 Prefab 位于 `Assets/ConvexHullDemo/Prefabs`，框架示例未修改。

## 验证

`AgentScripts/VerifyConvexHullDemo.cs` 保留可重复执行的检查。通过 `unity command --project-path <工程绝对路径> run_script --file AgentScripts/VerifyConvexHullDemo.cs --entry <入口>` 调用：

- `VerifyConvexHullDemo.Algorithm`：空集、退化集合、重复、共线、输入顺序、面积、逆时针顺序和点包含性；40 组随机点集与独立支撑线算法对照。
- `VerifyConvexHullDemo.Interactions`：在 Play Mode 验证选点、两种拖动、菜单、重叠点、UI 隔离、无效输入、远离原点时随机替换 1/2/20/1000 个点。结束时恢复初始点集与相机。
- `VerifyConvexHullDemo.InputFlow`：在 Play Mode **先聚焦 Unity 的 Game View**，以实际 Input System 事件和 UI 按钮验证输入链路。结束时恢复初始点集与相机。

已验证 1280×720 与 800×900 的 Game View。截图保存在 `Assets/Screenshots`。未构建 Player。

`AgentScripts/BuildConvexHullDemo.cs` 是 Pipeline 资产制作脚本，拒绝覆盖已有演示场景或丢弃任何未保存场景。
