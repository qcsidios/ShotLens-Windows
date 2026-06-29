# ShotLens Windows v0.2.0 详细技术规格

> 状态：依据已确认需求形成的实施规格  
> 产品基线：`REQUIREMENTS.md`（2026-06-28 已确认）  
> macOS 行为参考：ShotLens `ea0fdc6`  
> Windows 旧实现参考：ShotLens-Windows `3ea30b7`  
> 正式目标：`v0.2.0`  
> 开发通道：`v0.2.0-beta.N` GitHub Pre-release

## 一、文档权威与目标

优先级依次为：

1. `REQUIREMENTS.md` 决定产品范围与验收。
2. 本规格决定模块边界、数据契约、状态机和失败行为。
3. 分阶段 Plan 决定实施顺序。
4. Windows 旧代码只作技术参考，不决定新架构。
5. macOS `ea0fdc6` 只在需求明确要求行为对齐时作为事实来源。

目标：

- 从零重建，不在问题代码上继续打补丁。
- 截图、OCR、布局和渲染始终使用同一物理像素坐标系。
- OCR 原生运行时位于独立进程，崩溃不能带走主程序。
- v0.1.5 可覆盖升级到 beta.1，beta 可在应用内连续升级。
- 编译、质量、性能和实机行为分别留下证据。

非目标沿用 `REQUIREMENTS.md` 第 14 节，不增加 ARM64、云 OCR、Python、历史记录、账号、同步、ZIP、跨屏选区或插件系统。

## 二、不可改变的兼容身份

| 项目 | 稳定版 | beta |
| --- | --- | --- |
| 界面产品名 | `ShotLens` | `ShotLens`，附“测试版”标记 |
| 安装器 AppId | `{5A7B81D7-4566-4AB5-8A62-2CB0C96F0619}` | `{F6F10CFD-3739-4319-A150-E59C25CA3325}` |
| 安装目录 | `{autopf}\ShotLens` | `{autopf}\ShotLens Beta` |
| 主程序 | `ShotLens.Windows.App.exe` | `ShotLens.Windows.App.exe` |
| OCR 程序 | `ShotLens.Windows.Ocr.Worker.exe` | 同稳定版 |
| 设置目录 | `%APPDATA%\ShotLens` | `%APPDATA%\ShotLens Beta` |
| 安装包 | `ShotLens-Windows-{tag}-Setup.exe` | `ShotLens-Beta-{tag}-Setup.exe` |
| 版本 | `vMAJOR.MINOR.PATCH` | `vMAJOR.MINOR.PATCH-beta.N` |

两个通道共用更新仓库 `qcsidios/ShotLens-Windows`。beta 首次启动且自身设置不存在时，可只读导入稳定版设置一次；之后分别保存，禁止 beta 修改稳定版设置。

修改任何一项都必须另写迁移规格并获得确认。

## 三、工程边界

### 3.1 `ShotLens.Windows.Core`

目标框架为 `net8.0`，包含：

- 产品身份与 SemVer。
- 更新选择规则。
- 物理/逻辑坐标值对象。
- OCR JSON 契约。
- 布局分类与合并。
- 翻译解析、校验和重试规则。
- 设置模型与迁移。
- 工作流状态。

禁止引用 WPF、Win32、DXGI、注册表、原生 OCR 包或 UI 控件。

### 3.2 `ShotLens.Windows.Platform`

目标框架为 `net8.0-windows10.0.19041.0`，包含：

- Per-Monitor V2 检查。
- 显示器、DPI 与 DXGI。
- 全局快捷键。
- 开机启动。
- Mutex 与命名管道。
- OCR 子进程控制。
- 剪贴板。
- 安装器启动。

只返回 Core 数据对象，不操作 WPF 控件。

### 3.3 `ShotLens.Windows.Ocr.Worker`

每次只处理一张图：

1. 启动并加载选定 OCR 引擎。
2. 读取版本化请求。
3. 解码、预处理和识别。
4. 坐标映射回原图物理像素。
5. stdout 输出一个 JSON。
6. stderr 输出不含识别文字的诊断。
7. 退出并释放模型内存。

### 3.4 `ShotLens.Windows.App`

负责生命周期、主窗口、托盘、截图编排、框选窗、结果窗、取消与用户提示。不得包含 OCR 推理、SemVer 比较、翻译 JSON 修复或坐标数学。

