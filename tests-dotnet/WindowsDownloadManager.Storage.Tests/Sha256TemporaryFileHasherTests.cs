using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Storage.Tests;

[TestClass]
public sealed class Sha256TemporaryFileHasherTests
{
    [TestMethod]
    public async Task ComputeSha256_KnownContent_ReturnsCanonicalUppercaseHash()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-hash-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "fixture.download");
            await File.WriteAllBytesAsync(path, "hello"u8.ToArray());
            var hasher = new Sha256TemporaryFileHasher();

            var hash = await hasher.ComputeSha256Async(path, CancellationToken.None);

            Assert.AreEqual(
                "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824",
                hash);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task ComputeSha256_PreCancelled_DoesNotReadFile()
    {
        var hasher = new Sha256TemporaryFileHasher();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await hasher.ComputeSha256Async("C:\\missing.download", cancellation.Token));
    }
}
