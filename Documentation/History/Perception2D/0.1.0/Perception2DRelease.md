# Perception 2D 0.1.0 发布

包：`com.computerzhuxi.perception2d`；程序集：`Computerzhuxi.Perception2D`。固定 Git 标签：`perception2d-v0.1.0`。标签创建后不得移动；修复使用新版本和新标签。

```json
"com.computerzhuxi.perception2d": "https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.perception2d#perception2d-v0.1.0"
```

运行环境 Unity 6000.3，依赖 Physics2D 模块。首次发布包含二维视觉、事件听觉、独立启停、分感官记忆、目标生命周期身份、只读查询、批次事件、Gizmos、独立 Lab 和 Sample。ARPG 使用视觉，听觉关闭。

发布前验收：Lab EditMode 20/20、PlayMode 3/3；ARPG EditMode 169/169、PlayMode 6/6；dotnet 编译无警告或错误。面板、声音、遮挡、编辑模式及运行时 Gizmo 已由用户逐项观察确认。初轮与复核报告是发布前阶段的历史快照，相关本地候选状态描述不代表发布后的安装方式。

ARPG 切换 Git 标签后的干净导入与测试是独立发布门槛，结果记录在 ARPG 接入提交及本地 Artifacts/Perception2D/Release；该检查完成前不提交消费项目。原始日志、备份和构建产物不随源码发布。

API 与行为契约见包 Documentation~；不包含 3D、声学传播、目标选择、阵营、生命、导航或行为决策。
