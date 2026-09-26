# API 契约

命名空间 `Computerzhuxi.Navigation2D`，程序集 `Computerzhuxi.Navigation2D`。Runtime 没有友元程序集。

|公共类型|职责|
|---|---|
|GridSettings2D|不可变原点与格距；WorldToCell / CellToWorld|
|PathOptions2D|不可变圆形半径、展开预算、四/八方向|
|ITraversalSource2D|位置与整段净空；实现应包含线段两端|
|PhysicsTraversalSource2D|显式 PhysicsScene2D、LayerMask、Trigger 策略的圆形重叠/扫掠|
|GridPathfinder2D|同步 FindPath，复用内部工作区|
|PathResult2D / PathStatus|不可变结果、展开节点数、障碍接口调用次数|

`FindPath(start, destination, grid, options, source, path)` 覆盖调用方列表。成功路径不重复精确起点，保留必要网格中心，追加精确终点；同格路径仅含终点。失败为空，不提供部分路径。结果不持有路径引用。

|状态|意义|
|---|---|
|Success|完整路径已验证|
|InvalidInput|源为空、非有限坐标/半径、非正格距、负半径、非法方向、预算不在 1..1000000 或端点网格坐标超出 ±1000000|
|StartBlocked / DestinationBlocked|精确端点缺少净空，起点优先检查|
|StartNotConnected / DestinationNotConnected|吸附中心受阻，或精确端点无法安全连接中心|
|Unreachable|可搜索图已耗尽，或同格端点之间受阻|
|BudgetExceeded|还有开放节点但展开预算耗尽；不等同于证明不可达|

空列表是合法输出；输出引用为 null 抛 ArgumentNullException。同实例重入抛 InvalidOperationException，拒绝发生在改写列表之前，避免破坏外层查询；调用方应禁止重入。障碍源异常传播，输出清空并释放查询状态。实例不支持并发调用。源应在单次调用期间保持稳定；Physics2D 来源仅在 Unity 主线程使用。

格距与圆形半径采用世界单位。最近格中心使用 Unity 偶数取整；八方向成本 10/14，四方向成本 10。八方向任一正交邻格受阻就禁止对应对角边，无可穿角选项。优先级依次比较总代价、启发代价、首次发现顺序。确定性以相同障碍回答为前提，不承诺跨硬件物理浮点一致。

展开节点预算包含终点节点，不是毫秒截止时间。TraversalQueries 统计接口调用次数，不等同底层 Physics2D API 次数（线段查询内部还验证两端）。最大预算同时限制最坏搜索存储；正常游戏不应盲目使用上限。

配置结构是每次调用读取的值，不存在 Inspector 隐式实时更新。Physics 来源构造后固定；障碍域改变时由消费者创建新来源并使旧路径失效。来源不调用 SyncTransforms、不修改全局设置；刚修改 Transform / Tilemap 的调用方负责等待物理同步或显式同步。

## Agent2D（0.2.0）

|公共类型/成员|契约|
|---|---|
|AgentSettings2D|正到达容差、正移动阈值、非负最小重算间隔、正失败重试间隔；有限数值|
|NavigationAgent2D|每个角色一份同步状态，不读写 GameObject；构造时验证配置|
|SetDestination|新任务，丢弃旧路径，下一有效 Tick 立即查询；不要每帧使用|
|UpdateDestination|保留安全旧路径；相对最近查询终点累计偏移，达到阈值后仍遵守最小间隔|
|Tick(position, dt, source, revision, domainValid)|源引用、版本或可用性改变时立即失效；零时间冻结推进，负值和非有限数值抛异常|
|InvalidatePath|主动失效，下一有效帧立即重算；传送、已知地图更新使用|
|Pause / Resume|冻结计时并清零建议；恢复后验证当前位置连线，不假设期间地图不变|
|Stop|清除任务、路径与 LastResult；保留暂停标志和累计 QueryCount|
|CurrentPath / CurrentPathIndex|代理唯一持有的只读路径视图及进度；不是快照，调用方不可强转修改|
|DesiredDirection / RemainingWaypointDistance|本帧单位方向和安全路径点距离；执行器自行限幅、碰撞、处理实际速度|
|LastResult|可空的最近查询结果；尚未查询、停止、域失效或源异常时为空；到达不伪造查询结果|
|NavigationAgentState2D|Idle / Pending / Following / Arrived / Failed / DomainUnavailable；暂停另用 IsPaused 判断|
|NavigationAgent2DComponent|只持有单一代理、Inspector 配置与物理源，必须显式 Tick；禁用清零建议，启用失效旧路径；跨 PhysicsScene 的下一次 Tick 更换物理源、丢弃路径并保留任务|

