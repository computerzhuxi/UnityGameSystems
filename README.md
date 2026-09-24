# UnityGameSystems

面向多个 Unity 项目的个人可复用游戏系统库。正式源码以 UPM 包保存；Projects 只提供开发、测试和示例宿主，不是包运行时的一部分。

## 系统目录

|系统|包名|已发布版本|职责|文档|
|---|---|---|---|---|
|Health|com.computerzhuxi.health|1.0.1|当前生命、生命上限、伤害、治疗、死亡与复活|[快速入门](Packages/com.computerzhuxi.health/README.md)|
|Perception 2D|com.computerzhuxi.perception2d|0.1.0|二维视觉、事件式听觉、分感官记忆与目标生命周期|[快速入门](Packages/com.computerzhuxi.perception2d/README.md)|
|Navigation 2D|com.computerzhuxi.navigation2d|0.1.0|同层网格路径查询与 Physics2D 净空验证|[快速入门](Packages/com.computerzhuxi.navigation2d/README.md)|

新增系统时，应先建立独立包和测试宿主，再在本表登记。公共包不得引用某个具体游戏项目。

## 仓库结构

    UnityGameSystems
    ├─ Packages/       可发布的正式包源码
    ├─ Projects/       包的开发与验证工程
    ├─ Tools/          自动验证和发布脚本
    ├─ Documentation/ 仓库级规范、流程与历史记录
    └─ Artifacts/      本地验证证据，不提交 Git

## 我应该读什么

- 在游戏中使用系统：[Health 快速入门](Packages/com.computerzhuxi.health/README.md)或 [Perception 2D 快速入门](Packages/com.computerzhuxi.perception2d/README.md)。
- 修改系统源码：[开发流程](Documentation/DevelopmentWorkflow.md)，并用 [HealthLab](Projects/HealthLab/README.md) 或 [Perception2DLab](Projects/Perception2DLab/README.md) 验证。
- 建立新的可复用系统：[包规范](Documentation/PackageStandard.md)。
- 把游戏内系统提取成可复用包：[系统提取手册](Documentation/SystemExtractionPlaybook.md)。
- 发布新版本：[发布流程](Documentation/ReleaseWorkflow.md)。
- 理解仓库组织方式：[仓库架构](Documentation/RepositoryArchitecture.md)。
- 查看历史验收：[Health](Documentation/History/Health/README.md)或 [Perception 2D](Documentation/History/Perception2D/README.md)。

## 验证系统

先关闭对应 Lab 编辑器，再运行该系统的验证入口。Health 当前提供：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe'

测试 XML、Unity 日志、Windows 构建和程序冒烟标记统一写入被 Git 忽略的 Artifacts。
新增系统应复用通用验证流程；在通用工具完成前，不得把一次性临时脚本当作长期入口。

## Navigation 2D

- [Navigation 2D](Packages/com.computerzhuxi.navigation2d/README.md)：探索期版本 0.2.0，固定标签 `navigation2d-v0.2.0`；[Lab](Projects/Navigation2DLab/README.md)与[历史记录](Documentation/History/Navigation2D/README.md)。

0.2.0 包含 2D Agent、局部避让与 Inspector 导航。正式 Runtime 只负责路径、导航建议与避让计算；QuickStartExampleMover2D 仅在 Sample/Lab 演示消费建议，实际项目提供自己的运动组件。发布前的人工验收与独立复核见 [0.2.0 候选验收](Documentation/History/Navigation2D/0.2.0/QuickStartAcceptance.md)；远程标签与 ARPG 接入结果见 [0.2.0 发布验证](Documentation/History/Navigation2D/0.2.0/ReleaseVerification.md)。已发布的 0.1.0 标签保持不变。
