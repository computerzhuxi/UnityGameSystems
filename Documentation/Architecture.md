# 架构设计

## 仓库目录结构

- `Packages`：正式 UPM Package 源码存放目录。
- `Projects`：各包的开发与验证工程，通过本地路径引用对应 Package。
- `Documentation`：仓库级架构设计与维护文档。

## 模块标准结构 (Package Structure)

每个 Package 作为一个独立的模块，默认包含以下标准结构：

- `Runtime/`：核心运行时代码与资源。
- `Tests/`：单元测试与集成测试代码。
- `Samples~/`：示例场景与使用演示（使用 `~` 结尾以防自动导入）。
- `Documentation~/`：按需放置深入的 API 与专题文档。
- `README.md`：安装、快速开始与基础用法。
- `CHANGELOG.md`：版本更新日志。
- `package.json`：UPM 包配置信息。
- `*.asmdef`：程序集定义文件（**必填**，确保代码隔离与独立编译）；每个源码根只归属一个程序集定义。

> 按需增加 `Editor/` 目录存放编辑器扩展与自定义 Inspector 面板。

## 设计原则

### Package 独立与依赖
每个 Package 必须可以独立安装与运行，默认不依赖其他自定义 Package。若存在依赖关系，必须在 `package.json` 和对应的 `.asmdef` 文件中显式声明。

### 命名空间隔离 (Namespace)
所有 Runtime 和 Editor 代码必须放置在统一的根命名空间下（例如 `YourName.SystemName`），严禁在全局命名空间声明任何类或接口。

### Runtime 是唯一真实实现
正式功能仅存在于 Package 的 `Runtime` 目录中。`Projects` 仅作为宿主测试环境，绝不维护或重写第二份业务逻辑实现。

### Component 与 Programmatic API
系统应尽可能同时提供 Inspector 挂载组件与纯代码调用（Programmatic API）两种入口，但两者必须映射到底层的同一套核心逻辑，遵循相同的规则。

编程式入口可以使用 Unity 类型；只有存在真实的纯逻辑边界时才拆分 Core 与 Unity 程序集。公开接口表达稳定能力，查询优先使用只读契约，修改状态通过明确命令。

### 单一数据源 (Single Source of Truth)
同一运行状态在内存中只允许一个权威的管理者或数据源，严禁在不同类中冗余或同步相同状态，避免数据不一致。

序列化配置需说明何时生效；修改公开类型、字段或程序集时，还需保护 Unity 序列化身份。

### 明确边界 (Scope)
每个 Package 仅负责自身领域的底层机制（如通用血量计算、基础寻路），绝不越权接管或耦合具体的游戏业务逻辑（如玩家得分逻辑、特定怪物 AI）。
