# Perception 2D 发布验收历史

这里保存每个发布版本的实施、验证、复核和发布记录。它们是历史证据，不是当前使用指南；当前安装与 API 以包内文档为准。

|版本|状态|记录|
|---|---|---|
|0.1.0|首次发布并完成 ARPG 固定 Git 标签接入|[实施](0.1.0/Perception2DImplementation.md) · [初轮验证](0.1.0/Perception2DValidationReport.md) · [复核修复](0.1.0/Perception2DReviewFixesReport.md) · [发布](0.1.0/Perception2DRelease.md)|

详细测试 XML、Unity 日志、构建文件和哈希清单位于本地被 Git 忽略的 `Artifacts/Perception2D`。

后续 0.2.0 候选源码核对时，ARPG 曾固定引用提交 `2d02ccae31639ee669ae1568da0000fada8449f8`。这是当时的消费项目引用记录，不代表候选已发布，也不描述后续依赖更新状态。

## 已知维护项

0.1.0 的 Package Sample 与 Perception2DLab 中存在一份字节一致的 `BasicPerceptionDemo.cs` 镜像。它不构成重复运行时实现，但未来修改时存在漂移风险。后续版本应让 Lab 使用独立控制器，或建立由 Package Sample 到 Lab 的生成与哈希校验。
