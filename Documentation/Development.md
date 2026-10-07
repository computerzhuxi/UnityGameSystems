# 开发工作流

## 环境准备

使用 `Projects/` 下对应的 Lab 作为开发与验证工程，例如 `HealthLab`、`Perception2DLab`、`Navigation2DLab` 和 `StatsLab`。

各 Lab 通过**本地路径 (Local Path)** 关联 `Packages/` 中的模块。在 Lab 中修改包内容，会直接改动对应 Package 的源码。

## 修改模块

当需要修改或更新某个 Package 时，请遵循以下标准工作流：

1. **修改源码**：在对应 `Packages/<package>` 目录中修改逻辑实现；Lab 不维护第二份实现。
2. **更新测试**：根据修改的范围，更新或新增相关测试用例以保证代码健壮性。
3. **环境验证**：在对应 Lab 中运行测试场景，验证实际的 Unity 运行时行为。
4. **同步文档**：公开用法或 API 变化时，更新 Package 的 `README.md`，让读者只看它就能完成基础使用；精确契约按需写在随包专题文档中。
5. **记录日志**：如果发生了用户可见的更改（如新增功能、修复 Bug、破坏性更新等），请在包内的 `CHANGELOG.md` 中添加对应记录。

## 测试规范

测试采用“按需运行”的原则，优先运行与当前修改内容直接相关的测试。

规则和边界由包测试验证，真实组件、场景与 Prefab 在 Lab 中验证；发布候选再检查独立导入、构建和冒烟。测试选择、证据和停止条件见[测试规范](TestingStandard.md)。

- **EditMode 测试**：适用于纯 C# 逻辑、数据结构和算法。
- **PlayMode 测试**：适用于依赖 Unity 生命周期（如 `Start`/`Update`）、物理引擎、跨帧/协程行为的测试。
- **人工验证**：对于 Inspector 面板绘制、Scene 场景表现及 `Samples~` 示例，需在对应 Lab 中进行实际的交互验证。

> 💡 **原则**：保持敏捷，不要求每次微小修改都强制运行整个仓库的所有测试。

## 示例工程 (Samples)

`Samples~` 目录专用于展示 Package 的典型使用方式与最佳实践。

- Sample 工程应保持**简单、纯粹**，聚焦于核心 API 的演示。
- 请勿将开发过程中的临时草稿代码或脏逻辑残留在 `Samples~` 目录中。
- Sample 与 Lab 确需复制同一演示文件时，注明权威来源并核对镜像；Lab 场景和 Prefab 可独立维护。

## 新增模块

创建新的 Package 时，默认遵循以下精简目录结构：

```text
PackageName/
├── Runtime/        # 核心运行时代码
├── Tests/          # EditMode 与 PlayMode 测试
├── Samples~/       # 示例场景与演示代码
├── README.md       # 模块说明与快速开始
├── CHANGELOG.md    # 更新日志
├── package.json    # UPM 包配置文件
└── *.asmdef        # 程序集定义文件（必填）
```
