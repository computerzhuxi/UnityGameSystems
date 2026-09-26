# HealthLab

HealthLab 是 `com.computerzhuxi.health` 的长期开发与验证工程。`Packages/manifest.json` 通过本地路径引用仓库中唯一的包源码；无需先发布标签即可检验修改。

## 验证范围

- 运行包的 EditMode Core 与序列化测试、PlayMode 组件生命周期测试。
- 检查 Scene、Prefab、UnityEvent 和 Sample 镜像的序列化引用。
- 用已有 `Assets/BasicHealthDemo/BasicHealthDemo.unity` 构建并执行 Standalone 冒烟。

`Assets/BasicHealthDemo` 是包内 `Samples~/BasicHealthDemo` 的已跟踪镜像，不是本轮从 Package Manager 新导入的资产。`HealthLabBuild.ValidateSampleMirror` 比较脚本、场景、Prefab、asmdef 及其 `.meta`；构建会先执行此检查，但只读取已有场景，不重建或覆盖跟踪资产。发布前从固定 Git 标签在全新工程导入 Sample，仍需单独验收。

## 自动验证命令

在仓库根目录运行。轻量镜像检查不启动 Unity，只比较示例脚本与 asmdef：

    ./Tools/ValidateHealth.ps1 -MirrorOnly

针对组件销毁生命周期运行单项 PlayMode 测试；先关闭正在使用 HealthLab 的编辑器：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -Platform PlayMode -TestFilter 'Computerzhuxi.Health.Tests.HealthComponentTests.DestroyDetachesForwardedEvents'

需要 EditMode、PlayMode、构建和 Standalone 冒烟全套验证时，显式指定 `-Full`：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -Full

`-MirrorOnly` 只查脚本和 asmdef；`HealthLabBuild.ValidateSampleMirror` 在构建前检查包括场景、Prefab 和 `.meta` 的八项镜像。测试和构建输出写入被 Git 忽略的 `Artifacts`。不要提交 `Library`、`Logs`、`UserSettings` 或构建目录；运行后检查 Git 状态，排除 Unity 自动改写的非目标设置。

## 人工 GUI 验收

用 Unity 6000.3.21f1 打开 `D:/Unity/Project/UnityGameSystems/Projects/HealthLab`，进入 `Assets/BasicHealthDemo/BasicHealthDemo.unity` 的 Play 模式：

1. 初始组件显示 `10/10 Alive`；点击 `Damage 3`、`Heal 3`、`Kill`、`Revive full`，依次应显示 `7/10`、`10/10`、`0/10 Dead`、`10/10 Alive`。
2. 保持上限输入 `15`，点击 `Change Maximum / Preserve`、`Change Maximum / Refill`、`Restore 5 / 10`，依次应显示 `10/15`、`15/15`、`5/10`。每次实际变化使 `UnityEvent #` 加一，最终为 `#7`。
3. 独立 Core 初始显示 `6/8`；点击 `Core Damage 2` 后为 `4/8`，点击 `Core Restore 6 / 8` 后为 `6/8`，事件显示对应原因和快照。
4. 检查场景与 `Assets/BasicHealthDemo/HealthDemo.prefab` 均无 Missing Script，`HealthComponent.OnChanged` 的持久化监听仍指向 `RecordUnityEvent`。

这些操作由人工执行；Lab 自动测试和构建不能替代界面验收。
