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

UI 沿用 UIFramework 示例的 `UISetting.CreateUIInstance`、`UIFrame.ShowPanel`、`PanelController` 和 URP 叠加相机。工具栏保留三项统计、随机数量及随机、清空、算法选择和播放按钮，尺寸为 1240×64；右键菜单宽 160。说明使用 HullHelpPanel，通过 PlayerPrefs 保存偏好。工具栏、菜单和说明 Prefab 位于 `Assets/ConvexHullDemo/Prefabs`，框架示例未修改。HullUIFrame 的 CanvasScaler 使用 1280×720 参考分辨率、Scale With Screen Size / Expand；所有 UI 使用固定设计尺寸并统一等比适配，宽窄窗口均能容纳完整布局。

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

从顶部下拉框选择 Andrew 单调链、Jarvis March、QuickHull、MergeHull 或 Chan，然后点击“播放演示”。默认 Andrew；选择仅决定下一次演示，播放及暂停时不可切换。停止后保留本次场景的选择，不写入本地偏好。系统先将所有点居中显示于可用画布；若超过相机 Max Zoom Size，提示“凸包过大，无法演示”，相机与点集不变。空点集不能播放。

`TeachingAlgorithmRegistry` 根据 `TeachingAlgorithmKind` 创建独立的 `HullTeachingSession`。会话复制点坐标与稳定 ID，精确重复坐标保留最先创建的代表；排序仅改变教学下标。教学实现不调用或修改 `ConvexHull2D.Compute`。它用迭代器逐步生成通用事件，只保留当前链和子问题，不预存全部比较。每帧最多预留 4096 次几何比较；还没产生可见事件时保持当前画面。暂停停止计时和计算，单步推进到下一个可见事件并完成动画。

`HullAlgorithmPlayback` 管理播放，`HullTeachingOverlay` 按分组绘制独立线条及外圈，通过 `HullTeachingLine.prefab` 对象池复用渲染器，两个可重复使用的外圈由 `HullTeachingMarker.prefab` 制作；`HullPlaybackPanel` 显示本次演示编号和紧凑控制栏。每次播放从 1 连续重新编号，删除的点不占号，重复坐标只给代表点编号；Andrew 按 X、再按 Y 排序后的下链遍历顺序编号（上链反向遍历），其他算法按本次快照扫描/分组顺序编号。编号与内部永久 ID 分离，切换算法、移动或删增点后重新播放都会重新生成，不改变原始点标识。原有点和三项统计始终保留；演示期间 Chan 按本轮分组给原始点着色，其他算法统一使用普通点颜色，并暂时隐藏已求出的凸包轮廓、填充及选中外圈，停止或完成后恢复原来的显示状态和选择。演示不触发实时凸包重算。

| 算法 | 教学流程 | 计算复杂度（n 为不同坐标数量，h 为凸包顶点数） |
| --- | --- | --- |
| Andrew 单调链 | 坐标排序，分别构造下链与上链，弹出右转或共线中间点 | O(n log n) |
| Jarvis March | 每轮依次尝试所有其他点（含内部点），橙色试探边伸出；失败时红色试探边缩回并退回最佳候选，共线时取最远点，扫描结束后确认蓝色边界 | O(nh) |
| QuickHull | 左右极点与最远点以蓝圈持续标记；完整三角形带浅色填充，当前为橙色，历史三角形淡化保留；递归处理新边外侧点 | 常见 O(n log n)，最坏 O(n²) |
| MergeHull | 排序后平衡分割，最多三点的叶子，上下公共切线及外侧弧合并 | O(n log n) |
| Chan | 按创建顺序分组，组内独立单调链，二分切线全局包装；失败后分组规模平方增长 | O(n log h) |

