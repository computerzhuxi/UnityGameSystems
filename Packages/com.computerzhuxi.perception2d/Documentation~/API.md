# Perception 2D API

运行时程序集及命名空间：`Computerzhuxi.Perception2D`。所有 API 在 Unity 主线程调用，完整时间和事件语义见 Contracts.md。

## 装配

| 类型 / 成员 | 用途 |
| --- | --- |
| `PerceptionWorld2D` | 显式环境，管理目标、观察者注册与声音路由 |
| `PerceptionTarget2D.Bind(world)` | 绑定身份；子层级 Collider 聚合为一个目标 |
| `TargetRoot` / `SightPosition` / `SightDetectable` | 游戏根节点、单视觉检测点、视觉资格 |
| `Generation` / `IsRegistered` | 当前注册代次与有效性；不能用实例引用代替代次 |
| `PerceptionObserver2D.Configure(world, settings, visualOrigin, audioOrigin)` | 停用时初始化；复制配置并重建记忆 |
| `PerceptionObserver2D.Bind(world)` | 切换环境，取消旧环境命令，下一批清理旧状态；绑定后命令保留 |
| `SetFacingDirection(direction)` | 提交世界朝向，零向量保留上一次有效方向 |

创建观察者时先停用 GameObject，添加组件、Configure，再启用。Inspector 配置使用序列化组件字段。恢复旧观察者用启停；对象池新使用周期调用 ResetForReuse，并完成重置批次后再上报新声音。

## 控制与查询

| 成员 | 语义 |
| --- | --- |
| `SetSenseEnabled(sense, enabled)` | 下一批提交感官开关；非回调关闭听觉立即阻止接收 |
| `IsSenseEnabled(sense)` | 已提交的开关状态 |
| `Observations` | 复用只读目标列表，每目标含独立 `Sight` / `Hearing` 可空记录 |
| `HeardEvents` | 无来源声音只读列表，按事件 ID 区分并有限时过期 |
| `TryGetObservation(target, out info)` | 查询目标快照；关注 `Generation` 和 `IsVisible` |
| `GetObservations(sense, results, currentOnly)` | 清空并填充调用方 List；持续状态仅视觉有意义 |
| `TryGetKnownPosition(target, out stimulus)` | 当前视觉优先，否则最新有效记忆，同时间按主导感官 |
| `ForgetSense` / `ForgetTarget` / `ClearMemory` | 下一批显式清理，持续视觉后续可重新发现 |
| `ResetForReuse()` | 下一批恢复默认开关与时钟，取消待投递旧操作和通知 |
| `ObservationVersion` | 每个已提交变化批次更新；仅比较相等性 |

`PerceptionStimulus` 含 Position、ReceiverPosition、Sense、Time、Strength、Tag、IsCurrent。声音的 IsCurrent 为 false，听觉记忆不表示持续发声。列表不能跨更新枚举，需要保存历史时复制快照。游戏的生命、阵营、调查完成和目标优先级在包外处理；它们的变化不能只依赖 ObservationVersion 才重新评估。

## 声音和事件

`world.ReportNoise(new NoiseEvent2D(position, loudness: 1, source: target, maxRange: null, tag: "Footstep"))` 返回上报时接收的观察者数量。source 可为空；音频播放不会自动上报声音。零响度返回零；非法位置、响度或范围抛出参数异常。声音已接收仍可能因来源生命周期失效而取消投递。

事件顺序：提交列表与版本 → SenseUpdated → TargetForgotten → ObservationsUpdated。SenseUpdated 提供 PerceptionChange（目标、代次、感官、原因、刺激和声音 ID）；TargetForgotten 提供被遗忘的目标快照，只有该代目标在最终状态已无记录才发布。整体遗忘对应的感官清理原因见同批 SenseUpdated。订阅者异常隔离，回调中的记忆和开关命令延后到下一批。

## 配置

PerceptionSettings2D：SightEnabled、HearingEnabled、TargetLayers、ObstacleLayers、SightDistance、LoseSightDistance、ViewAngle、ScanInterval、SightMemory、HearingRange、HearingMemory、AnonymousMemory、DominantSense。

目标记忆时长 0 表示不按时间遗忘；AnonymousMemory 必须大于 0。LoseSightDistance 不小于 SightDistance，ViewAngle 为 0–360 度，ScanInterval 至少 0.02 秒。配置在初始化时复制，运行中修改外部配置对象不会改动会话。
