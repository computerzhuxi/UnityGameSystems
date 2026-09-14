# Perception 2D 实施记录

包名 com.computerzhuxi.perception2d，正式运行时源位于 Packages，独立宿主为 Projects/Perception2DLab。参考 UE 的多感官职责划分，不承诺逐项复制引擎内部行为。

ARPG 本轮只启用视觉。CharacterPerceptionFacing2D 将 Actor 朝向提交到包；PerceptionSceneBinding2D 用显式 World 引用绑定同场景对象，动态生成者需自行 Bind。Enemy 独占阵营、存活过滤和目标选择。调查完成登记已消费的目标代次和视觉时间；不会修改通用包记忆。

迁移前 ARPG HEAD 为 9d77293df3d1762a9e1b2c9e2b67078805dea657（本地 ahead 33），UnityGameSystems HEAD 为 cf3a00ca160a65eaec7f27c27040d04b49931a0e。既有 README 修改和未跟踪 SystemExtractionPlaybook.md 必须保留。

与旧行为差异：取消近身全向发现；追踪仍检查视角；不使用 lostTargetDelay；记忆不因抵达位置自动删除；由 Enemy 标记已调查信息。听觉配置关闭，游戏暂不上报脚步。

原始验证结果在 Artifacts/Perception2D；首次 EditMode 注册测试失败后改为显式注册测试，真实启停另由 PlayMode 覆盖，失败证据保留。最终状态与验证数量见同目录验收报告，不以本文预先宣称通过。

初轮实施保持不提交、不推送、不创建标签；功能及人工验收通过后，用户已授权发布 0.1.0。发布使用不可变标签 perception2d-v0.1.0，消费项目通过 manifest/lock 固定版本，详见 Perception2DRelease.md。
