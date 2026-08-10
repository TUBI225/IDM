using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Storage.Tests;

[TestClass]
public sealed class ReadOnlyTemporaryFileInspectorTests
{
    [TestMethod]
    public async Task Inspect_MissingFile_ReturnsAbsent()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "missing.download");
        var inspector = new ReadOnlyTemporaryFileInspector();

        var snapshot = await inspector.InspectAsync(path, CancellationToken.None);

        Assert.IsFalse(snapshot.Exists);
        Assert.IsNull(snapshot.Length);
        Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public async Task Inspect_ExistingFile_ReturnsLengthWithoutChangingContent()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "fixture.download");
        var content = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(path, content);
        var inspector = new ReadOnlyTemporaryFileInspector();

        var snapshot = await inspector.InspectAsync(path, CancellationToken.None);

        Assert.IsTrue(snapshot.Exists);
        Assert.AreEqual(5, snapshot.Length);
        CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(path));
    }

    [TestMethod]
    public async Task Inspect_RelativePath_IsRejected()
    {
        var inspector = new ReadOnlyTemporaryFileInspector();

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await inspector.InspectAsync("relative.download", CancellationToken.None));
    }

    [TestMethod]
    public async Task Inspect_PreCancelled_DoesNotOpenFile()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "fixture.download");
        await File.WriteAllBytesAsync(path, new byte[] { 1 });
        var inspector = new ReadOnlyTemporaryFileInspector();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            await inspector.InspectAsync(path, cancellation.Token));
    }

    [TestMethod]
    public async Task Inspect_FileLockedExclusively_DoesNotMisclassifyAsAbsent()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "locked.download");
        await File.WriteAllBytesAsync(path, new byte[] { 1 });
        using var exclusiveHandle = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var inspector = new ReadOnlyTemporaryFileInspector();

        await Assert.ThrowsExactlyAsync<IOException>(async () =>
            await inspector.InspectAsync(path, CancellationToken.None));
    }
}
