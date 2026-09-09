# API 与兼容契约

Core 唯一持有 Current、Maximum，IsAlive/IsDead/Normalized 均为派生数据。
HealthSnapshot 与 HealthChange 是只读值类型。HealthChange 提供 Reason、Before、After、Delta、ActualAmount、HasChanged。

|命令|契约|事件|
|---|---|---|
|构造|0 <= current <= maximum，maximum >= 1；非法值抛 ArgumentOutOfRangeException|无|
|Damage|非正数或死亡不变化；过量截断|Damaged, Changed, Died（致死时）|
|Heal|非正数、死亡或满血不变化；过量截断|Healed, Changed|
|Kill|重复调用不变化|Changed, Died|
|Revive|value 必须为 1..Maximum；存活时不变化|Changed, Revived|
|ChangeMaximum|上限必须为正；PreserveCurrent 截断，Refill 回满并允许复活|Changed, Revived（发生复活时）|
|Restore|同时校验和恢复当前及最大生命|仅 Changed，原因 Restore|

无变化操作不发事件。状态先完整提交，再顺序发事件；快照不会被后续操作修改。
仅支持单线程使用，事件回调内同步命令抛 InvalidOperationException，订阅者应在回调返回后提交下一命令。
订阅者抛出的异常向调用方传播，已提交状态不回滚，后续通知可能不执行；订阅者负责处理自身异常。重入保护在 finally 中释放。

HealthComponent Initialize 幂等，Awake 或首次访问时初始化；Inspector 初值无效时不静默修正。
组件不把运行时状态写回序列化初值。禁用不会使显式命令失效，也不清空订阅。
UnityEvent 使用 Changed/Damaged/Healed/Died/Revived 对应时序；监听者通过 State 读取事实。

1.x 保持公共契约及事件语义；破坏性改变升级主版本，发布标签不可移动。
