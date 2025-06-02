using System;
using System.Diagnostics;
using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public sealed class StreamlinkProcessor() : IProcessor
    {
        public async Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "streamlink",
                Arguments = $"--stream-url {streamInfo.Url} best",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            string result = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!result.StartsWith("http"))
            {
                throw new InvalidOperationException(result);
            }

            return result
                .Trim()
                .Replace(Environment.NewLine, string.Empty);
        }
    }
}