依赖方向只能是：

```text
App → Platform → Core
App → Core
OCR Worker → Core
Benchmark → Core / OCR Worker process
```

## 四、核心数据契约

```csharp
public readonly record struct PhysicalPoint(int X, int Y);
public readonly record struct PhysicalSize(int Width, int Height);
public readonly record struct PhysicalRect(int X, int Y, int Width, int Height);
public readonly record struct LogicalRect(double X, double Y, double Width, double Height);
```

- 屏幕物理坐标允许负数。
- 图像内物理坐标不得为负。
- Width/Height 在系统边界必须为正。
- Right/Bottom 使用右开区间。

```csharp
public sealed record MonitorDescriptor(
    string Id,
    string DeviceName,
    PhysicalRect DesktopBounds,
    PhysicalSize FrameSize,
    uint DpiX,
    uint DpiY,
    bool IsPrimary);

public sealed record CaptureSelection(
    string MonitorId,
    LogicalRect LogicalBounds,
    PhysicalRect FramePixelBounds,
    PhysicalRect DesktopPixelBounds);
```

一个选区只属于一台显示器；两个物理矩形必须指向同一组像素。

```csharp
public sealed record OcrTextBlock(
    string Text,
    PhysicalRect Bounds,
    double Confidence,
    string DetectedLanguage,
    double EstimatedFontSize,
    double ForegroundBrightness,
    double BackgroundBrightness,
    int EngineOrder);
```

Bounds 为截图内物理像素；Confidence 和亮度为 `0..1`。

```csharp
public sealed record LayoutTextBlock(
    string SourceText,
    PhysicalRect Bounds,
    string DetectedLanguage,
    double EstimatedFontSize,
    double ForegroundBrightness,
    double BackgroundBrightness,
    IReadOnlyList<int> SourceEngineOrders);
```

合并后仍能追溯所有原始 OCR 块。

## 五、应用生命周期

### 5.1 单实例

- 每用户命名 Mutex 选出主实例。
- 第二实例通过每用户命名管道发送 `ShowMainWindow` 后退出。
- 管道监听运行在后台任务，UI 线程不阻塞。
- 激活请求通过 Dispatcher 切回 UI 线程。

### 5.2 退出顺序

关闭主窗只隐藏。只有托盘退出、升级安装或致命启动错误可结束进程。退出顺序：

1. 取消当前流程。
2. 禁止新触发。
3. 注销快捷键。
4. 停止管道。
5. 释放托盘。
6. 终止 OCR 子进程。
7. 关闭窗口。
8. 退出。

### 5.3 截图流程状态

```csharp
public enum CaptureWorkflowState
{
    Idle, FreezingDisplays, Selecting, Recognizing,
    Translating, ShowingResult, Failed, Cancelling
}
```

按钮、托盘和快捷键全部调用 `TryStartCaptureAsync`。同一时刻只有一个流程。

```text
Idle → FreezingDisplays
FreezingDisplays → Selecting | Failed | Idle
Selecting → Recognizing | Idle
Recognizing → Translating | Failed
Translating → ShowingResult | Failed
ShowingResult → Translating | Idle
Failed → Translating | Idle
任意活动状态 → Cancelling → Idle
```

## 六、设置

```json
{
  "schemaVersion": 2,
  "shortcut": {"modifiers": ["Control", "Alt"], "key": "S"},
  "launchAtLogin": false,
  "api": {"mode": "default", "endpoint": "", "key": "", "model": ""}
}
```

API 模式：

- `default`：使用内置地址、模型和受保护默认凭据。
- `custom`：只使用用户字段，不自动回退。
- `disabled`：“清空”后的状态，禁止默认回退。

用户自定义 Key 以明文写入本地 `settings.json`。界面必须明确说明“自定义 Key 仅保存在本机设置文件中”，不得暗示已加密；ShotLens 不将设置同步到任何云端。

保存规则：

- 文本编辑防抖 300 ms。
- 开关与按钮操作立即保存。
- 写入 `settings.json.tmp` 后原子替换。
- 保留最后一次可解析的 `.bak`。
- 内容未变化不重复写盘。

v0.1.5 迁移：

- 默认回退开启且自定义字段为空 → `default`。
- 任一自定义字段有值 → `custom`。
- 默认回退关闭且字段为空 → `disabled`。
- 快捷键无效 → `Ctrl + Alt + S`。

