# HealthLab

HealthLab 是 `com.computerzhuxi.health` 的长期开发与验证工程。`Packages/manifest.json` 通过本地路径引用仓库中唯一的包源码；无需先发布标签即可检验修改。

## 验证范围

- 运行包的 EditMode Core 与序列化测试、PlayMode 组件生命周期测试。
- 检查 Scene、Prefab、UnityEvent 和 Sample 镜像的序列化引用。
- 用已有 `Assets/BasicHealthDemo/BasicHealthDemo.unity` 构建并执行 Standalone 冒烟。

`Assets/BasicHealthDemo` 是包内 `Samples~/BasicHealthDemo` 的已跟踪镜像，不是本轮从 Package Manager 新导入的资产。`HealthLabBuild.ValidateSampleMirror` 比较脚本、场景、Prefab、asmdef 及其 `.meta`；构建会先执行此检查，但只读取已有场景，不重建或覆盖跟踪资产。发布前从固定 Git 标签在全新工程导入 Sample，仍需单独验收。

## 验证入口

用 Unity 打开本 Lab，通过 **Window > General > Test Runner** 选择 EditMode 或 PlayMode，按本次风险选择测试。组件销毁解绑的定向入口为 PlayMode 中的 `Computerzhuxi.Health.Tests.HealthComponentTests.DestroyDetachesForwardedEvents`。发布候选再运行两种模式的包测试，并导出本轮 XML，按[测试规范](../../Documentation/TestingStandard.md)核对具体用例、数量和结果。

镜像可直接比较包 `Samples~/BasicHealthDemo` 与 Lab `Assets/BasicHealthDemo` 的脚本、场景、Prefab、asmdef 及 `.meta`。需要自动核对这八项时，现有 `HealthLabBuild.ValidateSampleMirror` 通过 Unity `-executeMethod` 调用；`HealthLabBuild.Build` 也会先执行同一检查再构建。例如在仓库根目录，确认本工程没有其他 Unity 实例占用后运行：

```powershell
& 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -batchmode -quit -projectPath 'Projects/HealthLab' -executeMethod HealthLabBuild.Build -logFile 'Artifacts/HealthLab-build.log'
```

运行前先创建日志目录 `Artifacts`。构建输出为 `Artifacts/Build/HealthLab.exe`；用 `--health-smoke` 启动后，应退出为 0 且日志包含 `HEALTH_SMOKE_PASS`。测试、镜像、构建和独立程序冒烟是分别执行的验证入口，均不能替代下面的人工操作或发布时从固定标签新导入 Sample 的验收。

日志、XML 和构建产物放在被 Git 忽略的 `Artifacts`；不要提交 `Library`、`Logs`、`UserSettings` 或构建目录。运行后检查 Git 状态，排除 Unity 自动改写的非目标设置。

## 人工 GUI 验收

用 Unity 6000.3.21f1 打开 `D:/Unity/Project/UnityGameSystems/Projects/HealthLab`，进入 `Assets/BasicHealthDemo/BasicHealthDemo.unity` 的 Play 模式：

1. 初始组件显示 `10/10 Alive`；点击 `Damage 3`、`Heal 3`、`Kill`、`Revive full`，依次应显示 `7/10`、`10/10`、`0/10 Dead`、`10/10 Alive`。
2. 保持上限输入 `15`，点击 `Change Maximum / Preserve`、`Change Maximum / Refill`、`Restore 5 / 10`，依次应显示 `10/15`、`15/15`、`5/10`。每次实际变化使 `UnityEvent #` 加一，最终为 `#7`。
3. 独立 Core 初始显示 `6/8`；点击 `Core Damage 2` 后为 `4/8`，点击 `Core Restore 6 / 8` 后为 `6/8`，事件显示对应原因和快照。
4. 检查场景与 `Assets/BasicHealthDemo/HealthDemo.prefab` 均无 Missing Script，`HealthComponent.OnChanged` 的持久化监听仍指向 `RecordUnityEvent`。

这些操作由人工执行；Lab 自动测试和构建不能替代界面验收。
