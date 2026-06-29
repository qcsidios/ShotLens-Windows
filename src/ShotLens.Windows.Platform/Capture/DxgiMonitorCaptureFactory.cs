using System.Runtime.InteropServices;
using SharpGen.Runtime;
using ShotLens.Windows.Core.Capture;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using D3D11Api = Vortice.Direct3D11.D3D11;
using DxgiApi = Vortice.DXGI.DXGI;
using DxgiMapFlags = Vortice.Direct3D11.MapFlags;
using DxgiModeRotation = Vortice.DXGI.ModeRotation;
using DxgiResultCode = Vortice.DXGI.ResultCode;

namespace ShotLens.Windows.Platform.Capture;

public sealed class DxgiMonitorCaptureFactory : IMonitorCaptureFactory
{
    public IReadOnlyList<MonitorCaptureTarget> EnumerateOutputs()
    {
        var targets = new List<MonitorCaptureTarget>();
        using var factory = DxgiApi.CreateDXGIFactory1<IDXGIFactory1>();
        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            var adapterResult = factory.EnumAdapters1(
                adapterIndex,
                out var adapter);
            if (adapterResult == DxgiResultCode.NotFound)
            {
                break;
            }

            adapterResult.CheckError();
            using (adapter)
            {
                EnumerateAdapterOutputs(targets, adapter, (int)adapterIndex);
            }
        }

