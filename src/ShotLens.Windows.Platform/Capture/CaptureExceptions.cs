namespace ShotLens.Windows.Platform.Capture;

public sealed class CaptureTimeoutException : Exception
{
    public CaptureTimeoutException()
        : base("在限定时间内未取得桌面帧。")
    {
    }
}

public sealed class CaptureAccessLostException : Exception
{
    public CaptureAccessLostException()
        : base("桌面复制会话已失效。")
    {
    }
}
