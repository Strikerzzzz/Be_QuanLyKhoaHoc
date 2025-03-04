using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class VideoConverterService
{
    private readonly ILogger<VideoConverterService> _logger;

    public VideoConverterService(ILogger<VideoConverterService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Chuyển đổi video sang HLS bằng FFmpeg.
    /// Sử dụng NVENC nếu có GPU NVIDIA, nếu không thì dùng CPU với multi-threading.
    /// Có xử lý timeout và logging để tối ưu hiệu quả.
    /// </summary>
    /// <param name="inputFilePath">Đường dẫn file video đầu vào.</param>
    /// <param name="outputFolder">Thư mục lưu kết quả HLS.</param>
    /// <param name="cancellationToken">Cancellation token để hủy nếu cần.</param>
    /// <returns>true nếu chuyển đổi thành công, false nếu có lỗi.</returns>
    public async Task<bool> ConvertVideoToHLS(string inputFilePath, string outputFolder, CancellationToken cancellationToken = default)
    {
        // Đảm bảo thư mục đầu ra tồn tại
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string outputFilePath = Path.Combine(outputFolder, "output.m3u8");

        // Kiểm tra xem có GPU NVIDIA hay không
        bool hasNvidiaGPU = await CheckNvidiaGpuAvailability(cancellationToken);

        string arguments;
        if (hasNvidiaGPU)
        {
            arguments = $"-hwaccel cuda -i \"{inputFilePath}\" -c:v h264_nvenc -preset fast -c:a aac -hls_time 4 -hls_playlist_type vod \"{outputFilePath}\"";
            _logger.LogInformation("Using NVIDIA GPU (NVENC) for conversion.");
        }
        else
        {
            int threadCount = Environment.ProcessorCount;
            arguments = $"-i \"{inputFilePath}\" -c:v libx264 -threads {threadCount} -preset fast -c:a aac -hls_time 4 -hls_playlist_type vod \"{outputFilePath}\"";
            _logger.LogInformation("Using CPU multi-threading for conversion with {ThreadCount} threads.", threadCount);
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using (var process = new Process { StartInfo = processInfo, EnableRaisingEvents = true })
            {
                var tcs = new TaskCompletionSource<bool>();
                process.Exited += (s, e) => tcs.TrySetResult(true);

                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    try { process.Kill(); } catch { }
                    _logger.LogError("FFmpeg conversion timed out.");
                    return false;
                }

                var outputResult = await outputTask;
                var errorResult = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, errorResult);
                    return false;
                }

                _logger.LogInformation("FFmpeg conversion succeeded. Output: {Output}", outputResult);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during FFmpeg conversion.");
            return false;
        }
    }

    /// <summary>
    /// Kiểm tra khả năng có GPU NVIDIA bằng cách gọi lệnh nvidia-smi.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>true nếu có GPU NVIDIA, false nếu không.</returns>
    private async Task<bool> CheckNvidiaGpuAvailability(CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists("/dev/nvidia0"))
            {
                _logger.LogInformation("NVIDIA GPU device found at /dev/nvidia0.");
                return true;
            }
            else
            {
                _logger.LogWarning("NVIDIA GPU device not found.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error checking for NVIDIA GPU: {Message}", ex.Message);
            return false;
        }
    }
}
