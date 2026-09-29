# Stats

Stats 为 Unity 项目提供可复用的属性计算与绑定。Core 是纯 C#：业务域持有基础值和最终值，`StatRegistry` 管理注册、修正、重算及变化通知。当前源码版本以包内 `package.json` 为准；发布状态以不可变 Git 标签及发布记录为准。

## 安装

本仓库的 `Projects/StatsLab/Packages/manifest.json` 使用本地 UPM 路径 `file:../../../Packages/com.computerzhuxi.stats`，路径相对该工程的 `Packages` 目录。从其他本地工程引用时应按其 `Packages` 目录重新计算相对路径。

0.1.0 的固定 Git 安装地址为 `https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.stats#stats-v0.1.0`。实际发布与远端复验状态见仓库 `Documentation/History/Stats/0.1.0` 的记录。

## 阅读

- [双入口与最小用例](Documentation~/GettingStarted.md)
- [公开 API](Documentation~/API.md)
- [状态、事件与失败契约](Documentation~/Contracts.md)
- [更新记录](CHANGELOG.md)
- [StatsLab 开发宿主与验收](https://github.com/computerzhuxi/UnityGameSystems/blob/stats-v0.1.0/Projects/StatsLab/README.md)

Stats 不决定角色成长、装备、Buff、存档格式或 UI。消费项目用稳定的属性 ID 和修正来源接入这些业务规则。
