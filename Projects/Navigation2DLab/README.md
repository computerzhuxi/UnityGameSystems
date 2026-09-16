# Navigation2DLab

Unity 版本见 ProjectSettings/ProjectVersion.txt。Lab 通过本地 UPM 路径引用唯一正式包，打开 Assets/NavigationLab.unity 运行。

Assets/Samples/BasicNavigation2D 是正式包 Samples~/BasicNavigation2D 的导入副本，Tools/ValidateNavigation2D.ps1 校验逐文件哈希；禁止独立修改副本。Assets/Tests 只测试 Sample 序列化。Editor 工具创建首次场景和构建。

运行仓库 Tools/ValidateNavigation2D.ps1 -UnityEditor <Unity.exe>。运行前关闭 Lab。人工观察必须开启 Scene Gizmos，并检查路径绕墙、半径圈、重置和动态障碍按钮。自动冒烟不能替代人工验收。
