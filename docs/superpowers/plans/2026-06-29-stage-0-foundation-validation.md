# ShotLens Windows v0.2.0 阶段 0 实施计划

> **供智能开发代理使用：** 必须使用 `executing-plans` 逐项执行；功能实现遵循 TDD。步骤使用复选框记录，未经验证不得勾选。

**目标：** 从零建立可重复构建、应用内 beta 覆盖升级、DXGI 多屏物理像素截图和 OCR 证据选型四项基础。

**需求：** `REQUIREMENTS.md`

**规格：** `docs/superpowers/specs/2026-06-29-shotlens-windows-v0.2.0-design.md`

## 一、阶段完成门槛

- [ ] 干净检出可在 `windows-latest` 构建、测试和打包。
- [ ] Core 测试可在 macOS 运行且不加载 Windows 组件。
- [ ] beta.1 可与 v0.1.5 稳定版并行安装，互不覆盖设置。
- [ ] `beta.1 → beta.2` 可在新应用内完成。
- [ ] 两次升级均保留 `%APPDATA%\ShotLens` 哨兵设置。
- [ ] 安装后自动启动新版本。
- [ ] DXGI 为每台显示器生成一张无缩放 PNG。
- [ ] Manifest 正确记录混合 DPI 和负坐标。
- [ ] 坐标测试覆盖 100%、125%、150%、200% 和四边。
- [ ] OCR 在独立进程运行并输出版本化 JSON。
- [ ] 两条 OCR 路线使用字节完全一致的项目生成数据集。
- [ ] 报告包含准确率、召回率、顺序、耗时、内存和体积。
- [ ] 未取得证据前不决定 OCR 路线。

## 二、任务 1：隔离工作区与兼容约束

**文件：**
- 新建：`docs/architecture/stage-0-decisions.md`
- 新建：`docs/testing/stage-0-windows-acceptance.md`

- [ ] 运行 `git status --short`，要求为空。
- [ ] 使用 `using-git-worktrees` 创建 `codex/windows-v0.2.0-stage0`。
- [ ] 记录起始 Commit。
- [ ] 记录需求基线、WPF、DXGI、OCR 子进程、稳定/beta 双 AppId 和更新通道。
- [ ] 建立 Windows 环境、显示器、升级、设置、DXGI、OCR 验收表。
- [ ] 提交：`docs: 定义 Windows 重构阶段零`。

## 三、任务 2：建立全新解决方案

**文件：**
- 重建：`src/**`、`tests/**`
- 新建：`ShotLens.Windows.sln`、`Directory.Packages.props`
- 新建：App、Core、Platform、Ocr.Worker、Benchmark 和三个测试项目

- [ ] 用 `git ls-tree` 保存旧源码清单，旧代码只留在 Git 历史。
- [ ] 删除旧 `src/`、`tests/`。
- [ ] Core 目标为 `net8.0`。
- [ ] Windows 项目目标为 `net8.0-windows10.0.19041.0`。
- [ ] 开启 nullable、warnings-as-errors、确定性构建和包锁定。
- [ ] 将八个项目加入 Solution。
- [ ] 先写失败测试，锁定产品名、exe 和设置目录。
- [ ] 实现最小 `ProductIdentity`。
- [ ] macOS 运行 Core 测试。
- [ ] Windows 运行 restore、build，要求零警告。
- [ ] 提交：`build: 搭建全新 Windows 解决方案`。

## 四、任务 3：beta 语义化版本

- [ ] 将 `VERSION` 改为 `v0.2.0-beta.1`。
- [ ] 测试正式版与 beta 解析。
- [ ] 测试 `v0.1.5 < beta.1 < beta.2 < v0.2.0 < v0.10.0`。
- [ ] 拒绝不完整、beta.0 和四段版本。
- [ ] 实现最小 SemVer。
- [ ] VERSION 缺失或无效时明确失败，不硬编码旧版本。
- [ ] 构建脚本接受 `^v\d+\.\d+\.\d+(?:-beta\.[1-9]\d*)?$`。
- [ ] 运行全部 Core 测试。
- [ ] 提交：`feat: 支持 beta 语义化版本`。

