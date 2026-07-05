# UniGetUI Widgets Simplified Chinese Adaptation

[简体中文](README.md)

This is a Simplified Chinese adaptation of `Widgets for UniGetUI`. It shows package updates detected by UniGetUI in the Windows Widgets panel and supports opening UniGetUI, viewing updates, refreshing the update list, and related widget actions.

## Changes

- Localized widget names, descriptions, loading pages, empty states, error pages, and update lists into Simplified Chinese.
- Updated the connector for the current UniGetUI IPC API under `/uniget/v1/...`.
- Fixed the packaged widget provider exiting immediately after startup.
- Improved the `Win + W` experience: update results are cached for 10 minutes, so opening the Widgets panel repeatedly no longer triggers a full check every time.
- Added local logs to make startup and IPC issues easier to diagnose.

## Installation

Download and install the MSIX package from this repository's Releases page:

```text
UniGetUI-zh-CN.msix
```

The package is intended for sideloading. If Windows shows a certificate or publisher warning, make sure the MSIX is signed with a trusted local test certificate.

After installation, open the Windows Widgets panel and add “UniGetUI 小组件”.

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
