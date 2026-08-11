using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
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

            await finalizer.FinalizeAsync(
                Guid.NewGuid(),
                temporaryPath,
                destinationPath,
                Sha256("hello"u8.ToArray()),
                CancellationToken.None);

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
                await finalizer.FinalizeAsync(
                    Guid.NewGuid(),
                    temporaryPath,
                    destinationPath,
                    Sha256("new"u8.ToArray()),
                    CancellationToken.None));

            CollectionAssert.AreEqual("new"u8.ToArray(), await File.ReadAllBytesAsync(temporaryPath));
            CollectionAssert.AreEqual("old"u8.ToArray(), await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }


    [TestMethod]
    public async Task Finalize_DifferentVolume_CopiesVerifiesAndRemovesSource()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            var content = new byte[300_000];
            Random.Shared.NextBytes(content);
            await File.WriteAllBytesAsync(temporaryPath, content);
            var finalizer = new AtomicTemporaryFileFinalizer(new StubVolumeComparer(sameVolume: false));

            await finalizer.FinalizeAsync(
                Guid.NewGuid(),
                temporaryPath,
                destinationPath,
                Sha256(content),
                CancellationToken.None);

            Assert.IsFalse(File.Exists(temporaryPath));
            CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Repair_DifferentVolumeBothVerified_RemovesSourceAndKeepsDestination()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            var content = "verified"u8.ToArray();
            await File.WriteAllBytesAsync(temporaryPath, content);
            await File.WriteAllBytesAsync(destinationPath, content);
            var finalizer = new AtomicTemporaryFileFinalizer(new StubVolumeComparer(sameVolume: false));

            await finalizer.RepairAsync(
                Guid.NewGuid(),
                temporaryPath,
                destinationPath,
                Sha256(content),
                CancellationToken.None);

            Assert.IsFalse(File.Exists(temporaryPath));
            CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Repair_DifferentVolumePartialStaging_ReplacesItAndCompletes()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var downloadId = Guid.NewGuid();
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            var stagingPath = Path.Combine(directory, $".wdm-finalizing-{downloadId:N}.tmp");
            var content = new byte[200_000];
            Random.Shared.NextBytes(content);
            await File.WriteAllBytesAsync(temporaryPath, content);
            await File.WriteAllBytesAsync(stagingPath, content[..1024]);
            var finalizer = new AtomicTemporaryFileFinalizer(new StubVolumeComparer(sameVolume: false));

            await finalizer.RepairAsync(
                downloadId,
                temporaryPath,
                destinationPath,
                Sha256(content),
                CancellationToken.None);

            Assert.IsFalse(File.Exists(temporaryPath));
            Assert.IsFalse(File.Exists(stagingPath));
            CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Repair_DifferentVolumeDestinationHashMismatch_PreservesSourceAndDestination()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            var content = "verified"u8.ToArray();
            await File.WriteAllBytesAsync(temporaryPath, content);
            await File.WriteAllBytesAsync(destinationPath, "changed"u8.ToArray());
            var finalizer = new AtomicTemporaryFileFinalizer(new StubVolumeComparer(sameVolume: false));

            await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
                await finalizer.RepairAsync(
                    Guid.NewGuid(),
                    temporaryPath,
                    destinationPath,
                    Sha256(content),
                    CancellationToken.None));

            CollectionAssert.AreEqual(content, await File.ReadAllBytesAsync(temporaryPath));
            CollectionAssert.AreEqual("changed"u8.ToArray(), await File.ReadAllBytesAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Repair_SameVolumeBothFilesExist_RefusesAmbiguousState()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wdm-finalizer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var temporaryPath = Path.Combine(directory, "fixture.download");
            var destinationPath = Path.Combine(directory, "fixture.bin");
            var content = "verified"u8.ToArray();
            await File.WriteAllBytesAsync(temporaryPath, content);
            await File.WriteAllBytesAsync(destinationPath, content);
            var finalizer = new AtomicTemporaryFileFinalizer(new StubVolumeComparer(sameVolume: true));

            await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
                await finalizer.RepairAsync(
                    Guid.NewGuid(),
                    temporaryPath,
                    destinationPath,
                    Sha256(content),
                    CancellationToken.None));

            Assert.IsTrue(File.Exists(temporaryPath));
            Assert.IsTrue(File.Exists(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string Sha256(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content));

    private sealed class StubVolumeComparer(bool sameVolume) : IFileVolumeComparer
    {
        public bool AreOnSameVolume(string firstPath, string secondPath) => sameVolume;
    }
}