默认 Key：

- 不提交源码。
- 不进入设置、UI、异常和日志。
- 只在构造默认模式 HTTP 请求时取得。
- 正式构建通过受保护的构建输入生成凭据提供器。
- 本地无凭据构建仍支持自定义 API。
- Secret 名固定为 `SHOTLENS_DEFAULT_API_KEY`。
- 发布任务从环境变量读取 Secret，在 `obj/` 生成未跟踪的编译输入，发布结束后删除。
- Secret 不得通过命令行参数传递，不得复制到 Artifact 旁的文本文件。
- Release 构建缺少 Secret 必须失败；普通 CI、Fork PR 和本地开发构建允许缺失。
- 默认地址和模型作为非敏感构建常量；安装后默认模式直接使用硅基流动 API。
- 默认地址固定为 `https://api.siliconflow.cn/v1`。
- 默认模型固定为 `tencent/Hunyuan-MT-7B`。

## 七、主界面

固定顺序：

1. Logo、产品名、版本和更新。
2. 快捷键卡片。
3. 开机启动卡片。
4. API 卡片。
5. 自动保存提示和主按钮。

API 展开只改变窗口高度，不移动头部对齐。空间不足时才使用纵向滚动。

快捷键录制：

1. 暂停旧快捷键。
2. 接收候选组合。
3. 校验修饰键和普通键。
4. 先尝试注册候选。
5. 成功后保存。
6. 失败则恢复旧快捷键并在卡片内提示。
7. Esc 取消。

开机启动写当前用户启动项。写入失败时开关回滚。

由开机启动项启动时只创建托盘、快捷键和后台服务，不显示主窗口；用户手动启动或第二实例唤起时显示主窗口。

API 卡片支持默认、自定义、不完整、可用、不可用状态；Key 输入框只显示用户 Key；模型支持输入和预设选择；API 测试必须发送最小真实聊天补全。

## 八、更新与覆盖安装

```csharp
public enum UpdateChannel { Stable, Beta }
```

- 正式版本使用 Stable，忽略 Draft 和 Pre-release。
- beta 使用 Beta，可发现更高 beta 和正式版。
- Stable 只认 `ShotLens-Windows-{tag}-Setup.exe`。
- Beta 对 Pre-release 只认 `ShotLens-Beta-{tag}-Setup.exe`；发现更高正式版时只认稳定资产 `ShotLens-Windows-{tag}-Setup.exe`。
- beta.1 由用户首次手动安装；旧版稳定更新器无法识别 beta 资产名。
- beta.1 之后由 beta 通道应用内覆盖升级。
- 接近正式版时单独验证 v0.1.5 到稳定候选版。

更新状态：

```text
Idle → Checking → UpToDate | Available | Failed
Available → Downloading → LaunchingInstaller | Failed
```

下载到唯一 `.partial` 文件；校验 HTTP、文件名、版本和非空；有可信校验和时验证 SHA-256；成功后原子改名，失败或取消删除残留。

安装参数：

```text
/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS
```

安装进程成功启动后，应用按退出顺序关闭。安装器保持 AppId、目录和 `%APPDATA%\ShotLens`，覆盖后自动重启。

beta 安装器使用独立 AppId、目录和设置目录，只覆盖其他 beta，不接触稳定版。正式候选安装器使用稳定版 AppId，负责验证 v0.1.5 的真实覆盖升级。

## 九、截图与框选

- Manifest 在 WPF 初始化前声明 Per-Monitor V2。
- 主窗隐藏并完成一帧渲染后再截图。
- 每台显示器独立建立 Desktop Duplication。
- 每台显示器取得一张原始物理像素 BGRA 帧。
- 不使用 `Graphics.CopyFromScreen`。
- 不合成鼠标指针。
- 选区完成后不二次截图。
- `DXGI_ERROR_ACCESS_LOST` 只允许重建重试一次。
- 所有 COM、帧和缓冲区确定性释放。

框选：

- 每屏一个无边框窗口。
- 背景是对应冻结帧。
- 外部约 36% 黑色蒙版。
- 选区白色细边框。
- 支持任意拖动方向。
- 最小 20×20 逻辑像素。
- Esc 或无效点击取消。
- 选区不得跨屏。

坐标换算：

