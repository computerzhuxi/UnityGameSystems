# Basic Health Demo

> [上手指南](../../Documentation~/GettingStarted.md) · [文档导航](../../Documentation~/Index.md)

以下按钮与流程对应本地 **Unreleased** Sample。固定 `health-v1.0.1` 标签不包含新增的组件 `Restore` 按钮、独立 Core 演示和销毁解绑；通过该标签导入时应以标签自身的示例为准。

通过 Package Manager 导入 Sample 后打开 `BasicHealthDemo.unity`。上半部分的组件按钮依次支持 Damage、Heal、Kill、Revive、Change Maximum（保持或回满）和 `Restore 5 / 10`。下半部分是另一个独立的 `Health(6, 8)`：`Core Damage 2` 和 `Core Restore 6 / 8` 展示直接组合及强类型变化事件。两个区域各有自己的状态和事件显示，不代表同一角色需要两份生命值。

组件初始为 `10 / 10 Alive`。依次点击 `Damage 3`、`Heal 3`、`Kill`、`Revive full`、输入默认上限 15 后点击 `Change Maximum / Preserve`、`Change Maximum / Refill`、`Restore 5 / 10`，结果依次为 `7/10`、`10/10`、`0/10 Dead`、`10/10 Alive`、`10/15`、`15/15`、`5/10`。每次实际变化让 `UnityEvent #` 加一；`OnChanged` 在 Scene 和 `HealthDemo.prefab` 中持久化绑定到 `RecordUnityEvent`。独立 Core 从 `6/8` 经 Damage 2 到 `4/8`，经 Restore 回到 `6/8`，其事件显示 `Reason` 与最终快照。

Standalone 启动参数 `--health-smoke` 执行两条入口的命令链，并以 `HEALTH_SMOKE_PASS` 与退出码 0 表示成功。此检查不替代从已发布包重新导入 Sample 的验收。
