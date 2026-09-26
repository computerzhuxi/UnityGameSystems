# Perception2DLab

Perception 2D 的独立开发与验证工程，通过本地 UPM 路径引用仓库中的包，不依赖 ARPG。当前本地源码为 0.2.0 未发布候选；已发布的 `perception2d-v0.1.0` 标签仍是旧 API。

## 人工查看

使用 Unity `6000.3.21f1` 打开 `D:\Unity\Project\UnityGameSystems\Projects\Perception2DLab`，加载 `D:\Unity\Project\UnityGameSystems\Projects\Perception2DLab\Assets\PerceptionDemo.unity` 并进入 Play Mode。场景与正交相机已提交，构建脚本不会临时生成它们。按面板实际按钮验收：

1. 初始目标位于观察者前方，目标行应显示 `visible=True`。按 **Move target behind wall / restore**，目标行应变为 `visible=False`，但保留最后成功视觉位置；再按一次恢复后应重新显示 `visible=True`。
2. 按 **Report target footstep**，目标行应出现 `hearing=True`，最近通知显示听觉 `Heard`。按 **Report anonymous sound**，`Anonymous memories` 数量应增加，且不应凭空出现目标行。
3. 按 **Hearing: True** 关闭听觉，再按上述两个声音按钮，不应新增听觉通知、目标听觉事实或匿名声音记忆；已有记忆可按其时长自然过期。
4. 先按 **Sight: True** 关闭视觉并等待一帧，再按 **Clear memory**；下一帧目标记录和匿名声音记忆应清空。先关闭视觉可避免自动扫描立即重新发现目标。

若检查 Gizmo，在 Hierarchy 中选中运行时创建的 Observer，在 Inspector 勾选 `Show Debug Gizmos`，并打开 Scene 视图的 Gizmos 开关。应看到青色发现距离、黄色丢失距离、绿色听觉范围；目标失去视觉后，其最后成功视觉位置以紫色标记。

面板展示组件入口；编程式 `PerceptionTargetRegistry`、`PerceptionCore2D`、`PhysicsSightScanner2D` 的创建、驱动、释放及事件责任见 [包上手指南](../../Packages/com.computerzhuxi.perception2d/Documentation~/GettingStarted.md)。两条入口共享核心规则。

## 验证命令

在仓库根目录使用 `Tools/ValidatePerception2D.ps1`：

```powershell
./Tools/ValidatePerception2D.ps1 -MirrorOnly
./Tools/ValidatePerception2D.ps1 -UnityEditor <Unity.exe绝对路径> -Platform EditMode -TestFilter 'Computerzhuxi.Perception2D.Tests.PerceptionCoreTests'
./Tools/ValidatePerception2D.ps1 -UnityEditor <Unity.exe绝对路径> -Full
```

`-MirrorOnly` 只核对 Sample 与 Lab 的演示脚本及 asmdef 镜像；`-Platform` 与 `-TestFilter` 做定向测试；`-Full` 执行 EditMode、PlayMode、现有场景 Windows 构建和独立程序 smoke。运行前确认同一 Lab 工程未被其他 Unity 实例占用。每次验证的日志及结果写入仓库根目录 `Artifacts/Logs/Perception2D/<运行目录>`；Windows 构建产物单独写入 `Artifacts/Perception2D/Build`。这些目录均不应提交，Lab 的 `Library`、`Temp`、`Logs` 和生成项目文件也不应提交。构建器只读取现有 `Assets/PerceptionDemo.unity`，不自动创建场景或改写 EditorBuildSettings。

包内 `Samples~/BasicPerception2D/BasicPerceptionDemo.cs` 与 Lab 镜像字节一致；修改任一份时须同步并运行 `-MirrorOnly`。正式运行时实现只位于包的 `Runtime`，Lab 不维护第二份感知实现。
