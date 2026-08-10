using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Storage.Tests;

[TestClass]
public sealed class ReadOnlyTemporaryFileRangeReaderTests
{
    [TestMethod]
    public async Task ReadRange_ExistingFile_ReturnsExactBytesWithoutChangingFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "fixture.download");
        byte[] content = [1, 2, 3, 4, 5];
        await File.WriteAllBytesAsync(path, content);

        var snapshot = await new ReadOnlyTemporaryFileRangeReader()
            .ReadRangeAsync(path, offset: 1, length: 3, CancellationToken.None);

        Assert.AreEqual(5, snapshot.FileLength);
        CollectionAssert.AreEqual(new byte[] { 2, 3, 4 }, snapshot.Content.ToArray());
        CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task ReadRange_RangeBeyondCurrentLength_ReturnsLengthWithoutContent()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "fixture.download");
        await File.WriteAllBytesAsync(path, new byte[] { 1, 2 });

        var snapshot = await new ReadOnlyTemporaryFileRangeReader()
            .ReadRangeAsync(path, offset: 0, length: 3, CancellationToken.None);

        Assert.AreEqual(2, snapshot.FileLength);
        Assert.AreEqual(0, snapshot.Content.Length);
    }

    [TestMethod]
    public async Task ReadRange_RelativePath_IsRejected()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await new ReadOnlyTemporaryFileRangeReader()
                .ReadRangeAsync("relative.download", 0, 1, CancellationToken.None));
    }

    [TestMethod]
    public async Task ReadRange_PreCancelled_DoesNotOpenFile()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await new ReadOnlyTemporaryFileRangeReader().ReadRangeAsync(
                Path.GetFullPath("missing.download"),
                0,
                1,
                cancellation.Token));
    }
}
