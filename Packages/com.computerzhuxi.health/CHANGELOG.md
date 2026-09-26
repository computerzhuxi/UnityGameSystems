# Changelog

## Unreleased

- 组件销毁时解除五个 Core 事件转发，避免外部保留状态后继续调用已销毁组件。
- Basic Health Demo 增加 Restore 与独立 Core 编程式入口展示。
- HealthLab 构建只读取已有场景，并检查 Sample 镜像内容与 GUID。
- 整理双入口上手、生命周期和 Lab 验收说明。

## 1.0.1

- 序列化测试使用独占临时目录和唯一资源路径，仅清理自己创建且 GUID 匹配的资源。
- 满血启动也完整校验序列化起始生命值，非法配置初始化失败且可修正后重试。
- 新增资源保护、异常清理及序列化初值边界回归。

## 1.0.0

- 独立整数生命核心、不可变命令结果及确定事件顺序。
- Inspector 生命组件与可选 UnityEvent。
- 状态恢复、显式复活、上限策略、溢出与重入保护。
- EditMode/PlayMode 测试、Basic Health Demo 和独立 HealthLab。
