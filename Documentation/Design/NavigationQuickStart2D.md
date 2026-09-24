# Navigation2D 快速使用路线设计

状态：2026-09-23 用户进一步确认正式 Runtime 只负责导航，运动仅为 Sample/Lab 适配示例；本次全量自动验证、用户 QuickStart 人工验收与独立复核均已通过；当前状态与证据见 [最终候选验收](../History/Navigation2D/0.2.0/QuickStartAcceptance.md)。

## 目标与现状

提供两种真正可用的接入方式：简单 Unity 项目挂组件并配置即可自动计算路径，项目自己的运动系统负责移动；已有运动器的项目通过公开查询、Agent、Avoidance API 组合。两者共用现有算法，不为目录形式拆程序集。

现有 NavigationAgent2DComponent 只负责 Inspector 配置及显式 Tick。保持其契约与序列化身份，不把它静默改成刚体写入者。新增独立 NavigationNavigator2D，内部只拥有一个 NavigationAgent2D，不公开可写 Agent，不另存路径、索引或重算时钟；可选的 QuickStartExampleMover2D 仅在 Sample/Lab 消费导航建议。ARPG 不添加自动组件，CharacterMovement2D 继续唯一写入角色刚体。

## 用户确认的职责选择

NavigationNavigator2D 自动计算只读路径与移动建议，本身不限制刚体类型，也不移动物体。其他运动组件可以消费建议；正常跟随不复制代理路径索引。QuickStartExampleMover2D 仅演示俯视 Kinematic 移动，位于独立 Sample 程序集，不是正式 API 或生产能力。Dynamic、重力、冲刺、受击与外力由自定义运动器负责。不能静默把用户已有 Dynamic 改为 Kinematic。

Unity 官方说明 MovePosition 在下一物理步执行，适用于 Kinematic；Kinematic 碰撞不会自动阻止自身移动，因此必须主动验证移动段，而不能只调用 MovePosition 后宣称碰撞安全。

参考：[Unity Rigidbody2D.MovePosition](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/rigidbody2d/moveposition)、[Unity Rigidbody2D.Cast](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rigidbody2d)。实际项目版本仍为 6000.3.21f1，最终以该版本编译与物理测试为准。

## 公共契约

- Inspector 配置目标 Transform、身体、障碍层、网格/预算与到达/重算参数。
- SetDestination 设置固定目的地并解除自动目标跟踪；SetTarget 显式设置跟踪对象。
- Stop 清除任务及目标跟踪，不能在下一帧被 Inspector 初始目标自动重启。
- Pause/Resume 保留任务；禁用清除导航建议，但不写外部身体速度，重新启用使旧路径失效。显式暂停不能被禁用/启用解除。
- 目标被销毁或失效的处理须明确且有测试；场景变更需要重建物理源，不能继续查询旧 PhysicsScene2D。
- 对外暴露只读状态、最后路径结果与阻塞/配置原因；禁止给调用方第二套路径写入口。
- 明确每个字段的初始化/实时读取语义，非法配置停止并给出可诊断原因。
- 碰撞体尺寸不能小于实际身体覆盖范围；障碍层不能包含自身。未指定身体时使用 Radius；指定的身体无效、净空不足或缓存超限时安全停止。
- Navigator 不写刚体，也不仲裁外部运动器。文档明确同一刚体只能有一个运动执行器；实际项目应由自己的运动组件消费导航建议，避免重复维护路径状态。

## 局部避让

自动单角色导航与群体批量避让分开评估。AvoidanceWorld2D 要求整批一致快照与统一应用，不能让各组件独立逐个求解伪装成群体避让。Navigator 不提供自动群体协调入口，单独挂载时不承诺角色相互绕行；保留 Crowd Sample 的最小 API 批量组合方案，不复制 ARPG 协调器或引入玩法规则。

## 验证矩阵

| 情况 | 必须证明 |
|---|---|
| 空项目 Inspector 配置 | 不写移动代码即可持续计算导航；要移动仍需项目运动组件，Sample 可演示该接入 |
| Scene/Prefab 序列化 | 配置、目标引用及 GUID 正确恢复 |
| 固定与移动目标 | 正常到达、目标更新节流，无旧目标重启 |
| Pause/Stop/禁用/重启 | 当步停止输出导航建议；暂停、任务保留与清除语义一致，实际停步由运动组件负责 |
| 动态封路/恢复 | 不穿墙，重试后重新到达 |
| 真实身体与薄墙/转角 | 尺寸、偏移和缩放安全，预算超限停止 |
| 非法或冲突配置 | 明确拒绝；不擅自更改已有刚体配置 |
| 物理场景迁移 | 使用新场景障碍源 |
| 现有手动组件与纯 API | 行为不变；ARPG 运动器仍唯一写刚体 |
| 生命周期 | 禁用对象、目标销毁、场景卸载后无残留导航建议；运动器自行处理实际速度 |
| Lab 与独立 Sample | 空 Library 导入、全部测试、构建及运行冒烟 |
| ARPG | 完整测试、构建、真实资产和 DLL/GUID 审计 |

新增 Sample 应为可直接打开的真实配置场景；QuickStartExampleMover2D 确实执行示例运动，但只属于 Sample/Lab，不是正式 Runtime 契约或生产运动能力。测试覆盖真实 FixedUpdate 与 Rigidbody2D 执行链，不能通过测试手动 Tick 掩盖组件缺失。

## 基线与停止条件

基线证据：Artifacts/Logs/Navigation2D/0.2.0/quickstart-baseline-20260922。已有 Agent/避让候选和 ARPG Bangers 修改保持原样。0.1.0 标签不可变。新验证不能引用旧证据冒充本轮结果。人工验收与独立复核现已通过；候选仍不提交、打标签或推送。

2026-09-23 产品边界收口：正式 Runtime 只负责导航。QuickStartExampleMover2D 仅在独立 Sample/Lab 程序集中演示消费建议，不是正式 API 或生产移动能力。实际项目提供自己的移动系统，ARPG 保持底层 API 接入。
