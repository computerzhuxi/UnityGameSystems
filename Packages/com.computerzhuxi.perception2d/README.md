# Perception 2D

Unity 6000.3 的二维视觉、听觉与分感官记忆包。包负责目标身份、已确认感知事实、记忆和通知；阵营、威胁评分、AI 决策、导航及音频播放由游戏负责。组件入口与编程式入口共享 `PerceptionCore2D` 状态规则。

当前仓库源码是 **0.2.0 未发布候选**。已发布标签 `perception2d-v0.1.0` 仍是旧 API；不要把安装该标签当成安装本候选。开发时让 Unity Package Manager 通过本地路径引用 `Packages/com.computerzhuxi.perception2d`。待正式发布后再使用相应发布标签。

## 安装

在本仓库的 Perception2DLab 中，`Packages/manifest.json` 使用 `"com.computerzhuxi.perception2d": "file:../../../Packages/com.computerzhuxi.perception2d"` 引用候选源码。其他本地工程应按其 `Packages` 目录到包目录的实际相对位置配置 `file:` 路径。若需要已发布的 0.1.0，可在 Package Manager 中添加 `https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.perception2d#perception2d-v0.1.0`；它不包含下述 0.2.0 编程式 API。

## 两种接入方式

- **组件装配**：创建 `PerceptionWorld2D`，给目标挂 `PerceptionTarget2D` 和 Collider，给观察者挂 `PerceptionObserver2D`；通过 Inspector 的 `world` 字段或 `Bind` 显式绑定。观察者按所属 Scene 的二维物理场景自动扫描，`World.ReportNoise` 路由声音。
- **编程式组合**：建立共享 `PerceptionTargetRegistry` 与每个观察者独立的 `PerceptionCore2D`；自行提交完整视觉帧、已确认的声音和单调时间。可直接使用 `PhysicsSightScanner2D` 生成视觉帧，也可从其他数据源生成事实；核心无需 GameObject。

从 [GettingStarted](Documentation~/GettingStarted.md) 获取两种方式的完整创建、驱动和释放示例。精确参数与 API 见 [API](Documentation~/API.md)，状态和生命周期规则见 [Contracts](Documentation~/Contracts.md)，版本变化见 [CHANGELOG](CHANGELOG.md)。

`Samples~/BasicPerception2D` 是组件交互演示，面板也指向编程式示例。维护者可在 `Projects/Perception2DLab` 打开现成场景；Lab 使用本地包，不维护第二份运行时实现。
