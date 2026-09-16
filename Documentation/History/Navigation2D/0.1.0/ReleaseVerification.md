# Navigation2D 0.1.0 发布验证

日期：2026-09-16。用户确认独立复核通过并明确授权按发布顺序执行。包与 ARPG 固定标签接入均已发布，远程安装及消费项目全量复验通过。

## 不可变发布身份

- 包：`com.computerzhuxi.navigation2d@0.1.0`
- 带注释标签：`navigation2d-v0.1.0`
- 标签对象：`1ad688e1612d37c3cdfcfff0c4ea7106da5d849e`
- 包提交：`8ebe60c54a7db122cd153c5ca26979bcbb4c2b57`
- 安装地址：`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.1.0`

先提交并推送 main，再创建、推送标签，最后通过 ls-remote 核对解引用提交。未移动任何既有标签。发布前只调整安装文档并清理新建 YAML/meta 行尾空白；GUID、配置值和已复核的运行时逻辑保持不变。

## 从远程标签安装的独立验证

新工程：`Artifacts/Logs/Navigation2D/release-20260916/RemoteConsumer`。创建时没有 Library、没有 PackageCache、没有本地 file 或嵌入包；仅配置远端固定标签，由 UPM 下载解析。沿用正常全局下载缓存，不声称清空整机缓存。

证据：`Artifacts/Logs/Navigation2D/release-20260916/RemoteEvidence`。

- packages-lock.json 的 source 为 git，hash 与发布提交完全一致。
- 通过 UPM Sample.Import 公共接口从已安装包实际导入 Basic Navigation 2D。
- EditMode 16/16（15 项包测试、1 项导入 Sample 序列化测试）。
- PlayMode 6/6。
- 独立 Standalone 构建成功，运行得到 NAVIGATION_SMOKE_PASS 并正常退出。

包核心边界、人工观察、行为兼容和局限见 [候选验收快照](CandidateAcceptance.md)。Enemy 自主找楼梯、跨层技能/多目标近战空间过滤、地面存在检测与自动脱困不属于此版本。

## 消费项目

ARPG 从本地 file 引用切换为上述固定标签，UPM 生成锁文件并核对 hash。真实工程完整 EditMode 176/176、PlayMode 10/10、Standalone 重建、dotnet 0 警告/0 错误及资产/DLL 审计均通过。既有 Bangers 修改不进入提交，也不被恢复或覆盖。


ARPG 正式接入提交：`c4647cc35e2e42a4f2de39e3b98fae825e95bb6d`；发布归档提交：`2727bfddb325eab1fc51bc4b2c24502b44bbdaf5`，均已推送 origin/main。其前置空间层级提交 e895862 随同推送；没有强推或改写历史。

消费证据位于 ARPG `Artifacts/Logs/Navigation2D/release-20260916`，真实 DLL/资产输出位于 `Artifacts/Logs/Navigation2D/audit-20260916-020601`。加载 73 个 GameObject，Missing Script/托管类型为 0。最新播放器完成 60 秒有界启动观察，无异常，后由测试脚本停止，非自然退出；不冒充人工游戏观察。

最终内容与 GUID 审计位于本仓库 `Artifacts/Logs/Navigation2D/final-audit-20260916-100803`：ARPG+包 722、Lab+包 27，缺失/孤立/重复为 0；既有资产及 meta 内容与候选基线一致。四项 Unity 自动改写已归档差异后恢复。22 张图的生成一致性和幂等再次通过，发布阶段只调整版本标签文字，既有用户人工确认保持有效，不声称新增人工观察。

Bangers SHA-256 仍为 `F551412F6599D7D2327E18E8004DE9B5CB0601EF2AE29F2C066DA5A8E1E07321`，留在 ARPG 工作区。系统仓库的发布归档提交不移动 navigation2d-v0.1.0；标签永久指向上述包提交。