## 五、任务 4：稳定版与 beta 更新选择

- [ ] 建立含 Draft、DMG、错名安装包、beta 和 v0.10.0 的 JSON fixture。
- [ ] 测试稳定通道忽略 Draft、Pre-release 和非 Windows 包。
- [ ] 测试 beta 发现更高 beta 和正式版。
- [ ] 测试当前版本不返回更新。
- [ ] 实现 `UpdateChannel`、`GitHubRelease`、`ReleaseSelector`。
- [ ] ReleaseSelector 不负责 HTTP。
- [ ] 运行聚焦测试。
- [ ] 提交：`feat: 定义正式与 beta 更新通道`。

## 六、任务 5：发现、下载和启动安装器

- [ ] 用假 HttpMessageHandler 测试 GitHub URL、User-Agent、HTTP 和 JSON 错误。
- [ ] 测试空文件、错名、取消和失败清理。
- [ ] 测试 `.partial` 成功后才原子改名。
- [ ] 测试进度单调递增。
- [ ] 锁定安装参数。
- [ ] 实现 GitHub 传输层。
- [ ] 实现原子下载。
- [ ] 注入进程启动器，测试不得启动真实安装器。
- [ ] 运行全部测试。
- [ ] 测试 GitHub API 10 秒超时、2 MiB 响应上限和取消。
- [ ] 测试下载文件 500 MiB 上限。
- [ ] 提交：`feat: 添加安全更新管线`。

## 七、任务 6：最小更新诊断界面

- [ ] 定义 Idle、Checking、UpToDate、Available、Downloading、LaunchingInstaller、Failed。
- [ ] 测试重复点击、非法升级、取消、失败重试和进度。
- [ ] 状态机不引用 WPF。
- [ ] 界面只显示 Logo、版本、通道、检测、升级和状态。
- [ ] 明确标注“阶段 0 诊断版本”。
- [ ] 安装器启动后按顺序释放服务并退出。
- [ ] 实现 `--smoke` 与 `--smoke-window`。
- [ ] macOS 只跑 Core 测试。
- [ ] Windows 两个冒烟命令均返回 0。
- [ ] 提交：`feat: 添加更新诊断外壳`。

## 八、任务 7：双通道安装器与覆盖升级脚本

- [ ] 源码测试分别锁定稳定版与 beta 的 AppId、目录、资产名和设置路径。
- [ ] beta 安装器使用独立身份并支持关闭、覆盖、重启。
- [ ] beta 安装器不得修改稳定版目录和 `%APPDATA%\ShotLens`。
- [ ] beta 首次启动可只读导入稳定设置一次。
- [ ] 卸载和安装都不得删除用户设置。
- [ ] 构建顺序固定为还原、测试、发布、冒烟、安装器、非空检查、SHA-256。
- [ ] 新建 `scripts/test-in-place-upgrade.ps1`。
- [ ] 脚本并行安装稳定版与 beta.1，验证两个卸载项和两个目录。
- [ ] 脚本写 beta 哨兵、安装 beta.2、核对覆盖路径和设置。
- [ ] 脚本验证稳定版与 beta 各有一个卸载项，beta 升级不新增重复项。
- [ ] 在一次性 CI 环境完成清理。
- [ ] 提交：`build: 保持 Windows 覆盖升级兼容`。

## 九、任务 8：GitHub Pre-release

- [ ] 契约测试保证分支/PR 只上传 Artifact。
- [ ] beta Tag 创建 Pre-release，正式 Tag 创建 Release。
- [ ] Tag 必须与 VERSION 完全一致。
- [ ] 只上传一个 Setup.exe，不上传 ZIP。
- [ ] 分离 `windows-ci.yml` 与 `windows-release.yml`。
- [ ] 增加同 Tag 并发保护。
- [ ] 分支 CI 成功且不产生 Release。
- [ ] 提交：`ci: 添加 Windows beta 发布通道`。

