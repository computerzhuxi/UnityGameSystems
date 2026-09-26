# Perception 2D API（0.2.0 未发布候选）

运行时程序集与命名空间为 `Computerzhuxi.Perception2D`。所有 API 限 Unity 主线程使用。完整的创建、驱动与释放流程见 [GettingStarted](GettingStarted.md)。

## 默认组件

创建一个 `PerceptionWorld2D`，给目标挂 `PerceptionTarget2D` 和子层级 Collider，给观察者挂 `PerceptionObserver2D`。用 Inspector 的 `world` 字段或 `Bind(world)` 绑定。`PerceptionTarget2D` 的单检测点默认为 `TargetRoot`，也可配置 `sightPoint`。观察者使用所属 Scene 的 `PhysicsScene2D`，分别配置目标和障碍 LayerMask。扫描间隔由组件调度，长帧至多扫描一次。

动态创建时先停用观察者 GameObject，添加组件并调用 `Configure(world, settings)`，再启用。`SetFacingDirection` 接收世界空间方向，零向量保留最后有效朝向。`World.ReportNoise(new NoiseEvent2D(position, source: target))` 完成接收者开关与距离筛选，返回接收人数。零响度不投递。默认听觉仍只有距离模型。

```csharp
observer.SetFacingDirection(Vector2.right);
observer.SetSenseEnabled(PerceptionSense.Sight, true);
if (observer.TryGetKnownPosition(target, out PerceptionStimulus known))
    Debug.Log($"{known.Sense}: {known.Position}");
```

`Observations` 和 `HeardEvents` 是复用只读视图；不要跨更新枚举。`TargetPerceptionInfo.Handle` 是完整身份，`Target` 仅在关联对象是 Unity 目标组件时存在。`Sight`、`Hearing` 独立，`IsVisible` 只代表当前视觉。`TryGetKnownPosition` 优先当前视觉，否则选最新有效记录，同时间使用 `DominantSense`。组件还保留 `TryGetObservation(target)`、`GetObservations`、`ForgetSense`、`ForgetTarget`、`ClearMemory` 和 `ResetForReuse` 便利方法。

## 没有组件的核心

普通 C# 调用方只需 Unity 的 `Vector2` 等数据类型，不需建立 GameObject 或使用 Physics。一个注册表可以由多个核心共享，每个核心的记忆、开关和事件独立。

```csharp
var registry = new PerceptionTargetRegistry();
PerceptionTargetHandle target = registry.Register(externalObject);
var observerA = new PerceptionCore2D(registry, new PerceptionSettings2D());
var observerB = new PerceptionCore2D(registry);

var frame = new List<SightObservation2D>
{
    new(target, targetPosition, observerPosition)
};
observerA.SubmitSightFrame(frame, gameTime);
frame.Clear(); // 提交时已复制必要数据
observerA.ReportHearing(soundPosition, observerPosition, gameTime, source: target);
observerA.Advance(gameTime);
observerB.Advance(gameTime); // 身份共享，记忆不共享
```

`SubmitSightFrame` 输入**本次来源的完整帧**：未调用表示没有新结论；提交空帧会结束所有当前视觉；非空帧会结束未出现目标。重复句柄只保留最后一项。单个核心只有一个有效视觉来源；调用方要自行合并多个来源。`ReportHearing` 表示已经确认听到，不再判断距离。无源声音不传 `source`；每次有效声音都有 `Heard` 通知，同目标只保留最新听觉记录。所有输入和 `Advance` 使用同一单调、有限时间轴。

`core.Settings` 返回防御性配置副本；修改它不会改变运行中的核心。运行时参数只能经 `core.UpdateSettings` 或组件 `observer.UpdateSettings` 提交。非法输入与**提交时就已陈旧**的视觉来源不占时间水位；已经接受的帧即使随后切换来源，也继续约束单调时间，只有状态作用会被取消。调用方须推进至已接受输入的最大时间，不得因来源切换让时间倒退。

