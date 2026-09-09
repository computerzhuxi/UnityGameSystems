# Basic Health Demo

导入 Sample 后打开 BasicHealthDemo.unity。按钮支持 Damage、Heal、Kill、Revive 和上限保持/回满。
上限输入必须为正整数，当前值和生死事实实时显示。
HealthComponent.OnChanged 通过 Inspector 持久化绑定到 RecordUnityEvent，界面显示调用次数。
HealthDemo.prefab 可用于验证配置和事件序列化。
Standalone 使用 --health-smoke 参数执行完整命令链并以 HEALTH_SMOKE_PASS 和退出码 0 表示成功。
