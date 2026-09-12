# HealthLab

HealthLab 是 com.computerzhuxi.health 的长期开发和验证工程，不是第二份包源码。

## 打开方式

用 Unity Hub 打开本目录。Packages/manifest.json 通过本地路径引用仓库中的 Packages/com.computerzhuxi.health，因此修改包源码后无需发布标签即可在 Lab 中验证。

## 用途

- 运行 Core、Unity、EditMode 和 PlayMode 测试；
- 验证组件、Prefab、Scene 和 UnityEvent 序列化；
- 导入并运行 Basic Health Demo；
- 构建 Standalone 并执行启动冒烟；
- 在发布前证明包不依赖 ARPG。

## 完整验证

先关闭 HealthLab 编辑器，再从仓库根目录运行：

    ./Tools/ValidateHealth.ps1 -UnityEditor 'D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe'

输出写入仓库根目录下被 Git 忽略的 Artifacts。不要提交 Library、Logs、UserSettings 或生成的构建目录。