注销调用 `registry.Unregister(target)`；旧句柄立即无效，各核心在下一批清理记忆并发布 `TargetInvalidated`。曾激活的组件 World 收到 Unity `OnDestroy` 时失效其注册表内全部活动句柄；此注册表不再接受新注册，仍持有旧核心的调用方可继续推进一个批次取得失效通知。目标可绑定到从未激活的 World，但 Unity 不保证给这种 World 发送 `OnDestroy`；销毁前须由身份所有者显式解绑或停用目标。外部对象重新注册获得新代次。传入其他注册表的句柄抛出参数异常。重置调用 `ResetForReuse()` 后先推进一个批次，再报告新生命周期输入。

## 扫描与运行时切换

`PhysicsSightScanner2D.Scan(scene, registry, settings, origin, facing, owner, observations, results)` 单次生成完整视觉帧。可在纯 API 调用方中使用，也可派生该类覆盖 `Scan` 并用组件的 `SetSightScanner` 替换。扫描器只读取当前 `IsVisible` 以选择发现或丢失距离，不修改核心记忆。候选和遮挡使用传入的同一个物理场景；障碍 Trigger 不遮挡，自身和目标自己的 Collider 不遮挡。

```csharp
observer.UpdateSettings(new PerceptionSettings2D { SightDistance = 7, LoseSightDistance = 9 });
observer.SetSightScanner(customScanner); // 传 null 恢复默认扫描器
observer.SetPhysicsSceneOverride(otherScene.GetPhysicsScene2D());
observer.SetPhysicsSceneOverride(null); // 恢复观察者所属 Scene
observer.SetAutomaticSight(false);
observer.SubmitSightFrame(customCompleteFrame, gameTime);
observer.SetAutomaticSight(true);
```

`UpdateSettings` 复制并验证配置，保留核心和记忆，安排立即扫描。切扫描器、物理场景和自动/手动模式会结束旧持续视觉，保留视觉记忆及听觉，下一批使用新来源代次；旧排队视觉帧无法复活。停止自定义来源时调用 `core.EndSightSource()`。直接用核心异步整理扫描结果时，把捕获的 `SightSourceGeneration` 传给 `SubmitSightFrame(frame, time, generation)`；切换后旧代会被丢弃。组件自动检测所属 Scene 的物理场景切换。`Bind` 到不同 World 则是不同注册表，会建立新核心，清除旧环境记忆。

`observer.SubmitSightFrame` 仅在 `SetAutomaticSight(false)` 后接受输入。`observer.Core` 仍是公开的低层入口；若直接向它提交视觉帧，调用方必须自行承担自动扫描与完整帧来源的协调。组件 `ResetForReuse` 先提交重置批次，下一次推进再用发现距离立即扫描；重启视觉或观察者也会安排立即扫描，不沿用旧持续视觉资格。

## 批次和事件

输入先排队，`Advance` 依次执行命令、验证身份/来源、更新状态、过期清理、提交查询视图和版本，然后发布 `SenseUpdated`、`TargetForgotten`、`ObservationsUpdated`。事件回调看到最终状态；回调提交的命令下批执行，更新重入抛异常。订阅者异常记录 Unity 日志，后续订阅者继续。整体遗忘只在该代目标的最终状态完全没有记录时发布。`ObservationVersion` 只用于比较是否变化。

`PerceptionChangeReason` 包括 `Acquired`、`Lost`、`Heard`、`SenseDisabled`、`Expired`、`Cleared`、`TargetInvalidated` 和 `SourceChanged`。`SourceChanged` 可区分环境/扫描来源切换导致的视觉结束。

## 从 0.1.0 迁移

- 旧组件的 `world`、观察点、配置、朝向和 Gizmo 序列化字段保持不变；常用组件查询和事件继续可用。
- `PerceptionObserver2D.Bind` 到新 World 仍清理旧环境状态。运行中修改参数改用 `UpdateSettings`；停用时的 `Configure` 仍会重建核心。
- 新的非组件集成直接使用 `PerceptionTargetRegistry`、`PerceptionCore2D`、完整视觉帧和 `Advance`；不要依赖旧内部 `PerceptionSession2D`。
- 0.1.0 固定 Git 标签不含这些 API；当前 0.2.0 是本地未发布候选。消费项目应按各自 manifest 核实引用。
