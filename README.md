# UnityGameSystems

面向多个 Unity 项目的个人可复用游戏系统库。正式源码以 UPM 包保存；Projects 只提供开发、测试和示例宿主，不是包运行时的一部分。

## 系统目录

|系统|包名|稳定版本|职责|文档|
|---|---|---|---|---|
|Health|com.computerzhuxi.health|1.0.1|当前生命、生命上限、伤害、治疗、死亡与复活|[快速入门](Packages/com.computerzhuxi.health/README.md)|

新增系统时，应先建立独立包和测试宿主，再在本表登记。公共包不得引用某个具体游戏项目。

## 仓库结构

    UnityGameSystems
    ├─ Packages/       可发布的正式包源码
    ├─ Projects/       包的开发与验证工程
    ├─ Tools/          自动验证和发布脚本
    ├─ Documentation/ 仓库级规范、流程与历史记录
    └─ Artifacts/      本地验证证据，不提交 Git

## 我应该读什么

- 在游戏中使用 Health：[Health 快速入门](Packages/com.computerzhuxi.health/README.md)。
- 修改 Health 源码：[开发流程](Documentation/DevelopmentWorkflow.md)和 [HealthLab 说明](Projects/HealthLab/README.md)。
- 建立新的可复用系统：[包规范](Documentation/PackageStandard.md)。
- 发布新版本：[发布流程](Documentation/ReleaseWorkflow.md)。
- 理解仓库组织方式：[仓库架构](Documentation/RepositoryArchitecture.md)。

## 快速验证 Health

先关闭 HealthLab 编辑器，然后运行：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe'

测试 XML、Unity 日志、Windows 构建和程序冒烟标记统一写入被 Git 忽略的 Artifacts。
