# Health 1.0.1 修复与重新验收记录

## 状态与版本

本轮按复核意见修复三项问题，自动验证完成，提交用户再次复核。原 1.0.0 的测试与发布记录属实，但此前“最终验收通过”的结论已撤回。
私有仓库 computerzhuxi/UnityGameSystems 发布 health-v1.0.1，包 com.computerzhuxi.health@1.0.1，固定源码提交 `d05303f629393a41f93d3eba3a45e60dc4f177ca`。
原 health-v1.0.0 标签对象 c74bef1657b51c9889fafa970d41ef9553fbd965、源码 ee41bb3538021c177dd30599107025a85aa658c2 均保持不变。

安装 URL：`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1`。
包路径：D:/Unity/Project/UnityGameSystems/Packages/com.computerzhuxi.health；命名空间 Computerzhuxi.Health。
HealthLab 保留本地引用用于开发；ARPG、独立 Git Sample、隔离 ARPG 的 UPM 锁文件都真实锁定上述提交，缓存源码与发布源码相符。

## 三项修复与回归

1. **P1 资源保护**：HealthSerializationTests 不再写入或删除固定的 Assets/HealthSerializationTest.prefab。每次测试创建独占目录，Prefab 使用 GenerateUniqueAssetPath；只有成功创建且 GUID 仍匹配的资源才能清理。两个回归验证同名既有资源在正常执行及模拟保存后异常时，字节内容、GUID 和加载名称全部不变。
2. **P2 完整初值校验**：HealthComponent 在创建 Core 前校验 maximum ≥ 1 和 startingHealth ∈ [0, maximum]，满血开关不跳过校验。八组非法配置覆盖两个开关状态，四组合法边界验证初值选择；失败不初始化、不发布事件，修正后可以重试且不会重复订阅。
3. **P2 属性边界**：CharacterAttributeId 移除 Health/MaxHealth，旧值 0、1 保留为空洞，其他编号显式保持 2..7。Stats UI 用私有 ValueSource 区分属性、当前生命和最大生命；生命行没有伪造的属性 ID。新增枚举集合/编号回归，原查询拒绝测试和实际场景 UI 更新测试继续通过。

## 本轮验证（Unity 6000.3.21f1）

|环境|EditMode|PlayMode|补充验证|
|---|---|---|---|
|HealthLab 本地 1.0.1|37/37|1/1|从空 Library 导入；Standalone 构建与实际启动冒烟通过，退出码 0|
|全新独立 Sample 本地导入|37/37|1/1|没有 ARPG 依赖|
|全新独立 Sample Git 导入|37/37|1/1|真实私有 Git 下载、固定新标签|
|ARPG 本地候选包|180/180|3/3|发布前验证|
|ARPG 固定 Git 版本|180/180|3/3|Standalone 构建通过；dotnet build 0 警告、0 错误|
|隔离 ARPG 固定 Git 版本|180/180|3/3|未复制 Library 和锁文件，完整重新导入与实际内容加载|

dotnet 命令为 `dotnet build ARPG.sln --no-restore -m:1 -v minimal`。先运行 HealthExtractionFinalize.GenerateSolution，验证项目 Compile 路径来自新 Git PackageCache；生成时恢复用户 IDE 偏好。
玩法链覆盖伤害、治疗、致死反应、击败经验、连续升级、一次回满、玩家/敌人死亡与目标失效、Health/Stats UI 更新。

## DLL、数据归属与资产审计

实际 DLL：Core → netstandard；Unity → Core、netstandard、UnityEngine.CoreModule。ARPG.Actor 引用 Core，包运行时无 ARPG 引用，程序集依赖无环。
Core 仍是当前生命和上限唯一存储；CombatAttributeSet 没有生命字段或查询投影，Progression 分别提交攻击与生命成长。
Scene、Prefab 和正式配置真实加载，Missing Script 与缺失托管类型均为 0；meta 共 699 个，缺失、孤立、重复 GUID 均为 0。
CharacterHealth/Settings 原 GUID、空命名空间、MovedFrom、序列化和 UnityEvent 保留。相比提取前仍只有四份预期配置差异；本次修复没有新增内容资产变更。
测试/构建产生的 Bangers 字体、TimeManager、URP 缓存与 ProjectSettings 改写已保存差异后恢复。没有提交日志、缓存、构建或凭据；不推送 ARPG。

## 异常记录与限制

首次新增资源测试因 Unity 将 Prefab 名称规范为文件名而失败；改为对比保存后的名称基线后全量通过。原 lab-editmode.xml、sample-editmode.xml 保留。
独立 Git Sample 首次因 schannel TLS 握手失败退出；正常重试下载成功，不关闭证书验证。隔离 ARPG 首次 UPM 导入停滞，核对本次进程后重启成功。失败/停滞日志不记为通过。
验证为自动化运行，没有额外人工视觉试玩。Core 单线程同步调用，拒绝回调重入；订阅者异常不回滚已提交状态。
完整初值校验会拒绝旧组件中即使满血启动也越界的 startingHealth，需要修正配置；原已发布标签不移动。未来包修复继续增加版本，私有开发机和 CI 需要读取权限。

## 证据路径

- ARPG：D:/Unity/Project/ARPG/Artifacts/HealthExtraction/Review101/
- HealthLab / Sample：D:/Unity/Project/UnityGameSystems/Artifacts/Review101/
- XML：lab-final-*、sample-final-*、sample-git-retry-*、local-*、git-*、isolated-retry-*。
- 构建/编译：lab-final-build.log、lab-smoke.log、git-build.log、git-solution.log、dotnet-build.log。
- 命令与退出码：各 *-commands.json；汇总：verification-summary.json。
- 资产和 DLL：content-difference-audit.json、content-loads.tsv、dll-references.tsv、health-dll-references.tsv。
- 原迁移、哈希基线与 1.0.0 日志继续保留在 ARPG Artifacts/HealthExtraction。

仓库最终提交和远程标签快照见 Review101/final-git-state.json（日志目录，不入库）。