到达要求距离在容差内且两端及整段净空通过。旧路径末端与小幅移动的新目的地有偏差时，会继续重算，不永久停住。路径点跳过前验证新连线，重算时只安全跳过首个中心，避免移动目标跟随回头。

普通移动目标更新、动态封路后的重算遵守间隔；封路立即清除建议。显式新任务、InvalidatePath 和域变化绕过旧计时。失败在 FailureRetryInterval 后自动重试，无法保证毫秒级截止；A* MaxExpandedNodes 仍是单次硬展开预算。

源异常传播并清空路径与建议，LastResult=null、State=Failed，之后可恢复；同一代理不允许源回调重入 Tick 或调用修改方法。参数异常在状态修改前拒绝。代理不支持并发共享。对象禁用时消费者必须自行清除其运动执行器的速度，组件清零建议不能代替执行器停止。

不缓存跨请求障碍答案；Physics2D 同步仍由消费者负责。瞬移应显式 InvalidatePath。包不会自主寻找楼梯、检查地面存在、处理重力或执行跨层移动。

## 可选局部避让（0.2.0）

`AvoidanceWorld2D(maxAgents, maxNeighbors, neighborDistance, timeHorizon, passingBias = .05f)` 预分配工作区；`Solve(inputs, deltaTime, outputs)` 按完整快照批量求解，再原子替换输出。输入必须在调用期间稳定，输出为同序结果，Id 用于核对身份。空输入清空输出；容量超限、重复 Id、非法值抛异常并保留原输出；同实例并发/重入被拒绝。预留输出容量后热路径无托管分配。

- `AvoidanceAgent2D`：Id、Group、Position、实际 Velocity、PreferredVelocity、Radius、MaxSpeed、Locked。不同 Group 完全隔离，Group 的地图/空间含义由消费者决定。Locked 原样输出实际速度，不承担互惠修正；其邻居承担全责。停止、动作控制和冻结身体应显式锁定。
- `AvoidanceResult2D`：Id、Velocity、TruncatedNeighbors、Infeasible。Velocity 是建议，不是强制安全保证。预算截断或保留约束不可行时调用方决定停步等降级；锁定对象本身不执行求解。
- 邻域按圆心距离，最多保留最近的 maxNeighbors；距离相同按稳定 Id。X 索引共享，但插入排序和密集扫描最坏仍是 O(N²)，不是毫秒截止预算。maxNeighbors 可为零，此时遇到邻居会明确报告截断。
- 坐标与速度分量、半径、最大速度绝对值上限 1,000,000；步长范围 [0.0001, 10000] 秒，时间范围同样限制，邻域距离不得超过 1,000,000。超大有限输入抛 ArithmeticException，非有限或负参数按参数异常拒绝。半径须为正。
- passingBias 在前方通道存在邻居时轻微偏向右侧，再执行 ORCA 约束；它帮助对称破局，不代表保证无死锁，也可能受同向前方邻居触发。
- 求解器不调用 Physics2D，不管理对象/位置/路径。执行方必须对建议位移做环境扫掠，并在下一步提供真实执行速度。物理阻挡、运动裁剪、初始重叠、邻居截断都会破坏理想互惠前提。

`AgentSettings2D.LookAheadDistance` 为可选前视距离，默认 0 保持逐点跟随。启用时从范围内最远路径点反向做整段净空验证，每 Tick 最多 8 次额外检查，捷径验证半径增加 max(0.02, 原半径×0.05) 以避开贴角接触容差，只由 Agent 推进唯一索引。前视使局部避让不必抵达被其他身体占据的中间网格点；真实墙角不能跳过。最终位移仍需执行方校验与限幅。

最小组合：所有 Agent Tick → 从实际位置/速度与 Agent 方向建立快照 → 单次 World.Solve → 校验建议位移 → 项目运动器执行。不要在遍历各代理时边 Solve 边更新位置。见 `Samples~/CrowdNavigation2D`。

## Inspector 自动导航

NavigationNavigator2D 自动 FixedUpdate，只暴露只读路径/方向建议及导航命令。身体 Collider2D 有附属刚体时，`Position` 读取该刚体的物理位置，包括位于子物体的独立刚体；无附属刚体时读取 Navigator 的 Transform。保守净空圆与路径推进采用同一锚点。正式 Runtime 不含实际移动组件，项目自己执行运动。配置生效、停止/暂停、目标失活与净空契约见 [Unity 使用指南](UnityGuide.md)。旧 NavigationAgent2DComponent 仍手动 Tick；同一角色不要并行驱动两个组件。纯查询、Agent 与 Avoidance 契约不变。QuickStartExampleMover2D 属于独立 Sample 程序集，不属于正式 API。
