# Perception 2D 上手

当前仓库的 0.2.0 源码是未发布候选；`perception2d-v0.1.0` 标签不包含编程式 API。运行时程序集为 `Computerzhuxi.Perception2D`。以下两条入口使用同一个 `PerceptionCore2D` 规则，均限 Unity 主线程调用。

## 组件装配

在场景中放一个 `PerceptionWorld2D`；目标挂 `PerceptionTarget2D`，其自身或子层级放 Collider；观察者挂 `PerceptionObserver2D`。Inspector 中分别绑定 `world`，配置目标层、障碍层、视距、听距和观察点。运行时创建观察者时先停用其 GameObject，再添加并配置组件，最后启用：

```csharp
using Computerzhuxi.Perception2D;
using UnityEngine;

var eye = new GameObject("Observer");
eye.SetActive(false);
var observer = eye.AddComponent<PerceptionObserver2D>();
observer.Configure(world, new PerceptionSettings2D
{
    TargetLayers = 1 << targetLayer,
    ObstacleLayers = 1 << obstacleLayer,
    SightDistance = 6,
    LoseSightDistance = 8
});
eye.SetActive(true);
observer.SetFacingDirection(Vector2.right);

var target = targetObject.AddComponent<PerceptionTarget2D>();
target.Bind(world);
observer.SenseUpdated += OnSenseUpdated;
world.ReportNoise(new NoiseEvent2D(target.transform.position, source: target, tag: "Footstep"));
```

这里的 `world`、`targetObject` 和层编号由游戏提供，`OnSenseUpdated` 是游戏定义的 `void OnSenseUpdated(PerceptionChange change)`。Unity 的 `Update` 驱动观察者扫描和核心批次。音频播放不会自动生成感知事件。停用目标会注销其身份，观察者在下一批清理旧代记忆。已激活的 World 收到 Unity `OnDestroy` 时立即失效它的活动身份；仍由核心下一次 `Advance` 发布失效通知。从未激活的 World 也可能被目标绑定并注册，但 Unity 不保证给它发送 `OnDestroy`；销毁这种装配前应由目标身份所有者显式解绑或停用目标。由订阅方解除事件订阅。

## 编程式组合

核心不要求 World、Target 或 Observer 组件。调用方拥有注册表、目标句柄、各核心和时间轴。完整视觉帧表示本次来源观察到的全部目标；空帧表示一个也未看到；不提交帧表示没有新的视觉结论。

```csharp
using System;
using System.Collections.Generic;
using Computerzhuxi.Perception2D;
using UnityEngine;

var registry = new PerceptionTargetRegistry();
object actor = new object();
PerceptionTargetHandle handle = registry.Register(actor);
var core = new PerceptionCore2D(registry, new PerceptionSettings2D());
core.SenseUpdated += OnSenseUpdated;

var frame = new List<SightObservation2D>
{
    new SightObservation2D(handle, new Vector2(2, 0), Vector2.zero)
};
core.SubmitSightFrame(frame, 1.0);
core.ReportHearing(new Vector2(2, 0), Vector2.zero, 1.0, source: handle, tag: "Footstep");
core.Advance(1.0);

// 没有新视觉结果时仅推进时间；完整空帧则结束当前视觉。
core.Advance(2.0);
core.SubmitSightFrame(Array.Empty<SightObservation2D>(), 3.0);
core.Advance(3.0);

// 释放身份并让核心发布失效，然后解除订阅。
registry.Unregister(handle);
core.Advance(4.0);
core.SenseUpdated -= OnSenseUpdated;
```

多个核心可共享一个注册表，却各自持有记忆。**目标身份的所有者**在该目标生命周期结束时调用 `registry.Unregister(handle)`；仅停止或丢弃其中一个观察者的 Core 时，不应注销其他观察者仍共用的目标。该观察者应解除自己的事件订阅，停止驱动并释放自身持有的 Core 引用；不再使用的核心由普通 C# 生命周期回收。`AssociatedObject` 可映射游戏对象，核心不读取其位置或状态。`Observations` 和 `HeardEvents` 是复用的只读视图，长期保存时复制结构体值。

## 可选物理扫描器

若已有 Unity 物理场景，也可由公开扫描器生成完整帧。扫描器要求 Collider 所属目标挂 `PerceptionTarget2D`，并绑定到与核心共用的 World 注册表；没有这些组件时按上例自行构造帧。

```csharp
using UnityEngine.SceneManagement;

var core = new PerceptionCore2D(world.Registry);
var scanner = new PhysicsSightScanner2D();
var results = new List<SightObservation2D>();
double now = Time.timeAsDouble;
scanner.Scan(gameObject.scene.GetPhysicsScene2D(), world.Registry,
    core.Settings, origin, facing, transform, core.Observations, results);
core.SubmitSightFrame(results, now);
core.Advance(now);
```

`origin` 和 `facing` 由驱动方提供。停止或更换视觉来源时调用 `EndSightSource()`；异步产生的帧应携带当时的 `SightSourceGeneration`。所有已接受输入约束同一条单调时间轴；来源切换取消旧帧的状态作用，但不回退其已占用的时间水位。详见 [API](API.md) 与 [Contracts](Contracts.md)。
