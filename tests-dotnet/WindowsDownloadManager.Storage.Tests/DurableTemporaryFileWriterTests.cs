using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Storage.Tests;

[TestClass]
public sealed class DurableTemporaryFileWriterTests
{
    [TestMethod]
    public async Task PrepareNew_CreatesEmptyFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "fixture.download");
        var writer = new DurableTemporaryFileWriter();

        await writer.PrepareNewAsync(path, CancellationToken.None);

        Assert.IsTrue(File.Exists(path));
        Assert.AreEqual(0, new FileInfo(path).Length);
    }

    [TestMethod]
    public async Task PrepareNew_ExistingFile_IsRejectedWithoutModification()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "existing.download");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3 });
        var writer = new DurableTemporaryFileWriter();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await writer.PrepareNewAsync(path, CancellationToken.None));

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task WriteAndFlush_AtOffset_WritesExactBytesAndReturnsBoundary()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "fixture.download");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3, 4 });
        var writer = new DurableTemporaryFileWriter();

        var confirmedBoundary = await writer.WriteAndFlushAsync(
            path,
            2,
            new byte[] { 9, 8 },
            CancellationToken.None);

        Assert.AreEqual(4, confirmedBoundary);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 9, 8 }, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task WriteAndFlush_RelativePath_IsRejected()
    {
        var writer = new DurableTemporaryFileWriter();

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await writer.WriteAndFlushAsync("relative.download", 0, new byte[] { 1 }, CancellationToken.None));
    }

    [TestMethod]
    public async Task WriteAndFlush_PreCancelled_DoesNotCreateFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "cancelled.download");
        var writer = new DurableTemporaryFileWriter();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await writer.WriteAndFlushAsync(path, 0, new byte[] { 1 }, cancellation.Token));

        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public async Task WriteAndFlush_MissingPreparedFile_DoesNotCreateFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "missing.download");
        var writer = new DurableTemporaryFileWriter();

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(async () =>
            await writer.WriteAndFlushAsync(path, 0, new byte[] { 1 }, CancellationToken.None));

        Assert.IsFalse(File.Exists(path));
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wdm-storage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
