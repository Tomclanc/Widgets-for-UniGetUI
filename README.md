# UniGetUI 小组件 简体中文适配

这是 `Widgets for UniGetUI` 的简体中文适配版本。它可以在 Windows 小组件面板中显示 UniGetUI 检测到的软件包更新，并支持跳转到 UniGetUI、查看更新、刷新更新列表等操作。

## 改动内容

- 将小组件名称、说明、加载页、空状态、错误页和更新列表适配为简体中文。
- 适配新版 UniGetUI IPC 接口 `/uniget/v1/...`。
- 修复小组件进程在 MSIX 环境中启动后立即退出的问题。
- 优化 `Win + W` 打开小组件时的检测体验：自动缓存更新结果 10 分钟，避免每次打开面板都重新检测。
- 增加本地日志，方便排查小组件启动和通信问题。

## 安装

下载并安装发布包：

```text
UniGetUI小组件_简体中文.msix
```

如果 Windows 提示证书或发布者相关警告，请确认该 MSIX 使用本机受信任的测试证书签名。安装完成后，打开 Windows 小组件面板，添加 “UniGetUI 小组件” 即可。

## 使用注意

- 请先安装并运行 UniGetUI。
- 建议以普通用户权限运行 UniGetUI，不要以管理员身份运行。Windows 小组件通常以普通权限运行，如果 UniGetUI 以管理员权限启动，小组件可能无法访问 UniGetUI 的 IPC 管道。
- 如果更新结果没有立即显示，可以点击小组件上的刷新按钮。刷新按钮会强制重新检测；普通打开小组件面板会优先复用 10 分钟内的缓存。

## 日志位置

小组件日志会写入以下位置：

```text
%LOCALAPPDATA%\Widgets-for-UniGetUI\widget.log
%TEMP%\Widgets-for-UniGetUI-widget.log
```

## 构建说明

本项目基于 .NET 8、Windows App SDK 1.6 和 Windows Widgets API。为了与官方 MSIX 布局兼容，打包时保留官方应用布局，并替换本适配版的小组件提供器二进制。

---

# UniGetUI Widgets Simplified Chinese Adaptation

This is a Simplified Chinese adaptation of `Widgets for UniGetUI`. It shows package updates detected by UniGetUI in the Windows Widgets panel and supports opening UniGetUI, viewing updates, refreshing the update list, and related widget actions.

## Changes

- Localized widget names, descriptions, loading pages, empty states, error pages, and update lists into Simplified Chinese.
- Updated the connector for the current UniGetUI IPC API under `/uniget/v1/...`.
- Fixed the packaged widget provider exiting immediately after startup.
- Improved the `Win + W` experience: update results are cached for 10 minutes, so opening the Widgets panel repeatedly no longer triggers a full check every time.
- Added local logs to make startup and IPC issues easier to diagnose.

## Installation

Download and install the release package:

```text
UniGetUI小组件_简体中文.msix
```

If Windows shows a certificate or publisher warning, make sure the MSIX is signed with a trusted local test certificate. After installation, open the Windows Widgets panel and add “UniGetUI 小组件”.

## Notes

- UniGetUI must be installed and running first.
- Run UniGetUI as a normal user whenever possible. Windows Widgets normally run without elevation; if UniGetUI is running as administrator, the widget may not be able to access UniGetUI's IPC pipe.
- Use the refresh button inside the widget when you need a forced update check. Normal panel activation reuses cached results for up to 10 minutes.

## Logs

Widget logs are written to:

```text
%LOCALAPPDATA%\Widgets-for-UniGetUI\widget.log
%TEMP%\Widgets-for-UniGetUI-widget.log
```

## Build Notes

This project uses .NET 8, Windows App SDK 1.6, and the Windows Widgets API. The package keeps the official MSIX layout and replaces the widget provider binaries with this adapted build.