## 十、任务 9：DPI 与物理像素几何

- [ ] 测试 96、120、144、192 DPI。
- [ ] 测试左侧与上方负坐标显示器。
- [ ] 测试 left/top 向下取整，right/bottom 向上取整。
- [ ] 测试四边最后一个像素。
- [ ] 测试越界限制、空区域和 20×20 最小区域。
- [ ] Core 不使用 `System.Drawing.Rectangle` 或 WPF `Rect`。
- [ ] 实现不可变坐标对象与换算。
- [ ] 运行全部几何测试。
- [ ] 提交：`feat: 定义 DPI 安全截图坐标`。

## 十一、任务 10：DXGI 按显示器截图原型

- [ ] 查一手文档确认包的 Win10、.NET 8、Desktop Duplication 和许可。
- [ ] 锁定包版本。
- [ ] 用假枚举器测试 Manifest。
- [ ] 测试成功、超时、access-lost、写图失败和取消后的释放。
- [ ] 按 DXGI output 枚举显示器和 DPI。
- [ ] 每屏取得一张 BGRA 帧，不合成鼠标、不缩放。
- [ ] `finally` 释放 frame 与 COM 资源。
- [ ] 诊断命令输出 `manifest.json` 与每屏 PNG。
- [ ] 验证 PNG 尺寸等于物理帧。
- [ ] CI 跑假测试；用户实机跑真实诊断。
- [ ] 提交：`feat: 原型验证按屏 DXGI 截图`。

## 十二、任务 11：OCR 子进程协议

- [ ] 测试请求、响应 JSON 往返。
- [ ] 每块包含文字、坐标、置信度、语言、字号、亮度和顺序。
- [ ] 拒绝未知版本、缺图、无效坐标、无效置信度和损坏 JSON。
- [ ] 假 Worker 覆盖超时、崩溃、非零退出、空输出、额外 stdout 和取消。
- [ ] 每次请求使用唯一临时目录。
- [ ] stdout 只允许 JSON，诊断走 stderr。
- [ ] 超时杀进程树。
- [ ] 所有结果都在 `finally` 清理。
- [ ] 实现 fixture 引擎先证明隔离。
- [ ] 提交：`feat: 添加隔离 OCR Worker 协议`。

## 十三、任务 12：OCR 生成数据集与评分器

- [ ] 定义样本 ID、图片、语言、主题、最小字高、期望行、顺序和隐私字段。
- [ ] 创建可重复的 Windows UI/网页渲染生成器。
- [ ] 固定字体、视口、DPI、随机种子和渲染参数。
- [ ] 生成 30 张英文、20 张中文、10 张中英混合样本。
- [ ] 覆盖浅色、深色、小字、标题、菜单、密集段落和混合布局。
- [ ] 校验缺图、空标注、重复 ID、错误语言和敏感数据。
- [ ] 校验两条候选路线读取的图片 SHA-256 完全一致。
- [ ] 用手算样本测试插入、删除、替换、漏行和错序。
- [ ] 实现字符错误率、行召回和阅读顺序。
- [ ] 记录启动、OCR、总耗时、峰值内存、尺寸和模型版本。
- [ ] 输出机器可读 JSON 与中文 Markdown。
- [ ] 编写 beta 漏识别截图的脱敏与回归收录规范。
- [ ] 报告明确标注“生成基准，不代表完整真实用户场景”。
- [ ] 提交：`test: 添加可重复 OCR 基准`。

## 十四、任务 13：候选 A——ONNX

