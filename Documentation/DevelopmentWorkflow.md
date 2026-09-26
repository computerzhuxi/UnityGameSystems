# 可复用系统开发流程

## 修改位置与入口

在 `Packages/<package-id>` 修改唯一正式源码；Lab 通过本地 UPM 路径引用它。通用规则和通用组件进入包，ARPG 的命中、成长、奖励、状态机、UI、存档格式及适配留在 ARPG。改变公共行为前先列出组件式与编程式入口的共同契约、权威状态、生命周期、序列化风险与消费项目调用。

修改后同步包测试、Sample、随包文档及 CHANGELOG。包 README 指向 GettingStarted、API/Contracts；仓库文档只维护开发、验证和发布流程。消费项目主动升级固定标签并验证，不能修改 `Library/PackageCache`。

## 按用途验证

|用途|何时运行|证据|
|---|---|---|
|Contract|修改规则、公共命令或事件时|定向包测试；两种入口共享的行为应有断言。|
|Integration|修改组件、序列化、Sample、Lab 或消费适配时|相关 EditMode/PlayMode、真实场景/Prefab 和业务链。|
|Validation|发布候选、版本引用或高风险迁移时|完整包与 Lab、独立 Sample、构建/冒烟、干净 Git 安装和适用的人工验收。|

这些是选择测试的目的，不要求新增目录。开发中先运行相关测试；候选发布才做完整验证。Editor/Gizmo 修改补人工视觉；纯文档检查链接与渲染；manifest/lock/标签改动做干净导入。消费项目仅通过公共 API、真实组件和业务链验证。

## 工具入口

先关闭对应 Lab 编辑器。三个入口使用相同参数：`-MirrorOnly` 只读核对 Sample 镜像、不需 Unity；默认提供 `-Platform EditMode|PlayMode -TestFilter '<测试名或筛选表达式>'` 做定向测试；`-Full` 才运行两种模式、构建和 Standalone 冒烟。`-TimeoutSeconds` 与 `-SmokeTimeoutSeconds` 可调节超时。每轮日志、XML 和结果 JSON 放在独立的 `Artifacts/Logs/<System>/<时间-随机码>`；脚本拒绝覆盖已有证据，并要求 XML 中实际有通过的用例。

    ./Tools/ValidateHealth.ps1 -MirrorOnly
    ./Tools/ValidatePerception2D.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -Platform PlayMode -TestFilter 'PerceptionLifecycleTests'
    ./Tools/ValidateNavigation2D.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -Full

完整模式仍需人工核对视觉/听觉以及运行后的动态字体、TimeManager、渲染设置、ProjectSettings、meta、GUID 和非目标资产差异。报告区分本轮和历史证据；提交使用显式白名单。Unity 进程不得与已打开的对应 Lab 同时操作同一工程。

## 禁止事项

- 不把凭据写入 Git URL、manifest、脚本或日志；不让公共包引用具体游戏。
- 不在多个项目维护同一运行时实现，不让 Sample/Lab 无保护地复制同一文件。
- 不覆盖已发布标签，不把旧构建或旧测试当成本轮结果。
