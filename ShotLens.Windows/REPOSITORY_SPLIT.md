# ShotLens Windows 仓库拆分设计

> 状态：已确认，待执行
> 日期：2026-06-29
> 原仓库：`qcsidios/ShotLens`
> 新仓库：`qcsidios/ShotLens-Windows`

## 1. 目标

将 ShotLens Windows 的代码、提交历史、构建工作流、安装包、Release 和 Tag 从 macOS 仓库中独立出来。

拆分完成后：

- 本地 macOS 仓库位于 `/Users/chenyilin/Vibe Coding/ShotLens`。
- 本地 Windows 仓库位于 `/Users/chenyilin/Vibe Coding/ShotLens-Windows`。
- GitHub macOS 仓库为 `qcsidios/ShotLens`，`main` 回退并固定在 macOS `v0.8.7`。
- GitHub Windows 仓库为公开仓库 `qcsidios/ShotLens-Windows`。
- Windows 历史代码保持原样，只调整版本号和独立仓库所必需的路径、URL、文档及工作流。
- 后续 Windows 重构从 `v0.2.0` 开始，不在本次仓库迁移中开发。

## 2. Windows 历史来源

Windows 代码从原仓库提交 `1633c27` 开始，当前 Windows 需求文档提交为 `4c2ec6c`。

历史安装包对应的源提交：

| 原 Release | 原源提交 | 新 Windows Release |
| --- | --- | --- |
| `v0.8.7` | `55f1ff8` | `v0.1.0` |
| `v0.8.8` | `9b95fbd` | `v0.1.1` |
| `v0.8.9` | `2769781` | `v0.1.2` |
| `v0.8.10` | `21f97f5` | `v0.1.3` |
| `v0.8.11` | `10c59df` | `v0.1.4` |
| `v0.8.12` | `b43dd6e` | `v0.1.5` |

新版本安装包必须从对应历史代码重新构建，不能只修改旧 EXE 文件名。安装器、程序内版本号、文件名、Tag 和 Release 必须一致。

## 3. 新 Windows 仓库结构

从原仓库历史中过滤并保留以下内容：

- `ShotLens.Windows/`
- `.github/workflows/windows-release.yml`
- `scripts/build-windows.ps1`
- Windows 版本对应的发布说明
- `VERSION`
- `LICENSE`

过滤后将 `ShotLens.Windows/` 内容提升到新仓库根目录：

```text
ShotLens-Windows/
├── .github/workflows/windows-release.yml
├── installer/
├── scripts/
├── src/
├── tests/
├── Directory.Build.props
├── LICENSE
├── README.md
├── REQUIREMENTS.md
├── REPOSITORY_SPLIT.md
└── VERSION
```

必要的仓库适配：

- 构建脚本路径改为新根目录结构。
- `Directory.Build.props` 从新仓库根目录读取 `VERSION`。
- 更新器、安装器和文档中的仓库 URL 改为 `qcsidios/ShotLens-Windows`。
- Windows Release 工作流只构建 Windows 安装包。
- README 改为 Windows-only 使用、构建和发布说明。
- `REQUIREMENTS.md` 的下一开发版本从 `v0.9.0` 改为 `v0.2.0`。

除上述版本和仓库适配外，不修改旧 Windows 业务功能。

## 4. Windows 提交历史与版本重排

新仓库使用过滤后的 Windows 提交历史，不复制 macOS 源码历史。

历史中的版本引用按以下规则替换：

- `v0.8.7` → `v0.1.0`
- `v0.8.8` → `v0.1.1`
- `v0.8.9` → `v0.1.2`
- `v0.8.10` → `v0.1.3`
- `v0.8.11` → `v0.1.4`
- `v0.8.12` → `v0.1.5`

在对应过滤后提交上重建 `v0.1.0–v0.1.5` Tag。原 `v0.8.x` Windows Tag 不进入新仓库。

当前 Windows 主分支版本保持 `v0.1.5`。未来完整重构使用 `v0.2.0`。

## 5. Windows Release 迁移

