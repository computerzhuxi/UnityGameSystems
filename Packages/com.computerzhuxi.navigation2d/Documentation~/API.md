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
