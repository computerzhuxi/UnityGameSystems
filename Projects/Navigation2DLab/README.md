# Navigation2DLab

使用 Unity 6000.3.21f1（见 `ProjectSettings/ProjectVersion.txt`）。`Packages/manifest.json` 通过本地 UPM 路径引用仓库中唯一的 Navigation2D 包源码。正式包只提供导航；实际运动属于示例或消费项目。[Navigation 2D README](../../Packages/com.computerzhuxi.navigation2d/README.md) 解释默认组件入口和公开类/接口组合入口。

| 场景 | 入口 | 人工操作与正常预期 |
|---|---|---|
| `Assets/NavigationLab.unity` | Basic 直接查询 | Play，开启 Scene Gizmos；角色绕灰墙，Reset / Repath 后重算，Toggle obstacle 后路径随障碍变化。 |
| `Assets/NavigationAgentLab.unity` | 保留的手动 Tick 组件 | Play，试 Moving target、Pause / Resume、Toggle obstacle、Reset；目标移动时持续跟随，暂停时位置与查询数不变，恢复后继续，不应反复后退。 |
| `Assets/NavigationCrowdLab.unity` | Agent + Avoidance 程序式组合 | Play，依次试 Head-on、Stopped actor、8-way crossing、Narrow corridor 与 Pause / Resume；角色不穿身或穿墙，窄道允许等待，不承诺自动脱困。画面显示最小圆间隙。 |
| `Assets/Samples/QuickStartNavigation2D/QuickStartNavigation2D.unity` | 默认 Inspector 自动 Navigator | Play，选中 Navigator 修改 Target、Obstacle Mask、Cell Size、净空与预算；在示例运动器修改 Speed；试暂停、停止、移动目标与障碍开关。Navigator 的路径持续更新；禁用示例运动器后角色停止，但导航仍工作。 |

四个场景都是已跟踪资产，构建方法 `NavigationLabBuild.Build`、`BuildAgent`、`BuildCrowd` 及 `NavigationQuickStartBuild.Build` 只读取现有场景，不隐式生成场景或改写 BuildSettings。只有维护者明确需要首次生成缺失场景时才显式运行对应 `Prepare` 并审查资产。正常验证不调用 Prepare。

`Assets/Samples` 下的四份导入样例是包 `Samples~` 的逐文件镜像；请先改包源，再同步镜像，不能独立维护两套实现。Basic 的路径列表与索引由示例持有；Agent、Crowd、QuickStart 的任务和路径由各自唯一 Agent 持有。不要让同一角色并行驱动 `NavigationNavigator2D` 与 `NavigationAgent2DComponent`。QuickStart 的 `QuickStartExampleMover2D` 只在独立 Sample 程序集中演示运动，不是正式 Runtime。

## 验证入口

打开本 Lab，通过 **Window > General > Test Runner** 在 EditMode 或 PlayMode 选择与修改有关的包测试；Lab 的 Sample、QuickStart 和 Crowd 接入测试也在对应模式中选择。发布候选再运行相关的完整测试，导出本轮 XML 并按[测试规范](../../Documentation/TestingStandard.md)核对具体用例、数量和结果。ARPG 集成须在实际消费工程单独验证，并先核对其 manifest 引用的版本。

镜像直接比较 `Packages/com.computerzhuxi.navigation2d/Samples~` 与 `Assets/Samples` 的对应文件；可用 `Get-FileHash -Algorithm SHA256` 核对内容，文件清单也须对应，避免漏查新增或缺失文件。现有构建入口分别为 `NavigationLabBuild.Build`、`BuildAgent`、`BuildCrowd` 与 `NavigationQuickStartBuild.Build`。例如在仓库根目录，确认本工程没有其他 Unity 实例占用后运行：

```powershell
& 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe' -batchmode -quit -projectPath 'Projects/Navigation2DLab' -executeMethod NavigationLabBuild.Build -logFile 'Artifacts/Navigation2DLab-build.log'
```

运行前先创建日志目录 `Artifacts`。四个构建产物位于 `Artifacts/Logs/Navigation2D/Build`，文件名分别为 `Navigation2DLab.exe`、`NavigationAgentLab.exe`、`NavigationCrowdLab.exe`、`NavigationQuickStartLab.exe`；冒烟参数分别为 `--navigation-smoke`、`--agent-smoke`、`--crowd-smoke`、`--quickstart-smoke`。各程序应退出为 0，日志分别包含 `NAVIGATION_SMOKE_PASS`、`NAVIGATION_AGENT_SMOKE_PASS`、`NAVIGATION_CROWD_SMOKE_PASS`、`NAVIGATION_QUICKSTART_SMOKE_PASS`。自动测试、构建和冒烟不能替代上面的视觉观察；日志、XML、构建产物和 Lab 缓存不应提交。

本仓库的 Unreleased 修复尚未进入固定 `navigation2d-v0.2.0` 标签。消费工程的安装来源与版本以自身 manifest 和锁文件为准；此 Lab 使用本地包供修改与验证。
