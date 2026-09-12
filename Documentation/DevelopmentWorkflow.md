# 可复用系统开发流程

## 修改已有系统

以 Health 为例：

1. 在 Packages/com.computerzhuxi.health 修改正式源码。
2. 用 Unity 打开 Projects/HealthLab；它通过本地 UPM 路径引用该包。
3. 先运行针对改动的测试，再运行完整验证。
4. 更新包文档、测试和 CHANGELOG.md。
5. 发布新版本后，让消费项目主动升级并运行集成测试。

当前电脑已有仓库时不需要重新克隆，也不需要新建 Unity 工程。换电脑时克隆整个仓库，然后直接打开已有 Lab。

## 修改位置判断

修改前先回答：这项能力是否对多个游戏具有相同语义？

- 通用生命规则、通用事件、通用 Unity 组件：修改 Health 包。
- ARPG 的命中、成长、奖励、状态机、UI 或存档格式：留在 ARPG 或其适配层。
- 只有多个项目都出现同一种稳定需求时，才考虑把能力下沉到公共包。

## 本地验证

关闭 HealthLab 后运行：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe'

至少检查 Core 边界值、Unity 组件和序列化、EditMode、PlayMode、Sample 独立导入、Standalone，以及 meta、GUID 和非目标资产差异。

## 禁止事项

- 不修改消费项目的 Library/PackageCache，它只是 Unity 生成的缓存。
- 不把凭据写入 Git URL、manifest、脚本或日志。
- 不让公共包引用 ARPG 等消费项目。
- 不通过复制源码在多个项目分别维护同一系统。
- 不移动或覆盖已经发布的版本标签。
