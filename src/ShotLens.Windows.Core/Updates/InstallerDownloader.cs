namespace ShotLens.Windows.Core.Updates;

public sealed class InstallerDownloader(HttpClient httpClient, string downloadDirectory)
{
    private const long MaxInstallerBytes = 500L * 1024 * 1024;

    public async Task<string> DownloadAsync(
        AvailableUpdate update,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        ValidateAssetName(update);
        Directory.CreateDirectory(downloadDirectory);

        var prefix = Guid.NewGuid().ToString("N");
        var finalPath = Path.Combine(downloadDirectory, $"{prefix}-{update.Installer.Name}");
        var partialPath = $"{finalPath}.partial";
        progress?.Report(0);

        try
        {
            using var response = await httpClient.GetAsync(
                update.Installer.DownloadUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new UpdateTransportException("下载安装包失败。");
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength == 0)
            {
                throw new UpdateTransportException("下载的安装包为空。");
            }

            if (contentLength is > MaxInstallerBytes)
            {
                throw new UpdateTransportException("安装包超过 500 MiB 限制。");
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = new FileStream(
                partialPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                128 * 1024,
                FileOptions.Asynchronous);
            var buffer = new byte[128 * 1024];
            long total = 0;

            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total > MaxInstallerBytes)
                {
                    throw new UpdateTransportException("安装包超过 500 MiB 限制。");
                }

                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                if (contentLength is > 0)
                {
                    progress?.Report(Math.Min(1, (double)total / contentLength.Value));
                }
            }

            if (total == 0)
            {
                throw new UpdateTransportException("下载的安装包为空。");
            }

            await target.FlushAsync(cancellationToken);
            target.Close();
            File.Move(partialPath, finalPath);
            progress?.Report(1);
            return finalPath;
        }
        finally
        {
            if (File.Exists(partialPath))
            {
                File.Delete(partialPath);
            }
        }
    }

    private static void ValidateAssetName(AvailableUpdate update)
    {
        var expected = update.Version.BetaNumber is null
            ? $"ShotLens-Windows-{update.Version}-Setup.exe"
            : $"ShotLens-Beta-{update.Version}-Setup.exe";
        if (!string.Equals(update.Installer.Name, expected, StringComparison.Ordinal))
        {
            throw new UpdateTransportException("安装包名称与目标版本不匹配。");
        }
    }
}
