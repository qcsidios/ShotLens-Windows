# ShotLens Windows 阶段 0 技术决策

> 日期：2026-06-29  
> 状态：已确认并开始执行

## 一、权威来源

1. 产品与验收以仓库根目录 `REQUIREMENTS.md` 为准。
2. 实现边界以 `docs/superpowers/specs/2026-06-29-shotlens-windows-v0.2.0-design.md` 为准。
3. 当前执行顺序以 `docs/superpowers/plans/2026-06-29-stage-0-foundation-validation.md` 为准。
4. Windows 旧实现只存在于 Git 历史，不能直接作为新架构。
5. macOS `ea0fdc6` 只用于需求明确要求的行为对齐。

## 二、已确认架构

- 主应用使用 .NET 8 与 WPF。
- 跨平台规则位于 `ShotLens.Windows.Core`。
- Windows API 位于 `ShotLens.Windows.Platform`。
- OCR 通过独立 `ShotLens.Windows.Ocr.Worker` 进程执行。
- 截图必须使用 DXGI Desktop Duplication。
- 截图、OCR、布局和渲染统一使用物理像素坐标。
- 设置、托盘、更新和安装器均重新实现，仅参考旧版行为。

## 三、安装与更新通道

| 项目 | 稳定版 | beta |
| --- | --- | --- |
| AppId | `{5A7B81D7-4566-4AB5-8A62-2CB0C96F0619}` | `{F6F10CFD-3739-4319-A150-E59C25CA3325}` |
| 安装目录 | `ShotLens` | `ShotLens Beta` |
| 设置目录 | `%APPDATA%\ShotLens` | `%APPDATA%\ShotLens Beta` |
| 安装包前缀 | `ShotLens-Windows-` | `ShotLens-Beta-` |

- beta.1 首次手动安装，后续 beta 应用内覆盖升级。
- beta 不得覆盖稳定版程序或设置。
- beta 首次启动可只读导入稳定版设置一次。
- 正式候选阶段再验证 v0.1.5 到 v0.2.0 的稳定通道升级。

## 四、API

- 默认服务：硅基流动。
- 默认地址：`https://api.siliconflow.cn/v1`。
- 默认模型：`tencent/Hunyuan-MT-7B`。
- 默认 Key 由 GitHub Actions Secret `SHOTLENS_DEFAULT_API_KEY` 注入发布构建。
- 用户自定义 Key 以明文保存在本机设置文件，不上传云端。
- 本地与普通 CI 构建没有默认 Key 时仍支持自定义 API。

## 五、OCR 选型

- 候选 A：ONNX Runtime + PP-OCR Mobile。
- 候选 B：PaddleSharp。
- 阶段 0 由项目生成 30 英文、20 中文、10 混合的可重复渲染样本。
- 两个候选必须使用 SHA-256 完全相同的图片。
- 选型以准确率、行召回、顺序、耗时、内存、体积和整段漏识别为依据。

## 六、开发和发布

- 开机启动只进入托盘。
- 当前没有代码签名证书，允许显示未知发布者。
- 分支和 PR 只产出 CI Artifact。
- beta Tag 产出 Pre-release。
- 正式 Tag 产出稳定 Release。
- 任何远程 Tag 或 Release 创建前再次确认授权。

## 七、旧基线记录

在 Commit `276fbac` 创建 Worktree 后运行：

```text
dotnet run --project tests/ShotLens.Windows.Core.Tests/ShotLens.Windows.Core.Tests.csproj --configuration Release
```

旧测试失败：

```text
Expected 0.8.12, got 0.1.5.
```

同一测试先要求 `VersionInfo.Current == "v0.1.5"`，又要求 `VersionInfo.SemVer == "0.8.12"`，属于仓库拆分后的既有矛盾。阶段 0 将删除并重建旧测试，不对旧实现打补丁。
