# Health 常见问题

## 修改 PackageCache 后内容消失

Library/PackageCache 是 Unity 生成缓存，不是源码。请在 UnityGameSystems 的 Packages/com.computerzhuxi.health 修改，然后发布新版本。

## 私有 Git 包无法安装

确认当前用户的 Git Credential Manager 对仓库具有读取权限。不要把 token 写进 URL 或 manifest。CI 应配置自己的最小只读凭据。

## 满血启动仍报告 startingHealth 非法

这是预期行为。Start With Full Health 只决定初始当前值，所有序列化字段仍必须保持合法。将 startingHealth 调整到 0 到 maximum 之间。

## Damage 或 Heal 没有变化

非正数不会改变状态；死亡实例不能 Damage 或 Heal；满血时 Heal 也不会变化。检查返回的 HealthChange.HasChanged。

## 事件回调中提交命令抛出异常

Core 拒绝同步重入，以保证一次命令的通知顺序不会被嵌套写入打断。请把后续命令排队到回调结束后执行。

## 中型项目是否必须挂 HealthComponent

不必须。已有角色架构时可以由项目适配层直接持有 Health Core。不要同时创建 HealthComponent 和另一份 Health，否则会出现两个权威状态。
