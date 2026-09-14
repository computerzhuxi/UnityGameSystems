# 可复用系统开发流程

## 修改已有系统

以任一系统为例：

1. 在 `Packages/<package-id>` 修改唯一正式源码。
2. 用 Unity 打开对应 `Projects/<System>Lab`；它通过本地 UPM 路径引用该包。
3. 先运行针对改动的测试，再运行完整验证。
4. 更新包文档、测试和 CHANGELOG.md。
5. 发布新版本后，让消费项目主动升级并运行集成测试。

当前电脑已有仓库时不需要重新克隆，也不需要新建 Unity 工程。换电脑时克隆整个仓库，然后直接打开已有 Lab。不要直接修改消费项目的 `Library/PackageCache`。

## 修改位置判断

修改前先回答：这项能力是否对多个游戏具有相同语义？

- 通用生命规则、通用事件、通用 Unity 组件：修改 Health 包。
- ARPG 的命中、成长、奖励、状态机、UI 或存档格式：留在 ARPG 或其适配层。
- 只有多个项目都出现同一种稳定需求时，才考虑把能力下沉到公共包。

修改前还要判断现有行为是否属于公共契约。兼容修复直接进入包；项目规则留在适配层；需要改变公共行为时先更新行为矩阵、测试和版本计划。

## 本地验证

关闭 HealthLab 后运行：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe'

至少检查适用的 Core 边界值、Unity 组件和序列化、EditMode、PlayMode、Sample 独立导入、Standalone，以及 meta、GUID 和非目标资产差异。按变更风险选择范围：Runtime/API 变化全量验证；Editor/Gizmo 变化补人工视觉；纯文档只做链接、生成和渲染检查；Git 引用变化必须干净导入。

包内部行为由包测试验证，消费项目只允许通过公开 API 和真实组件验证。禁止为了消费项目测试新增友元程序集。

Unity 运行后检查动态字体、TimeManager、渲染设置和 ProjectSettings 等自动改写。提交使用显式白名单，测试日志和构建产物只写入被忽略的 Artifacts。

## 禁止事项

- 不修改消费项目的 Library/PackageCache，它只是 Unity 生成的缓存。
- 不把凭据写入 Git URL、manifest、脚本或日志。
- 不让公共包引用 ARPG 等消费项目。
- 不通过复制源码在多个项目分别维护同一系统。
- 不移动或覆盖已经发布的版本标签。
- 不让 Sample 与 Lab 无保护地维护同一份源码副本。
- 不把上一次构建或测试结果描述成本轮重新验证。
