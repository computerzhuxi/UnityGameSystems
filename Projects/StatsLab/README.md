# StatsLab

本 Lab 通过本地 UPM 引用 `Packages/com.computerzhuxi.stats`。运行 `Assets/BasicStatsDemo/BasicStatsDemo.unity` 可观察组件式和纯 C# 两名角色。示例源码以包内 `Samples~/BasicStatsDemo` 为准；Lab 镜像 `BasicStatsDemo.cs`、`SampleCharacterStats.cs` 与 `Computerzhuxi.Stats.Samples.asmdef`，场景、Prefab、定义资产由 Lab 独立验证并与 Sample 保持对应。

在首次创建或资源需要重建时，可调用 `StatsLabBuild.CreateDemoAssets`。打开场景并进入 Play，初始组件角色应显示 HP 100/100、攻击 30、移速 5；纯 C# 角色应显示 HP 80/80、攻击 25、移速 4、自定义 Bonus 7。点击“安装同来源装备”，两名角色攻击应分别变为 60 和 54。点击“两名角色永久成长”，攻击变为 66 和 60，生命上限变为 110 和 90，当前生命仍为 100 和 80。先点击“查看基础快照”，再次成长，再点击“恢复基础快照”，基础攻击和生命上限恢复到快照值，装备倍率仍生效。点击“卸下同来源装备”，攻击只保留当前永久成长。自动测试结果以 `Artifacts/Logs/Stats` 当轮 XML 为准。

可读性复核：在 Game 窗口以常见宽度和较窄宽度分别查看。标题约 24px、正文和按钮约 20px，白字/浅蓝字在深色面板上应清楚可辨；按钮应有足够点击高度。缩小窗口时通过面板内滚动查看最下方“恢复基础快照”和状态文字，不应被裁切。若本机没有受支持的中文系统字体，示例回退到 Unity 默认字体。2026-09-29 用户反馈“人工测试没问题”，本轮 GUI 人工验收据此记录为通过；未另行记录逐项截图或分辨率。
