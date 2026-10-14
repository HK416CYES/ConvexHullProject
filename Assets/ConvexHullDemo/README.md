# 二维凸包演示

打开 `Assets/Scenes/ConvexHullDemo.unity`，按 Play 运行。默认首先打开操作说明，点击右上角 × 关闭；左下角 ? 可随时重开。勾选“不再显示”会保存本地偏好并禁止下次进入时自动弹出；重新打开说明并取消勾选可恢复。初始示例为 12 个点、6 个凸包顶点，面积为 45。

## 操作

- 左键单击点：选中，显示橙色外圈；单击空白处取消选择。
- 鼠标在已选中点上按住左键拖动：移动该点，实时更新凸包。
- 其他位置按住左键拖动：平移无限画布。拖动未选中的点也会平移画布，需先单击选中再拖动。
- 右键单击：在点击位置打开菜单。空白处只有“新建点”；命中点时选中该点，并增加“删除点”。
- 新建点使用打开菜单时记录的世界坐标。重合点允许存在；优先命中选中点，否则同位置优先命中最后创建的点。
- 菜单操作后关闭；Escape、菜单外单击也可关闭。菜单外左键仍可正常开始画布操作。
- 随机点数：输入 1–1000 的整数，点击“随机生成”，替换原有点集并在当前可见画布内生成新点。
- “清空点集”移除所有点。清空和随机替换保留相机位置；无效输入保留现有点集，数量输入框标红。

蓝色点为凸包顶点，灰色点为内部点，橙色圈为当前选择。边上共线的非端点不计入凸包顶点。两个点或全共线点集显示线段，面积为零。滚轮向上放大、向下缩小，以鼠标位置为中心；鼠标在 UI 上、说明弹窗打开或正在左键拖动时不执行缩放。

## 结构与调整

`ConvexHull2D.Compute(IEnumerable<Vector2>)` 返回去除重复坐标、逆时针排列且不重复首点的凸包；使用 Andrew 单调链，复杂度 O(n log n)。`Area` 使用三角扇和双精度叉积计算面积。

`ConvexHullDemoController` 管理稳定点标识、鼠标手势、点对象池及变更事件。修改点集后每帧统一刷新；平移相机不会重新计算凸包。`InfiniteGrid` 仅重建可见网格。新点、填充网格与网格变化属于演示的运行时数据；场景、Prefab、材质与 UISetting 均已在编辑器中制作并保存。

在场景的 `Convex Hull Demo` 对象上调整颜色、世界单位点直径（Point Diameter，默认 0.3）、世界单位轮廓宽度（Outline Width，默认 0.04）、世界单位命中半径（Hit Radius，默认 0.24）、4 像素拖动阈值及初始点集。Zoom 分组中的 Min Zoom Size / Max Zoom Size 限定正交相机投影大小（默认 1–30，数值越小画布放得越大）；Zoom Speed 控制滚轮速度。在 `Infinite coordinate grid` 上调整网格间距、世界单位线宽（Line Width，默认 0.01）及配色。点直径修改会同步更新编辑器中的初始点预览。点、选中圈、凸包和网格尺寸不再从客户端像素反算。窗口变化只改变可见范围和渲染分辨率；滚轮缩放调整相机投影大小，点的世界尺寸保持不变。

UI 沿用 UIFramework 示例的 `UISetting.CreateUIInstance`、`UIFrame.ShowPanel`、`PanelController` 和 URP 叠加相机。工具栏保留三项统计、随机数量及随机、清空、播放按钮，尺寸为 1040×64；右键菜单宽 160。说明使用 HullHelpPanel，通过 PlayerPrefs 保存偏好。工具栏、菜单和说明 Prefab 位于 `Assets/ConvexHullDemo/Prefabs`，框架示例未修改。HullUIFrame 的 CanvasScaler 使用 1280×720 参考分辨率、Scale With Screen Size / Expand；所有 UI 使用固定设计尺寸并统一等比适配，宽窄窗口均能容纳完整布局。

## 验证

`AgentScripts/VerifyConvexHullDemo.cs` 保留可重复执行的检查。通过 `unity command --project-path <工程绝对路径> run_script --file AgentScripts/VerifyConvexHullDemo.cs --entry <入口>` 调用：

