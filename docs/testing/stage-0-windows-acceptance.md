# ShotLens Windows 阶段 0 实机验收

> 状态：待执行  
> 填写规则：没有实际输出、截图或日志时不得标记通过。

## 一、测试环境

- [ ] Windows 版本与 Build：
- [ ] CPU：
- [ ] 内存：
- [ ] 是否有独立显卡：
- [ ] 电源模式：
- [ ] 显示器数量：
- [ ] 显示器排列：
- [ ] 每台显示器分辨率与缩放：

## 二、稳定版与 beta 并行安装

- [ ] v0.1.5 稳定版已安装。
- [ ] beta.1 无需卸载稳定版即可安装。
- [ ] 系统中存在一个稳定版和一个 beta 卸载项。
- [ ] 两个版本位于不同安装目录。
- [ ] beta 不修改 `%APPDATA%\ShotLens`。
- [ ] beta 首次启动可导入稳定版设置。

证据：

```text
安装器：
安装时间：
安装目录：
卸载项截图：
设置文件 SHA-256：
```

## 三、beta.1 到 beta.2 应用内升级

- [ ] beta.1 能发现 beta.2。
- [ ] 显示下载进度。
- [ ] 安装器能关闭 beta.1。
- [ ] beta.2 安装到同一 beta 目录。
- [ ] 安装完成后自动启动 beta.2。
- [ ] beta 设置哨兵保持不变。
- [ ] 升级后仍只有一个 beta 卸载项。

证据：

```text
旧版本：
新版本：
检查时间：
下载时间：
重启时间：
安装器日志：
设置文件 SHA-256：
```

## 四、DXGI 诊断

安装 beta 后，在仓库根目录运行：

```powershell
.\scripts\run-dxgi-diagnostics.ps1
```

默认输出到桌面的 `ShotLens-DXGI-时间戳`。如安装位置不同：

```powershell
.\scripts\run-dxgi-diagnostics.ps1 `
  -ExecutablePath "D:\ShotLens Beta\ShotLens.Windows.App.exe"
```

- [ ] 每台显示器生成一张 PNG。
- [ ] PNG 尺寸等于显示器物理像素。
- [ ] Manifest 记录正确的桌面坐标。
- [ ] Manifest 记录正确的 DPI。
- [ ] 左侧或上方显示器使用负坐标。
- [ ] 图片不包含 ShotLens 蒙版、边框或鼠标。
- [ ] 图片没有虚拟桌面拉伸或模糊。

证据目录：

```text
manifest.json：
PNG：
用户描述的显示器排列：
```

## 五、OCR 基准

- [ ] 生成 30 张英文样本。
- [ ] 生成 20 张中文样本。
- [ ] 生成 10 张中英混合样本。
- [ ] ONNX 与 PaddleSharp 使用相同语料 SHA-256。
- [ ] 两个候选各完成一次冷跑和两次热跑。
- [ ] 报告包含准确率、召回、顺序、P50、P95、内存与体积。
- [ ] 所有整段漏识别已人工检查。
- [ ] 选型结论已写入 `docs/architecture/ocr-engine-decision.md`。

## 六、结论

- [ ] 阶段 0 全部自动化测试通过。
- [ ] 阶段 0 所有 Windows 专属项目有真实证据。
- [ ] 阶段 0 可以关闭。

未通过项与后续动作：

```text
待填写
```

## 七、v0.2.0-beta.1 基础可用版 CI 安装包证据

- CI Run：`28384906746`
- Commit：`a20b7ea76f9ba9354f5bd5d7bc3c2b276da0cb80`
- 结果：Windows CI 全部通过。
- 已验证步骤：
  - 锁定依赖还原；
  - Solution 测试；
  - Solution 构建；
  - OCR 生成数据集；
  - ONNX OCR 完整基准；
  - PaddleSharp OCR 完整基准；
  - 命令冒烟；
  - 窗口冒烟；
  - beta 安装包构建；
  - beta 安装包 Artifact 上传。
- 安装包 Artifact：`ShotLens-Beta-v0.2.0-beta.1`
- 本地下载路径：`/tmp/shotlens-fast-a20b7ea/ShotLens-Beta-v0.2.0-beta.1-Setup.exe`
- 文件大小：218 MiB
- SHA-256：`e44729f4cfa3e544f6ef68604e725e0461b3e8064246cadb90a6096ca4582389`

说明：这是按“基础可用版”策略生成的安装包。它优先保证主界面、应用内升级管线、截图框选、PaddleSharp OCR、硅基流动翻译配置、结果窗口和复制能力闭环。OCR 精度优化、安装包体积优化、全量快捷键/托盘体验和 DXGI 正式选择窗留到后续应用内升级版本继续补。
