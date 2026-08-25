using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LightHashTab.Hashing;

public static class HashEngine
{
    private const int BufferSize = 1024 * 1024; // 1 MB buffer for optimal sequential read throughput

    public static async Task ComputeHashesAsync(
        string filePath,
        List<HashItem> targetList,
        Action<int>? onProgress,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            foreach (var item in targetList)
            {
                item.Status = "File not found";
            }
            return;
        }

        var fileInfo = new FileInfo(filePath);
        long totalBytes = fileInfo.Length;

        // Initialize active hashers
        using var blake3 = Blake3.Hasher.New();
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var sha512 = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        var crc32 = new System.IO.Hashing.Crc32();
        var xxh64 = new System.IO.Hashing.XxHash64();

        byte[] buffer = new byte[BufferSize];
        long bytesProcessed = 0;
        int lastReportedPercent = -1;

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: BufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
            if (bytesRead <= 0)
            {
                break;
            }

            ReadOnlySpan<byte> span = buffer.AsSpan(0, bytesRead);

            // Feed chunk to all hashers in single pass
            blake3.Update(span);
            sha256.AppendData(span);
            sha512.AppendData(span);
            sha1.AppendData(span);
            md5.AppendData(span);
            crc32.Append(span);
            xxh64.Append(span);

            bytesProcessed += bytesRead;

            if (totalBytes > 0 && onProgress != null)
            {
                int currentPercent = (int)((bytesProcessed * 100) / totalBytes);
                if (currentPercent != lastReportedPercent)
                {
                    lastReportedPercent = currentPercent;
                    onProgress(currentPercent);
                }
            }
        }

        // Finalize all hashes
        Span<byte> b3Hash = stackalloc byte[32];
        blake3.Finalize(b3Hash);
        string b3Str = Convert.ToHexStringLower(b3Hash);

        string sha256Str = Convert.ToHexStringLower(sha256.GetHashAndReset());
        string sha512Str = Convert.ToHexStringLower(sha512.GetHashAndReset());
        string sha1Str = Convert.ToHexStringLower(sha1.GetHashAndReset());
        string md5Str = Convert.ToHexStringLower(md5.GetHashAndReset());

        Span<byte> crcBytes = stackalloc byte[4];
        crc32.GetCurrentHash(crcBytes);
        string crcStr = Convert.ToHexStringLower(crcBytes);

        Span<byte> xxhBytes = stackalloc byte[8];
        xxh64.GetCurrentHash(xxhBytes);
        string xxhStr = Convert.ToHexStringLower(xxhBytes);

        foreach (var item in targetList)
        {
            item.Value = item.Type switch
            {
                HashAlgorithmType.Blake3 => b3Str,
                HashAlgorithmType.Sha256 => sha256Str,
                HashAlgorithmType.Sha512 => sha512Str,
                HashAlgorithmType.Sha1 => sha1Str,
                HashAlgorithmType.Md5 => md5Str,
                HashAlgorithmType.Crc32 => crcStr,
                HashAlgorithmType.Xxh64 => xxhStr,
                _ => string.Empty
            };
            item.Status = "Completed";
        }

        onProgress?.Invoke(100);
    }
}