- [ ] 核实 PP-OCR 中英文模型、URL、SHA-256 和许可。
- [ ] 核实 ONNX Runtime 版本与许可。
- [ ] 下载脚本强制校验哈希。
- [ ] 测试 BGRA、RGB、等比缩放、小字放大和坐标还原。
- [ ] 测试所有框位于原图。
- [ ] 只实现检测、识别和原始块输出，不做布局合并。
- [ ] 跑完整项目生成数据集。
- [ ] 记录报告、Worker 和模型体积。
- [ ] 提交：`feat: 添加 ONNX PaddleOCR 候选`。

## 十五、任务 14：候选 B——PaddleSharp

- [ ] 核实包、原生运行时、CPU x64、.NET 8 和许可。
- [ ] 锁定依赖与模型。
- [ ] 测试与候选 A 完全一致的协议和坐标。
- [ ] 只写适配层，不改数据集和评分器。
- [ ] 跑同一完整数据集。
- [ ] 记录报告和发布体积。
- [ ] 提交：`feat: 添加 PaddleSharp OCR 候选`。

## 十六、任务 15：OCR 选型

- [ ] 记录 CPU、RAM、GPU、Windows Build、电源模式和 DPI。
- [ ] 每个引擎跑一次冷启动和两次热启动。
- [ ] 核对语料校验和、样本数、指标版本和机器完全相同。
- [ ] 比较英文≥97%、中文≥95%、行召回≥95%。
- [ ] 比较顺序、P50/P95、内存和体积。
- [ ] 逐张检查所有整段漏识别。
- [ ] 结论只能是选择 ONNX、选择 PaddleSharp 或两者均拒绝。
- [ ] 记录落选路线不打包的原因。
- [ ] 提交：`docs: 确定 Windows OCR 架构`。

## 十七、任务 16：发布并首次安装 beta.1

- [ ] 完整还原、测试、构建和冒烟。
- [ ] 检查名称、版本、图标、AppId、无 ZIP、无敏感信息。
- [ ] 写中文且仅描述本次变化的说明。
- [ ] 远程操作前取得明确授权。
- [ ] 创建 `v0.2.0-beta.1` Tag。
- [ ] 验证 Pre-release 只有一个正确安装包。
- [ ] 用户手动安装 beta.1，无需卸载 v0.1.5。
- [ ] 验证稳定版与 beta 可并行启动、目录和设置互不覆盖。
- [ ] 验证 beta 可一次性导入稳定版已有设置。
- [ ] 将截图、时间和日志路径写入验收文档。

## 十八、任务 17：发布并验收 beta.2

- [ ] 只修 beta.1 验收失败项。
- [ ] 每个修复先增加回归测试。
- [ ] VERSION 改为 `v0.2.0-beta.2`。
- [ ] 运行完整验证。
- [ ] 远程操作前取得明确授权。
- [ ] 发布 beta.2 Pre-release。
- [ ] 从 beta.1 应用内升级。
- [ ] 验证进度、关闭、重启、设置和唯一安装项。
- [ ] 记录证据并提交验收结果。

## 十九、任务 18：关闭阶段 0

- [ ] macOS 运行全部 Core 测试。
- [ ] Windows 运行锁定还原、Solution 测试和完整安装包构建。
- [ ] 逐条核对本计划完成门槛。
- [ ] Windows 专属项目不得仅凭代码检查通过。
- [ ] 记录阶段 1 延后内容。
- [ ] 确认 OCR 与 DXGI 边界足够稳定。
- [ ] 编写独立阶段 1 细计划。
- [ ] 提交：`docs: 关闭 Windows 重构阶段零`。

## 二十、用户检查点与外部依赖

停止并请求用户复核：

1. 全新骨架与删除旧源码差异。
2. beta 更新和安装器 CI。
3. Windows DXGI 诊断。
4. OCR 生成数据集设计与选型结论。
5. 两次远程 beta 发布。
6. 阶段 0 关闭和阶段 1 计划。

真实 Windows、默认 API 凭据和远程发布授权是显式外部依赖。OCR 阶段 0 数据集由项目生成，不要求用户提供现有截图。v0.1.5 到稳定版的覆盖升级留到正式候选阶段验证。
