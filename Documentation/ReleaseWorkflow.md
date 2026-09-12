# 包发布流程

## 版本规则

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
10. 将本次结论归档到 Documentation/History/<System>。

Tools/PublishHealth.ps1 从 package.json 读取 Health 版本。运行前应确认目标提交；凭据不得写入任何文件。

## 不可变发布

已发布标签永久保留原指向。修复旧版本时创建新版本，不强推、不删除重建、不移动标签。

## 当前 Health 发布

- 包：com.computerzhuxi.health@1.0.1
- 标签：health-v1.0.1
- 固定提交：d05303f629393a41f93d3eba3a45e60dc4f177ca
- Git URL：https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1

私有仓库使用者和 CI 必须通过凭据管理器获得最小只读权限。