- `VerifyConvexHullDemo.Algorithm`：空集、退化集合、重复、共线、输入顺序、面积、逆时针顺序和点包含性；40 组随机点集与独立支撑线算法对照。
- `VerifyConvexHullDemo.Interactions`：在 Play Mode 验证选点、两种拖动、菜单、重叠点、UI 隔离、无效输入、远离原点时随机替换 1/2/20/1000 个点。结束时恢复初始点集与相机。
- `VerifyConvexHullDemo.InputFlow`：在 Play Mode **先聚焦 Unity 的 Game View**，以实际 Input System 事件和 UI 按钮验证输入链路。结束时恢复初始点集与相机。

已验证 1280×720 与 800×900 的 Game View。截图保存在 `Assets/Screenshots`。未构建 Player。

`AgentScripts/BuildConvexHullDemo.cs` 是 Pipeline 资产制作脚本，拒绝覆盖已有演示场景或丢弃任何未保存场景。

## UI 更新验证

`AgentScripts/CompactHullUI.cs` 的 `CompactHullUI.Apply` 在编辑器中保存紧凑 UI 和点尺寸（只对已保存的演示场景执行）。若从零重建，先执行初始制作脚本，再执行此更新脚本，最后进入 Play Mode。

`AgentScripts/VerifyCompactHullUI.cs` 的 `VerifyCompactHullUI.Run` 在 Play Mode 聚焦 Game View 后验证叉号、问号、复选框、偏好保存与恢复、说明遮挡、工具栏和右键菜单尺寸及点的配置世界直径；会恢复测试前的偏好。说明与问号、叉号和复选框使用现有 UI 控件及字体制作，无需另外寻找素材。

`AgentScripts/ConfigureHullSizes.cs` 保存世界单位尺寸与 UI 参考分辨率；`VerifyHullSizes.Check` 在不同 Game View 分辨率下验证世界尺寸、线宽、世界命中范围和 UI 等比适配，并验证运行时修改点直径。

演示 UI 字体统一使用 `Fonts/GenSenRounded2-M.ttc`。`AgentScripts/UpdateHullFontZoom.cs` 用于在编辑器替换字体和更新说明；`VerifyHullFontZoom.Run` 验证缩放边界、鼠标锚定、UI 隔离及缩放后的命中和拖动。

## 算法播放演示

点击工具栏“播放演示”。系统先将所有点居中显示于可用画布；若超过相机 Max Zoom Size，提示“凸包过大，无法演示”，相机与点集不变。空点集不能播放。

教学算法 `HullTeachingAlgorithm.Record` 复制点坐标并独立排序、去重、计算上下链，生成增量步骤；它不调用或修改 `ConvexHull2D.Compute`。`HullAlgorithmPlayback` 管理播放，`HullTeachingOverlay` 绘制独立辅助线和外圈，`HullPlaybackPanel` 显示编号和说明。原有点和三项统计始终保留；演示期间所有原始点统一使用普通点颜色，并暂时隐藏已求出的凸包轮廓、填充及选中外圈，停止或完成后恢复原来的显示状态和选择。演示不触发实时凸包重算。

下链为蓝色，上链为绿色，当前点外圈为橙色，被移除点外圈为红色。底部控制栏仅保留暂停/继续、下一步、停止与速度下拉框，不显示阶段、步骤说明、进度或速度标签。暂停后可以“下一步”；每次步骤动画完成后仍保持暂停。速度支持 0.25×、0.5×、1×、2×、4×。停止或完成后清理全部教学标记。控制栏设计尺寸为 460×54；速度下拉框模板与子对象均使用 UI 层，确保展开列表由 URP UI 相机正确显示。

演示期间不能选点、移动、新建、删除、随机替换或清空点集；左键在点上拖动同样平移相机，滚轮缩放仍可使用。打开说明弹窗会暂停，关闭后需手动继续。Escape 优先关闭说明，否则停止演示。

在场景 `Convex Hull Demo` 的 `Hull Algorithm Playback` 组件调整 Step Interval（默认 0.6 秒）和 Transition Duration（默认 0.2 秒）；在其 Overlay 引用对象调整教学线宽、标记尺寸及颜色。教学对象和 UI 均为编辑器制作的持久化 Prefab。

验证脚本：`VerifyHullPlayback.Algorithm` 对照独立算法和实时算法，并验证增量重放；`Playback` 验证状态控制、点操作锁定、相机、统计和原始渲染保持不变；`Degenerates` 验证退化集合与 1000 点；`VerifyHullPlaybackUI.Run` 使用测试鼠标验证实际按钮和速度选项。编辑器 CLI 测试中会显式单帧运行，让新面板完成 Canvas 射线深度生成。

重建或更新资产时，先执行已有制作脚本及 UI/尺寸/字体更新，再执行 `BuildHullPlayback.Apply`，保存播放面板注册和场景绑定。
