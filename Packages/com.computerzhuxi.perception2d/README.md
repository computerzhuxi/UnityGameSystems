# Perception 2D

Unity 6000.3 的通用二维视觉、听觉和分感官记忆。当前源码为 **0.2.0 未发布候选**；下面的固定 Git 标签仍指向已发布的 0.1.0，不包含本次重构。

## 接入

通过 Package Manager 的 Add package from git URL 安装：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.perception2d#perception2d-v0.1.0
```

本地开发可从本仓库的 package.json 安装。默认组件路径：创建 `PerceptionWorld2D`，给目标添加 `PerceptionTarget2D`，给监听者添加 `PerceptionObserver2D`，用 Inspector 的 world 引用或 `Bind` 装配。同一目标的 Collider 放在身份组件的子层级。观察者自动从所属 Scene 的 `PhysicsScene2D` 扫描；目标和障碍使用同一场景。

运行时构造观察者时，先停用 GameObject，添加组件并调用 Configure，再启用。运行时 SetFacingDirection 提交世界方向；零向量保留最后有效方向。动态生成目标必须绑定环境。音效播放不自动产生 AI 声音，游戏调用 world.ReportNoise(new NoiseEvent2D(position, source: target))。

纯 API 路径：建立共享 `PerceptionTargetRegistry`，给每个观察者建立独立 `PerceptionCore2D`。调用方提供完整 `SightObservation2D` 帧、已确认的声音和时间，再调用 `Advance`。不需要 GameObject、MonoBehaviour、Transform 或 Physics 查询。也可直接调用 `PhysicsSightScanner2D.Scan` 生成视觉帧。

`Observations` 是目标列表，每个目标有独立 Sight/Hearing。`HeardEvents` 保存无来源声音。列表为复用的只读视图，若需历史请自行复制值。读取 `IsVisible` 判断持续视觉，不把听觉记忆当成持续发声。

导入 Basic Perception 2D Sample，把 BasicPerceptionDemo 放到空场景并添加正交摄像机，即可用按钮验证视觉、听觉、声音和记忆。正式 Lab 位于 Projects/Perception2DLab。

公共接口见 [API](Documentation~/API.md)，详细行为见 [Contracts](Documentation~/Contracts.md)。包不负责阵营、生命、威胁评分、导航、调查完成或 AI 状态机。