Chan 从 min(4,n) 开始，每轮最多包装 m 步；失败时清理该轮分组并以 min(m²,n) 重试。不对所有点做公共排序，也不逐组扫描所有子凸包顶点。切线查询对点/线段为常数时间，对严格凸多边形为二分查询；查询点恰好是组凸包顶点时通过索引字典直接取下一顶点。参考 [Chan 原论文](https://tmc.web.engr.illinois.edu/conv23d.pdf) 的分组、包装与重试设计，以及 [KIT 计算几何讲义](https://i11www.iti.kit.edu/_media/teaching/sommer2018/compgeom/psession1.pdf) 的二分切线思路。五种结果均为逆时针、无重复起点、无共线中间点；空集拒绝播放，单点直接完成；两点及全共线集合由各教学实现独立处理，Jarvis 展示逐点扫描，其他算法使用简短退化流程。


Andrew 下链为蓝色，上链为绿色；分组和递归使用稳定区分色，候选为橙色，移除时为红色，最终闭合边界为蓝色。合并后清理两个子凸包，Chan 重试时清理整轮覆盖层。底部控制栏仅保留暂停/继续、下一步、停止与速度下拉框，不显示阶段、步骤说明、进度或速度标签。暂停后可以“下一步”；每次步骤动画完成后仍保持暂停。速度支持 0.25×、0.5×、1×、2×、4×。停止或完成后清理全部教学标记。控制栏设计尺寸为 460×54；速度下拉框模板与子对象均使用 UI 层，确保展开列表由 URP UI 相机正确显示。

演示期间不能选点、移动、新建、删除、随机替换或清空点集；左键在点上拖动同样平移相机，滚轮缩放仍可使用。打开说明弹窗会暂停，关闭后需手动继续。Escape 优先关闭说明，否则停止演示。

在场景 `Convex Hull Demo` 的 `Hull Algorithm Playback` 组件调整 Step Interval（默认 0.6 秒）和 Transition Duration（默认 0.2 秒），Granularity 默认 Key Steps（关键步骤），Detailed Steps 额外显示几何比较；在其 Overlay 引用对象调整教学线宽、标记尺寸及颜色。教学对象和 UI 均为编辑器制作的持久化 Prefab。

验证脚本：`VerifyMultiHull.Algorithms` 对照五种独立算法和实时算法，覆盖两种粒度、重复/共线/同 X/Y、输入逆序、远离原点、随机及 1000 点边界集合，验证 Chan 重试和对数切线查询上限；`VerifyHullPlayback.Playback` 验证状态控制、点操作锁定、相机、统计和原始渲染保持不变；`VerifyMultiHullLifecycle.Run` 对每种算法验证 0/1/2/20/1000 点的完成、重播、中途停止及组件禁用清理；`VerifyMultiHullUI.Run` 使用测试鼠标逐个选择五种算法，验证实际播放、暂停、单步、速度、继续和停止；`VerifyHullPlaybackUI.Run` 保留速度下拉框回归检查。`VerifyMultiHullVisual.Run` 验证候选与移除外圈分别保留、移除事件的叉积值及已确认 QuickHull 边界的蓝色。编辑器 CLI 测试中会显式单帧运行，让新面板完成 Canvas 射线深度生成。

重建或更新资产时，先执行已有制作脚本及 UI/尺寸/字体更新，再执行 `BuildHullPlayback.Apply`，保存播放面板注册和场景绑定。

本次验证：3680 个算法会话通过；两种粒度结果一致。场景生命周期检查通过 17236 项断言；横屏 2560×1440 和窄窗口 800×900 中五种算法及两个下拉框的实际鼠标点击通过。示例截图：`Assets/Screenshots/MultiHull*.png`。

Jarvis March 在关键与详细模式下都逐点展示试探和回退；详细模式额外显示三点比较。两点及全共线集合也展示扫描，单点直接完成。`VerifyJarvisScan.Algorithm` 验证每轮完整扫描、拒绝后保留最佳候选及最终结果；`Visual` 验证试探伸出、红边缩回、候选外圈恢复及实时凸包不重算。

QuickHull 教学覆盖层：三角形包括原基线和两条新边，进入子递归时保留并淡化父三角形。蓝色外圈表示已确定的凸包顶点，蓝色线段表示已确认边界；原始点本体仍为普通点颜色。完成时清理辅助三角形并短暂停留最终边界及已确定点，停止或退出时清除全部覆盖层。Overlay Inspector 可调整 Quick Triangle Color / Quick Context Color、两种填充透明度和 Confirmed Point Diameter（世界单位）；自动取景包含确认外圈尺寸。

`BuildHullPlayback.RefreshQuickHullAssets` 通过编辑器制作 `HullQuickTriangleFill.prefab`、模板网格和独立材质，并绑定已有 `HullTeachingMarker.prefab` 作为确认外圈模板；它仅在场景已保存且退出播放时执行。运行中只复制模板网格，不修改共享资产；填充、外圈和线条均复用对象池。`VerifyQuickHullTeaching.Algorithm` 验证三角形及确认点与最终凸包一致，`Visual` 验证父子上下文、暂停、最终显示和清理，`PreviewChild` 可准备递归截图。

QuickHull 寻找最远点时，关键与详细模式均依次比较当前子问题的全部外侧候选点；当前候选点为橙圈，当前最大候选为绿圈，绿色垂线保留当前最大距离，本次垂线伸出到比较点，未刷新最大值时呈红色。只有完整扫描结束后才把最远点标记为蓝色并构造三角形。比较的是到基线所在直线的垂距，垂足在延长线上时显示辅助延长线；先前已排除的三角形内部点不重复进入子问题。Overlay 的 Quick Best Color 可调整绿色候选配色。`VerifyQuickHullScan.Algorithm` 专门覆盖首点已最远但后续点仍全部比较的情况，`Visual` 检查垂线长度、候选保留和暂停，`Cleanup` 检查标记与辅助线清理。

MergeHull 的最小组（最多三个点）直接显示点、线段或三角形，不播放 Andrew 步骤。合并时橙色线连接左凸包最右点与右凸包最左点，每次都显示相邻顶点检查（两种粒度均显示）；绿线表示移动切点，红线表示保留当前切点。确认上下公共切线后，同一步删去内部弧并统一蓝色，复用已有线条对象，不逐组缩回或逐边重建。

Chan 每轮先将所有点按组着色并显示组号，再逐组完整播放独立 Andrew 的候选、上下链构造与移除过程。完成组保留为较淡轮廓，未构造组不提前显示凸包。随后逐组加粗正在查询的子凸包，展示二分邻边判断、区间收缩及组间候选比较（包括失败比较）；全部组查询完成后才确认下一全局顶点。失败后清理并扩大 m 重试。关键模式缩短重复步骤自动停留，不跳过步骤；分组、组完成和重试使用完整间隔，暂停单步及详细模式保留完整间隔。停止后恢复原点颜色。
