# 调研与首版立项

调研日期：2026-09-15。当前消费项目 Unity 6000.3.21f1。结论：提取路径查询与障碍查询边界，保留项目导航执行层；采用按需 Physics2D 隐式网格 A*，不迁移 EnemyNavigation 整体。

## 一手来源与许可

|来源|参考内容|许可处理|
|---|---|---|
|[Unity AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html)|Surface、Agent、Obstacle、Link 的职责分离与烘焙工作流|[官方许可证](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/license/LICENSE.html)为 Unity Companion License；未复制源码或引入依赖|
|[NavMeshPlus](https://github.com/h8man/NavMeshPlus)|把二维 Collider/Tilemap 收集到 NavMesh 的接入思想|[MIT](https://github.com/h8man/NavMeshPlus/blob/master/LICENSE)；未复制源码|
|[A* Pathfinding Project GridGraph](https://arongranberg.com/astar/documentation/stable/gridgraph.html)|预生成网格、碰撞、净空、邻接与图更新的职责|商业/免费发行有各自许可，[下载页](https://arongranberg.com/astar/download)不授予本包源码再分发权；只参考公开文档，不复制、不打包|
|[My_ARPG](https://github.com/KAIDO-YONAGI/My_ARPG)|实际敌人追击、路径跟随、Tilemap 网格和层级处理|仓库 LICENSE 为 GPL-3.0；仅审计设计，不复制任何代码或资产|

My_ARPG 实际读取位置：`Assets/Scripts/Gameplay/Units/Enemy/EnemyMovement.cs`、`Gameplay/Grid/ElevationEntry.cs` / `ElevationExit.cs`、`Pipeline/Pathfinding/AStarNodeManager.cs`、`AStarPathFinder.cs`、`PathFollower.cs`。读取的是当时 master 页面，未取得固定 commit，不作为可复现算法基线。GitHub API 限流失败已在任务工具记录中保留。

该参考项目扫描 Tilemap 建立有限节点字典，查询与跟随分开；敌人负责目标、运动与攻击。层级触发器切换碰撞体和渲染顺序，没有证据支持直接照搬为本项目的跨层图。当前 ARPG 已有更明确的空间层级权威模型，应沿用它。

## 方案比较

|方案|收益|当前代价与决定|
|---|---|---|
|预生成网格图|有限边界、重复查询成本稳定、易标注区域|需维护动态同步与地图权威；当前已有 Collider 地图，首版不新增第二份缓存图|
|NavMesh / NavMeshPlus|开放区域较少路径点，具备区域和链接工具|需要二维收集、烘焙与半径配置迁移；没有当前性能证据要求更换工作流|
|按需 Physics2D 隐式网格|沿用真实碰撞体，动态变化无需重新烘焙，薄适配|同步物理调用较多、预算不等于时间保证；用严格预算、复用内存和性能证据限制风险|

算法与障碍来源可稳定独立测试，有独立 Sample 消费者，不需要 ARPG 类型。因此立项成立。未来大量角色或大地图需要图缓存/调度时应重新测量，不以首版接口承诺全部场景。

## 行为兼容矩阵

|旧行为|处理|验证|
|---|---|---|
|原点零、最近格中心、八方向 10/14|保留|坐标/方向/确定性测试|
|任一正交邻格阻止斜向切角|保留|墙角测试|
|目的地、速度、重算阈值、失败重试、进度|留在 ARPG|真实角色集成测试和人工巡逻/追击|
|起点受阻仍搜索|用户同意修正为明确失败|端点测试|
|只检查节点、不检查整段|用户同意修正为完整净空|薄墙与端点连接测试|
|终点替换最后格点|改为保留中心再追加精确终点，避免捷径穿墙|路径边逐段验证|
|统一 false|包细分失败，项目保留 PathFailed 兼容布尔值|状态测试|
|同时检查两个空间层障碍|适配器只选择当前已提交层|Profile/过渡/真实场景测试|
|跨层楼梯规划|推迟，用户确认归适配层|明确异层目标不直接寻路|

序列化风险：旧 GridPathfinder2D 和 EnemyNavigation 是非序列化内部对象，保留其项目类型和 meta；EnemyAIDefinition 与角色配置不移动、不改字段。World 仅新增只读掩码查询。现有 Scene/Prefab、SerializeReference、UnityEvent 和 MovedFrom 通过真实资产加载验证。

补充交叉核对：[PathFinding.js](https://github.com/qiao/PathFinding.js) 的 Grid 与 Finder 分离、方向/穿角选项，以及 AStarFinder 的开放堆和启发选择。其 package.json 声明 MIT（https://github.com/qiao/PathFinding.js/blob/master/package.json）；本实现不复制代码，仅使用通用 A* 思想。与此库不同，本包不修改障碍图节点，把临时搜索状态保留在独立工作区，并检查实际移动线段。
