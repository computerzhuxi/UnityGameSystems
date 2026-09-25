# Perception2DLab

Perception 2D 的独立开发与验证工程。它通过本地 UPM 路径引用 `Packages/com.computerzhuxi.perception2d`，不包含 ARPG 运行时依赖。

## 打开与运行

1. 使用 Unity 6000.3 打开本目录。
2. 加载 `Assets/PerceptionDemo.unity`。
3. 进入 Play Mode，使用面板切换视觉与听觉、上报目标声音或无来源声音、移动目标到遮挡物后方，以及清空记忆。
4. 运行包的 EditMode 与 PlayMode 测试，确认公共行为和生命周期。

测试、构建日志和临时输出写入仓库根目录下被 Git 忽略的 `Artifacts`，不要提交 Lab 的 `Library`、`Temp`、`Logs` 或生成项目文件。

## 与 Package Sample 的关系

包内 `Samples~/BasicPerception2D` 面向使用者展示最小接入；本 Lab 面向维护者执行开发、交互和构建验证。

Lab 的 `BasicPerceptionDemo.cs` 与 Package Sample 是字节一致的镜像。修改其中任何一份时必须同步另一份并核对哈希；后续版本应改为 Lab 独立控制器或自动生成镜像。

## 0.2.0 候选回归

EditMode 测试覆盖无 GameObject 核心、完整视觉帧、注册代次、独立记忆、声音、时间与事件批次；PlayMode 测试覆盖启停、对象池、暂停及独立物理场景。运行时源切换可按包内 API 文档操作。人工视觉检查：选中观察者打开 Gizmo，确认青色视距、黄色丢失距离、绿色听觉范围、紫色丢失记忆位置；运行场景用面板移动目标穿过遮挡物并检查通知。自动测试通过不能替代此项人工检查。

正式运行时源码只允许位于 `Packages/com.computerzhuxi.perception2d/Runtime`；Lab 不得维护第二份感知实现。
