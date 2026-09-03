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

        // Determine which algorithms are requested
        bool needBlake3 = false, needSha256 = false, needSha512 = false;
        bool needSha384 = false, needSha1 = false, needMd5 = false;
        bool needCrc32 = false, needXxh64 = false, needXxh128 = false;

        foreach (var item in targetList)
        {
            switch (item.Type)
            {
                case HashAlgorithmType.Blake3: needBlake3 = true; break;
                case HashAlgorithmType.Sha256: needSha256 = true; break;
                case HashAlgorithmType.Sha512: needSha512 = true; break;
                case HashAlgorithmType.Sha384: needSha384 = true; break;
                case HashAlgorithmType.Sha1:   needSha1 = true; break;
                case HashAlgorithmType.Md5:    needMd5 = true; break;
                case HashAlgorithmType.Crc32:  needCrc32 = true; break;
                case HashAlgorithmType.Xxh64:  needXxh64 = true; break;
                case HashAlgorithmType.Xxh128: needXxh128 = true; break;
            }
        }

        // Initialize only active hashers
        using var blake3 = needBlake3 ? Blake3.Hasher.New() : null;
        using var sha256 = needSha256 ? IncrementalHash.CreateHash(HashAlgorithmName.SHA256) : null;
        using var sha512 = needSha512 ? IncrementalHash.CreateHash(HashAlgorithmName.SHA512) : null;
        using var sha384 = needSha384 ? IncrementalHash.CreateHash(HashAlgorithmName.SHA384) : null;
        using var sha1   = needSha1   ? IncrementalHash.CreateHash(HashAlgorithmName.SHA1) : null;
        using var md5    = needMd5    ? IncrementalHash.CreateHash(HashAlgorithmName.MD5) : null;
        var crc32        = needCrc32  ? new System.IO.Hashing.Crc32() : null;
        var xxh64        = needXxh64  ? new System.IO.Hashing.XxHash64() : null;
        var xxh128       = needXxh128 ? new System.IO.Hashing.XxHash128() : null;

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

            // Feed chunk to active hashers in single pass
            blake3?.Update(span);
            sha256?.AppendData(span);
            sha512?.AppendData(span);
            sha384?.AppendData(span);
            sha1?.AppendData(span);
            md5?.AppendData(span);
            crc32?.Append(span);
            xxh64?.Append(span);
            xxh128?.Append(span);

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

        // Finalize active hashes
        string b3Str = string.Empty;
        if (blake3 != null)
        {
            Span<byte> b3Hash = stackalloc byte[32];
            blake3.Finalize(b3Hash);
            b3Str = Convert.ToHexStringLower(b3Hash);
        }

        string sha256Str = sha256 != null ? Convert.ToHexStringLower(sha256.GetHashAndReset()) : string.Empty;
        string sha512Str = sha512 != null ? Convert.ToHexStringLower(sha512.GetHashAndReset()) : string.Empty;
        string sha384Str = sha384 != null ? Convert.ToHexStringLower(sha384.GetHashAndReset()) : string.Empty;
        string sha1Str   = sha1 != null   ? Convert.ToHexStringLower(sha1.GetHashAndReset()) : string.Empty;
        string md5Str    = md5 != null    ? Convert.ToHexStringLower(md5.GetHashAndReset()) : string.Empty;

        string crcStr = string.Empty;
        if (crc32 != null)
        {
            Span<byte> crcBytes = stackalloc byte[4];
            crc32.GetCurrentHash(crcBytes);
            crcStr = Convert.ToHexStringLower(crcBytes);
        }

        string xxh64Str = string.Empty;
        if (xxh64 != null)
        {
            Span<byte> xxhBytes = stackalloc byte[8];
            xxh64.GetCurrentHash(xxhBytes);
            xxh64Str = Convert.ToHexStringLower(xxhBytes);
        }

        string xxh128Str = string.Empty;
        if (xxh128 != null)
        {
            Span<byte> xxh128Bytes = stackalloc byte[16];
            xxh128.GetCurrentHash(xxh128Bytes);
            xxh128Str = Convert.ToHexStringLower(xxh128Bytes);
        }

        foreach (var item in targetList)
        {
            item.Value = item.Type switch
            {
                HashAlgorithmType.Blake3 => b3Str,
                HashAlgorithmType.Sha256 => sha256Str,
                HashAlgorithmType.Sha512 => sha512Str,
                HashAlgorithmType.Sha384 => sha384Str,
                HashAlgorithmType.Sha1   => sha1Str,
                HashAlgorithmType.Md5    => md5Str,
                HashAlgorithmType.Crc32  => crcStr,
                HashAlgorithmType.Xxh64  => xxh64Str,
                HashAlgorithmType.Xxh128 => xxh128Str,
                _ => string.Empty
            };
            item.Status = "Completed";
        }

        onProgress?.Invoke(100);
    }
}
