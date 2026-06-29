# ShotLens for Windows

ShotLens 是一款 Windows 截图翻译工具。它支持全局快捷键截图、本地 OCR、兼容 OpenAI 的翻译 API、原位结果浮层、托盘常驻和应用内更新。

## 系统要求

- Windows 10 版本 2004（Build 19041）或更高版本
- Windows 11
- x64 Intel/AMD 处理器

## 安装

从 [GitHub Releases](https://github.com/qcsidios/ShotLens-Windows/releases) 下载最新的：

```text
ShotLens-Windows-vX.Y.Z-Setup.exe
```

双击中文安装包即可安装。默认截图快捷键为 `Ctrl + Alt + S`。

## 当前版本

当前历史稳定版本为 `v0.1.5`。下一次完整重构版本为 `v0.2.0`。

## 本地构建

在 Windows PowerShell 中运行：

```powershell
.\scripts\build-windows.ps1
```

脚本会运行核心测试、发布 self-contained `win-x64` 应用、执行启动 smoke 检查，并使用 Inno Setup 生成中文安装包。

## 发布

仓库根目录的 `VERSION` 是唯一版本来源。推送与 `VERSION` 一致的三段式 Tag 后，GitHub Actions 会构建并上传：

```text
ShotLens-Windows-vX.Y.Z-Setup.exe
```

每次 Release 说明只写相对上一版本的本次变化，不重复历史内容。

## 文档

- [Windows 重构需求](REQUIREMENTS.md)
- [仓库拆分记录](REPOSITORY_SPLIT.md)
- [迁移执行计划](MIGRATION_PLAN.md)

## 隐私

截图和 OCR 在本机处理。只有 OCR 识别出的文字会发送到默认或用户配置的翻译 API。

## 许可证

MIT
