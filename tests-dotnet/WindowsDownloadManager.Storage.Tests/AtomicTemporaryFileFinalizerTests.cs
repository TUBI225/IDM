using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Storage.Files;

namespace WindowsDownloadManager.Storage.Tests;

[TestClass]
public sealed class AtomicTemporaryFileFinalizerTests
{
    [TestMethod]
    public async Task MoveAtomically_SameVolume_MovesWithoutOverwriting()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            await File.WriteAllBytesAsync(temporaryPath, "hello"u8.ToArray());
            var finalizer = new AtomicTemporaryFileFinalizer();

            await finalizer.MoveAtomicallyAsync(temporaryPath, destinationPath, CancellationToken.None);

            Assert.IsFalse(File.Exists(temporaryPath));
            CollectionAssert.AreEqual("hello"u8.ToArray(), await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task MoveAtomically_DestinationExists_PreservesBothFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            await File.WriteAllBytesAsync(temporaryPath, "new"u8.ToArray());
            await File.WriteAllBytesAsync(destinationPath, "old"u8.ToArray());
            var finalizer = new AtomicTemporaryFileFinalizer();

            await Assert.ThrowsExactlyAsync<IOException>(async () =>
                await finalizer.MoveAtomicallyAsync(temporaryPath, destinationPath, CancellationToken.None));

            CollectionAssert.AreEqual("new"u8.ToArray(), await File.ReadAllBytesAsync(temporaryPath));
            CollectionAssert.AreEqual("old"u8.ToArray(), await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
