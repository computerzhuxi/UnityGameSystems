# 包发布流程

## 版本规则

新系统的契约仍在探索时从 `0.1.0` 开始；准备承诺稳定兼容边界时再进入 `1.0.0`。`0.x` 版本同样不可覆盖历史标签。

|变化|版本变化示例|
|---|---|
|兼容性错误修复|1.0.1 → 1.0.2|
|新增向后兼容功能|1.0.1 → 1.1.0|
|破坏公共 API、序列化或既有行为|1.x → 2.0.0|

## 发布步骤

1. 保证工作区只包含目标改动。
2. 更新测试、包文档、CHANGELOG.md 和 package.json 版本。
3. 在 Lab 中完成全量验证。
4. 检查运行时 DLL 依赖和内容资产差异。
5. 提交并推送发布提交。
6. 创建与包版本对应的带注释标签，例如 health-v1.0.2。
7. 推送标签并只读核对远端标签指向。
8. 在全新独立工程中通过 Git URL 安装并复验。
9. 消费项目主动更新标签和锁文件，再运行集成测试。
10. 将本次结论归档到 `Documentation/History/<System>/<Version>`。

发布标签统一使用 `<system>-v<semver>`，例如 `health-v1.0.1`、`perception2d-v0.1.0`。发布与消费项目提交都使用显式文件白名单；Unity 自动改写的动态字体、TimeManager、渲染设置和其他非目标资产必须排除。

第 8 步必须从远端标签安装，不能继续使用本地 `file:` 引用或原项目 PackageCache 冒充发布验证。失败与重试日志都应保留并解释。

## 复测范围

|变更|最低要求|
|---|---|
|Runtime、API、序列化、行为|包、Lab、独立 Sample、消费项目全量验证|
|Editor、Gizmo|相关 EditMode 与人工视觉验收|
|manifest、lock、标签|固定 Git 标签干净导入与消费项目测试|
|纯文档|链接、生成、渲染、幂等和 diff 检查|

报告必须注明证据来自本轮还是历史，不把旧 Standalone 构建写成本轮新构建。

Tools/PublishHealth.ps1 从 package.json 读取 Health 版本。运行前应确认目标提交；凭据不得写入任何文件。

## 不可变发布

已发布标签永久保留原指向。修复旧版本时创建新版本，不强推、不删除重建、不移动标签。

## 当前 Health 发布

- 包：com.computerzhuxi.health@1.0.1
- 标签：health-v1.0.1
- 固定提交：d05303f629393a41f93d3eba3a45e60dc4f177ca
- Git URL：https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1

私有仓库使用者和 CI 必须通过凭据管理器获得最小只读权限。

## 当前 Perception 2D 发布

- 包：com.computerzhuxi.perception2d@0.1.0
- 标签：perception2d-v0.1.0
- 固定提交：ddeaa63e8f2e17d80a31ccb4eda1edf4477e8821
- Git URL：https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.perception2d#perception2d-v0.1.0

## 当前 Navigation2D 发布

- 包：com.computerzhuxi.navigation2d@0.2.0
- 标签：navigation2d-v0.2.0
- 固定提交：2c5d452cf6cc801e3d9f4a9b1cd66dc8ae55cf49
- Git URL：https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.2.0
- 验收：[发布记录](History/Navigation2D/0.2.0/ReleaseVerification.md)

## 历史 Navigation2D 0.1.0 发布

- 包：com.computerzhuxi.navigation2d@0.1.0
- 标签：navigation2d-v0.1.0
- 固定提交：8ebe60c54a7db122cd153c5ca26979bcbb4c2b57
- Git URL：https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.1.0
- 验收：[发布记录](History/Navigation2D/0.1.0/ReleaseVerification.md)
