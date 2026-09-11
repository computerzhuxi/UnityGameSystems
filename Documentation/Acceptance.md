# Health 1.0.0 本地发布候选验收

Unity 6000.3.21f1；HealthLab 干净导入成功；EditMode 23/23，PlayMode 1/1；空项目 Sample 导入测试 23/23。
Windows Standalone 构建并实际运行命令链通过，HEALTH_SMOKE_PASS，退出码 0。
ARPG 本地接入 EditMode 178/178、PlayMode 3/3，dotnet 0 警告/0 错误，正式场景 Standalone 构建通过。
Core DLL 仅引用 netstandard，Unity DLL 仅引用 netstandard、UnityEngine.CoreModule、Computerzhuxi.Health.Core。
包运行时源码无 ARPG 引用；没有复制或修改包源码供 ARPG 使用。

当前远程仓库与 Git UPM 安装尚待用户按 Publishing.md 手动发布后验证。
固定标签 health-v1.0.0 一经发布不得移动，后续兼容修复使用新版本。
完整机器日志与构建在被忽略的 Artifacts 目录；ARPG 内容审计在其 Artifacts/HealthExtraction。
