# Basic Perception 2D

演示组件入口的视觉、听觉、感官开关和记忆查询。安装、基本装配与纯代码入口见 [包 README](../../README.md)；本示例不包含预制场景。

## 运行

1. 安装当前候选源码后，在 Package Manager 选中 **Perception 2D**，导入 Samples 下的 **Basic Perception 2D**。脚本会出现在 `Assets/Samples/Perception 2D/0.2.0/Basic Perception 2D/BasicPerceptionDemo.cs`。
2. 新建 **2D 场景**，保留 Main Camera。确认相机位置为 `(0, 0, -10)`、Projection 为 **Orthographic**、Size 为 `5`、Culling Mask 为 **Everything**；示例不会自动创建相机。
3. 新建空物体 `Perception Demo`，添加导入的 `BasicPerceptionDemo` 脚本，进入 Play 模式。

`Start` 会自动创建 World、观察者、目标与墙，无须另行拖入组件引用。Game 视图应显示青色观察者、黄色目标、灰色墙及左上角操作面板，目标记录初始为 `visible=True`。这些颜色方块只用于展示位置，不是产品资源。

## 操作与正常表现

| 操作 | 预期表现 |
| --- | --- |
| **Move target behind wall / restore** | 首次点击移动目标，下一次扫描后显示 `visible=False`；再次点击恢复位置，显示 `visible=True` |
| **Report target footstep** | 上报目标声音，观察者更新后目标记录显示 `hearing=True`，最近通知显示 `Hearing / Heard` |
| **Report anonymous sound** | 上报无来源声音，`Anonymous memories` 计数增加；默认记忆 5 秒后过期 |
| **Hearing: True** | 关闭听觉，按钮变为 `Hearing: False`；此后上报声音不新增听觉记录，原有记忆仍会保留至过期 |
| **Sight: True** | 关闭视觉，下一次更新后当前视觉结束，记忆仍保留；再次点击开启后安排重新扫描 |
| **Clear memory** | 清除已有记录；视觉仍开启且目标可见时，后续扫描可以重新发现目标 |

本示例用按钮显式上报声音，实际播放音频不会自动产生听觉事件。关闭听觉后再次点击按钮可重新开启，再验证脚步声和无来源声音。

仓库中同一演示入口位于 `Projects/Perception2DLab/Assets/BasicPerception2D/BasicPerceptionDemo.cs`。Lab 通过本地路径引用包，用于维护验证；纯代码组合不依赖此面板，按包 README 驱动 `PerceptionCore2D` 即可。
