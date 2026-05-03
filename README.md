# JiXingFlashTool

极星系统工具，基于 `C#`、`WPF`、`CommunityToolkit.Mvvm` 和 `HandyControl` 开发。

## 功能

- 设备管理
- 设备投屏
- 文件传输
- 系统维护指令
- 多设备列表与状态展示

## 技术栈

- .NET Framework 4.8
- WPF
- CommunityToolkit.Mvvm
- HandyControl
- JXAdbCore

## 项目结构

- `JiXingFlashTool/` 主程序
- `JXAdbCore/` ADB 通信核心库
- `Resource/` 图片、ffmpeg、scrcpy 等资源

## 运行说明

1. 使用 Visual Studio 打开 `JiXingFlashTool.sln`
2. 还原 NuGet 包
3. 选择 `Debug` 配置并运行

## 说明

- 项目使用 WPF 默认窗口样式，主窗口背景为白色。
- 左侧菜单与主界面布局按 Figma 设计实现。
