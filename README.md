# UnityGameSystems

个人可复用 Unity 系统库，私有仓库。

Health 包：Packages/com.computerzhuxi.health。
HealthLab：Projects/HealthLab，Unity 6000.3.21f1，本地 UPM 引用。

测试与构建证据存放在被忽略的 Artifacts 中。

## 重跑验证

关闭 HealthLab 编辑器后运行：

```powershell
./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe'
```

结果包括测试 XML、Unity 日志、Windows 构建与独立程序冒烟标记。
