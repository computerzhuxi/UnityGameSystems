# 👁️ Perception 2D

Unity 6000.3 的二维视觉、听觉与分感官记忆包。它管理目标身份、已确认的感知事实、记忆和通知；阵营、威胁评分、AI 决策、导航和音频播放由游戏负责。组件入口与纯代码入口共用 `PerceptionCore2D` 的状态规则。

## 📦 安装与版本

下文使用 **0.2.0 未发布候选** API，要求 Unity 6000.3。首次使用请安装当前仓库源码：

1. 克隆或下载 [UnityGameSystems 仓库](https://github.com/computerzhuxi/UnityGameSystems)，保留完整目录。
2. 在自己的 Unity 工程打开 **Window > Package Manager**，选择 **+ > Add package from disk**。
3. 选择下载目录中的 `Packages/com.computerzhuxi.perception2d/package.json`，确认包版本为 `0.2.0`。

仓库内的 `Projects/Perception2DLab` 已使用以下本地引用测试同一份候选源码：

```json
"com.computerzhuxi.perception2d": "file:../../../Packages/com.computerzhuxi.perception2d"
```

若手动编辑其他工程的 manifest，应按自己的 `Packages` 目录到本包目录的实际相对路径调整 `file:` 值。

**需要旧版时**，已发布的 Git 标签仅有 `perception2d-v0.1.0`，可通过 **Add package from git URL** 添加：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.perception2d#perception2d-v0.1.0
```

该标签提供旧版 0.1.0 API，**不包含**下文的 `PerceptionTargetRegistry`、`PerceptionCore2D` 等 0.2.0 编程式入口。需要下文功能时安装当前候选源码，并在消费工程的 manifest 中核对实际来源。

## 🚀 快速开始

两种方式都限 Unity 主线程使用。

| 方式 | 适用场景 |
| --- | --- |
| **组件装配** | 使用 GameObject、Inspector、二维物理场景和自动扫描 |
| **纯代码组合** | 自己管理目标身份、输入来源、时间轴和核心生命周期 |

### 组件装配

在同一个新场景中完成以下装配：

1. 新建空物体 `World`，添加 `PerceptionWorld2D`，保持启用。
2. 新建空物体 `Observer`，位置设为 `(0, 0, 0)`，添加 `PerceptionObserver2D`；将 `World` 组件拖入它的 **World** 字段。
3. 在 **Tags and Layers** 中创建 `PerceptionTarget` 层。新建空物体 `Target`，位置设为 `(2, 0, 0)`，Layer 设为 `PerceptionTarget`，添加 `PerceptionTarget2D` 和 `BoxCollider2D`；将同一个 `World` 组件拖入目标的 **World** 字段，保持 **Sight Detectable** 勾选。
4. 在观察者的 **Settings** 中，将 **Target Layers** 设为 `PerceptionTarget`，**Obstacle Layers** 设为 `Nothing`；保持视觉/听觉开启，视距 `6`、丢失视距 `8`、视角 `100`、听距 `10`、视觉记忆 `10`、扫描间隔 `0.1`。保持 **Automatic Sight** 勾选和 **Initial Facing** 为 `(1, 0)`。观察点和听觉点留空时使用观察者自身位置；目标的检测点留空时使用自身位置。

将下面代码保存为 `PerceptionReader.cs`，挂到 `Observer` 上，把观察者和目标组件拖入对应引用栏：

```csharp
using Computerzhuxi.Perception2D;
using UnityEngine;

public class PerceptionReader : MonoBehaviour
{
    [SerializeField] private PerceptionObserver2D observer;
    [SerializeField] private PerceptionTarget2D target;

    /// <summary>在 Play 模式读取当前视觉和最近一次有效感知位置。</summary>
    [ContextMenu("Demo/Read Perception")]
    private void ReadPerception()
    {
        // 编辑模式尚未运行扫描，不把配置状态当作感知结果。
        if (!Application.isPlaying) return;
        bool visible = observer.TryGetObservation(target, out var info) && info.IsVisible;
        Debug.Log($"Visible: {visible}");
        if (observer.TryGetKnownPosition(target, out PerceptionStimulus known))
            Debug.Log($"Known position: {known.Position}, sense: {known.Sense}");
        else
            Debug.Log("No known position");
    }

    /// <summary>在 Play 模式向共同环境报告目标的一次脚步声。</summary>
    [ContextMenu("Demo/Report Footstep")]
    private void ReportFootstep()
    {
        if (!Application.isPlaying) return;
        // 声音由游戏显式上报；播放音频不会自动产生感知事实。
        observer.World.ReportNoise(new NoiseEvent2D(
            target.transform.position, source: target, tag: "Footstep"));
    }
}
```

进入 Play 模式，等一次扫描后，右键 `PerceptionReader` 组件标题，选择 **Demo > Read Perception**：Console 应显示 `Visible: True`，已知位置为 `(2, 0)`。把目标移到 `(-2, 0, 0)`，等待下一次扫描后在 10 秒记忆期内再次读取，应显示 `Visible: False`，已知位置仍为最后看见的 `(2, 0)`。此时选择 **Demo > Report Footstep**，再等观察者更新并读取，已知位置应变为 `(-2, 0)`，感官为 `Hearing`。

观察者由 Unity `Update` 自动扫描所属 Scene 的 `PhysicsScene2D` 并提交结果；暂停时不扫描或推进记忆。若添加遮挡物，给它添加非 Trigger 的 `Collider2D`，将它所在层加入 **Obstacle Layers**。运行时创建观察者应先停用 GameObject，再调用 `Configure(world, settings)`，最后启用；运行中改配置使用 `UpdateSettings`，朝向使用 `SetFacingDirection`。目标停用会注销身份，旧记录在观察者下一批失效。

### 纯代码组合

不建立 World、Target 或 Observer 组件时，调用方拥有共享注册表、目标句柄、各观察者的核心，以及一条单调时间轴。核心仍使用 Unity 的 `Vector2` 数据类型，但不查询 GameObject 或 Physics。以下是顺序执行的代码片段：把 `using` 放在文件顶部，其余内容放入自己的初始化或演示方法；实际游戏由持有核心的系统提交输入并定期调用 `Advance`，这里的固定时间只用于演示。

```csharp
using System;
using System.Collections.Generic;
using Computerzhuxi.Perception2D;
using UnityEngine;

var registry = new PerceptionTargetRegistry();
object actor = new object();
PerceptionTargetHandle handle = registry.Register(actor);
var core = new PerceptionCore2D(registry, new PerceptionSettings2D());
core.SenseUpdated += change => Debug.Log($"{change.Sense}: {change.Reason}");

var frame = new List<SightObservation2D>
{
    new SightObservation2D(handle, new Vector2(2, 0), Vector2.zero)
};
core.SubmitSightFrame(frame, 1.0);
core.ReportHearing(new Vector2(2, 0), Vector2.zero, 1.0, source: handle, tag: "Footstep");
core.Advance(1.0);
if (core.TryGetObservation(handle, out var info)) Debug.Log($"Visible: {info.IsVisible}");
if (core.TryGetKnownPosition(handle, out PerceptionStimulus known)) Debug.Log(known.Position);

// 没有新视觉结论时只推进时间；完整空帧则结束当前视觉。
core.Advance(2.0);
core.SubmitSightFrame(Array.Empty<SightObservation2D>(), 3.0);
core.Advance(3.0);

// 由目标身份的所有者注销；各核心在下一批发布失效通知。
registry.Unregister(handle);
core.Advance(4.0);
```

每个核心独立保存记忆。多个核心共用目标时，仅停止其中一个核心不应注销共享句柄；应停止驱动该核心、解除它的事件订阅，并释放引用。若需要从 Unity 物理场景生成完整帧，可用 `PhysicsSightScanner2D.Scan`；它要求 Collider 所属目标挂 `PerceptionTarget2D`，并绑定到与核心共用的 World 注册表。没有这些组件时自行构造 `SightObservation2D` 帧。

## 🎮 常用操作

组件与核心都支持 `SetSenseEnabled(PerceptionSense.Sight, false)`、`SetSenseEnabled(PerceptionSense.Hearing, false)`、`ForgetSense`、`ForgetTarget`、`ClearMemory` 和 `ResetForReuse`；组件方法接收 `PerceptionTarget2D`，核心方法接收 `PerceptionTargetHandle`。清理记忆不改变感官开关。

运行中修改设置时，先取得 `var settings = observer.Core.Settings`（纯代码入口为 `core.Settings`），修改需要的字段，再调用 `observer.UpdateSettings(settings)` 或 `core.UpdateSettings(settings)`。`Settings` 返回配置副本，直接改该副本不会更新核心；传入新的默认配置会同时覆盖此前的层、距离等设置。

自定义视觉时，组件切换扫描器、物理场景或自动/手动视觉模式会结束旧持续视觉，保留听觉和有效记忆。手动模式先调用 `observer.SetAutomaticSight(false)`，再调用 `observer.SubmitSightFrame(frame, time)`；自动模式下该提交会抛异常。自行管理来源的核心可调用 `EndSightSource()`，异步帧可携带捕获时的 `SightSourceGeneration`；这些输入约束详见感知契约。

读取已提交状态时使用 `Observations`、`HeardEvents`、`TryGetObservation` 或 `TryGetKnownPosition`。`TargetPerceptionInfo.IsVisible` 只表示当前视觉；`TryGetKnownPosition` 优先当前视觉，否则采用最新有效记忆。`Observations` 和 `HeardEvents` 是复用的只读视图，不能跨更新长期枚举；需要保留结果时复制结构体值。

## 🔔 事件与使用规则

`SenseUpdated` 报告一次感官变化，`PerceptionChange` 提供 `Handle`、`Sense`、`Reason` 和 `Stimulus`。原因包括 `Acquired`、`Lost`、`Heard`、`SenseDisabled`、`Expired`、`Cleared`、`TargetInvalidated` 与 `SourceChanged`。`TargetForgotten` 仅在该代目标完全没有感官记录时发布；`ObservationsUpdated` 表示已提交查询视图变化。事件读取的是本批最终状态，回调内提交的命令在下一批执行；不要在回调中重入 `Advance`。

- **完整视觉帧**：不提交表示没有新结论；提交空帧结束全部当前视觉；非空帧结束未出现目标。一个核心只有一个有效视觉来源，多来源由游戏合并。
- **声音入口**：`World.ReportNoise` 按开关和距离筛选接收者；`core.ReportHearing` 接收已经确认听到的事实，不再判断距离。无来源声音用独立事件身份保存。
- **身份与时间**：目标由身份所有者注销；重注册会取得新代次，旧句柄不复活。核心输入与 `Advance` 使用同一条有限、单调时间轴；已接受帧即使随后切换来源，也仍占用其时间水位。
- **生命周期**：已激活 World 销毁时使活动句柄失效，核心下一批清理并通知。从未激活的 World 不保证收到 `OnDestroy`，销毁前须由身份所有者显式解绑或停用目标。`ResetForReuse` 后先推进重置批次，再提交新生命周期输入。

详尽的来源切换、记忆过期、批次与物理扫描规则见 [感知契约](Documentation~/Contracts.md)。

## 🎮 示例

1. 在 Package Manager 选中 **Perception 2D**，导入 Samples 下的 **Basic Perception 2D**。
2. 新建 2D 场景并保留 Main Camera，新建一个空物体，给它添加导入的 `BasicPerceptionDemo` 脚本，再进入 Play 模式。示例没有预制场景，`Start` 会自动创建 World、观察者、目标与墙；相机由场景提供。
3. Game 视图应显示操作面板，目标记录初始为 `visible=True`。点击 **Move target behind wall / restore** 后应变为 `visible=False`；点击 **Report target footstep** 后记录出现 `hearing=True`。
4. 点击 **Report anonymous sound** 后，`Anonymous memories` 计数应增加；关闭 **Hearing** 再报告声音，不应增加听觉记录。面板也可切换视觉和清空记忆；视觉仍开启时，清空后目标可以在后续扫描重新被发现。

导入路径、相机设置和完整操作见 [示例说明](Samples~/BasicPerception2D/README.md)。仓库维护者也可打开 `Projects/Perception2DLab`；它通过本地路径引用本包，不维护第二份运行时实现。

## 迁移说明

从 0.1.0 迁移时，旧组件的 `world`、观察点、配置、朝向和 Gizmo 序列化字段仍可用，常用组件查询和事件继续保留。运行中改参数使用 `UpdateSettings`；停用时的 `Configure` 会重建核心。跨 World 绑定会建立新核心并清除旧环境记忆。新的纯代码集成使用注册表、完整视觉帧与 `Advance`，不要依赖内部 `PerceptionSession2D`。固定 0.1.0 标签无法获得这些候选 API。

版本变化见 [CHANGELOG](CHANGELOG.md)。
