# Health 版本迁移

## 基本原则

消费项目通过不可变标签锁定版本。升级需要主动修改 manifest，等待 Unity 更新 packages-lock.json，再运行项目自己的集成测试。

已发布标签不会移动：

- health-v1.0.0 永久指向原 1.0.0 源码；
- health-v1.0.1 永久指向原 1.0.1 源码；
- 后续修复创建新的标签。

## 升级到 1.0.1

安装地址：

    https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1

1.0.1 会完整校验 HealthComponent 的 maximum 和 startingHealth，即使选择满血启动也一样。旧组件如果保存了越界 startingHealth，需要先修正配置。

## 升级检查

1. 阅读 CHANGELOG。
2. 更新 manifest 并确认 lock 文件提交哈希。
3. 让 Unity 完成包重新解析和脚本编译。
4. 验证 Scene、Prefab、UnityEvent 和 Missing Script。
5. 运行消费项目 EditMode、PlayMode 和关键玩法链。
6. 只提交预期的 manifest、lock 和适配代码变化。
