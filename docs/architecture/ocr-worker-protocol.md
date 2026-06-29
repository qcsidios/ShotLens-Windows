# ShotLens OCR Worker 协议

> 协议版本：1
>
> 状态：阶段 0 已实现

## 一、进程边界

每次识别只启动一个 Worker，并且只处理一张图片：

1. 主进程创建唯一临时目录并写入 `input.png`。
2. 主进程关闭 Shell 启动 Worker，重定向 stdin、stdout 和 stderr。
3. stdin 写入一个 UTF-8 JSON 请求后立即关闭。
4. stdout 只能返回一个 UTF-8 JSON 响应，不允许日志或额外文字。
5. stderr 只记录事件代码、耗时和错误类型，不允许记录识别文字。
6. 主进程在 15 秒超时、取消或通信失败时终止整个进程树。
7. 主进程在 `finally` 中释放进程并删除本次临时目录。

限制：

| 项目 | 上限 |
| --- | ---: |
| Worker 总时长 | 15 秒 |
| stdout | 16 MiB |
| stderr | 1 MiB |

## 二、请求

```json
{
  "protocolVersion": 1,
  "requestId": "1ecfbf0e8f6c4e0e923c826eb8ef33ab",
  "engine": "fixture",
  "imagePath": "C:\\Temp\\request-id\\input.png",
  "imageSize": {
    "width": 800,
    "height": 600
  },
  "languageHints": [
    "en",
    "zh-Hans"
  ]
}
```

约束：

- `protocolVersion` 当前只能为 `1`。
- `requestId` 必须是无连字符的小写或大写 GUID。
- `engine` 只能是 `fixture`、`onnx-paddleocr` 或 `paddlesharp`。
- `imagePath` 必须是存在的绝对路径。
- `imageSize` 宽高必须为正数。
- `languageHints` 可以为空数组，但元素不得为空。

## 三、响应

```json
{
  "protocolVersion": 1,
  "requestId": "1ecfbf0e8f6c4e0e923c826eb8ef33ab",
  "engine": "fixture",
  "engineVersion": "fixture-1",
  "elapsedMilliseconds": 42,
  "blocks": [
    {
      "text": "Fixture",
      "bounds": {
        "x": 10,
        "y": 20,
        "width": 120,
        "height": 32
      },
      "confidence": 0.98,
      "language": "en",
      "fontSizePixels": 24,
      "brightness": 0.72,
      "order": 0
    }
  ]
}
```

约束：

- 响应版本、`requestId` 和 `engine` 必须与请求一致。
- `engineVersion` 不能为空，`elapsedMilliseconds` 不得为负数。
- 文字、语言不能为空。
- 坐标使用原图物理像素，宽高为正数，且完整位于原图内。
- `confidence` 和 `brightness` 必须是 `0` 到 `1` 的有限数。
- `fontSizePixels` 必须是正有限数。
- `order` 必须从 `0` 开始且在一次响应内唯一。

## 四、失败

主进程拒绝以下结果：

- 未知协议版本、损坏 JSON、空 stdout 或额外 stdout。
- 图片缺失、无效尺寸、越界坐标或无效置信度。
- requestId 或引擎不匹配。
- Worker 无法启动、异常退出、非零退出或输出超限。

fixture Worker 的退出码：

| 退出码 | 含义 |
| ---: | --- |
| 0 | 成功 |
| 1 | 未分类运行错误 |
| 2 | 协议错误 |
| 3 | 引擎不可用 |