```text
left/top     = floor(logical * dpi / 96)
right/bottom = ceil(logical * dpi / 96)
```

向外取整后限制在 `[0, frameWidth] × [0, frameHeight]`，裁剪只读取冻结帧并能包含四边最后一个像素。

## 十、OCR

阶段 0 用同一数据集比较：

1. ONNX Runtime + PP-OCR Mobile。
2. PaddleSharp。

协议请求包含 protocolVersion、requestId、engine、绝对图片路径、尺寸和语言提示；响应包含相同 requestId、引擎版本、耗时和完整 OcrTextBlock 数组。stdout 只能有一个 JSON，stderr 不得输出识别文本。

主进程：

1. 创建唯一临时目录。
2. 写原始裁剪 PNG。
3. 无 Shell 启动 Worker。
4. 重定向 stdout/stderr。
5. 设置硬超时。
6. 超时杀进程树。
7. 校验退出码、JSON、版本、requestId 和每个块。
8. `finally` 清理临时文件。

预处理允许小字等比例放大，但必须保存精确变换并映射回原图；禁止吞掉细字体、标点和浅色字的激进二值化。

阶段 0 基准集由项目自动生成，至少包含 30 张英文、20 张中文、10 张中英混合 UI/网页渲染图。生成器必须使用 Windows 实际字体与渲染引擎，覆盖浅色、深色、不同字号、缩放和密集段落，并保存随机种子、渲染参数和期望文本。两条候选路线必须使用字节完全一致的图片。

生成数据只用于公平技术选型，不宣称代表全部真实用户场景。beta 阶段出现的漏识别截图在脱敏后加入回归集；正式发布结论同时参考生成基准与 beta 反馈。报告字符准确率、行召回率、顺序、中位数/P95、启动耗时、峰值内存、运行时与模型体积及所有整段漏识别。

## 十一、运行参数与资源限制

| 边界 | 默认限制 | 失败行为 |
| --- | ---: | --- |
| GitHub Release API | 10 秒；响应 ≤2 MiB | 显示无法连接，可重试 |
| API 测试 | 15 秒；响应 ≤1 MiB | 卡片显示 API 不可用 |
| 正式翻译请求 | 60 秒；响应 ≤2 MiB | 进入可重试失败状态 |
| OCR Worker | 15 秒；stdout ≤16 MiB | 杀进程树并清理临时目录 |
| DXGI 单次取帧 | 500 ms | 重建一次后失败 |
| 安装包下载 | 文件 ≤500 MiB；无固定总时长 | 超限或中断删除 `.partial` |

- 所有网络请求支持 CancellationToken。
- 重试只发生在规格明确允许的层级，不做无限重试。
- 上限应定义为命名常量并有边界测试。
- 超时值如需调整，必须以实机数据和测试变更为依据。

## 十二、布局与翻译

布局流水线：

```text
原始 OCR → 边界校验 → 文本规范化 → 噪声过滤
→ 阅读顺序 → 布局分类 → 条件合并
```

只删除空文本、纯无意义标点和高概率图标。不得因置信度略低删除有效整行。

分类包括 ShortText、MenuList、Paragraph、Article、Mixed。行为与阈值从 macOS `TextLayoutOptimizer` 移植并用样本锁定；短文本与菜单不合并；标题不并入正文；不同栏不合并。

翻译地址接受 `/v1`、`/v1/`、`/v1/models`、`/v1/chat/completions` 并规范化。源语言从 OCR 文本推断；目标语言取 Windows 首选显示语言，失败回退简体中文。

模型首选返回与输入等长的 JSON 字符串数组。允许修复：

- JSON 代码围栏。
- translations 对象包装。
- 数字键对象。
- 带索引对象数组。
- 0/1 起始编号列表。
- 单输入时的纯文本。

错误数量、解释、拒答、空项和 HTML 错误页均拒绝。顺序为：批量请求 → 一次格式修复 → 逐项降级 → 单项重试 → `翻译失败`。重试不重新截图和 OCR。

## 十三、结果浮层

- 结果窗位于原选区，置顶、无边框、可拖动。
- 透明背景窗接收外部点击。
- 相邻状态窗不遮挡主体并跟随拖动。
- 窗口逻辑尺寸由截图物理像素除以该屏 DPI。
- OCR 坐标只在最终绘制边界转换。
- 原文/译文切换复用同一截图，不重新采样。