新仓库按顺序创建六个 Release：

- `v0.1.0`
- `v0.1.1`
- `v0.1.2`
- `v0.1.3`
- `v0.1.4`
- `v0.1.5`

每个 Release：

- 使用对应历史代码和新版本号重新构建中文 Setup.exe。
- 安装包命名为 `ShotLens-Windows-v0.1.x-Setup.exe`。
- Release 标题使用 `ShotLens Windows v0.1.x`。
- 发布说明只写该版本相对上一 Windows 版本的变化，不复制历史条目。
- 不上传 ZIP 或 macOS DMG。

发布顺序必须从 `v0.1.0` 到 `v0.1.5`。只有六个构建全部成功并可下载后，才能清理原仓库。

## 6. 原 macOS 仓库回退与清理

新仓库验证通过后，对 `qcsidios/ShotLens` 执行：

1. 将 `main` 回退到提交 `ea0fdc6`，即 macOS `v0.8.7`。
2. 强制推送回退后的 `main`。
3. 保留 macOS `v0.8.7` Tag、Release 和 DMG。
4. 从 `v0.8.7` Release 删除 Windows Setup.exe。
5. 删除原仓库 `v0.8.8–v0.8.12` Release。
6. 删除原仓库 `v0.8.8–v0.8.12` Tag。
7. 删除 Windows 专用远程分支 `codex/windows-version`。
8. 确认默认分支不再包含 `ShotLens.Windows/`、Windows Release 工作流和 Windows 构建脚本。
9. 删除原仓库中 `Windows Release` 工作流的历史运行记录。

原仓库回退后，最新 macOS 版本为 `v0.8.7`。不保留 `v0.8.8` 和 `v0.8.9` DMG，因为这两个版本来自 Windows 版本号联动，用户已确认 macOS 没有继续更新。

## 7. 安全顺序与回滚

任何远端删除前必须完成：

1. 创建包含全部分支和 Tag 的 Git Bundle。
2. 导出原仓库 Release 元数据。
3. 下载并校验原 Windows 安装包，记录 SHA-256。
4. 创建新 GitHub 仓库并推送 Windows `main`。
5. 推送新 Tag。
6. 完成六个 Windows Release 和安装包构建。
7. 验证新仓库默认分支、Tag、Release、安装包和更新 URL。

若新仓库验证失败，停止清理原仓库。

若 Mac 回退后发现问题，可从 Git Bundle 恢复原 `main`、Tag、Release 元数据和安装包。

## 8. 验收标准

### 8.1 本地

- 两个独立文件夹均为独立 Git 仓库。
- Windows 仓库没有 macOS Swift、Xcode、DMG 或 ScreenCaptureKit 内容。
- Mac 仓库工作树与 `v0.8.7` 一致。
- Windows 仓库 `VERSION` 为 `v0.1.5`。
- Windows 仓库保留需求文档。

### 8.2 新 GitHub 仓库

- 仓库公开且默认分支为 `main`。
- 提交历史只包含 Windows 相关内容。
- `v0.1.0–v0.1.5` Tag 均存在并指向正确代码状态。
- 六个 Release 均存在。
- 每个 Release 只有一个对应版本的 Setup.exe。
- Windows 更新器和安装器链接指向新仓库。

### 8.3 原 GitHub 仓库

- 默认分支回到 macOS `v0.8.7`。
- 最新 Tag 和 Release 为 `v0.8.7`。
- `v0.8.7` Release 只保留 DMG。
- 不存在 `v0.8.8–v0.8.12` Release 或 Tag。
- 不存在 Windows 构建工作流、Windows 安装包和 Windows 专用分支。
- Actions 页面不再保留 `Windows Release` 历史运行记录。

## 9. 非目标

本次迁移不包括：

- 修复 Windows 旧版本 Bug。
- 实现 Windows `v0.2.0`。
- 修改 macOS `v0.8.7` 功能。
- 修改默认 API 行为。
- 新增 Windows ARM64、OCR 或 UI 功能。