        return targets;
    }

    public IMonitorCaptureSession CreateSession(MonitorCaptureTarget target)
    {
        var factory = DxgiApi.CreateDXGIFactory1<IDXGIFactory1>();
        try
        {
            factory.EnumAdapters1(
                checked((uint)target.AdapterIndex),
                out var adapter).CheckError();
            IDXGIOutput? output = null;
            try
            {
                adapter.EnumOutputs(
                    checked((uint)target.OutputIndex),
                    out output).CheckError();
                return new DxgiMonitorCaptureSession(
                    factory,
                    adapter,
                    output,
                    target);
            }
            catch
            {
                output?.Dispose();
                adapter.Dispose();
                throw;
            }
        }
        catch
        {
            factory.Dispose();
            throw;
        }
    }

    private static void EnumerateAdapterOutputs(
        List<MonitorCaptureTarget> targets,
        IDXGIAdapter1 adapter,
        int adapterIndex)
    {
        for (uint outputIndex = 0; ; outputIndex++)
        {
            var outputResult = adapter.EnumOutputs(outputIndex, out var output);
            if (outputResult == DxgiResultCode.NotFound)
            {
                break;
            }

            outputResult.CheckError();
            using (output)
            {
                var description = output.Description;
                if (!description.AttachedToDesktop)
                {
                    continue;
                }

                var bounds = description.DesktopCoordinates;
                var width = checked(bounds.Right - bounds.Left);
                var height = checked(bounds.Bottom - bounds.Top);
                GetDpi(description.Monitor, out var dpiX, out var dpiY);
                var id = $"adapter-{adapterIndex}-output-{outputIndex}";
                targets.Add(
                    new MonitorCaptureTarget(
                        adapterIndex,
                        (int)outputIndex,
                        new MonitorDescriptor(
                            id,
                            description.DeviceName,
                            new PhysicalRect(bounds.Left, bounds.Top, width, height),
                            new PhysicalSize(width, height),
                            dpiX,
                            dpiY,
                            bounds.Left == 0 && bounds.Top == 0),
                        ToMonitorRotation(description.Rotation)));
            }
        }
    }

    private static MonitorRotation ToMonitorRotation(
        DxgiModeRotation rotation) =>
        rotation switch
        {
            DxgiModeRotation.Unspecified or DxgiModeRotation.Identity =>
                MonitorRotation.Identity,
            DxgiModeRotation.Rotate90 => MonitorRotation.Rotate90,
            DxgiModeRotation.Rotate180 => MonitorRotation.Rotate180,
            DxgiModeRotation.Rotate270 => MonitorRotation.Rotate270,
            _ => throw new ArgumentOutOfRangeException(nameof(rotation))
        };

    private static void GetDpi(
        nint monitor,
        out uint dpiX,
        out uint dpiY)
    {
        const int effectiveDpi = 0;
        var result = GetDpiForMonitor(
            monitor,
            effectiveDpi,
            out dpiX,
            out dpiY);
        if (result != 0)
        {
            dpiX = 96;
            dpiY = 96;
        }
    }

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(
        nint monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);

    private sealed class DxgiMonitorCaptureSession : IMonitorCaptureSession
    {
        private static readonly FeatureLevel[] FeatureLevels =
        [
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0
        ];

        private readonly IDXGIFactory1 _factory;
        private readonly IDXGIAdapter1 _adapter;
        private readonly IDXGIOutput _output;
        private readonly MonitorCaptureTarget _target;
        private readonly ID3D11Device _device;
        private readonly ID3D11DeviceContext _context;
        private readonly IDXGIOutputDuplication _duplication;
        private bool _disposed;

        public DxgiMonitorCaptureSession(
            IDXGIFactory1 factory,
            IDXGIAdapter1 adapter,
            IDXGIOutput output,
            MonitorCaptureTarget target)
        {
            _factory = factory;
            _adapter = adapter;
            _output = output;
            _target = target;

            ID3D11Device? device = null;
            ID3D11DeviceContext? context = null;
            IDXGIOutputDuplication? duplication = null;
            try
            {
                D3D11Api.D3D11CreateDevice(
                    adapter,
                    DriverType.Unknown,
                    DeviceCreationFlags.BgraSupport,
                    FeatureLevels,
                    out device,
                    out context).CheckError();
                using var output1 = output.QueryInterface<IDXGIOutput1>();
                duplication = output1.DuplicateOutput(device);
                _device = device;
                _context = context;
                _duplication = duplication;
            }
            catch
            {
                duplication?.Dispose();
                context?.Dispose();
                device?.Dispose();
                throw;
            }
        }

        public ValueTask<CapturedBgraFrame> AcquireFrameAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            var timeoutMilliseconds = checked(
                (uint)Math.Clamp(
                    Math.Ceiling(timeout.TotalMilliseconds),
                    0,
                    uint.MaxValue));
            var result = _duplication.AcquireNextFrame(
                timeoutMilliseconds,
                out _,
                out var desktopResource);
            if (result == DxgiResultCode.WaitTimeout)
            {
                throw new CaptureTimeoutException();
            }

            if (result == DxgiResultCode.AccessLost)
            {
                throw new CaptureAccessLostException();
            }

            result.CheckError();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (desktopResource)
                using (var desktopTexture =
                    desktopResource.QueryInterface<ID3D11Texture2D>())
                {
                    return ValueTask.FromResult(
                        CopyFrameToCpu(desktopTexture, cancellationToken));
                }
            }
            finally
            {
                _duplication.ReleaseFrame();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _duplication.Dispose();
            _context.Dispose();
            _device.Dispose();
            _output.Dispose();
            _adapter.Dispose();
            _factory.Dispose();
        }

        private CapturedBgraFrame CopyFrameToCpu(
            ID3D11Texture2D desktopTexture,
            CancellationToken cancellationToken)
        {
            var sourceDescription = desktopTexture.Description;
            if (sourceDescription.Format != Format.B8G8R8A8_UNorm)
            {
                throw new InvalidOperationException(
                    $"不支持的桌面帧格式：{sourceDescription.Format}。");
            }

            var stagingDescription = sourceDescription;
            stagingDescription.Usage = ResourceUsage.Staging;
            stagingDescription.BindFlags = BindFlags.None;
            stagingDescription.CPUAccessFlags = CpuAccessFlags.Read;
            stagingDescription.MiscFlags = ResourceOptionFlags.None;
            using var stagingTexture =
                _device.CreateTexture2D(stagingDescription);
            _context.CopyResource(stagingTexture, desktopTexture);

            _context.Map(
                stagingTexture,
                0,
                MapMode.Read,
                DxgiMapFlags.None,
                out var mapped).CheckError();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var width = checked((int)sourceDescription.Width);
                var height = checked((int)sourceDescription.Height);
                var rowPitch = checked((int)mapped.RowPitch);
                var pixels = new byte[checked(rowPitch * height)];
                Marshal.Copy(
                    mapped.DataPointer,
                    pixels,
                    0,
                    pixels.Length);
                using var rawFrame = new CapturedBgraFrame(
                    new PhysicalSize(width, height),
                    rowPitch,
                    pixels);
                var orientedFrame = BgraFrameRotation.ToDisplayOrientation(
                    rawFrame,
                    _target.Rotation);
                if (orientedFrame.Size != _target.Monitor.FrameSize)
                {
                    orientedFrame.Dispose();
                    throw new InvalidOperationException(
                        "旋转后的帧尺寸与显示器物理尺寸不一致。");
                }

                return orientedFrame;
            }
            finally
            {
                _context.Unmap(stagingTexture, 0);
            }
        }
    }
}
