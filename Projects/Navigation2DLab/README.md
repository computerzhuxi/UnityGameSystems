# Navigation2DLab

使用 Unity 6000.3.21f1（见 `ProjectSettings/ProjectVersion.txt`）。`Packages/manifest.json` 通过本地 UPM 路径引用仓库中唯一的 Navigation2D 包源码。正式包只提供导航；实际运动属于示例或消费项目。[Getting Started](../../Packages/com.computerzhuxi.navigation2d/Documentation~/GettingStarted.md) 解释默认组件入口和公开类/接口组合入口。

| 场景 | 入口 | 人工操作与正常预期 |
|---|---|---|
| `Assets/NavigationLab.unity` | Basic 直接查询 | Play，开启 Scene Gizmos；角色绕灰墙，Reset / Repath 后重算，Toggle obstacle 后路径随障碍变化。 |
| `Assets/NavigationAgentLab.unity` | 保留的手动 Tick 组件 | Play，试 Moving target、Pause / Resume、Toggle obstacle、Reset；目标移动时持续跟随，暂停时位置与查询数不变，恢复后继续，不应反复后退。 |
| `Assets/NavigationCrowdLab.unity` | Agent + Avoidance 程序式组合 | Play，依次试 Head-on、Stopped actor、8-way crossing、Narrow corridor 与 Pause / Resume；角色不穿身或穿墙，窄道允许等待，不承诺自动脱困。画面显示最小圆间隙。 |
| `Assets/Samples/QuickStartNavigation2D/QuickStartNavigation2D.unity` | 默认 Inspector 自动 Navigator | Play，选中 Navigator 修改 Target、Obstacle Mask、Cell Size、净空与预算；在示例运动器修改 Speed；试暂停、停止、移动目标与障碍开关。Navigator 的路径持续更新；禁用示例运动器后角色停止，但导航仍工作。 |

四个场景都是已跟踪资产，构建方法 `NavigationLabBuild.Build`、`BuildAgent`、`BuildCrowd` 及 `NavigationQuickStartBuild.Build` 只读取现有场景，不隐式生成场景或改写 BuildSettings。只有维护者明确需要首次生成缺失场景时才显式运行对应 `Prepare` 并审查资产。正常验证不调用 Prepare。

`Assets/Samples` 下的四份导入样例是包 `Samples~` 的逐文件镜像；请先改包源，再同步镜像，不能独立维护两套实现。Basic 的路径列表与索引由示例持有；Agent、Crowd、QuickStart 的任务和路径由各自唯一 Agent 持有。不要让同一角色并行驱动 `NavigationNavigator2D` 与 `NavigationAgent2DComponent`。QuickStart 的 `QuickStartExampleMover2D` 只在独立 Sample 程序集中演示运动，不是正式 Runtime。

## 验证入口

从仓库根目录运行 `Tools/ValidateNavigation2D.ps1`。静态镜像检查可用 `-MirrorOnly`；定向 Unity 测试使用 `-UnityEditor <Unity.exe> -Platform EditMode|PlayMode -TestFilter <测试名>`；发布候选的完整测试、四种构建与冒烟使用 `-UnityEditor <Unity.exe> -Full`。带 ARPG 集成时仅在完整验证中传 `-ArpgProject <路径>`。Unity batch 前确认同一工程没有用户编辑器占用；不要关闭用户编辑器以抢占工程。自动测试、构建和冒烟不能替代以上视觉观察。

本仓库的 Unreleased 修复尚未进入固定 `navigation2d-v0.2.0` 标签。ARPG 仍使用固定标签；此 Lab 使用本地包供修改与验证。