译文绘制：

1. 从估算原字号开始。
2. 保持标题与正文层级。
3. 使用近乎不透明浅色背景。
4. 按字符换行。
5. 逐步缩小直至适配。
6. 绘制区域必须在原块和截图内。
7. 仍无法适配时保留原文，不覆盖邻块。

状态包括正在识别、正在翻译、译文、原文、失败。支持只重试翻译、Esc、外部点击、拖动和复制。复制时一次向剪贴板写入当前位图及按阅读顺序排列的 Unicode 译文；成功后关闭，失败时保留窗口。

## 十四、并发、错误与隐私

- 应用生命周期、截图流程、更新操作分别拥有 CancellationToken。
- 安装开始前取消截图。
- UI 线程只更新控件和创建窗口，不等待 HTTP、OCR、DXGI 或大 JSON。
- 用户取消不是错误；清理资源后回 Idle。

错误分类：Startup、HotKey、Settings、Capture、Ocr、Translation、Clipboard、Update、Installer。内部保留稳定错误码和安全上下文；UI 只显示简短中文消息，绝不直接显示原异常。

隐私：

- 图片只在本机。
- 只有 OCR 文字发给翻译 API。
- 不做遥测。
- 日志可记录版本、OS、错误码、屏幕尺寸/DPI、数量、耗时和退出码。
- 日志禁止图片、OCR/译文、Key、Authorization、完整请求响应和默认凭据。

## 十五、打包、测试与性能

正式发布目录只包含主程序、.NET 运行时、胜出 OCR Worker、中英文模型、资源、VERSION 和许可。不得包含 Python、CUDA、落选引擎、基准集、调试符号或 ZIP。

当前按无代码签名证书设计。安装包允许出现未知发布者提示；若以后提供证书，再为主程序、OCR Worker 和安装器增加签名步骤。

构建顺序：

1. 校验 Tag 与 VERSION。
2. 锁定依赖还原。
3. 运行测试。
4. 发布 self-contained `win-x64`。
5. 运行命令和窗口冒烟。
6. 扫描禁用文件与敏感信息。
7. 编译安装器。
8. 验证名称、非空与 SHA-256。
9. 上传 CI Artifact 或经授权的 Release。

测试必须覆盖：

- SemVer、Release 过滤、地址规范化。
- 设置迁移、原子保存、默认 Key 不落盘。
- DPI 换算、边界裁剪与负坐标。
- OCR 超时、崩溃、空输出、损坏 JSON。
- 布局分类与合并。
- 翻译修复、数量校验和降级。
- 译文框包含与不重叠属性。
- 单实例、快捷键、启动项、DXGI、剪贴板和升级实机集成。

性能预算：

| 操作 | 目标 |
| --- | --- |
| 触发到可框选 | ≤300 ms |
| OCR 中位数 | ≤1.5 s |
| OCR P95 | ≤3 s |
| 布局与首次绘制 | ≤200 ms |

网络翻译单独统计，冷启动与热启动 OCR 分开记录。

## 十六、阶段边界与所需输入

阶段顺序固定为：

1. 基础与可行性。
2. 应用外壳与主界面。
3. 截图与框选。
4. OCR、布局与翻译。
5. 结果浮层。
6. 打包、升级与正式发布。

执行中仍需：

- 项目生成的 60 张可重复 OCR 渲染图，以及 beta 阶段逐步补充的脱敏回归样本。
- 用户 Windows 电脑的 DXGI 诊断结果。
- 用户执行的安装与升级验收。
- 通过受保护构建输入提供的默认 API 凭据。
- 每次远程发布前的明确授权。

缺少输入时必须明确标为未完成，不得降低验收标准。

## 十七、需求追踪

| 需求 | 本规格 |
| --- | --- |
| UI-001..007 | 七 |
| APP-001..002 | 五 |
| CAP-001..005 | 四、五、九 |
| OCR-001..004 | 四、十 |
| LAYOUT-001..002 | 十二 |
| TRANS-001..004 | 六、十二 |
| RESULT-001..004 | 十三 |
| UPDATE-001..002 | 八、十五 |
| VERSION/RELEASE | 二、八、十五 |
| 自动化、OCR、性能、实机矩阵 | 十、十一、十五 |

只有 `REQUIREMENTS.md` 第 12.7 节全部通过，才能发布 v0.2.0。
