# Health 1.0.1 复核修复

## 修复

- P1：序列化测试在每次测试创建的独占目录内使用唯一 Prefab 路径；成功和异常清理均核对所创建资源 GUID。回归保留同名哨兵资源，并比较文件内容、GUID 和加载名称。
- P2：HealthComponent 初始化先校验 maximum ≥ 1 和 startingHealth ∈ [0, maximum]，满血模式也不跳过。回归涵盖两个开关状态下的非法上下限、合法端点、初始化无事件及修正后重试。
- P2：ARPG 删除 Health/MaxHealth 属性成员，旧编号 0、1 留空，其余编号 2..7 显式保留；UI 独立描述生命来源。回归验证枚举集合/编号和完整场景 UI 更新。

## 发布前验证

Unity 6000.3.21f1，HealthLab 从空 Library 导入后 EditMode 37/37、PlayMode 1/1，Standalone 构建与实际启动 HEALTH_SMOKE_PASS、退出码 0。
全新独立 Sample 导入项目 EditMode 37/37、PlayMode 1/1。ARPG 本地 1.0.1 包 EditMode 180/180、PlayMode 3/3。
首次新增资源保护测试仅因 Unity 将 Prefab 根名规范为文件名而失败；改为比较保存后的基线名称后通过，首次日志保留。
当前准备发布新标签 health-v1.0.1；原 health-v1.0.0 保持不动。固定 Git 下载与隔离项目最终结果将在发布后补齐。

日志：D:/Unity/Project/UnityGameSystems/Artifacts/Review101 和 D:/Unity/Project/ARPG/Artifacts/HealthExtraction/Review101。缓存、日志、构建均不入库。
