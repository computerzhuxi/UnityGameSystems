# Perception2DLab

Perception 2D 的独立开发与验证工程，通过本地 UPM 路径引用仓库中的包，不依赖 ARPG。当前本地源码为 0.2.0 未发布候选；已发布的 `perception2d-v0.1.0` 标签仍是旧 API。

## 人工查看

使用 Unity `6000.3.21f1` 打开 `D:\Unity\Project\UnityGameSystems\Projects\Perception2DLab`，加载 `D:\Unity\Project\UnityGameSystems\Projects\Perception2DLab\Assets\PerceptionDemo.unity` 并进入 Play Mode。场景与正交相机已提交，构建脚本不会临时生成它们。按面板实际按钮验收：

1. 初始目标位于观察者前方，目标行应显示 `visible=True`。按 **Move target behind wall / restore**，目标行应变为 `visible=False`，但保留最后成功视觉位置；再按一次恢复后应重新显示 `visible=True`。
2. 按 **Report target footstep**，目标行应出现 `hearing=True`，最近通知显示听觉 `Heard`。按 **Report anonymous sound**，`Anonymous memories` 数量应增加，且不应凭空出现目标行。
3. 按 **Hearing: True** 关闭听觉，再按上述两个声音按钮，不应新增听觉通知、目标听觉事实或匿名声音记忆；已有记忆可按其时长自然过期。
4. 先按 **Sight: True** 关闭视觉并等待一帧，再按 **Clear memory**；下一帧目标记录和匿名声音记忆应清空。先关闭视觉可避免自动扫描立即重新发现目标。

若检查 Gizmo，在 Hierarchy 中选中运行时创建的 Observer，在 Inspector 勾选 `Show Debug Gizmos`，并打开 Scene 视图的 Gizmos 开关。应看到青色发现距离、黄色丢失距离、绿色听觉范围；目标失去视觉后，其最后成功视觉位置以紫色标记。

面板展示组件入口；编程式 `PerceptionTargetRegistry`、`PerceptionCore2D`、`PhysicsSightScanner2D` 的创建、驱动、释放及事件责任见 [包 README](../../Packages/com.computerzhuxi.perception2d/README.md)。两条入口共享核心规则。

## 验证入口

打开本 Lab，通过 **Window > General > Test Runner** 选择 EditMode 或 PlayMode，按本次修改范围选择测试。例如核心规则的定向入口为 EditMode 中的 `Computerzhuxi.Perception2D.Tests.PerceptionCoreTests`；生命周期和物理扫描需选择相应 PlayMode 测试。发布候选再运行两种模式的包测试，导出本轮 XML，并按[测试规范](../../Documentation/TestingStandard.md)核对具体用例、数量和结果。

需要 Windows 构建时使用现有 `PerceptionLabBuild.Build`。例如在仓库根目录，确认本工程未被其他 Unity 实例占用后运行：

```powershell
& 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -batchmode -quit -projectPath 'Projects/Perception2DLab' -executeMethod PerceptionLabBuild.Build -logFile 'Artifacts/Perception2DLab-build.log'
```

运行前先创建日志目录 `Artifacts`。构建产物为 `Artifacts/Perception2D/Build/Perception2DLab.exe`；用 `--perception-smoke` 启动后，应退出为 0 且日志包含 `PERCEPTION_SMOKE_PASS`。构建器只读取现有 `Assets/PerceptionDemo.unity`，不自动创建场景或改写 EditorBuildSettings。测试、构建和独立程序冒烟分别执行，不能替代上面的人工查看。

日志、XML 和构建产物放在被 Git 忽略的 `Artifacts`；这些目录及 Lab 的 `Library`、`Temp`、`Logs`、生成项目文件不应提交。

包内 `Samples~/BasicPerception2D/BasicPerceptionDemo.cs` 与 Lab `Assets/BasicPerception2D/BasicPerceptionDemo.cs` 是镜像，修改时须同步。使用 `Get-FileHash -Algorithm SHA256` 比较两份脚本和 `Computerzhuxi.Perception2D.Sample.asmdef`，哈希应分别一致。正式运行时实现只位于包的 `Runtime`，Lab 不维护第二份感知实现。
