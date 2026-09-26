# 中型项目集成指南

> 第一次使用请先阅读 [上手指南](GettingStarted.md)。本页讨论已有角色架构的适配层职责。

## 两种接入方式

没有既有角色架构时，直接使用 HealthComponent。已有 Combat、Actor、Progression 和 UI 模块时，建议建立项目自己的适配层并直接组合 Health Core。

    Combat / Progression / UI
                │
                ▼
          项目生命门面
                │
                ▼
            Health Core

## 适配层职责

适配层可以：

- 从项目配置创建 Health；
- 保留项目原有的方法名和事件；
- 把项目的成长、存档和表现语义转换成 Core 命令；
- 为旧 Scene、Prefab 和序列化类型保留兼容信息。

适配层不应：

- 再保存一份 Current 或 Maximum；
- 修改 Core 内部字段；
- 把具体游戏类型加入 Health 包；
- 让 UI 或 Animation 成为生命数据拥有者。

## ARPG 示例

ARPG.Actor 的 CharacterHealth 是项目门面。Combat 提交最终伤害，Progression 提交生命上限成长，UI 读取 CharacterHealth；真正的当前生命和上限只存在于 Health Core。

ARPG 没有同时挂 HealthComponent，因为那会创建第二个生命实例。HealthComponent 是面向简单项目的通用入口，不是每个消费项目都必须使用的层。

## 何时扩展公共包

先在项目适配层实现差异。只有多个项目出现相同语义、接口已经稳定，并且不依赖特定游戏概念时，才把能力下沉到公共包并发布新版本。
