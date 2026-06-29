using System.Diagnostics;
using System.Text;

namespace ShotLens.Windows.Platform.Ocr;

public sealed class SystemOcrWorkerProcessFactory : IOcrWorkerProcessFactory
{
    public IOcrWorkerProcess Start(OcrWorkerStartInfo startInfo)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = startInfo.FileName,
                UseShellExecute = startInfo.UseShellExecute,
                RedirectStandardInput = startInfo.RedirectStandardInput,
                RedirectStandardOutput = startInfo.RedirectStandardOutput,
                RedirectStandardError = startInfo.RedirectStandardError,
                CreateNoWindow = startInfo.CreateNoWindow,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new OcrWorkerProcessException(
                    "无法启动 OCR Worker。");
            }

            return new SystemOcrWorkerProcess(process);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    private sealed class SystemOcrWorkerProcess(
        Process process) : IOcrWorkerProcess
    {
        public bool HasExited => process.HasExited;

        public int ExitCode => process.ExitCode;

        public async Task WriteStandardInputAsync(
            string value,
            CancellationToken cancellationToken)
        {
            await process.StandardInput.WriteAsync(
                value.AsMemory(),
                cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
            process.StandardInput.Close();
        }

        public Task<string> ReadStandardOutputAsync(
            int maxUtf8Bytes,
            CancellationToken cancellationToken) =>
            ReadLimitedAsync(
                process.StandardOutput,
                maxUtf8Bytes,
                cancellationToken);

        public Task<string> ReadStandardErrorAsync(
            int maxUtf8Bytes,
            CancellationToken cancellationToken) =>
            ReadLimitedAsync(
                process.StandardError,
                maxUtf8Bytes,
                cancellationToken);

        public Task WaitForExitAsync(CancellationToken cancellationToken) =>
            process.WaitForExitAsync(cancellationToken);

        public void Kill(bool entireProcessTree)
        {
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree);
            process.WaitForExit();
        }

        public void Dispose() => process.Dispose();

        private static async Task<string> ReadLimitedAsync(
            TextReader reader,
            int maxUtf8Bytes,
            CancellationToken cancellationToken)
        {
            var buffer = new char[4096];
            var builder = new StringBuilder();
            var utf8Bytes = 0;
            while (true)
            {
                var count = await reader.ReadAsync(
                    buffer.AsMemory(),
                    cancellationToken);
                if (count == 0)
                {
                    return builder.ToString();
                }

                utf8Bytes = checked(
                    utf8Bytes + Encoding.UTF8.GetByteCount(buffer, 0, count));
                if (utf8Bytes > maxUtf8Bytes)
                {
                    throw new OcrWorkerOutputException(
                        "OCR Worker 输出超过大小限制。");
                }

                builder.Append(buffer, 0, count);
            }
        }
    }
}
