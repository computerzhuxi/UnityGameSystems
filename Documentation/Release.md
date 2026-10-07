# Release

仓库内的每个 Package 均独立维护版本号并进行独立发布。

## Version

版本号统一记录在对应 Package 的 `package.json` 文件中，并严格遵循 [Semantic Versioning (语义化版本)](https://semver.org/) 规范 (`MAJOR.MINOR.PATCH`)：

`package.json` 表示当前源码版本，已发布版本以对应的固定 Git Tag 为准。

- **Patch (修订号)**：向下兼容的 Bug 修复。
- **Minor (次版本号)**：向下兼容的新功能新增。
- **Major (主版本号)**：包含破坏性更改、不向下兼容的 API 修改。

## Before Release

在正式打 Tag 发布前，请执行以下检查清单：

1. **更新版本号**：修改对应包内 `package.json` 的 `version` 字段。
2. **编写日志**：更新 `CHANGELOG.md`，清晰记录本次版本的变动内容。
3. **运行测试**：执行与该包相关的自动化测试，确保 EditMode 与 PlayMode 测试通过。
4. **环境验证**：在对应 `Projects/<Lab>` 中运行验证，确保该 Package 的核心功能与 `Samples~` 示例表现正常。
5. **文档校验**：确认 `README.md` 中的说明、示例代码与当前版本的实际 API 保持一致。

## Tag

为了在单仓库中管理多个包，发布时需使用携带包名的独立 Git Tag 格式：`<package_name>-v<version>`。

**示例：**
- `health-v1.0.1`
- `perception2d-v0.2.0`
- `navigation2d-v0.3.0`

> ⚠️ **警告**：已发布的 Git Tag 视为不可变记录。严禁修改、覆盖或移动已推送到远端的 Tag。若出现紧急问题，请发布新的 Patch 版本。

## Install

消费方项目（使用这些包的游戏工程）应通过固定的 Tag 进行安装，以确保版本锁定和工程的稳定性。

在 Unity Package Manager 中通过 Git URL 导入时，格式如下：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/<package-id>#<tag>
```

推送发布 Tag 后，再从远端固定 Tag 在全新工程安装并复验。
