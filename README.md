# UnityGameSystems

面向多个 Unity 项目的可复用游戏系统库。`Packages` 保存正式 UPM 包；`Projects` 是开发、测试和示例宿主。

## 系统入口

|系统|职责|包入口|开发宿主|
|---|---|---|---|
|Health|生命值、上限、伤害、治疗、死亡与复活|[Health](Packages/com.computerzhuxi.health/README.md)|[HealthLab](Projects/HealthLab/README.md)|
|Perception 2D|视觉、事件式听觉、记忆与目标生命周期|[Perception 2D](Packages/com.computerzhuxi.perception2d/README.md)|[Perception2DLab](Projects/Perception2DLab/README.md)|
|Navigation 2D|路径查询、可选代理与局部避让|[Navigation 2D](Packages/com.computerzhuxi.navigation2d/README.md)|[Navigation2DLab](Projects/Navigation2DLab/README.md)|

每个包的 README 指向 GettingStarted、API 与契约文档。GettingStarted 分别说明组件式装配和通过公开类/接口自行组合驱动；两条路线遵循同一规则和权威状态。`package.json` 表示当前源码版本，已发布固定标签与历史证据见[发布流程](Documentation/ReleaseWorkflow.md)及各系统历史索引。消费项目通过自身 manifest 和 lock 文件选择安装版本。

## 仓库结构

    Packages/       正式包源码、测试、示例与随包文档
    Projects/       本地包的开发与验证工程
    Tools/          定向/全量验证及人工发布工具
    Documentation/ 仓库级规范、流程与历史记录
    Artifacts/      本地验证证据，不提交 Git

## 开发与验证

- 修改系统：[开发流程](Documentation/DevelopmentWorkflow.md)；设计新包：[包规范](Documentation/PackageStandard.md)；提取已有游戏系统：[提取手册](Documentation/SystemExtractionPlaybook.md)。
- 理解依赖与状态所有权：[仓库架构](Documentation/RepositoryArchitecture.md)；准备发布：[发布流程](Documentation/ReleaseWorkflow.md)。
- 验证脚本：`Tools/ValidateHealth.ps1`、`Tools/ValidatePerception2D.ps1`、`Tools/ValidateNavigation2D.ps1`。`-MirrorOnly` 不需 Unity；默认传 `-TestFilter` 做定向测试；候选发布时显式传 `-Full` 执行 EditMode、PlayMode、构建和冒烟。运行 Unity 前关闭对应 Lab 编辑器。每轮日志独立写入被忽略的 `Artifacts/Logs`。

    ./Tools/ValidateHealth.ps1 -MirrorOnly
    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -Platform EditMode -TestFilter 'HealthCoreTests'
    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -Full

Navigation Runtime 只提供路径、导航建议与避让计算；`QuickStartExampleMover2D` 是 Sample/Lab 中的演示适配器，消费项目自行决定运动实现。人工视觉验收操作见各 Lab README。
