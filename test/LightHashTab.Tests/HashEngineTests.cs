using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightHashTab.Hashing;
using Xunit;

namespace LightHashTab.Tests;

public class HashEngineTests
{
    [Fact]
    public async Task ComputeHashes_EmptyFile_MatchesStandardVectors()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, []);

            var hashes = new List<HashItem>
            {
                new() { Type = HashAlgorithmType.Blake3, Name = "BLAKE3" },
                new() { Type = HashAlgorithmType.Sha256, Name = "SHA-256" },
                new() { Type = HashAlgorithmType.Sha512, Name = "SHA-512" },
                new() { Type = HashAlgorithmType.Sha1, Name = "SHA-1" },
                new() { Type = HashAlgorithmType.Md5, Name = "MD5" },
                new() { Type = HashAlgorithmType.Crc32, Name = "CRC32" }
            };

            await HashEngine.ComputeHashesAsync(tempFile, hashes, null, CancellationToken.None);

            var b3 = hashes.Find(h => h.Type == HashAlgorithmType.Blake3);
            var sha256 = hashes.Find(h => h.Type == HashAlgorithmType.Sha256);
            var md5 = hashes.Find(h => h.Type == HashAlgorithmType.Md5);
            var sha1 = hashes.Find(h => h.Type == HashAlgorithmType.Sha1);
            var crc32 = hashes.Find(h => h.Type == HashAlgorithmType.Crc32);

            Assert.Equal("af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262", b3?.Value);
            Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", sha256?.Value);
            Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", md5?.Value);
            Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", sha1?.Value);
            Assert.Equal("00000000", crc32?.Value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComputeHashes_TextFile_ComputesAllAccurately()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            byte[] content = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");
            File.WriteAllBytes(tempFile, content);

            var hashes = new List<HashItem>
            {
                new() { Type = HashAlgorithmType.Blake3, Name = "BLAKE3" },
                new() { Type = HashAlgorithmType.Sha256, Name = "SHA-256" },
                new() { Type = HashAlgorithmType.Md5, Name = "MD5" }
            };

            await HashEngine.ComputeHashesAsync(tempFile, hashes, null, CancellationToken.None);

            var b3 = hashes.Find(h => h.Type == HashAlgorithmType.Blake3);
            var sha256 = hashes.Find(h => h.Type == HashAlgorithmType.Sha256);
            var md5 = hashes.Find(h => h.Type == HashAlgorithmType.Md5);

            // Standard test vectors for "The quick brown fox jumps over the lazy dog"
            Assert.Equal("2f1514181aadccd913abd94cfa592701a5686ab23f8df1dff1b74710febc6d4a", b3?.Value);
            Assert.Equal("d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592", sha256?.Value);
            Assert.Equal("9e107d9d372bb6826bd81d3542a419d6", md5?.Value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
