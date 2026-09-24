# Navigation2DLab

Unity 版本见 ProjectSettings/ProjectVersion.txt。通过本地 UPM 引用唯一包源码。

- `Assets/NavigationLab.unity`：直接查询与自行跟随。
- `Assets/NavigationAgentLab.unity`：可选 Agent 组件、移动目标、暂停/恢复和障碍变化；由 NavigationLabBuild.PrepareAgent 首次创建。

Assets/Samples 下四份样例均为正式 Samples~ 的受校验镜像，不能独立修改。仓库 Tools/ValidateNavigation2D.ps1 验证文件集合及哈希、全部测试、四种构建及冒烟。

运行前关闭编辑器。人工分别打开四个场景，开启 Gizmos 检查绕墙与半径；Agent 场景开关 Moving target、Pause / Resume、Toggle obstacle 并 Reset，确认不后退抖动、暂停不移动或重算、恢复可继续。自动冒烟不能替代观察。


## 群体避让 Lab

打开 `Assets/NavigationCrowdLab.unity`，或运行忽略目录 `Artifacts/Logs/Navigation2D/Build/NavigationCrowdLab.exe`。此场景与之前的 NavigationAgentLab 不同。

- Head-on：两个圆迎面通行并抵达各自小方块。
- Stopped actor：青色圆绕过中间灰色停止身体，不推开或穿过它。
- 8-way crossing：八个圆交叉；观察是否能前进及是否出现抖动/穿身。
- Narrow corridor：两圆在不能并排通过的窄道可以等待，但不应穿墙；不宣称自动脱困。
- Pause / Resume：暂停后位置不变，恢复继续。

画面直接显示身体半径与最小间隙，无需编辑器 Gizmos。自动测试与构建不能替代以上人工观察。

## Inspector 快速使用场景

打开 Assets/Samples/QuickStartNavigation2D/QuickStartNavigation2D.unity。正式源码在包的同名 Sample 中，镜像文件集合及哈希由验证脚本检查。选择 Navigator 在 Inspector 运行时修改目标/障碍/网格/预算，选择示例运动适配器（仅 Sample/Lab）修改速度；人工检查暂停、停止、目标移动、绕障和 Gizmo。

2026-09-23 产品边界收口：正式 Runtime 只负责导航。QuickStartExampleMover2D 仅在独立 Sample/Lab 程序集中演示消费建议，不是正式 API 或生产移动能力。实际项目提供自己的移动系统，ARPG 保持底层 API 接入。
