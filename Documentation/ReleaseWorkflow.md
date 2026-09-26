# 包发布流程

## 版本事实

`package.json` 写当前源码版本，`CHANGELOG.md` 写变化；这两者本身不证明该版本已发布。已发布状态由不可变 Git 标签、固定提交和该次发布的历史记录证明；消费项目安装状态由自己的 manifest、lock 文件证明。历史入口：[Health](History/Health/README.md)、[Perception 2D](History/Perception2D/README.md)、[Navigation 2D](History/Navigation2D/README.md)。

契约仍在探索时可以使用 `0.x`，标签同样不可覆盖。兼容修复递增补丁，兼容新功能递增次版本，破坏公共 API、序列化或既有行为需规划主版本和迁移。不要因三包重构而自动统一版本号。

## 候选与发布步骤

1. 确认目标改动、行为矩阵、兼容与序列化影响。更新测试、Sample、包文档、CHANGELOG 和需要改变时的 `package.json`。
2. 在 Lab 定向验证后，以 `-Full` 完成两种测试、构建及冒烟；按改动补独立 Sample、消费项目、人工视觉/听觉、DLL/资产/GUID 审计。保留失败证据。
3. 检查工作区，只提交目标文件；推送明确的发布提交。记录完整提交 SHA。
4. 创建与包版本对应的带注释标签 `<system>-v<semver>`，并确认本地标签 peeled commit 等于发布提交。旧标签不得移动、删除重建或强推。
5. 只推送目标标签，核对远端标签对象和 peeled commit。远端不可读取时停止，不把失败解释成仓库或标签不存在。
6. 在全新独立工程从远端固定标签安装并复验；不能用本地 `file:` 包或原 PackageCache 代替。
7. 消费项目主动更新依赖和 lock 文件，再运行相关集成测试；归档本轮结论到 `Documentation/History/<System>/<Version>`。

Health 的人工标签推送入口为 `./Tools/PublishHealth.ps1 -TargetCommit '<40位提交SHA>'`。它从 `package.json` 组成标签名，只接受干净且 HEAD 等于显式目标提交的工作树，核对本地注释标签与远端对象，不推送 `main`、不创建仓库。该脚本不代替前述候选验证或人工发布决策。

## 复测范围与报告

|变更|最低要求|
|---|---|
|Runtime、API、序列化、行为|相关 Contract/Integration；发布候选时完整 Validation、消费项目与迁移检查。|
|Editor、Gizmo|相关测试与人工视觉验收。|
|Sample、Lab 场景|镜像检查、独立导入与交互验收。|
|manifest、lock、标签|固定 Git 标签干净导入与消费项目测试。|
|纯文档|链接、渲染和 diff 检查。|

报告标明每项证据是本轮还是历史，不把旧 Standalone 构建写成本轮新构建。发布与消费项目提交使用显式白名单，排除 Unity 自动改写的非目标资产。私有仓库使用者与 CI 通过凭据管理器取得最小只读权限。
