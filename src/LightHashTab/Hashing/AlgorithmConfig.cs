using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace LightHashTab.Hashing;

public static class AlgorithmConfig
{
    private const string RegKey = @"Software\LightHashTab";
    private const string RegValue = "EnabledAlgorithms";

    public static readonly (HashAlgorithmType Type, string Name)[] AllAlgorithms =
    [
        (HashAlgorithmType.Blake3, "BLAKE3"),
        (HashAlgorithmType.Sha256, "SHA-256"),
        (HashAlgorithmType.Sha512, "SHA-512"),
        (HashAlgorithmType.Sha384, "SHA-384"),
        (HashAlgorithmType.Sha1,   "SHA-1"),
        (HashAlgorithmType.Md5,    "MD5"),
        (HashAlgorithmType.Crc32,  "CRC32"),
        (HashAlgorithmType.Xxh64,  "XXH64"),
        (HashAlgorithmType.Xxh128, "XXH128"),
    ];

    public static readonly HashAlgorithmType[] DefaultAlgorithms =
    [
        HashAlgorithmType.Blake3,
        HashAlgorithmType.Sha256,
        HashAlgorithmType.Sha512,
        HashAlgorithmType.Sha1,
        HashAlgorithmType.Md5,
        HashAlgorithmType.Crc32,
        HashAlgorithmType.Xxh64,
    ];

    public static List<HashItem> GetActiveHashList()
    {
        var enabled = LoadEnabledAlgorithms();
        var list = new List<HashItem>();
        foreach (var (type, name) in AllAlgorithms)
        {
            if (enabled.Contains(type))
            {
                list.Add(new HashItem { Type = type, Name = name });
            }
        }
        if (list.Count == 0)
        {
            foreach (var type in DefaultAlgorithms)
            {
                list.Add(new HashItem { Type = type, Name = GetAlgorithmName(type) });
            }
        }
        return list;
    }

    public static HashSet<HashAlgorithmType> LoadEnabledAlgorithms()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey);
            if (key?.GetValue(RegValue) is string str && !string.IsNullOrWhiteSpace(str))
            {
                var set = new HashSet<HashAlgorithmType>();
                var parts = str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    if (Enum.TryParse<HashAlgorithmType>(part, ignoreCase: true, out var t))
                        set.Add(t);
                }
                if (set.Count > 0) return set;
            }
        }
        catch { }

        return new HashSet<HashAlgorithmType>(DefaultAlgorithms);
    }

    public static void SaveEnabledAlgorithms(HashSet<HashAlgorithmType> enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegKey);
            var names = new List<string>();
            foreach (var t in enabled)
                names.Add(t.ToString());
            key.SetValue(RegValue, string.Join(",", names));
        }
        catch { }
    }

    public static string GetAlgorithmName(HashAlgorithmType type) => type switch
    {
        HashAlgorithmType.Blake3 => "BLAKE3",
        HashAlgorithmType.Sha256 => "SHA-256",
        HashAlgorithmType.Sha512 => "SHA-512",
        HashAlgorithmType.Sha384 => "SHA-384",
        HashAlgorithmType.Sha1   => "SHA-1",
        HashAlgorithmType.Md5    => "MD5",
        HashAlgorithmType.Crc32  => "CRC32",
        HashAlgorithmType.Xxh64  => "XXH64",
        HashAlgorithmType.Xxh128 => "XXH128",
        _ => type.ToString()
    };
}
