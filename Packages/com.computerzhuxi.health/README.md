# Health System

独立整数生命系统。Unity 6000.3.21f1 验证，包版本 1.0.1。

## 安装

私有仓库需要开发机的 Git 凭据管理已登录且具有读取权限。
UPM Git URL：`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1`。
凭据不能写入 URL、manifest 或仓库。CI 应在其凭据管理环境配置最小只读访问。

## 使用

添加 `Computerzhuxi.Health.HealthComponent`，通过 Inspector 设置生命初值并绑定 UnityEvent。
代码调用 `Damage(3)`、`Heal(3)`、`Kill()`、`Revive(5)`、`ChangeMaximum(20, MaximumHealthPolicy.Refill)`。
`State` 提供 IReadOnlyHealth；没有 Update，禁用重启不重置状态。
纯 C# 使用 `new Computerzhuxi.Health.Health(10, 10)`，参数依次为当前生命和最大生命。

Core 不依赖 Unity；包不处理命中、无敌、护甲、奖励、存档格式或界面。
导入 Basic Health Demo Sample 后打开其场景即可操作全部命令。
详细契约见 Documentation~/API.md。

序列化初值要求 maximum ≥ 1，startingHealth ∈ [0, maximum]，即使启用满血启动也必须合法；满血开关只决定初始当前值。非法配置抛出 ArgumentOutOfRangeException，不创建核心或发布事件，修正后可再次初始化。
