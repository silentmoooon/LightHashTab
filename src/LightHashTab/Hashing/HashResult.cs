using System;
using System.Collections.Generic;

namespace LightHashTab.Hashing;

public enum HashAlgorithmType
{
    Blake3,
    Sha256,
    Sha512,
    Sha384,
    Sha1,
    Md5,
    Crc32,
    Xxh64,
    Xxh128
}

public sealed class HashItem
{
    public required HashAlgorithmType Type { get; init; }
    public required string Name { get; init; }
    public string Value { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public bool IsMatch { get; set; }
}

public sealed class FileHashSummary
{
    public required string FilePath { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public double ElapsedMs { get; set; }
    public bool IsUppercase { get; set; }
    public List<HashItem> Hashes { get; init; } = [];
}
